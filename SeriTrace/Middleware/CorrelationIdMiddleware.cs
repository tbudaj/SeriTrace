using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System.Diagnostics;
using System.Text.Json;

namespace SeriTrace.Middleware
{
    /// <summary>
    /// Middleware for automatic management of CorrelationId in HTTP requests.
    /// - Retrieves or generates a CorrelationId for each request.
    /// - Stores CorrelationId and request start time in HttpContext.Items.
    /// - Adds CorrelationId to response headers.
    /// - Integrates with Serilog LogContext for logging.
    /// - Updates ServiceResponse envelopes with correlation metadata.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        // Name of the HTTP header used for CorrelationId
        private const string CorrelationIdHeaderName = "X-Correlation-ID";
        // Key for storing CorrelationId in HttpContext.Items
        private const string CorrelationIdContextKey = "CorrelationId";
        // Key for storing request start time in HttpContext.Items
        private const string RequestStartTimeKey = "RequestStartTime";

        private readonly RequestDelegate next;
        private readonly ILogger<CorrelationIdMiddleware> logger;

        /// <summary>
        /// Initializes the middleware with the next delegate and logger.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">Logger for diagnostic messages.</param>
        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            this.next = next ?? throw new ArgumentNullException(nameof(next));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Main middleware method that processes each HTTP request.
        /// Steps:
        /// 1. Retrieves or generates a CorrelationId.
        /// 2. Stores CorrelationId and request start time in HttpContext.Items.
        /// 3. Adds CorrelationId to response headers.
        /// 4. Pushes CorrelationId to Serilog LogContext.
        /// 5. Captures the response body for further processing.
        /// 6. Processes the response body to update ServiceResponse envelopes with correlation metadata if applicable.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Record the start time of the request
            var startTime = DateTime.Now;
            // Start a stopwatch to measure request duration
            var stopwatch = Stopwatch.StartNew();

            // Step 1: Retrieve or generate CorrelationId
            var correlationId = GetOrGenerateCorrelationId(context);

            // Step 2: Store CorrelationId and start time in HttpContext.Items for downstream access
            context.Items[CorrelationIdContextKey] = correlationId;
            context.Items[RequestStartTimeKey] = startTime;

            // Step 3: Add CorrelationId to response headers for client visibility
            context.Response.Headers[CorrelationIdHeaderName] = correlationId;

            logger.LogDebug("CorrelationId {CorrelationId} assigned to request {Method} {Path}",
                correlationId, context.Request.Method, context.Request.Path);

            // Step 4: Capture the original response body stream
            var originalBodyStream = context.Response.Body;
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            // Step 5: Push CorrelationId to Serilog LogContext for logging
            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                try
                {
                    // Execute the next middleware in the pipeline
                    await next(context);
                    stopwatch.Stop();

                    // Step 6: Process the response body to update ServiceResponse envelope if applicable
                    await ProcessResponseAsync(context, correlationId, startTime, stopwatch.ElapsedMilliseconds);
                }
                finally
                {
                    // Always restore the original response body stream
                    responseBodyStream.Seek(0, SeekOrigin.Begin);
                    await responseBodyStream.CopyToAsync(originalBodyStream);
                    context.Response.Body = originalBodyStream;
                }
            }
        }

        /// <summary>
        /// Retrieves the CorrelationId from the X-Correlation-ID header if present, otherwise generates a new one.
        /// Generated format: CID-{timestamp}-{random}
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <returns>The CorrelationId string.</returns>
        private string GetOrGenerateCorrelationId(HttpContext context)
        {
            // Check if the request contains the CorrelationId header
            if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var headerValue) && !string.IsNullOrWhiteSpace(headerValue))
            {
                var correlationId = headerValue.ToString();
                logger.LogDebug("CorrelationId {CorrelationId} found in request header", correlationId);
                return correlationId;
            }

            // Generate a new CorrelationId if not present in the header
            var timestamp = DateTime.Now.ToString("yyMMddHHmmss");
            var randomPart = Guid.NewGuid().ToString("N")[..12];
            var newCorrelationId = $"CID-{timestamp}-{randomPart}";

            logger.LogDebug("Generated new CorrelationId {CorrelationId}", newCorrelationId);
            return newCorrelationId;
        }

        /// <summary>
        /// Checks if the response is a ServiceResponse and updates its envelope with correlation metadata.
        /// Conditions: JSON response with an "envelope" property at the root level.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <param name="correlationId">The CorrelationId for the request.</param>
        /// <param name="startTime">The request start time.</param>
        /// <param name="durationMs">The request duration in milliseconds.</param>
        private async Task ProcessResponseAsync(HttpContext context, string correlationId, DateTime startTime, long durationMs)
        {
            // If the response body is empty, skip processing
            if (context.Response.Body.Length == 0)
                return;

            // Read the response body as a string
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();

            // If the response body is empty or whitespace, skip processing
            if (string.IsNullOrWhiteSpace(responseBody))
                return;

            try
            {
                // Only process JSON responses
                if (context.Response.ContentType?.Contains("application/json") == true)
                {
                    using var jsonDocument = JsonDocument.Parse(responseBody);
                    var root = jsonDocument.RootElement;

                    // Update the ServiceResponse envelope with correlation metadata
                    var updatedResponse = UpdateServiceResponseEnvelope(responseBody, correlationId, startTime, durationMs);

                    // Replace the response body with the updated content
                    context.Response.Body.Seek(0, SeekOrigin.Begin);
                    context.Response.Body.SetLength(0);

                    var updatedBytes = System.Text.Encoding.UTF8.GetBytes(updatedResponse);
                    await context.Response.Body.WriteAsync(updatedBytes);
                    context.Response.ContentLength = updatedBytes.Length;

                    logger.LogDebug("ServiceResponse envelope updated with CorrelationId {CorrelationId}, duration {DurationMs}ms", correlationId, durationMs);
                }
            }
            catch (JsonException ex)
            {
                // Log a warning if the response cannot be parsed as JSON
                logger.LogWarning("Failed to parse response as JSON: {Error}", ex.Message);
            }
            catch (Exception ex)
            {
                // Log any other errors encountered during envelope processing
                logger.LogError(ex, "Error processing ServiceResponse envelope for CorrelationId {CorrelationId}", correlationId);
            }
        }

        /// <summary>
        /// Creates an updated version of the ServiceResponse with a filled envelope.
        /// For non-object JSON (arrays, primitives), wraps them in a data envelope before adding metadata.
        /// Updates: metadata.correlationId, metadata.timestamp, metadata.durationMs
        /// Preserves: envelope.serverId, envelope.apiVersion, envelope.sessionId, and other custom fields
        /// </summary>
        /// <param name="responseBody">The original response body as a string.</param>
        /// <param name="correlationId">The CorrelationId for the request.</param>
        /// <param name="startTime">The request start time.</param>
        /// <param name="durationMs">The request duration in milliseconds.</param>
        /// <returns>The updated response body as a string.</returns>
        private string UpdateServiceResponseEnvelope(string responseBody, string correlationId, DateTime startTime, long durationMs)
        {
            using var jsonDocument = JsonDocument.Parse(responseBody);
            var root = jsonDocument.RootElement;

            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false });

            writer.WriteStartObject();

            // Handle non-object JSON (arrays, primitives, etc.) by wrapping in data envelope
            if (root.ValueKind != JsonValueKind.Object)
            {
                logger.LogDebug("Wrapping non-object JSON response (ValueKind: {ValueKind}) in data envelope", root.ValueKind);
                
                // Write the original content as "data" property
                writer.WritePropertyName("data");
                root.WriteTo(writer);
                
                // Add metadata at the same level as data
                writer.WritePropertyName("metadata");
                WriteUpdatedMetadata(writer, default, correlationId, durationMs);
            }
            else
            {
                // Copy all properties except metadata (which will be updated)
                foreach (var property in root.EnumerateObject())
                {
                    if (property.Name == "metadata")
                    {
                        // For root-level metadata, update correlation info
                        writer.WritePropertyName("metadata");
                        WriteUpdatedMetadata(writer, property.Value, correlationId, durationMs);
                    }
                    else
                    {
                        // Copy other properties unchanged (envelope, data, pagination, etc.)
                        property.WriteTo(writer);
                    }
                }

                // If metadata did not exist at the root level, create a new one
                if (!root.TryGetProperty("metadata", out _))
                {
                    writer.WritePropertyName("metadata");
                    WriteUpdatedMetadata(writer, default, correlationId, durationMs);
                }
            }

            writer.WriteEndObject();
            writer.Flush();

            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }

        /// <summary>
        /// Writes updated metadata with correlation information.
        /// Adds/updates: correlationId, timestamp, duration-ms.
        /// Preserves any other existing metadata fields.
        /// </summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="originalMetadata">The original metadata JSON element.</param>
        /// <param name="correlationId">The CorrelationId for the request.</param>
        /// <param name="durationMs">The request duration in milliseconds.</param>
        private void WriteUpdatedMetadata(Utf8JsonWriter writer, JsonElement originalMetadata,
            string correlationId, long durationMs)
        {
            writer.WriteStartObject();

            // Add or update correlation fields
            writer.WriteString("correlationId", correlationId);
            writer.WriteString("timestamp", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffK"));
            writer.WriteNumber("duration-ms", durationMs);

            // Preserve any other existing metadata fields, except those just set
            if (originalMetadata.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in originalMetadata.EnumerateObject())
                {
                    if (property.Name != "correlationId" && property.Name != "timestamp" && property.Name != "duration-ms")
                    {
                        property.WriteTo(writer);
                    }
                }
            }

            writer.WriteEndObject();
        }
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SeriTrace.Options;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace SeriTrace.Middleware
{
    /// <summary>
    /// Middleware for logging incoming HTTP requests and outgoing HTTP responses
    /// with full configuration via appsettings.json.
    /// 
    /// Key features:
    /// - Separate loggers for requests and responses with independent configuration
    /// - Full parameterization via appsettings.json without hardcoded values
    /// - Structured logging with dedicated folders (logs/requests, logs/responses)
    /// - Secure logging: excludes authorization headers and binary files
    /// - Performance optimized: buffering with size limits
    /// - CorrelationId integration: automatically added to every log
    /// - Content-Type aware: intelligently detects binary files
    /// </summary>
    /// <remarks>
    /// <b>PIPELINE USAGE:</b>
    /// - UseCorrelationId() - FIRST position
    /// - UseRequestResponseLogging() - SECOND position (after CorrelationId)
    /// - UseSerilogRequestLogging() - THIRD position (standard HTTP logs)
    /// 
    /// <b>CONFIGURATION:</b>
    /// - RequestResponseLogging:Requests - request log configuration
    /// - RequestResponseLogging:Responses - response log configuration
    /// - Each logger has independent path, log level, file rotation
    /// </remarks>
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
        private readonly ILogger _requestLogger;
        private readonly ILogger _responseLogger;
        private readonly RequestResponseLoggingOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestResponseLoggingMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">Logger for internal middleware errors.</param>
        /// <param name="loggerFactory">Factory for creating dedicated request/response loggers.</param>
        /// <param name="options">Request/response logging options from configuration.</param>
        public RequestResponseLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestResponseLoggingMiddleware> logger,
            ILoggerFactory loggerFactory,
            IOptions<RequestResponseLoggingOptions> options)
        {
            _next = next;
            _logger = logger;
            _options = options.Value;

            // Create dedicated loggers for requests and responses
            _requestLogger = loggerFactory.CreateLogger("RequestLogger");
            _responseLogger = loggerFactory.CreateLogger("ResponseLogger");
        }

        /// <summary>
        /// Invokes the middleware logic for logging HTTP requests and responses.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = context.Items["CorrelationId"] as string ?? "UNKNOWN";

            // 1. Log incoming request (if enabled)
            if (_options.Requests.Enabled && !IsPathExcluded(context.Request.Path, _options.Requests.ExcludedPaths))
            {
                await LogRequestAsync(context, correlationId);
            }

            // 2. Capture response for logging
            Stream originalResponseBody = context.Response.Body;
            using var responseBody = new MemoryStream();

            try
            {
                // Replace response body with our buffer
                context.Response.Body = responseBody;

                // 3. Execute the application pipeline
                await _next(context);

                // 4. Log outgoing response (if enabled)
                if (_options.Responses.Enabled && !IsPathExcluded(context.Request.Path, _options.Responses.ExcludedPaths))
                {
                    await LogResponseAsync(context, correlationId, stopwatch.ElapsedMilliseconds);
                }

                // 5. Copy response from buffer to original stream
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalResponseBody);
            }
            finally
            {
                // Restore original response body
                context.Response.Body = originalResponseBody;
                stopwatch.Stop();
            }
        }

        /// <summary>
        /// Logs the incoming HTTP request with full context.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <param name="correlationId">The correlation identifier for the request.</param>
        private async Task LogRequestAsync(HttpContext context, string correlationId)
        {
            try
            {
                var request = context.Request;
                var requestInfo = new
                {
                    Timestamp = DateTimeOffset.Now,
                    CorrelationId = correlationId,
                    Method = request.Method,
                    Path = request.Path.Value,
                    QueryString = request.QueryString.Value,
                    ContentType = request.ContentType,
                    ContentLength = request.ContentLength,
                    Headers = GetSafeHeaders(request.Headers, _options.Requests.ExcludedHeaders),
                    ClientIP = context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    UserAgent = request.Headers.UserAgent.FirstOrDefault() ?? string.Empty,
                    Body = await GetRequestBodyAsync(request)
                };

                using (var scope = _logger.BeginScope(new Dictionary<string, object>
                {
                    ["CorrelationId"] = correlationId,
                    ["RequestMethod"] = request.Method,
                    ["RequestPath"] = request.Path.Value ?? string.Empty,
                    ["ClientIP"] = requestInfo.ClientIP,
                    ["UserAgent"] = requestInfo.UserAgent,
                    ["ContentType"] = requestInfo.ContentType ?? string.Empty,
                    ["ContentLength"] = requestInfo.ContentLength ?? 0,
                    ["RequestHeaders"] = requestInfo.Headers,
                    ["QueryString"] = requestInfo.QueryString ?? string.Empty,
                    ["RequestBody"] = requestInfo.Body ?? string.Empty
                }))
                {
                    // Simplified message - details are in Properties
                    _requestLogger.LogInformation("📥 HTTP Request from {ClientIP}",
                        requestInfo.ClientIP);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error logging HTTP request for CorrelationId {CorrelationId}", correlationId);
            }
        }

        /// <summary>
        /// Logs the outgoing HTTP response with full context and timing.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <param name="correlationId">The correlation identifier for the request.</param>
        /// <param name="durationMs">The duration of the request in milliseconds.</param>
        private async Task LogResponseAsync(HttpContext context, string correlationId, long durationMs)
        {
            try
            {
                var response = context.Response;
                var request = context.Request;

                var responseInfo = new
                {
                    Timestamp = DateTimeOffset.Now,
                    CorrelationId = correlationId,
                    StatusCode = response.StatusCode,
                    ContentType = response.ContentType,
                    ContentLength = response.ContentLength ?? context.Response.Body.Length,
                    Headers = GetSafeHeaders(response.Headers, _options.Responses.ExcludedHeaders),
                    Duration = durationMs,
                    Body = await GetResponseBodyAsync(response)
                };

                using (var scope = _logger.BeginScope(new Dictionary<string, object>
                {
                    ["CorrelationId"] = correlationId,
                    ["StatusCode"] = response.StatusCode,
                    ["RequestPath"] = request.Path.Value ?? string.Empty,
                    ["Duration"] = durationMs,
                    ["ContentType"] = responseInfo.ContentType ?? string.Empty,
                    ["ContentLength"] = responseInfo.ContentLength,
                    ["ResponseHeaders"] = responseInfo.Headers,
                    ["ResponseBody"] = responseInfo.Body ?? string.Empty
                }))
                {
                    var level = GetLogLevelForStatusCode(response.StatusCode);
                    // Simplified message - details are in Properties
                    _responseLogger.Log(level, "📤 HTTP Response");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error logging HTTP response for CorrelationId {CorrelationId}", correlationId);
            }
        }

        /// <summary>
        /// Gets the safe request body (excluding binary files).
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <returns>The request body as a string, or a placeholder if excluded.</returns>
        private async Task<string?> GetRequestBodyAsync(HttpRequest request)
        {
            if (!_options.Requests.LogRequestBody)
                return null;

            if (request.ContentLength == 0 || !request.Body.CanRead)
                return null;

            var contentType = request.ContentType ?? string.Empty;
            if (IsExcludedContentType(contentType, _options.Requests.ExcludedContentTypes))
            {
                return $"[BINARY_CONTENT:{contentType}:Length={request.ContentLength}]";
            }

            try
            {
                // Enable buffering to allow rereading
                request.EnableBuffering();

                var buffer = new byte[Math.Min(_options.Requests.MaxRequestBodySize, (int)(request.ContentLength ?? _options.Requests.MaxRequestBodySize))];
                var position = request.Body.Position;

                request.Body.Position = 0;
                var bytesRead = await request.Body.ReadAsync(buffer, 0, buffer.Length);
                request.Body.Position = position; // Restore position

                if (bytesRead == 0)
                    return null;

                var body = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                // Try to flatten JSON
                return TryFlattenJson(body) ?? body;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Failed to read request body");
                return "[ERROR_READING_BODY]";
            }
        }

        /// <summary>
        /// Gets the safe response body (excluding binary files).
        /// </summary>
        /// <param name="response">The HTTP response.</param>
        /// <returns>The response body as a string, or a placeholder if excluded.</returns>
        private async Task<string?> GetResponseBodyAsync(HttpResponse response)
        {
            if (!_options.Responses.LogResponseBody)
                return null;

            if (response.StatusCode == 204 || _options.Responses.ExcludedStatusCodes.Contains(response.StatusCode))
                return null;

            var contentType = response.ContentType ?? string.Empty;
            if (IsExcludedContentType(contentType, _options.Responses.ExcludedContentTypes))
            {
                return $"[BINARY_CONTENT:{contentType}:Length={response.ContentLength ?? response.Body.Length}]";
            }

            try
            {
                // Get data from MemoryStream buffer
                if (response.Body is MemoryStream memoryStream)
                {
                    if (memoryStream.Length == 0)
                        return null;

                    var buffer = new byte[Math.Min(_options.Responses.MaxResponseBodySize, (int)memoryStream.Length)];
                    var position = memoryStream.Position;

                    memoryStream.Position = 0;
                    var bytesRead = await memoryStream.ReadAsync(buffer, 0, buffer.Length);
                    memoryStream.Position = position; // Restore position

                    if (bytesRead == 0)
                        return null;

                    var body = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    // Try to flatten JSON
                    return TryFlattenJson(body) ?? body;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Failed to read response body");
                return "[ERROR_READING_BODY]";
            }
        }

        /// <summary>
        /// Checks if the Content-Type is excluded from logging.
        /// </summary>
        /// <param name="contentType">The content type string.</param>
        /// <param name="excludedContentTypes">List of excluded content types.</param>
        /// <returns>True if excluded, otherwise false.</returns>
        private static bool IsExcludedContentType(string contentType, List<string> excludedContentTypes)
        {
            if (string.IsNullOrEmpty(contentType))
                return false;

            var mediaType = contentType.Split(';')[0].Trim().ToLowerInvariant();

            return excludedContentTypes.Any(excluded =>
            {
                var excludedType = excluded.ToLowerInvariant();
                return excludedType.EndsWith("*")
                    ? mediaType.StartsWith(excludedType[..^1])
                    : mediaType == excludedType;
            });
        }

        /// <summary>
        /// Checks if the path is excluded from logging.
        /// </summary>
        /// <param name="path">The request path.</param>
        /// <param name="excludedPaths">List of excluded paths.</param>
        /// <returns>True if excluded, otherwise false.</returns>
        private static bool IsPathExcluded(PathString path, List<string> excludedPaths)
        {
            var pathValue = path.Value?.ToLowerInvariant() ?? string.Empty;
            return excludedPaths.Any(excluded => pathValue.StartsWith(excluded.ToLowerInvariant()));
        }

        /// <summary>
        /// Gets safe headers (excluding sensitive data).
        /// </summary>
        /// <param name="headers">The header dictionary.</param>
        /// <param name="excludedHeaders">List of excluded headers.</param>
        /// <returns>Dictionary of safe headers.</returns>
        private static Dictionary<string, string> GetSafeHeaders(IHeaderDictionary headers, List<string> excludedHeaders)
        {
            var safeHeaders = new Dictionary<string, string>();

            foreach (var header in headers)
            {
                if (!excludedHeaders.Contains(header.Key, StringComparer.OrdinalIgnoreCase))
                {
                    safeHeaders[header.Key] = string.Join(", ", header.Value.Where(v => v != null));
                }
                else
                {
                    safeHeaders[header.Key] = "[REDACTED]";
                }
            }

            return safeHeaders;
        }

        /// <summary>
        /// Attempts to flatten JSON to a single line.
        /// </summary>
        /// <param name="content">The content string.</param>
        /// <returns>Flattened JSON string or original content if not JSON.</returns>
        private static string? TryFlattenJson(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            try
            {
                using var document = JsonDocument.Parse(content);
                return JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
            }
            catch
            {
                // Not JSON, return original content
                return content;
            }
        }

        /// <summary>
        /// Determines the log level based on status code.
        /// </summary>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <returns>LogLevel for the status code.</returns>
        private static LogLevel GetLogLevelForStatusCode(int statusCode)
        {
            return statusCode switch
            {
                >= 500 => LogLevel.Error,
                >= 400 => LogLevel.Warning,
                _ => LogLevel.Information
            };
        }
    }
}


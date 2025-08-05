using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace SeriTrace.Extensions.Serilog
{
    /// <summary>
    /// Serilog enricher that automatically adds a CorrelationId property to all HTTP logs.
    /// Retrieves the CorrelationId from HttpContext.Items, enabling request tracing in distributed systems.
    /// This class is especially useful in web applications where CorrelationId is propagated by middleware.
    /// </summary>
    public class CorrelationIdEnricher : ILogEventEnricher
    {
        private const string CorrelationIdContextKey = "CorrelationId";
        private const string CorrelationIdPropertyName = "CorrelationId";

        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Creates a new instance of <see cref="CorrelationIdEnricher"/> with the default IHttpContextAccessor.
        /// Mainly used outside of DI or in tests. In production, it is recommended to use DI.
        /// </summary>
        public CorrelationIdEnricher() : this(new HttpContextAccessor())
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="CorrelationIdEnricher"/> with the provided IHttpContextAccessor.
        /// </summary>
        /// <param name="httpContextAccessor">Instance of IHttpContextAccessor to access the current HttpContext.</param>
        public CorrelationIdEnricher(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Enriches the log event with the CorrelationId property if available in the current HTTP context.
        /// Allows automatic inclusion of the correlation identifier in every log entry.
        /// </summary>
        /// <param name="logEvent">The log event to enrich.</param>
        /// <param name="propertyFactory">Factory for creating log properties.</param>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var correlationId = GetCorrelationId();

            if (!string.IsNullOrEmpty(correlationId))
            {
                var property = propertyFactory.CreateProperty(CorrelationIdPropertyName, correlationId);
                logEvent.AddPropertyIfAbsent(property);
            }
        }

        /// <summary>
        /// Retrieves the CorrelationId from HttpContext.Items if available.
        /// Returns null if there is no current HTTP context or the identifier is not set.
        /// </summary>
        /// <returns>The CorrelationId value or null if unavailable.</returns>
        private string? GetCorrelationId()
        {
            try
            {
                // Attempts to get CorrelationId from the current HttpContext's Items.
                // Returns null if HttpContext does not exist or does not contain the key.
                return _httpContextAccessor.HttpContext?.Items[CorrelationIdContextKey] as string;
            }
            catch
            {
                // In case of error (e.g. no HttpContext), return null
                return null;
            }
        }
    }

    /// <summary>
    /// Extension methods for easily adding CorrelationIdEnricher to Serilog configuration.
    /// </summary>
    public static class CorrelationIdEnricherExtensions
    {
        /// <summary>
        /// Adds CorrelationIdEnricher to Serilog configuration.
        /// Allows manual passing of IHttpContextAccessor or using the default.
        /// </summary>
        /// <param name="enrichmentConfiguration">Serilog enrichment configuration.</param>
        /// <param name="httpContextAccessor">Optional IHttpContextAccessor (if not provided, the default will be used).</param>
        /// <returns>LoggerConfiguration for fluent API.</returns>
        public static LoggerConfiguration WithCorrelationId(
            this LoggerEnrichmentConfiguration enrichmentConfiguration,
            IHttpContextAccessor? httpContextAccessor = null)
        {
            if (enrichmentConfiguration == null)
                throw new ArgumentNullException(nameof(enrichmentConfiguration));

            var enricher = httpContextAccessor != null
                ? new CorrelationIdEnricher(httpContextAccessor)
                : new CorrelationIdEnricher();

            return enrichmentConfiguration.With(enricher);
        }

        /// <summary>
        /// Adds CorrelationIdEnricher to Serilog configuration, retrieving IHttpContextAccessor from dependency injection.
        /// Enables integration with the DI container, ensuring correct operation in ASP.NET Core environments.
        /// </summary>
        /// <param name="enrichmentConfiguration">Serilog enrichment configuration.</param>
        /// <param name="serviceProvider">Service provider to retrieve IHttpContextAccessor.</param>
        /// <returns>LoggerConfiguration for fluent API.</returns>
        /// <exception cref="InvalidOperationException">Thrown if IHttpContextAccessor is not registered in DI.</exception>
        public static LoggerConfiguration WithCorrelationIdFromDI(
            this LoggerEnrichmentConfiguration enrichmentConfiguration,
            IServiceProvider serviceProvider)
        {
            if (enrichmentConfiguration == null)
                throw new ArgumentNullException(nameof(enrichmentConfiguration));

            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            var httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
            if (httpContextAccessor == null)
                throw new InvalidOperationException("IHttpContextAccessor not registered in DI. Add services.AddHttpContextAccessor() in Program.cs");

            return enrichmentConfiguration.WithCorrelationId(httpContextAccessor);
        }
    }

}

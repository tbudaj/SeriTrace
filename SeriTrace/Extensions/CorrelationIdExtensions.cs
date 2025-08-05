using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Configuration;
using SeriTrace.Extensions.Serilog;
using SeriTrace.Middleware;
using SeriTrace.Services;

namespace SeriTrace.Extensions
{
    /// <summary>
    /// Provides extension methods for registering CorrelationId services and middleware.
    /// These methods simplify the integration of CorrelationId tracking into the application's
    /// dependency injection container and HTTP request pipeline.
    /// </summary>
    public static class CorrelationIdExtensions
    {
        /// <summary>
        /// Registers CorrelationId-related services in the dependency injection container.
        /// Adds <see cref="IHttpContextAccessor"/> for accessing the current HTTP context and
        /// <see cref="ICorrelationIdService"/> for retrieving CorrelationId and request start time.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <returns>The service collection for fluent API usage.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> is null.</exception>
        public static IServiceCollection AddCorrelationIdServices(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register IHttpContextAccessor for accessing HttpContext in services
            services.AddHttpContextAccessor();
            // Register CorrelationIdService for retrieving correlation information per request
            services.AddScoped<ICorrelationIdService, CorrelationIdService>();

            return services;
        }

        /// <summary>
        /// Adds the <see cref="CorrelationIdMiddleware"/> to the HTTP request pipeline.
        /// This middleware automatically manages CorrelationId for each request, making it available
        /// throughout the request lifecycle and in logs.
        /// <para>NOTE: This should be called at the beginning of the pipeline, before other middleware,</para>
        /// <para>to ensure the CorrelationId is available for the entire request.</para>
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The application builder for fluent API usage.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="app"/> is null.</exception>
        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            // Add CorrelationIdMiddleware to the request pipeline
            return app.UseMiddleware<CorrelationIdMiddleware>();
        }
    }

    /// <summary>
    /// Provides extension methods for easily adding a CorrelationId enricher to Serilog configuration.
    /// These methods allow you to enrich log events with CorrelationId from the current HTTP context.
    /// </summary>
    public static class CorrelationIdEnricherExtensions
    {
        /// <summary>
        /// Adds a CorrelationId enricher to the Serilog enrichment configuration.
        /// This enricher will add the CorrelationId from the current HTTP context to each log event.
        /// </summary>
        /// <param name="enrichmentConfiguration">The Serilog enrichment configuration.</param>
        /// <param name="httpContextAccessor">Optional IHttpContextAccessor (if not provided, a default will be used).</param>
        /// <returns>The logger configuration for fluent API usage.</returns>
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
        /// Adds a CorrelationId enricher to the Serilog enrichment configuration using dependency injection.
        /// This method retrieves IHttpContextAccessor from the provided service provider.
        /// </summary>
        /// <param name="enrichmentConfiguration">The Serilog enrichment configuration.</param>
        /// <param name="serviceProvider">The service provider for dependency injection.</param>
        /// <returns>The logger configuration for fluent API usage.</returns>
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

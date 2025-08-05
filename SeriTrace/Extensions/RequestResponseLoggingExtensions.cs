using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using SeriTrace.Middleware;
using SeriTrace.Options;

namespace SeriTrace.Extensions
{
    /// <summary>
    /// Extension methods for registering HTTP request/response logging middleware
    /// with full Serilog integration and configuration via appsettings.json.
    /// 
    /// Key features:
    /// - Dedicated loggers for requests and responses with independent configuration
    /// - Full parameterization via appsettings.json
    /// - Automatic creation of logs/requests and logs/responses directories
    /// - Safe default values if configuration is missing
    /// - Performance optimized with buffering and size limits
    /// - Automatic integration with CorrelationId
    /// </summary>
    /// <remarks>
    /// <b>RECOMMENDED PIPELINE ORDER:</b>
    /// 1. app.UseCorrelationId()
    /// 2. app.UseRequestResponseLogging()  &lt;-- This method
    /// 3. app.UseSerilogRequestLogging()
    /// 4. Other middleware
    /// 
    /// <b>APPSETTINGS CONFIGURATION EXAMPLE:</b>
    /// {
    ///   "RequestResponseLogging": {
    ///     "Requests": {
    ///       "Enabled": true,
    ///       "LogPath": "logs/requests/requests-.log"
    ///     },
    ///     "Responses": {
    ///       "Enabled": true,
    ///       "LogPath": "logs/responses/responses-.log"
    ///     }
    ///   }
    /// }
    /// </remarks>
    public static class RequestResponseLoggingExtensions
    {
        /// <summary>
        /// Adds services for request/response logging with configuration from appsettings.json.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="configuration">Application configuration.</param>
        /// <returns>IServiceCollection for fluent API.</returns>
        public static IServiceCollection AddRequestResponseLogging(this IServiceCollection services, IConfiguration configuration)
        {
            // Register configuration options
            services.Configure<RequestResponseLoggingOptions>(
                configuration.GetSection(RequestResponseLoggingOptions.SectionName));

            return services;
        }

        /// <summary>
        /// Adds request/response logging middleware with automatic Serilog logger configuration.
        /// </summary>
        /// <param name="app">Application builder.</param>
        /// <param name="configuration">Application configuration (optional).</param>
        /// <returns>IApplicationBuilder for fluent API.</returns>
        /// <remarks>
        /// This method:
        /// 1. Retrieves configuration from DI or parameter
        /// 2. Provides backward compatibility if configuration is missing
        /// 3. Adds middleware to the pipeline with proper configuration
        /// 4. Loggers are already configured by SerilogExtensions
        /// </remarks>
        public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder app, IConfiguration? configuration = null)
        {
            // Get configuration from DI or parameter
            var options = app.ApplicationServices.GetService<IOptions<RequestResponseLoggingOptions>>()?.Value;

            if (options == null && configuration != null)
            {
                options = new RequestResponseLoggingOptions();
                configuration.GetSection(RequestResponseLoggingOptions.SectionName).Bind(options);
            }

            // Use default values if configuration is missing
            options ??= new RequestResponseLoggingOptions();

            // Add middleware (loggers are already configured by SerilogExtensions)
            return app.UseMiddleware<RequestResponseLoggingMiddleware>();
        }

        /// <summary>
        /// Parses a string log level to LogEventLevel.
        /// </summary>
        /// <param name="level">Log level as string.</param>
        /// <returns>Parsed LogEventLevel, or Information if not recognized.</returns>
        private static LogEventLevel ParseLogLevel(string level)
        {
            return level?.ToLowerInvariant() switch
            {
                "verbose" => LogEventLevel.Verbose,
                "debug" => LogEventLevel.Debug,
                "information" => LogEventLevel.Information,
                "warning" => LogEventLevel.Warning,
                "error" => LogEventLevel.Error,
                "fatal" => LogEventLevel.Fatal,
                _ => LogEventLevel.Information
            };
        }

        /// <summary>
        /// Parses a string rolling interval to RollingInterval.
        /// </summary>
        /// <param name="interval">Rolling interval as string.</param>
        /// <returns>Parsed RollingInterval, or Day if not recognized.</returns>
        private static RollingInterval ParseRollingInterval(string interval)
        {
            return interval?.ToLowerInvariant() switch
            {
                "minute" => RollingInterval.Minute,
                "hour" => RollingInterval.Hour,
                "day" => RollingInterval.Day,
                "month" => RollingInterval.Month,
                "year" => RollingInterval.Year,
                "infinite" => RollingInterval.Infinite,
                _ => RollingInterval.Day
            };
        }
    }

}

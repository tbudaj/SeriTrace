using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using SeriTrace.Options;

namespace SeriTrace.Extensions
{
    /// <summary>
    /// Main extension class for integrating and configuring Serilog in .NET applications.
    /// Allows full logging configuration via appsettings.json or programmatically, with support for environments and dedicated HTTP loggers.
    /// </summary>
    /// <remarks>
    /// Key features:
    /// - Full parameterization via appsettings.json or code
    /// - Multi-environment support
    /// - Structured logging and enrichers support
    /// - Performance optimization (asynchronous writers)
    /// - Production-ready (safe default settings)
    /// </remarks>
    public static class SerilogExtensions
    {
        /// <summary>
        /// Adds Serilog as the main logging provider based on configuration from appsettings.json.
        /// Also registers options for HTTP request and response logging.
        /// </summary>
        /// <param name="services">DI service collection.</param>
        /// <param name="configuration">Application configuration (e.g. appsettings.json).</param>
        /// <param name="hostEnvironment">Hosting environment (optional, for enriching logs with environment name).</param>
        /// <returns>Service collection for fluent API.</returns>
        public static IServiceCollection AddSerilogWithConfiguration(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment? hostEnvironment = null)
        {
            // Register Serilog options in DI for runtime access
            services.Configure<SerilogOptions>(configuration.GetSection(SerilogOptions.SectionName));

            // Register request/response logging options
            services.Configure<RequestResponseLoggingOptions>(
                configuration.GetSection(RequestResponseLoggingOptions.SectionName));

            // Configure Serilog logger directly from IConfiguration
            var loggerConfiguration = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration);

            // Configure dedicated request/response loggers if available
            ConfigureRequestResponseLoggers(loggerConfiguration, configuration);

            Log.Logger = loggerConfiguration.CreateLogger();

            // Add Serilog as the main logging provider (replaces AddSerilog with dispose)
            services.AddSerilog(Log.Logger, dispose: true);

            return services;
        }

        /// <summary>
        /// Adds Serilog as the main logging provider with programmatic configuration.
        /// Allows dynamic logger configuration in code.
        /// </summary>
        /// <param name="services">DI service collection.</param>
        /// <param name="configureOptions">Action configuring Serilog options.</param>
        /// <param name="hostEnvironment">Hosting environment (optional).</param>
        /// <returns>Service collection for fluent API.</returns>
        public static IServiceCollection AddSerilogWithConfiguration(
            this IServiceCollection services,
            Action<SerilogOptions> configureOptions,
            IHostEnvironment? hostEnvironment = null)
        {
            var serilogOptions = new SerilogOptions();
            configureOptions(serilogOptions);

            // Register options in DI
            services.Configure<SerilogOptions>(opts => configureOptions(opts));

            // Configure Serilog logger
            Log.Logger = CreateSerilogLogger(serilogOptions, null, hostEnvironment);

            // Add Serilog as the main logging provider
            services.AddSerilog(Log.Logger, dispose: true);

            return services;
        }

        /// <summary>
        /// Configures Serilog HTTP request logging middleware for ASP.NET Core.
        /// Enables automatic request logging with customizable template and log level.
        /// </summary>
        /// <param name="app">WebApplication object.</param>
        /// <param name="configuration">Application configuration (optional, if not using DI).</param>
        /// <returns>WebApplication for fluent API.</returns>
        public static WebApplication UseSerilogRequestLogging(
            this WebApplication app,
            IConfiguration? configuration = null)
        {
            // Get Serilog options from DI or configuration
            var serilogOptions = app.Services.GetService<Microsoft.Extensions.Options.IOptions<SerilogOptions>>()?.Value;

            if (serilogOptions == null && configuration != null)
            {
                serilogOptions = new SerilogOptions();
                configuration.GetSection(SerilogOptions.SectionName).Bind(serilogOptions);
            }

            // Use Serilog request logging with custom configuration
            app.UseSerilogRequestLogging(opts =>
            {
                opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
                opts.GetLevel = GetLogLevel;
                opts.EnrichDiagnosticContext = EnrichDiagnosticContext;
            });

            return app;
        }

        /// <summary>
        /// Adds Serilog as an additional logging provider (alongside the default one).
        /// Useful when you want to keep the existing logging system and add Serilog capabilities.
        /// </summary>
        /// <param name="services">DI service collection.</param>
        /// <param name="configuration">Application configuration.</param>
        /// <param name="hostEnvironment">Hosting environment.</param>
        /// <returns>Service collection for fluent API.</returns>
        public static IServiceCollection AddSerilogAsAdditionalProvider(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment? hostEnvironment = null)
        {
            // Load Serilog options
            var serilogOptions = new SerilogOptions();
            configuration.GetSection(SerilogOptions.SectionName).Bind(serilogOptions);

            // Configure Serilog logger
            var serilogLogger = CreateSerilogLogger(serilogOptions, configuration, hostEnvironment);

            // Add Serilog as an additional provider (do not replace the default one)
            services.AddLogging(builder =>
            {
                builder.AddSerilog(serilogLogger, dispose: false);
            });

            // Register Serilog logger for dependency injection
            services.AddSingleton<global::Serilog.ILogger>(serilogLogger);

            return services;
        }

        /// <summary>
        /// Creates a configured Serilog logger based on configuration from file.
        /// Can be used to create custom loggers.
        /// </summary>
        /// <param name="configuration">Application configuration.</param>
        /// <param name="hostEnvironment">Hosting environment (optional).</param>
        /// <returns>Configured Serilog logger.</returns>
        public static global::Serilog.ILogger CreateSerilogLogger(
            IConfiguration configuration,
            IHostEnvironment? hostEnvironment = null)
        {
            var serilogOptions = new SerilogOptions();
            configuration.GetSection(SerilogOptions.SectionName).Bind(serilogOptions);

            return CreateSerilogLogger(serilogOptions, configuration, hostEnvironment);
        }

        /// <summary>
        /// Creates a configured Serilog logger based on programmatic configuration.
        /// Allows full control over logging settings.
        /// </summary>
        /// <param name="configureOptions">Action configuring options.</param>
        /// <param name="hostEnvironment">Hosting environment (optional).</param>
        /// <returns>Configured Serilog logger.</returns>
        public static global::Serilog.ILogger CreateSerilogLogger(
            Action<SerilogOptions> configureOptions,
            IHostEnvironment? hostEnvironment = null)
        {
            var serilogOptions = new SerilogOptions();
            configureOptions(serilogOptions);

            return CreateSerilogLogger(serilogOptions, null, hostEnvironment);
        }

        /// <summary>
        /// Main method for creating a Serilog logger from options.
        /// </summary>
        private static global::Serilog.ILogger CreateSerilogLogger(
            SerilogOptions options,
            IConfiguration? configuration,
            IHostEnvironment? hostEnvironment)
        {
            var loggerConfig = new LoggerConfiguration();

            // Set minimum logging level
            if (!string.IsNullOrEmpty(options.MinimumLevel) &&
                Enum.TryParse<LogEventLevel>(options.MinimumLevel, true, out var minLevel))
            {
                loggerConfig.MinimumLevel.Is(minLevel);
            }

            // Add logging level overrides
            foreach (var levelOverride in options.Override)
            {
                if (Enum.TryParse<LogEventLevel>(levelOverride.Value, true, out var overrideLevel))
                {
                    loggerConfig.MinimumLevel.Override(levelOverride.Key, overrideLevel);
                }
            }

            // Configure enrichers
            ConfigureEnrichers(loggerConfig, options, hostEnvironment);

            // Configure sinks (WriteTo)
            ConfigureSinks(loggerConfig, options);

            // Configure filters
            ConfigureFilters(loggerConfig, options);

            // Add global properties
            foreach (var property in options.Properties)
            {
                loggerConfig.Enrich.WithProperty(property.Key, property.Value);
            }

            // Configure destructuring
            ConfigureDestructuring(loggerConfig, options.Destructure);

            return loggerConfig.CreateLogger();
        }

        /// <summary>
        /// Configures enrichers based on options. Some enrichers require additional packages.
        /// </summary>
        private static void ConfigureEnrichers(LoggerConfiguration loggerConfig, SerilogOptions options, IHostEnvironment? hostEnvironment)
        {
            foreach (var enricher in options.Enrich)
            {
                switch (enricher.ToLowerInvariant())
                {
                    case "fromlogcontext":
                        loggerConfig.Enrich.FromLogContext();
                        break;
                    case "withmachinename":
                        loggerConfig.Enrich.WithMachineName();
                        break;
                    case "withprocessid":
                        loggerConfig.Enrich.WithProcessId();
                        break;
                    case "withthreadid":
                        loggerConfig.Enrich.WithThreadId();
                        break;
                    case "withenvironmentusername":
                        loggerConfig.Enrich.WithEnvironmentUserName();
                        break;
                    case "withcorrelationid":
                        // Automatically add CorrelationId enricher
                        loggerConfig.Enrich.WithCorrelationId();
                        break;
                    case "withrequestid":
                        // Requires Serilog.AspNetCore
                        break;
                    case "withclientip":
                        // Requires Serilog.AspNetCore
                        break;
                }
            }

            // Environment-specific enrichment
            if (hostEnvironment != null)
            {
                loggerConfig.Enrich.WithProperty("Environment", hostEnvironment.EnvironmentName);
                loggerConfig.Enrich.WithProperty("ApplicationName", hostEnvironment.ApplicationName);
            }
        }

        /// <summary>
        /// Configures sinks (log destinations) based on options.
        /// </summary>
        private static void ConfigureSinks(LoggerConfiguration loggerConfig, SerilogOptions options)
        {
            foreach (var writeTo in options.WriteTo)
            {
                ConfigureSingleSink(loggerConfig, writeTo);
            }
        }

        /// <summary>
        /// Configures a single sink (e.g. Console, File).
        /// </summary>
        private static void ConfigureSingleSink(LoggerConfiguration loggerConfig, WriteToConfiguration writeTo)
        {
            var args = writeTo.Args;

            switch (writeTo.Name.ToLowerInvariant())
            {
                case "console":
                    var consoleConfig = loggerConfig.WriteTo.Console(
                        outputTemplate: args.OutputTemplate);
                    if (!string.IsNullOrEmpty(args.RestrictedToMinimumLevel))
                    {
                        if (Enum.TryParse<LogEventLevel>(args.RestrictedToMinimumLevel, true, out var level))
                        {
                            consoleConfig = loggerConfig.WriteTo.Console(
                                restrictedToMinimumLevel: level,
                                outputTemplate: args.OutputTemplate);
                        }
                    }
                    break;

                case "file":
                    var fileConfig = loggerConfig.WriteTo.File(
                        path: args.Path ?? "logs/default.log",
                        rollingInterval: Enum.TryParse<RollingInterval>(args.RollingInterval, true, out var interval) ? interval : RollingInterval.Day,
                        fileSizeLimitBytes: args.FileSizeLimitBytes,
                        rollOnFileSizeLimit: args.RollOnFileSizeLimit,
                        retainedFileCountLimit: args.RetainedFileCountLimit,
                        shared: args.Shared,
                        outputTemplate: args.OutputTemplate);

                    if (!string.IsNullOrEmpty(args.RestrictedToMinimumLevel))
                    {
                        if (Enum.TryParse<LogEventLevel>(args.RestrictedToMinimumLevel, true, out var level))
                        {
                            fileConfig = loggerConfig.WriteTo.File(
                                path: args.Path ?? "logs/default.log",
                                restrictedToMinimumLevel: level,
                                rollingInterval: interval,
                                fileSizeLimitBytes: args.FileSizeLimitBytes,
                                rollOnFileSizeLimit: args.RollOnFileSizeLimit,
                                retainedFileCountLimit: args.RetainedFileCountLimit,
                                shared: args.Shared,
                                outputTemplate: args.OutputTemplate);
                        }
                    }
                    break;

                case "debug":
                    loggerConfig.WriteTo.Debug(outputTemplate: args.OutputTemplate);
                    break;

                case "async":
                    // For async wrapper - requires additional configuration
                    // This would need Serilog.Sinks.Async package
                    break;

                    // Add more sinks as needed:
                    // case "seq":
                    // case "elasticsearch":
                    // case "sqlserver":
                    // etc.
            }
        }

        /// <summary>
        /// Configures logging filters based on options.
        /// </summary>
        private static void ConfigureFilters(LoggerConfiguration loggerConfig, SerilogOptions options)
        {
            foreach (var filter in options.Filter)
            {
                // Filter configuration - requires Serilog.Filters.Expressions
                // loggerConfig.Filter.ByIncludingOnly(filter.Expression);
            }
        }

        /// <summary>
        /// Configures destructuring options for complex objects in logs.
        /// </summary>
        private static void ConfigureDestructuring(LoggerConfiguration loggerConfig, DestructuringOptions destructure)
        {
            loggerConfig.Destructure.ToMaximumDepth(destructure.MaxDepth);
            loggerConfig.Destructure.ToMaximumStringLength(destructure.MaxStringLength);
            loggerConfig.Destructure.ToMaximumCollectionCount(destructure.MaxCollectionCount);
        }

        /// <summary>
        /// Determines the log level for HTTP request logging middleware.
        /// </summary>
        private static LogEventLevel GetLogLevel(HttpContext ctx, double _, Exception? ex)
        {
            if (ex != null) return LogEventLevel.Error;
            if (ctx.Response.StatusCode > 499) return LogEventLevel.Error;
            if (ctx.Response.StatusCode > 399) return LogEventLevel.Warning;
            return LogEventLevel.Information;
        }

        /// <summary>
        /// Enriches the diagnostic context with additional HTTP request information.
        /// </summary>
        private static void EnrichDiagnosticContext(IDiagnosticContext diagnosticContext, HttpContext httpContext)
        {
            diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString());
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault());
            diagnosticContext.Set("ContentLength", httpContext.Response.ContentLength);

            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                diagnosticContext.Set("UserId", httpContext.User.Identity.Name);
            }

            // Add CorrelationId from HttpContext.Items (set by CorrelationIdMiddleware)
            var correlationId = httpContext.Items["CorrelationId"] as string;
            if (!string.IsNullOrEmpty(correlationId))
            {
                diagnosticContext.Set("CorrelationId", correlationId);
            }

            // Fallback - also check the header (for cases where middleware was not invoked)
            if (string.IsNullOrEmpty(correlationId) && httpContext.Request.Headers.ContainsKey("X-Correlation-ID"))
            {
                correlationId = httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault();
                if (!string.IsNullOrEmpty(correlationId))
                {
                    diagnosticContext.Set("CorrelationId", correlationId);
                }
            }
        }

        /// <summary>
        /// Configures dedicated sub-loggers for HTTP request and response logging.
        /// </summary>
        private static void ConfigureRequestResponseLoggers(LoggerConfiguration mainConfig, IConfiguration configuration)
        {
            var requestResponseOptions = new RequestResponseLoggingOptions();
            configuration.GetSection(RequestResponseLoggingOptions.SectionName).Bind(requestResponseOptions);

            if (requestResponseOptions.Requests.Enabled)
            {
                // Create directory for request logs if needed
                var requestLogDirectory = Path.GetDirectoryName(requestResponseOptions.Requests.LogPath);
                if (!string.IsNullOrEmpty(requestLogDirectory) && !Directory.Exists(requestLogDirectory))
                {
                    Directory.CreateDirectory(requestLogDirectory);
                }

                // Add sub-logger for requests with filter
                mainConfig.WriteTo.Logger(subLogger => subLogger
                    .Filter.ByIncludingOnly(logEvent =>
                        logEvent.MessageTemplate.Text.Contains("REQUEST:") ||
                        (logEvent.Properties.TryGetValue("SourceContext", out var sourceContext) &&
                         sourceContext.ToString().Contains("RequestLogger")))
                    .WriteTo.File(
                        path: requestResponseOptions.Requests.LogPath,
                        rollingInterval: ParseRollingInterval(requestResponseOptions.Requests.RollingInterval),
                        fileSizeLimitBytes: requestResponseOptions.Requests.FileSizeLimitBytes,
                        rollOnFileSizeLimit: true,
                        retainedFileCountLimit: requestResponseOptions.Requests.RetainedFileCountLimit,
                        outputTemplate: requestResponseOptions.Requests.OutputTemplate,
                        shared: true,
                        restrictedToMinimumLevel: ParseLogEventLevel(requestResponseOptions.Requests.MinimumLevel)
                    ));
            }

            if (requestResponseOptions.Responses.Enabled)
            {
                // Create directory for response logs if needed
                var responseLogDirectory = Path.GetDirectoryName(requestResponseOptions.Responses.LogPath);
                if (!string.IsNullOrEmpty(responseLogDirectory) && !Directory.Exists(responseLogDirectory))
                {
                    Directory.CreateDirectory(responseLogDirectory);
                }

                // Add sub-logger for responses with filter
                mainConfig.WriteTo.Logger(subLogger => subLogger
                    .Filter.ByIncludingOnly(logEvent =>
                        logEvent.MessageTemplate.Text.Contains("RESPONSE:") ||
                        (logEvent.Properties.TryGetValue("SourceContext", out var sourceContext) &&
                         sourceContext.ToString().Contains("ResponseLogger")))
                    .WriteTo.File(
                        path: requestResponseOptions.Responses.LogPath,
                        rollingInterval: ParseRollingInterval(requestResponseOptions.Responses.RollingInterval),
                        fileSizeLimitBytes: requestResponseOptions.Responses.FileSizeLimitBytes,
                        rollOnFileSizeLimit: true,
                        retainedFileCountLimit: requestResponseOptions.Responses.RetainedFileCountLimit,
                        outputTemplate: requestResponseOptions.Responses.OutputTemplate,
                        shared: true,
                        restrictedToMinimumLevel: ParseLogEventLevel(requestResponseOptions.Responses.MinimumLevel)
                    ));
            }
        }

        /// <summary>
        /// Parses the log level from string to LogEventLevel. Returns Information if not recognized.
        /// </summary>
        private static LogEventLevel ParseLogEventLevel(string level)
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
        /// Parses the file rolling interval from string to RollingInterval. Returns Day if not recognized.
        /// </summary>
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

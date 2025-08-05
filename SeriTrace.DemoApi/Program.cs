using SeriTrace.Extensions;

// Create the web application builder with command line arguments and default configuration
// Automatically loads appsettings.json, environment variables, and command line arguments
// Sets up the basic dependency injection container and configuration system
var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureLogging((context, logging) =>
{
    logging.ClearProviders(); // Remove all default providers
});

// ✅ Serilog Configuration as Main Logging Provider
// Loads settings from appsettings.json "Serilog" section with full configuration support
// Configures intelligent environment-specific defaults:
//   - Development: Debug level + Console output with colored themes
//   - Production: Warning level + File output with async writers for performance
// Registers Serilog as the main logging provider replacing default .NET logging
// Configures enrichers automatically: MachineName, ProcessId, CorrelationId, Environment
// Additionally configures dedicated sub-loggers for Request/Response HTTP traffic logging
// Supports multiple sinks: Console, File, Seq, ElasticSearch, SQL Server, and more
// Enables structured logging with correlation context across the entire application
// Creates log directories automatically if they don't exist (logs/, logs/requests/, logs/responses/)
builder.Services.AddSerilogWithConfiguration(builder.Configuration, builder.Environment);

// ✅ Correlation ID Services Registration
// Adds IHttpContextAccessor to DI container for accessing current HTTP context in services
// Registers ICorrelationIdService as scoped service for retrieving correlation information
// Enables access to CorrelationId and request start time in controllers and business services
// Provides thread-safe access to correlation context throughout request lifecycle
// Essential for distributed tracing and request correlation across microservices architecture
// Allows business logic to access correlation context without direct middleware dependencies
builder.Services.AddCorrelationIdServices();

// ✅ Request/Response Logging Services Registration
// Registers configuration options for dedicated HTTP request and response loggers
// Configures separate log files with full parameterization via appsettings.json:
//   - logs/requests/requests-YYYYMMDD.log for incoming HTTP requests
//   - logs/responses/responses-YYYYMMDD.log for outgoing HTTP responses
// Enables detailed HTTP traffic logging with smart filtering capabilities:
//   - Automatic exclusion of binary content types (images, videos, PDFs)
//   - Filtering of sensitive headers (Authorization, Cookie, API keys)
//   - Exclusion of health check endpoints and system paths
// Supports configurable body size limits, file retention policies, and rolling intervals
// Creates log directories with proper permissions and configurable retention policies
builder.Services.AddRequestResponseLogging(builder.Configuration);

// Standard ASP.NET Core services registration
builder.Services.AddControllers();                    // MVC controllers for API endpoints
builder.Services.AddEndpointsApiExplorer();           // API explorer for OpenAPI/Swagger discovery
builder.Services.AddSwaggerGen();                     // Swagger/OpenAPI documentation generation

// Build the web application from configured services
// Creates the HTTP request pipeline and initializes all registered services
var app = builder.Build();

// ═══════════════════════════════════════════════════════════════════════════════════════
// MIDDLEWARE PIPELINE CONFIGURATION - ORDER IS CRITICAL FOR PROPER FUNCTIONALITY
// ═══════════════════════════════════════════════════════════════════════════════════════

// ✅ CRITICAL: CorrelationId Middleware - MUST BE FIRST in pipeline
// Position: #1 - Highest priority, executed before any other custom middleware
// Automatically extracts CorrelationId from X-Correlation-ID request header if present
// Generates unique CorrelationId (format: CID-YYMMDDHHMMSS-RandomHex) if header missing
// Stores CorrelationId and request start timestamp in HttpContext.Items for global access
// Enriches JSON response metadata with correlationId, timestamp, and processing duration:
//   - For object responses: Adds metadata property at root level
//   - For array/primitive responses: Wraps in envelope with data and metadata sections
//   - Preserves existing metadata fields while adding correlation information
// Adds correlation ID to response headers (X-Correlation-ID) for backward compatibility
// Integrates with Serilog LogContext for automatic log enrichment across all loggers
// Enables end-to-end request tracking across distributed systems and microservices
// Captures response body stream for metadata processing without affecting performance
app.UseCorrelationId();

// ✅ Request/Response Logging Middleware - SECOND in pipeline after CorrelationId
// Position: #2 - Executed after correlation context is established
// Logs incoming HTTP requests with comprehensive context information:
//   - Request method, path, query string, and headers (filtered for security)
//   - Request body content (with configurable size limits and content-type filtering)
//   - Client IP address, User-Agent, and authentication context
//   - Request timestamp and correlation ID for traceability
// Logs outgoing HTTP responses with complete timing and context:
//   - Response status code, headers, and body content (with filtering)
//   - Processing duration from request start to response completion
//   - Content-Type, content length, and response metadata
//   - Correlation ID linking request and response logs
// Uses dedicated log files with independent configuration:
//   - logs/requests/requests-YYYYMMDD.log for detailed request analysis
//   - logs/responses/responses-YYYYMMDD.log for response monitoring
// Automatically excludes binary files, sensitive headers, and system endpoints
// Provides detailed HTTP traffic analysis and debugging capabilities for production systems
// Includes correlation ID in all request/response logs for easy distributed tracing
app.UseRequestResponseLogging(builder.Configuration);

// ✅ Serilog HTTP Request Logging - THIRD in pipeline for high-level structured logging
// Position: #3 - Executed after detailed request/response logging is configured
// Provides automatic structured logging of all HTTP requests with performance metrics
// Uses optimized template: "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms"
// Automatically enriches logs with contextual information:
//   - ClientIP: Remote client IP address for security and analytics
//   - UserAgent: Browser/client information for compatibility analysis
//   - CorrelationId: Request correlation identifier from middleware above
//   - UserId: Authenticated user identity if authentication is enabled
// Determines log levels dynamically based on response status:
//   - Information: Successful requests (2xx-3xx status codes)
//   - Warning: Client errors (4xx status codes)
//   - Error: Server errors (5xx status codes) and exceptions
// NOTE: Automatically inherits CorrelationId from LogContext set by middleware above
// Provides high-level request metrics and performance monitoring for dashboards
// Integrates seamlessly with existing Serilog configuration, sinks, and enrichers
// Optimized for high-throughput scenarios with minimal performance impact
app.UseSerilogRequestLogging(builder.Configuration);

// Development-specific middleware - only active in Development environment
if (app.Environment.IsDevelopment())
{
    // Enable Swagger UI for interactive API documentation and testing
    // Provides web-based interface for exploring and testing API endpoints
    // Automatically generates OpenAPI specification from controller attributes
    app.UseSwagger();                                  // Serve OpenAPI/Swagger JSON specification
    app.UseSwaggerUI();                                // Serve Swagger UI web interface
}

// Standard ASP.NET Core middleware pipeline for production functionality
app.UseHttpsRedirection();                            // Redirect HTTP requests to HTTPS for security
app.UseAuthorization();                               // Handle authentication and authorization policies
app.MapControllers();                                 // Map controller routes and endpoint discovery

// Start the web application and begin listening for HTTP requests
// Blocks the main thread and runs the application until shutdown is requested
app.Run();

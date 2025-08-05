namespace SeriTrace.Options
{
    /// <summary>
    /// Main Serilog configuration for the application. Maps the 'Serilog' section from appsettings.json.
    /// Allows full customization of log destinations (sinks), enrichers, filters, and logging options.
    /// </summary>
    public class SerilogOptions
    {
        /// <summary>
        /// Section name in appsettings.json for Serilog configuration.
        /// </summary>
        public const string SectionName = "Serilog";

        /// <summary>
        /// List of used packages (e.g. sinks, enrichers, extensions). Enables dynamic loading of extensions.
        /// </summary>
        public List<string> Using { get; set; } = new();

        /// <summary>
        /// Minimum logging level for the entire application (Verbose, Debug, Information, Warning, Error, Fatal).
        /// </summary>
        public string MinimumLevel { get; set; } = "Information";

        /// <summary>
        /// Configuration of log destinations (e.g. Console, File, Seq, ElasticSearch).
        /// </summary>
        public List<WriteToConfiguration> WriteTo { get; set; } = new();

        /// <summary>
        /// List of enrichers adding extra information to logs (e.g. CorrelationId, MachineName).
        /// </summary>
        public List<string> Enrich { get; set; } = new();

        /// <summary>
        /// Logging level overrides for specific namespaces or components.
        /// Key: namespace name, Value: minimum logging level.
        /// </summary>
        public Dictionary<string, string> Override { get; set; } = new();

        /// <summary>
        /// Global properties added to all logs (e.g. Application, Environment).
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new();

        /// <summary>
        /// Logging filter configuration, allowing exclusion or inclusion of logs based on expressions.
        /// </summary>
        public List<FilterConfiguration> Filter { get; set; } = new();

        /// <summary>
        /// Options for destructuring objects in logs (e.g. depth, string length, collection limits).
        /// </summary>
        public DestructuringOptions Destructure { get; set; } = new();

        /// <summary>
        /// Configuration for dedicated HTTP request and response loggers.
        /// Allows separate logging of HTTP traffic with additional options.
        /// </summary>
        public RequestResponseLoggingOptions RequestResponseLogging { get; set; } = new();
    }

    /// <summary>
    /// Configuration for HTTP request and response logging.
    /// Allows separate settings for request and response logging.
    /// </summary>
    public class RequestResponseLoggingOptions
    {
        /// <summary>
        /// Section name in appsettings.json for this configuration.
        /// </summary>
        public const string SectionName = "RequestResponseLogging";

        /// <summary>
        /// Configuration for HTTP request logger (incoming requests).
        /// </summary>
        public RequestLoggingOptions Requests { get; set; } = new();

        /// <summary>
        /// Configuration for HTTP response logger (outgoing responses).
        /// </summary>
        public ResponseLoggingOptions Responses { get; set; } = new();
    }

    /// <summary>
    /// Configuration for logging incoming HTTP requests.
    /// Allows detailed settings for request logging, including body, headers, and exclusions.
    /// </summary>
    public class RequestLoggingOptions
    {
        /// <summary>
        /// Whether to enable HTTP request logging.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Path to the request log file (can include rolling pattern).
        /// </summary>
        public string LogPath { get; set; } = "logs/requests/requests-.log";

        /// <summary>
        /// Minimum logging level for requests.
        /// </summary>
        public string MinimumLevel { get; set; } = "Information";

        /// <summary>
        /// Log file rolling interval (e.g. Day, Hour, Month).
        /// </summary>
        public string RollingInterval { get; set; } = "Day";

        /// <summary>
        /// Maximum log file size in bytes before rolling.
        /// </summary>
        public long FileSizeLimitBytes { get; set; } = 536870912; // 512MB

        /// <summary>
        /// Maximum number of retained log files.
        /// </summary>
        public int RetainedFileCountLimit { get; set; } = 31;

        /// <summary>
        /// Whether to log the request body for POST/PUT/PATCH methods.
        /// </summary>
        public bool LogRequestBody { get; set; } = true;

        /// <summary>
        /// Maximum size of the logged request body in bytes.
        /// </summary>
        public int MaxRequestBodySize { get; set; } = 32768; // 32KB

        /// <summary>
        /// Whether to log request headers.
        /// </summary>
        public bool LogRequestHeaders { get; set; } = true;

        /// <summary>
        /// List of headers excluded from logging (e.g. Authorization, Cookie).
        /// </summary>
        public List<string> ExcludedHeaders { get; set; } = new()
        {
            "Authorization",
            "Cookie",
            "Set-Cookie",
            "X-API-Key",
            "X-Auth-Token"
        };

        /// <summary>
        /// Content-Types excluded from body logging (e.g. binary files, images).
        /// </summary>
        public List<string> ExcludedContentTypes { get; set; } = new()
        {
            "multipart/form-data",
            "application/octet-stream",
            "image/*",
            "video/*",
            "audio/*",
            "application/pdf",
            "application/zip"
        };

        /// <summary>
        /// URL paths excluded from logging (e.g. /health, /metrics).
        /// </summary>
        public List<string> ExcludedPaths { get; set; } = new()
        {
            "/health",
            "/metrics",
            "/swagger"
        };

        /// <summary>
        /// Output template for request log messages.
        /// Available properties: Timestamp, Level, CorrelationId, RequestMethod, RequestPath, Message, Properties, NewLine.
        /// </summary>
        public string OutputTemplate { get; set; } = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{CorrelationId}] REQUEST: {RequestMethod} {RequestPath} {Message:lj} {Properties:j}{NewLine}";
    }

    /// <summary>
    /// Configuration for logging outgoing HTTP responses.
    /// Allows detailed settings for response logging, including body, headers, and exclusions.
    /// </summary>
    public class ResponseLoggingOptions
    {
        /// <summary>
        /// Whether to enable HTTP response logging.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Path to the response log file (can include rolling pattern).
        /// </summary>
        public string LogPath { get; set; } = "logs/responses/responses-.log";

        /// <summary>
        /// Minimum logging level for responses.
        /// </summary>
        public string MinimumLevel { get; set; } = "Information";

        /// <summary>
        /// Log file rolling interval (e.g. Day, Hour, Month).
        /// </summary>
        public string RollingInterval { get; set; } = "Day";

        /// <summary>
        /// Maximum log file size in bytes before rolling.
        /// </summary>
        public long FileSizeLimitBytes { get; set; } = 536870912; // 512MB

        /// <summary>
        /// Maximum number of retained log files.
        /// </summary>
        public int RetainedFileCountLimit { get; set; } = 31;

        /// <summary>
        /// Whether to log the response body.
        /// </summary>
        public bool LogResponseBody { get; set; } = true;

        /// <summary>
        /// Maximum size of the logged response body in bytes.
        /// </summary>
        public int MaxResponseBodySize { get; set; } = 32768; // 32KB

        /// <summary>
        /// Whether to log response headers.
        /// </summary>
        public bool LogResponseHeaders { get; set; } = true;

        /// <summary>
        /// List of headers excluded from response logging (e.g. Set-Cookie).
        /// </summary>
        public List<string> ExcludedHeaders { get; set; } = new()
        {
            "Set-Cookie",
            "X-Internal-Token"
        };

        /// <summary>
        /// Content-Types excluded from body logging (e.g. binary files, images).
        /// </summary>
        public List<string> ExcludedContentTypes { get; set; } = new()
        {
            "application/octet-stream",
            "image/*",
            "video/*",
            "audio/*",
            "application/pdf",
            "application/zip"
        };

        /// <summary>
        /// Status codes excluded from response body logging (e.g. 204 No Content, 304 Not Modified).
        /// </summary>
        public List<int> ExcludedStatusCodes { get; set; } = new()
        {
            204, // No Content
            304  // Not Modified
        };

        /// <summary>
        /// URL paths excluded from logging (e.g. /health, /metrics).
        /// </summary>
        public List<string> ExcludedPaths { get; set; } = new()
        {
            "/health",
            "/metrics",
            "/swagger"
        };

        /// <summary>
        /// Output template for response log messages.
        /// Available properties: Timestamp, Level, CorrelationId, StatusCode, RequestPath, Duration, Message, Properties, NewLine.
        /// </summary>
        public string OutputTemplate { get; set; } = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{CorrelationId}] RESPONSE: {StatusCode} {RequestPath} in {Duration}ms {Message:lj} {Properties:j}{NewLine}";
    }

    /// <summary>
    /// Configuration for log destination (sink).
    /// Allows specifying the sink type and its detailed parameters.
    /// </summary>
    public class WriteToConfiguration
    {
        /// <summary>
        /// Sink name (e.g. Console, File, Seq, ElasticSearch).
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Sink-specific configuration arguments.
        /// </summary>
        public WriteToArgsConfiguration Args { get; set; } = new();
    }

    /// <summary>
    /// Configuration arguments for a sink (e.g. File, Seq, ElasticSearch).
    /// Allows detailed setting of log write parameters.
    /// </summary>
    public class WriteToArgsConfiguration
    {
        /// <summary>
        /// Path to the log file (for File sink).
        /// </summary>
        public string? Path { get; set; } = "logs/log-.log";

        /// <summary>
        /// File rolling interval (Day, Hour, Month, Year, Infinite).
        /// </summary>
        public string RollingInterval { get; set; } = "Day";

        /// <summary>
        /// Maximum file size in bytes before rolling.
        /// </summary>
        public long FileSizeLimitBytes { get; set; } = 1073741824; // 1GB

        /// <summary>
        /// Whether to enable rolling based on file size.
        /// </summary>
        public bool RollOnFileSizeLimit { get; set; } = true;

        /// <summary>
        /// Maximum number of retained log files.
        /// </summary>
        public int RetainedFileCountLimit { get; set; } = 31;

        /// <summary>
        /// Whether to enable shared mode (multi-process file writing).
        /// </summary>
        public bool Shared { get; set; } = false;

        /// <summary>
        /// Output template for log messages.
        /// </summary>
        public string OutputTemplate { get; set; } = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}";

        /// <summary>
        /// Log level for this sink (if different from global).
        /// </summary>
        public string? RestrictedToMinimumLevel { get; set; }

        /// <summary>
        /// Log formatting (json, compact, rendered, etc.).
        /// </summary>
        public string? Formatter { get; set; }

        /// <summary>
        /// Server URL (for Seq, ElasticSearch, etc.).
        /// </summary>
        public string? ServerUrl { get; set; }

        /// <summary>
        /// API Key (for Seq, etc.).
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Index name (for ElasticSearch).
        /// </summary>
        public string? IndexFormat { get; set; }

        /// <summary>
        /// Connection string (for SQL Server, PostgreSQL, etc.).
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Table name (for database sinks).
        /// </summary>
        public string? TableName { get; set; }

        /// <summary>
        /// Whether to auto-create the table (for database sinks).
        /// </summary>
        public bool AutoCreateSqlTable { get; set; } = true;

        /// <summary>
        /// Batch size for asynchronous sinks (e.g. Seq, ElasticSearch).
        /// </summary>
        public int BatchPostingLimit { get; set; } = 50;

        /// <summary>
        /// Batch posting interval in seconds.
        /// </summary>
        public double Period { get; set; } = 2.0;

        /// <summary>
        /// Whether to log to Theme Console (colored output).
        /// </summary>
        public string? Theme { get; set; }

        /// <summary>
        /// Additional sink-specific properties.
        /// </summary>
        public Dictionary<string, object> AdditionalProperties { get; set; } = new();
    }

    /// <summary>
    /// Logging filter configuration.
    /// Allows specifying the filter type and conditional expression.
    /// </summary>
    public class FilterConfiguration
    {
        /// <summary>
        /// Filter type (e.g. ByIncluding, ByExcluding).
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Filter expression (e.g. based on log property).
        /// </summary>
        public string Expression { get; set; } = "";
    }

    /// <summary>
    /// Options for destructuring objects in logs.
    /// Allows limiting depth, string length, and collection size.
    /// </summary>
    public class DestructuringOptions
    {
        /// <summary>
        /// Maximum destructuring depth for objects.
        /// </summary>
        public int MaxDepth { get; set; } = 10;

        /// <summary>
        /// Maximum string length in logs.
        /// </summary>
        public int MaxStringLength { get; set; } = 1024;

        /// <summary>
        /// Maximum number of items in collections.
        /// </summary>
        public int MaxCollectionCount { get; set; } = 10;

        /// <summary>
        /// Whether to destructure collections as objects (instead of as collections).
        /// </summary>
        public bool DestructureByInterfaceAsObject { get; set; } = false;
    }
}

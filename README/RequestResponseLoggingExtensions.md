# RequestResponseLoggingExtensions

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-Extensions-blue.svg)](https://docs.microsoft.com/en-us/aspnet/core/)
[![Serilog](https://img.shields.io/badge/Serilog-Integrated-green.svg)](https://serilog.net/)

Extension methods for seamless integration of HTTP request/response logging middleware with full Serilog configuration and appsettings.json support. These extensions provide a simple, declarative way to enable comprehensive HTTP traffic logging in ASP.NET Core applications.

## 📋 Overview

The `RequestResponseLoggingExtensions` class provides two main extension methods that simplify the integration of detailed HTTP logging:

- **`AddRequestResponseLogging`** - Registers logging services and configuration
- **`UseRequestResponseLogging`** - Adds the middleware to the HTTP pipeline

Both methods support full configuration via `appsettings.json` with intelligent defaults and backward compatibility.

## 🚀 Quick Start

### Basic Integration

```csharp
using SeriTrace.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 1. Register services
builder.Services.AddRequestResponseLogging(builder.Configuration);

var app = builder.Build();

// 2. Add middleware (IMPORTANT: Order matters!)
app.UseCorrelationId();                              // FIRST
app.UseRequestResponseLogging(builder.Configuration); // SECOND
app.UseSerilogRequestLogging();                      // THIRD

app.UseRouting();
app.MapControllers();
app.Run();
```

### Minimal Configuration

Add this to your `appsettings.json`:

```json
{
  "RequestResponseLogging": {
    "Requests": {
      "Enabled": true,
      "LogPath": "logs/requests/requests-.log"
    },
    "Responses": {
      "Enabled": true,
      "LogPath": "logs/responses/responses-.log"
    }
  }
}
```

## 📚 API Reference

### AddRequestResponseLogging(IServiceCollection, IConfiguration)

Registers request/response logging services with configuration from appsettings.json.

**Signature:**
```csharp
public static IServiceCollection AddRequestResponseLogging(
    this IServiceCollection services, 
    IConfiguration configuration)
```

**Parameters:**
- `services` - The service collection to add services to
- `configuration` - Application configuration containing logging settings

**Returns:**
- `IServiceCollection` - The service collection for fluent API usage

**What it does:**
1. **Registers Options** - Binds `RequestResponseLoggingOptions` from configuration
2. **Validates Configuration** - Ensures proper settings are loaded
3. **Sets Up DI** - Makes configuration available to middleware

**Example:**
```csharp
var builder = WebApplication.CreateBuilder(args);

// Register with automatic configuration binding
builder.Services.AddRequestResponseLogging(builder.Configuration);

// Configuration is now available via IOptions<RequestResponseLoggingOptions>
```

**Configuration Section:**
The method looks for the `"RequestResponseLogging"` section in your configuration:

```json
{
  "RequestResponseLogging": {
    "Requests": {
      "Enabled": true,
      "LogPath": "logs/requests/requests-.log",
      "MinimumLevel": "Information",
      "RollingInterval": "Day",
      "FileSizeLimitBytes": 536870912,
      "RetainedFileCountLimit": 31,
      "LogRequestBody": true,
      "MaxRequestBodySize": 32768,
      "LogRequestHeaders": true,
      "ExcludedHeaders": ["Authorization", "Cookie"],
      "ExcludedContentTypes": ["image/*", "video/*"],
      "ExcludedPaths": ["/health", "/metrics"]
    },
    "Responses": {
      "Enabled": true,
      "LogPath": "logs/responses/responses-.log",
      "MinimumLevel": "Information",
      "RollingInterval": "Day",
      "FileSizeLimitBytes": 536870912,
      "RetainedFileCountLimit": 31,
      "LogResponseBody": true,
      "MaxResponseBodySize": 32768,
      "LogResponseHeaders": true,
      "ExcludedHeaders": ["Set-Cookie"],
      "ExcludedContentTypes": ["application/octet-stream", "image/*"],
      "ExcludedStatusCodes": [204, 304],
      "ExcludedPaths": ["/health", "/metrics"]
    }
  }
}
```

### UseRequestResponseLogging(IApplicationBuilder, IConfiguration?)

Adds the request/response logging middleware to the HTTP pipeline.

**Signature:**
```csharp
public static IApplicationBuilder UseRequestResponseLogging(
    this IApplicationBuilder app, 
    IConfiguration? configuration = null)
```

**Parameters:**
- `app` - The application builder
- `configuration` - Optional configuration (uses DI if not provided)

**Returns:**
- `IApplicationBuilder` - The application builder for fluent API usage

**What it does:**
1. **Retrieves Configuration** - Gets options from DI or parameter
2. **Provides Defaults** - Uses safe defaults if configuration is missing
3. **Adds Middleware** - Registers `RequestResponseLoggingMiddleware` in pipeline
4. **Backward Compatibility** - Works even without explicit configuration

**Example:**
```csharp
var app = builder.Build();

// Option 1: Use configuration from DI (recommended)
app.UseRequestResponseLogging();

// Option 2: Explicitly pass configuration
app.UseRequestResponseLogging(builder.Configuration);

// Option 3: Works without any configuration (uses defaults)
app.UseRequestResponseLogging();
```

**⚠️ Critical Pipeline Order:**
```csharp
// ✅ Correct Order
app.UseCorrelationId();                    // 1. FIRST - Correlation context
app.UseRequestResponseLogging();           // 2. SECOND - HTTP logging
app.UseSerilogRequestLogging();           // 3. THIRD - Serilog HTTP logging
app.UseAuthentication();                  // 4. Authentication
app.UseRouting();                         // 5. Routing

// ❌ Incorrect Order - Won't work properly
app.UseRouting();                         // ❌ Too early
app.UseRequestResponseLogging();           // ❌ After routing
app.UseCorrelationId();                   // ❌ Too late
```

## 🔧 Configuration Examples

### Development Configuration

**appsettings.Development.json:**
```json
{
  "RequestResponseLogging": {
    "Requests": {
      "Enabled": true,
      "LogPath": "logs/dev/requests/requests-.log",
      "MinimumLevel": "Debug",
      "LogRequestBody": true,
      "LogRequestHeaders": true,
      "MaxRequestBodySize": 65536,
      "ExcludedPaths": [
        "/swagger",
        "/favicon.ico"
      ]
    },
    "Responses": {
      "Enabled": true,
      "LogPath": "logs/dev/responses/responses-.log", 
      "MinimumLevel": "Debug",
      "LogResponseBody": true,
      "LogResponseHeaders": true,
      "MaxResponseBodySize": 65536
    }
  }
}
```

### Production Configuration

**appsettings.Production.json:**
```json
{
  "RequestResponseLogging": {
    "Requests": {
      "Enabled": true,
      "LogPath": "/var/log/myapp/requests/requests-.log",
      "MinimumLevel": "Warning",
      "LogRequestBody": false,
      "LogRequestHeaders": false,
      "FileSizeLimitBytes": 1073741824,
      "RetainedFileCountLimit": 90,
      "ExcludedPaths": [
        "/health",
        "/metrics",
        "/swagger",
        "/favicon.ico",
        "/robots.txt",
        "/_next/static"
      ]
    },
    "Responses": {
      "Enabled": true,
      "LogPath": "/var/log/myapp/responses/responses-.log",
      "MinimumLevel": "Error",
      "LogResponseBody": false,
      "LogResponseHeaders": false,
      "ExcludedStatusCodes": [200, 204, 304]
    }
  }
}
```

### Microservice Configuration

**appsettings.json for microservice:**
```json
{
  "RequestResponseLogging": {
    "Requests": {
      "Enabled": true,
      "LogPath": "logs/service-name/requests/requests-.log",
      "LogRequestBody": true,
      "MaxRequestBodySize": 16384,
      "ExcludedHeaders": [
        "Authorization",
        "X-API-Key",
        "X-Service-Token"
      ],
      "ExcludedPaths": [
        "/health",
        "/ready",
        "/metrics"
      ]
    },
    "Responses": {
      "Enabled": true,
      "LogPath": "logs/service-name/responses/responses-.log",
      "LogResponseBody": true,
      "MaxResponseBodySize": 16384,
      "ExcludedHeaders": [
        "X-Internal-Token"
      ]
    }
  }
}
```

## 🏗️ Integration Patterns

### Complete SeriTrace Setup

```csharp
using SeriTrace.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Serilog with correlation support
builder.Services.AddSerilogWithConfiguration(builder.Configuration, builder.Environment);

// 2. Register correlation ID services
builder.Services.AddCorrelationIdServices();

// 3. Register request/response logging
builder.Services.AddRequestResponseLogging(builder.Configuration);

// 4. Add your business services
builder.Services.AddControllers();
builder.Services.AddScoped<IWeatherService, WeatherService>();

var app = builder.Build();

// CRITICAL: Middleware order is essential
app.UseCorrelationId();                              // 1. FIRST - Correlation management
app.UseRequestResponseLogging(builder.Configuration); // 2. SECOND - Detailed HTTP logging
app.UseSerilogRequestLogging(builder.Configuration);  // 3. THIRD - Serilog HTTP summary

// Standard middleware
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### Conditional Registration

```csharp
public void ConfigureServices(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
{
    // Always register Serilog and correlation
    services.AddSerilogWithConfiguration(configuration, environment);
    services.AddCorrelationIdServices();
    
    // Conditionally register detailed HTTP logging
    if (environment.IsDevelopment() || configuration.GetValue<bool>("EnableDetailedLogging"))
    {
        services.AddRequestResponseLogging(configuration);
    }
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseCorrelationId();
    
    // Conditionally use detailed logging
    if (env.IsDevelopment() || configuration.GetValue<bool>("EnableDetailedLogging"))
    {
        app.UseRequestResponseLogging();
    }
    
    app.UseSerilogRequestLogging();
    app.UseRouting();
    app.MapControllers();
}
```

### Multi-Environment Registration

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Base configuration
        services.AddSerilogWithConfiguration(Configuration, Environment);
        services.AddCorrelationIdServices();
        
        // Environment-specific logging
        switch (Environment.EnvironmentName.ToLowerInvariant())
        {
            case "development":
                services.AddRequestResponseLogging(Configuration);
                break;
                
            case "staging":
                services.AddRequestResponseLogging(Configuration);
                break;
                
            case "production":
                // Only enable if explicitly configured
                if (Configuration.GetValue<bool>("RequestResponseLogging:Enabled"))
                {
                    services.AddRequestResponseLogging(Configuration);
                }
                break;
        }
    }
}
```

## 🎯 Advanced Usage Examples

### Custom Configuration Provider

```csharp
public class CustomLoggingConfiguration
{
    public static RequestResponseLoggingOptions CreateOptions(IWebHostEnvironment environment)
    {
        return new RequestResponseLoggingOptions
        {
            Requests = new RequestLoggingOptions
            {
                Enabled = true,
                LogPath = $"logs/{environment.EnvironmentName.ToLower()}/requests/requests-.log",
                MinimumLevel = environment.IsDevelopment() ? "Debug" : "Information",
                LogRequestBody = environment.IsDevelopment(),
                LogRequestHeaders = environment.IsDevelopment(),
                MaxRequestBodySize = environment.IsDevelopment() ? 65536 : 8192,
                ExcludedPaths = GetExcludedPaths(environment)
            },
            Responses = new ResponseLoggingOptions
            {
                Enabled = true,
                LogPath = $"logs/{environment.EnvironmentName.ToLower()}/responses/responses-.log",
                MinimumLevel = environment.IsDevelopment() ? "Debug" : "Information",
                LogResponseBody = environment.IsDevelopment(),
                LogResponseHeaders = environment.IsDevelopment(),
                MaxResponseBodySize = environment.IsDevelopment() ? 65536 : 8192
            }
        };
    }

    private static List<string> GetExcludedPaths(IWebHostEnvironment environment)
    {
        var basePaths = new List<string> { "/health", "/metrics" };
        
        if (environment.IsDevelopment())
        {
            basePaths.AddRange(new[] { "/swagger", "/swagger-resources" });
        }
        
        return basePaths;
    }
}

// Usage
services.Configure<RequestResponseLoggingOptions>(options =>
{
    var customOptions = CustomLoggingConfiguration.CreateOptions(environment);
    options.Requests = customOptions.Requests;
    options.Responses = customOptions.Responses;
});
```

### Feature Flag Integration

```csharp
public static class FeatureFlagLoggingExtensions
{
    public static IServiceCollection AddConditionalRequestResponseLogging(
        this IServiceCollection services,
        IConfiguration configuration,
        string featureFlagKey = "Features:DetailedHttpLogging")
    {
        if (configuration.GetValue<bool>(featureFlagKey, false))
        {
            services.AddRequestResponseLogging(configuration);
        }
        
        return services;
    }
    
    public static IApplicationBuilder UseConditionalRequestResponseLogging(
        this IApplicationBuilder app,
        IConfiguration configuration,
        string featureFlagKey = "Features:DetailedHttpLogging")
    {
        if (configuration.GetValue<bool>(featureFlagKey, false))
        {
            app.UseRequestResponseLogging(configuration);
        }
        
        return app;
    }
}

// Usage
builder.Services.AddConditionalRequestResponseLogging(builder.Configuration);
app.UseConditionalRequestResponseLogging(builder.Configuration);
```

### Health Check Integration

```csharp
public class RequestResponseLoggingHealthCheck : IHealthCheck
{
    private readonly RequestResponseLoggingOptions _options;
    
    public RequestResponseLoggingHealthCheck(IOptions<RequestResponseLoggingOptions> options)
    {
        _options = options.Value;
    }
    
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var checks = new List<string>();
        
        // Check if log directories exist and are writable
        if (_options.Requests.Enabled)
        {
            var requestDir = Path.GetDirectoryName(_options.Requests.LogPath);
            if (!Directory.Exists(requestDir))
            {
                checks.Add($"Request log directory does not exist: {requestDir}");
            }
        }
        
        if (_options.Responses.Enabled)
        {
            var responseDir = Path.GetDirectoryName(_options.Responses.LogPath);
            if (!Directory.Exists(responseDir))
            {
                checks.Add($"Response log directory does not exist: {responseDir}");
            }
        }
        
        if (checks.Any())
        {
            return Task.FromResult(HealthCheckResult.Degraded($"Logging issues: {string.Join(", ", checks)}"));
        }
        
        return Task.FromResult(HealthCheckResult.Healthy("Request/Response logging is configured correctly"));
    }
}
```

## 📊 Configuration Schema Reference

### Complete Configuration Schema

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "RequestResponseLogging": {
      "type": "object",
      "properties": {
        "Requests": {
          "type": "object",
          "properties": {
            "Enabled": { "type": "boolean", "default": true },
            "LogPath": { "type": "string", "default": "logs/requests/requests-.log" },
            "MinimumLevel": { 
              "type": "string", 
              "enum": ["Verbose", "Debug", "Information", "Warning", "Error", "Fatal"], 
              "default": "Information" 
            },
            "RollingInterval": { 
              "type": "string", 
              "enum": ["Minute", "Hour", "Day", "Month", "Year", "Infinite"], 
              "default": "Day" 
            },
            "FileSizeLimitBytes": { "type": "integer", "minimum": 1, "default": 536870912 },
            "RetainedFileCountLimit": { "type": "integer", "minimum": 1, "default": 31 },
            "LogRequestBody": { "type": "boolean", "default": true },
            "MaxRequestBodySize": { "type": "integer", "minimum": 0, "default": 32768 },
            "LogRequestHeaders": { "type": "boolean", "default": true },
            "ExcludedHeaders": { 
              "type": "array", 
              "items": { "type": "string" },
              "default": ["Authorization", "Cookie", "Set-Cookie", "X-API-Key"]
            },
            "ExcludedContentTypes": { 
              "type": "array", 
              "items": { "type": "string" },
              "default": ["multipart/form-data", "application/octet-stream", "image/*", "video/*"]
            },
            "ExcludedPaths": { 
              "type": "array", 
              "items": { "type": "string" },
              "default": ["/health", "/metrics", "/swagger"]
            },
            "OutputTemplate": { 
              "type": "string", 
              "default": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{CorrelationId}] REQUEST: {RequestMethod} {RequestPath} {Message:lj} {Properties:j}{NewLine}"
            }
          }
        },
        "Responses": {
          "type": "object",
          "properties": {
            "Enabled": { "type": "boolean", "default": true },
            "LogPath": { "type": "string", "default": "logs/responses/responses-.log" },
            "MinimumLevel": { 
              "type": "string", 
              "enum": ["Verbose", "Debug", "Information", "Warning", "Error", "Fatal"], 
              "default": "Information" 
            },
            "RollingInterval": { 
              "type": "string", 
              "enum": ["Minute", "Hour", "Day", "Month", "Year", "Infinite"], 
              "default": "Day" 
            },
            "FileSizeLimitBytes": { "type": "integer", "minimum": 1, "default": 536870912 },
            "RetainedFileCountLimit": { "type": "integer", "minimum": 1, "default": 31 },
            "LogResponseBody": { "type": "boolean", "default": true },
            "MaxResponseBodySize": { "type": "integer", "minimum": 0, "default": 32768 },
            "LogResponseHeaders": { "type": "boolean", "default": true },
            "ExcludedHeaders": { 
              "type": "array", 
              "items": { "type": "string" },
              "default": ["Set-Cookie", "X-Internal-Token"]
            },
            "ExcludedContentTypes": { 
              "type": "array", 
              "items": { "type": "string" },
              "default": ["application/octet-stream", "image/*", "video/*"]
            },
            "ExcludedStatusCodes": { 
              "type": "array", 
              "items": { "type": "integer" },
              "default": [204, 304]
            },
            "ExcludedPaths": { 
              "type": "array", 
              "items": { "type": "string" },
              "default": ["/health", "/metrics", "/swagger"]
            },
            "OutputTemplate": { 
              "type": "string", 
              "default": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{CorrelationId}] RESPONSE: {StatusCode} {RequestPath} in {Duration}ms {Message:lj} {Properties:j}{NewLine}"
            }
          }
        }
      }
    }
  }
}
```

### Default Configuration Values

```json
{
  "RequestResponseLogging": {
    "Requests": {
      "Enabled": true,
      "LogPath": "logs/requests/requests-.log",
      "MinimumLevel": "Information",
      "RollingInterval": "Day",
      "FileSizeLimitBytes": 536870912,
      "RetainedFileCountLimit": 31,
      "LogRequestBody": true,
      "MaxRequestBodySize": 32768,
      "LogRequestHeaders": true,
      "ExcludedHeaders": [
        "Authorization",
        "Cookie",
        "Set-Cookie",
        "X-API-Key"
      ],
      "ExcludedContentTypes": [
        "multipart/form-data",
        "application/octet-stream",
        "image/*",
        "video/*"
      ],
      "ExcludedPaths": [
        "/health",
        "/metrics",
        "/swagger"
      ]
    },
    "Responses": {
      "Enabled": true,
      "LogPath": "logs/responses/responses-.log",
      "MinimumLevel": "Information",
      "RollingInterval": "Day",
      "FileSizeLimitBytes": 536870912,
      "RetainedFileCountLimit": 31,
      "LogResponseBody": true,
      "MaxResponseBodySize": 32768,
      "LogResponseHeaders": true,
      "ExcludedHeaders": [
        "Set-Cookie",
        "X-Internal-Token"
      ],
      "ExcludedContentTypes": [
        "application/octet-stream",
        "image/*",
        "video/*",
        "audio/*"
      ],
      "ExcludedStatusCodes": [204, 304],
      "ExcludedPaths": [
        "/health",
        "/metrics",
        "/swagger"
      ]
    }
  }
}
```

## 🔍 Troubleshooting

### Common Configuration Issues

#### Services Not Registered

**Problem:** `InvalidOperationException` when using middleware.

**Solution:** Ensure `AddRequestResponseLogging()` is called before `UseRequestResponseLogging()`:

```csharp
// ✅ Correct
builder.Services.AddRequestResponseLogging(builder.Configuration);
// ... build app
app.UseRequestResponseLogging();

// ❌ Missing service registration
app.UseRequestResponseLogging(); // Will throw exception
```

#### Configuration Not Loading

**Problem:** Middleware uses default values instead of configuration.

**Solutions:**
1. Verify `"RequestResponseLogging"` section exists in appsettings.json
2. Check section name spelling (case-sensitive)
3. Ensure configuration is properly bound

```csharp
// Debug configuration loading
var section = builder.Configuration.GetSection("RequestResponseLogging");
if (!section.Exists())
{
    throw new InvalidOperationException("RequestResponseLogging section not found in configuration");
}
```

#### Wrong Middleware Order

**Problem:** Correlation IDs missing from logs or middleware not working.

**Solution:** Ensure proper middleware order:

```csharp
// ✅ Correct order
app.UseCorrelationId();           // FIRST
app.UseRequestResponseLogging();  // SECOND
app.UseSerilogRequestLogging();   // THIRD

// ❌ Wrong order
app.UseRequestResponseLogging();  // Too early - no correlation ID
app.UseCorrelationId();           // Too late
```

### Configuration Validation

Add validation to catch configuration issues early:

```csharp
public static class RequestResponseLoggingValidation
{
    public static IServiceCollection ValidateRequestResponseLogging(this IServiceCollection services, IConfiguration configuration)
    {
        services.PostConfigure<RequestResponseLoggingOptions>(options =>
        {
            if (options.Requests.Enabled)
            {
                ValidateLoggingOptions(options.Requests, "Requests");
            }
            
            if (options.Responses.Enabled)
            {
                ValidateLoggingOptions(options.Responses, "Responses");
            }
        });
        
        return services;
    }
    
    private static void ValidateLoggingOptions(dynamic options, string section)
    {
        if (string.IsNullOrEmpty(options.LogPath))
        {
            throw new InvalidOperationException($"{section}.LogPath cannot be empty");
        }
        
        if (options.MaxRequestBodySize < 0)
        {
            throw new InvalidOperationException($"{section}.MaxRequestBodySize must be non-negative");
        }
    }
}
```

### Debug Configuration

Enable detailed logging to troubleshoot issues:

```json
{
  "Logging": {
    "LogLevel": {
      "SeriTrace.Extensions.RequestResponseLoggingExtensions": "Debug",
      "SeriTrace.Middleware.RequestResponseLoggingMiddleware": "Debug"
    }
  }
}
```

## 🎯 Best Practices

### 1. Environment-Specific Configuration

Use different settings per environment:

```csharp
// Development: Full logging for debugging
// Staging: Moderate logging for testing
// Production: Minimal logging for performance
```

### 2. Service Registration Pattern

Always register services before using middleware:

```csharp
// Services first
builder.Services.AddSerilogWithConfiguration(config, env);
builder.Services.AddCorrelationIdServices();
builder.Services.AddRequestResponseLogging(config);

// Then middleware
app.UseCorrelationId();
app.UseRequestResponseLogging();
```

### 3. Configuration Validation

Validate configuration at startup:

```csharp
builder.Services.PostConfigure<RequestResponseLoggingOptions>(options =>
{
    if (options.Requests.Enabled && string.IsNullOrEmpty(options.Requests.LogPath))
    {
        throw new InvalidOperationException("Request logging enabled but LogPath not configured");
    }
});
```

### 4. Security Considerations

Always exclude sensitive data:

```json
{
  "ExcludedHeaders": [
    "Authorization",
    "Cookie",
    "Set-Cookie",
    "X-API-Key",
    "X-Auth-Token",
    "X-Secret-Key"
  ]
}
```

### 5. Performance Monitoring

Monitor the impact of detailed logging:

```csharp
// Disable body logging for high-traffic endpoints
"ExcludedPaths": [
  "/api/high-traffic-endpoint",
  "/api/bulk-operations"
]
```

## 🚨 Utility Methods

### ParseLogLevel(string)

Converts string log level to `LogEventLevel` enum.

**Signature:**
```csharp
private static LogEventLevel ParseLogLevel(string level)
```

**Supported Values:**
- `"verbose"` → `LogEventLevel.Verbose`
- `"debug"` → `LogEventLevel.Debug`
- `"information"` → `LogEventLevel.Information`
- `"warning"` → `LogEventLevel.Warning`
- `"error"` → `LogEventLevel.Error`
- `"fatal"` → `LogEventLevel.Fatal`
- Other values → `LogEventLevel.Information` (default)

### ParseRollingInterval(string)

Converts string rolling interval to `RollingInterval` enum.

**Signature:**
```csharp
private static RollingInterval ParseRollingInterval(string interval)
```

**Supported Values:**
- `"minute"` → `RollingInterval.Minute`
- `"hour"` → `RollingInterval.Hour`
- `"day"` → `RollingInterval.Day`
- `"month"` → `RollingInterval.Month`
- `"year"` → `RollingInterval.Year`
- `"infinite"` → `RollingInterval.Infinite`
- Other values → `RollingInterval.Day` (default)

## 📄 Dependencies

- **.NET 8.0** - Target framework
- **Microsoft.AspNetCore.Http** - HTTP abstractions
- **Microsoft.Extensions.Configuration** - Configuration system
- **Microsoft.Extensions.DependencyInjection** - Dependency injection
- **Microsoft.Extensions.Options** - Options pattern
- **Serilog** - Structured logging framework
- **Serilog.Events** - Log level enumerations

## 📊 Performance Considerations

### Memory Usage
- Configuration options are singleton instances
- No additional memory allocation during registration
- Middleware uses existing DI configuration

### Registration Overhead
- Minimal impact - single method calls
- Configuration binding is done once at startup
- Default value creation only when needed

### Runtime Performance
- Extension methods have zero runtime overhead
- Configuration access through DI is optimized
- Middleware registration is standard ASP.NET Core pattern

## 📜 License

This component is part of the SeriTrace library and follows the same MIT License.

---

**RequestResponseLoggingExtensions** - Simple, powerful integration for comprehensive HTTP traffic logging in ASP.NET Core applications.
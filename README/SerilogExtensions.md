# SerilogExtensions

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Serilog](https://img.shields.io/badge/Serilog-Integrated-green.svg)](https://serilog.net/)

Comprehensive extension methods for seamless Serilog integration in ASP.NET Core applications with advanced configuration, correlation ID support, and dedicated HTTP request/response logging capabilities. This module provides production-ready logging configuration with environment-aware defaults and performance optimization.

## 📋 Overview

The `SerilogExtensions` class provides a complete set of extension methods for integrating and configuring Serilog in .NET applications:

- **Configuration-based Setup** - Full parameterization via `appsettings.json`
- **Programmatic Configuration** - Dynamic logger configuration in code
- **Environment-Aware** - Different settings for Development, Staging, Production
- **HTTP Request Logging** - Built-in ASP.NET Core request logging integration
- **Dedicated Loggers** - Separate loggers for HTTP requests and responses
- **Performance Optimized** - Async writers and buffering for high-throughput scenarios

## 🚀 Quick Start

### Basic Configuration-Based Setup

```csharp
using SeriTrace.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog from appsettings.json
builder.Services.AddSerilogWithConfiguration(builder.Configuration, builder.Environment);

var app = builder.Build();

// Add Serilog HTTP request logging
app.UseSerilogRequestLogging(builder.Configuration);

app.Run();
```

### Programmatic Configuration

```csharp
using SeriTrace.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog programmatically
builder.Services.AddSerilogWithConfiguration(options =>
{
    options.MinimumLevel = "Information";
    options.Enrich.Add("FromLogContext");
    options.Enrich.Add("WithCorrelationId");
    options.WriteTo.Add(new WriteToConfiguration
    {
        Name = "Console",
        Args = new WriteToArgsConfiguration
        {
            OutputTemplate = "{Timestamp} [{Level}] [{CorrelationId}] {Message}{NewLine}"
        }
    });
}, builder.Environment);

var app = builder.Build();
app.UseSerilogRequestLogging();
app.Run();
```

## 📚 API Reference

### Main Extension Methods

#### AddSerilogWithConfiguration(IServiceCollection, IConfiguration, IHostEnvironment?)

Configures Serilog as the main logging provider based on configuration from `appsettings.json`.

**Signature:**
```csharp
public static IServiceCollection AddSerilogWithConfiguration(
    this IServiceCollection services,
    IConfiguration configuration,
    IHostEnvironment? hostEnvironment = null)
```

**Parameters:**
- `services` - DI service collection
- `configuration` - Application configuration (loads from `appsettings.json`)
- `hostEnvironment` - Optional hosting environment for enriching logs with environment name

**Returns:**
- `IServiceCollection` - Service collection for fluent API usage

**What it does:**
1. **Registers Options** - Adds `SerilogOptions` and `RequestResponseLoggingOptions` to DI
2. **Configures Logger** - Creates Serilog logger from configuration
3. **Dedicated Loggers** - Sets up separate loggers for HTTP requests/responses
4. **Environment Enrichment** - Adds environment-specific properties
5. **Replaces Default** - Makes Serilog the main logging provider

**Example:**
```csharp
var builder = WebApplication.CreateBuilder(args);

// Load configuration from appsettings.json "Serilog" section
builder.Services.AddSerilogWithConfiguration(builder.Configuration, builder.Environment);

// Serilog is now the main logging provider
// All ILogger<T> injections will use Serilog
```

**Required appsettings.json Structure:**
```json
{
  "Serilog": {
    "MinimumLevel": "Information",
    "Enrich": ["FromLogContext", "WithCorrelationId"],
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "OutputTemplate": "{Timestamp} [{Level}] [{CorrelationId}] {Message}{NewLine}"
        }
      }
    ]
  }
}
```

#### AddSerilogWithConfiguration(IServiceCollection, Action<SerilogOptions>, IHostEnvironment?)

Configures Serilog as the main logging provider with programmatic configuration.

**Signature:**
```csharp
public static IServiceCollection AddSerilogWithConfiguration(
    this IServiceCollection services,
    Action<SerilogOptions> configureOptions,
    IHostEnvironment? hostEnvironment = null)
```

**Parameters:**
- `services` - DI service collection
- `configureOptions` - Action to configure Serilog options programmatically
- `hostEnvironment` - Optional hosting environment

**Returns:**
- `IServiceCollection` - Service collection for fluent API usage

**Example:**
```csharp
builder.Services.AddSerilogWithConfiguration(options =>
{
    // Set global minimum level
    options.MinimumLevel = "Information";
    
    // Configure namespace-specific levels
    options.Override["Microsoft"] = "Warning";
    options.Override["System"] = "Warning";
    
    // Add enrichers
    options.Enrich.Add("FromLogContext");
    options.Enrich.Add("WithMachineName");
    options.Enrich.Add("WithCorrelationId");
    
    // Add global properties
    options.Properties["Application"] = "MyApp";
    options.Properties["Version"] = "1.0.0";
    
    // Configure console output
    options.WriteTo.Add(new WriteToConfiguration
    {
        Name = "Console",
        Args = new WriteToArgsConfiguration
        {
            OutputTemplate = "{Timestamp:HH:mm:ss} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}",
            RestrictedToMinimumLevel = "Debug"
        }
    });
    
    // Configure file output
    options.WriteTo.Add(new WriteToConfiguration
    {
        Name = "File",
        Args = new WriteToArgsConfiguration
        {
            Path = "logs/app-.log",
            RollingInterval = "Day",
            FileSizeLimitBytes = 1073741824, // 1GB
            RetainedFileCountLimit = 31,
            OutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"
        }
    });
    
    // Configure destructuring limits
    options.Destructure.MaxDepth = 10;
    options.Destructure.MaxStringLength = 1000;
    options.Destructure.MaxCollectionCount = 10;
    
}, builder.Environment);
```

#### UseSerilogRequestLogging(WebApplication, IConfiguration?)

Configures Serilog HTTP request logging middleware for ASP.NET Core with correlation ID integration.

**Signature:**
```csharp
public static WebApplication UseSerilogRequestLogging(
    this WebApplication app,
    IConfiguration? configuration = null)
```

**Parameters:**
- `app` - WebApplication object
- `configuration` - Optional application configuration

**Returns:**
- `WebApplication` - Application for fluent API usage

**What it does:**
1. **Custom Message Template** - Uses optimized template with timing information
2. **Dynamic Log Levels** - Sets appropriate levels based on response status codes
3. **Context Enrichment** - Adds ClientIP, UserAgent, CorrelationId, UserId
4. **Performance Optimized** - Minimal overhead for high-throughput scenarios

**Example:**
```csharp
var app = builder.Build();

// Add after UseCorrelationId() for correlation ID integration
app.UseCorrelationId();
app.UseSerilogRequestLogging(builder.Configuration);

app.UseRouting();
app.MapControllers();
app.Run();
```

**Log Output Example:**
```
2024-01-15 10:30:45.170 +00:00 [INF] [CID-240115103045-abc123def456] HTTP GET /weather responded 200 in 47.2534 ms
```

**Enriched Properties:**
- `ClientIP` - Remote client IP address
- `UserAgent` - Browser/client information
- `CorrelationId` - Request correlation identifier
- `UserId` - Authenticated user identity (if available)
- `ContentLength` - Response content length

#### AddSerilogAsAdditionalProvider(IServiceCollection, IConfiguration, IHostEnvironment?)

Adds Serilog as an additional logging provider alongside the default one.

**Signature:**
```csharp
public static IServiceCollection AddSerilogAsAdditionalProvider(
    this IServiceCollection services,
    IConfiguration configuration,
    IHostEnvironment? hostEnvironment = null)
```

**Parameters:**
- `services` - DI service collection
- `configuration` - Application configuration
- `hostEnvironment` - Optional hosting environment

**Returns:**
- `IServiceCollection` - Service collection for fluent API usage

**Use Case:**
When you want to keep existing logging infrastructure and add Serilog capabilities without replacing the default provider.

**Example:**
```csharp
var builder = WebApplication.CreateBuilder(args);

// Keep default logging AND add Serilog
builder.Services.AddSerilogAsAdditionalProvider(builder.Configuration, builder.Environment);

// Both ILogger<T> and Serilog.ILogger are available
```

### Logger Creation Methods

#### CreateSerilogLogger(IConfiguration, IHostEnvironment?)

Creates a standalone Serilog logger from configuration without registering it in DI.

**Signature:**
```csharp
public static global::Serilog.ILogger CreateSerilogLogger(
    IConfiguration configuration,
    IHostEnvironment? hostEnvironment = null)
```

**Example:**
```csharp
// Create logger for specific use cases
var logger = SerilogExtensions.CreateSerilogLogger(configuration, environment);

logger.Information("Standalone logger created");

// Don't forget to dispose
logger.Dispose();
```

#### CreateSerilogLogger(Action<SerilogOptions>, IHostEnvironment?)

Creates a standalone Serilog logger with programmatic configuration.

**Signature:**
```csharp
public static global::Serilog.ILogger CreateSerilogLogger(
    Action<SerilogOptions> configureOptions,
    IHostEnvironment? hostEnvironment = null)
```

**Example:**
```csharp
var logger = SerilogExtensions.CreateSerilogLogger(options =>
{
    options.MinimumLevel = "Debug";
    options.WriteTo.Add(new WriteToConfiguration
    {
        Name = "Console",
        Args = new WriteToArgsConfiguration
        {
            OutputTemplate = "{Timestamp} {Message}{NewLine}"
        }
    });
});

logger.Debug("Custom logger message");
logger.Dispose();
```

## 🔧 Configuration Options

### Complete appsettings.json Example

```json
{
  "Serilog": {
    "Using": [
      "Serilog.Sinks.Console",
      "Serilog.Sinks.File",
      "Serilog.Enrichers.Environment"
    ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "Properties": {
      "Application": "SeriTrace.DemoApi",
      "Environment": "Development",
      "Version": "1.0.0"
    },
    "Enrich": [
      "FromLogContext",
      "WithMachineName",
      "WithProcessId",
      "WithThreadId",
      "WithCorrelationId"
    ],
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] {SourceContext} {Message:lj}{NewLine}{Exception}",
          "theme": "Literate",
          "restrictedToMinimumLevel": "Debug"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/app-.log",
          "rollingInterval": "Day",
          "fileSizeLimitBytes": 1073741824,
          "rollOnFileSizeLimit": true,
          "retainedFileCountLimit": 31,
          "shared": false,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] {SourceContext} {Message:lj}{NewLine}{Exception}",
          "restrictedToMinimumLevel": "Information"
        }
      }
    ],
    "Filter": [
      {
        "Name": "ByExcluding",
        "Args": {
          "expression": "RequestPath like '/health%'"
        }
      }
    ],
    "Destructure": {
      "MaxDepth": 10,
      "MaxStringLength": 1024,
      "MaxCollectionCount": 10,
      "DestructureByInterfaceAsObject": false
    }
  },
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
      ],
      "OutputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{CorrelationId}] REQUEST: {RequestMethod} {RequestPath} {Message:lj} {Properties:j}{NewLine}"
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
        "Set-Cookie"
      ],
      "ExcludedContentTypes": [
        "application/octet-stream",
        "image/*",
        "video/*"
      ],
      "ExcludedStatusCodes": [204, 304],
      "ExcludedPaths": [
        "/health",
        "/metrics",
        "/swagger"
      ],
      "OutputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{CorrelationId}] RESPONSE: {StatusCode} {RequestPath} in {Duration}ms {Message:lj} {Properties:j}{NewLine}"
    }
  }
}
```

### Environment-Specific Configurations

#### Development (`appsettings.Development.json`)
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Warning",
        "System": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "{Timestamp:HH:mm:ss.fff} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Properties:j}{NewLine}{Exception}",
          "theme": "Literate"
        }
      },
      {
        "Name": "Debug",
        "Args": {
          "outputTemplate": "{Timestamp:HH:mm:ss.fff} [{Level:u3}] [{CorrelationId}] {SourceContext} {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  },
  "RequestResponseLogging": {
    "Requests": {
      "LogRequestBody": true,
      "LogRequestHeaders": true,
      "MaxRequestBodySize": 65536
    },
    "Responses": {
      "LogResponseBody": true,
      "LogResponseHeaders": true,
      "MaxResponseBodySize": 65536
    }
  }
}
```

#### Production (`appsettings.Production.json`)
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Warning",
      "Override": {
        "MyApp": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "/var/log/myapp/app-.log",
          "rollingInterval": "Day",
          "fileSizeLimitBytes": 2147483648,
          "retainedFileCountLimit": 90,
          "shared": true,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] {SourceContext} {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  },
  "RequestResponseLogging": {
    "Requests": {
      "LogRequestBody": false,
      "LogRequestHeaders": false,
      "MinimumLevel": "Warning"
    },
    "Responses": {
      "LogResponseBody": false,
      "LogResponseHeaders": false,
      "MinimumLevel": "Warning"
    }
  }
}
```

## 🏭 Advanced Usage Examples

### Complete Production Setup

```csharp
using SeriTrace.Extensions;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with advanced options
builder.Services.AddSerilogWithConfiguration(builder.Configuration, builder.Environment);

// Register other services
builder.Services.AddCorrelationIdServices();
builder.Services.AddRequestResponseLogging(builder.Configuration);
builder.Services.AddControllers();

var app = builder.Build();

// Configure middleware pipeline (ORDER IS CRITICAL!)
app.UseCorrelationId();                              // 1. FIRST - Correlation ID management
app.UseRequestResponseLogging(builder.Configuration); // 2. SECOND - Detailed HTTP logging
app.UseSerilogRequestLogging(builder.Configuration);  // 3. THIRD - Serilog HTTP logging

// Add standard middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### Custom Sink Integration

```csharp
builder.Services.AddSerilogWithConfiguration(options =>
{
    options.MinimumLevel = "Information";
    options.Enrich.Add("FromLogContext");
    options.Enrich.Add("WithCorrelationId");
    
    // Console sink for development
    options.WriteTo.Add(new WriteToConfiguration
    {
        Name = "Console",
        Args = new WriteToArgsConfiguration
        {
            OutputTemplate = "{Timestamp:HH:mm:ss} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}",
            RestrictedToMinimumLevel = "Debug"
        }
    });
    
    // File sink for persistent logging
    options.WriteTo.Add(new WriteToConfiguration
    {
        Name = "File",
        Args = new WriteToArgsConfiguration
        {
            Path = "logs/app-.log",
            RollingInterval = "Day",
            FileSizeLimitBytes = 1073741824,
            RetainedFileCountLimit = 31,
            OutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"
        }
    });
    
}, builder.Environment);
```

### Logger Factory Usage

For scenarios where you need multiple specialized loggers:

```csharp
public class LoggerFactory
{
    public static ILogger CreateAuditLogger(IConfiguration configuration)
    {
        return SerilogExtensions.CreateSerilogLogger(options =>
        {
            options.MinimumLevel = "Information";
            options.WriteTo.Add(new WriteToConfiguration
            {
                Name = "File",
                Args = new WriteToArgsConfiguration
                {
                    Path = "logs/audit-.log",
                    RollingInterval = "Day",
                    OutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [AUDIT] [{CorrelationId}] {Message:lj}{NewLine}"
                }
            });
        });
    }
    
    public static ILogger CreatePerformanceLogger(IConfiguration configuration)
    {
        return SerilogExtensions.CreateSerilogLogger(options =>
        {
            options.MinimumLevel = "Debug";
            options.WriteTo.Add(new WriteToConfiguration
            {
                Name = "File",
                Args = new WriteToArgsConfiguration
                {
                    Path = "logs/performance-.log",
                    RollingInterval = "Hour",
                    OutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [PERF] [{CorrelationId}] {Message:lj} {Properties:j}{NewLine}"
                }
            });
        });
    }
}
```

## 📊 Supported Sinks and Enrichers

### Built-in Sinks

| Sink Name | Description | Configuration Example |
|-----------|-------------|----------------------|
| `Console` | Console output with optional themes | `"Name": "Console", "Args": { "theme": "Literate" }` |
| `File` | File output with rolling capabilities | `"Name": "File", "Args": { "path": "logs/app-.log", "rollingInterval": "Day" }` |
| `Debug` | Debug output window (Visual Studio) | `"Name": "Debug"` |

### Built-in Enrichers

| Enricher Name | Description | Properties Added |
|---------------|-------------|------------------|
| `FromLogContext` | Context properties from `LogContext` | Dynamic properties |
| `WithMachineName` | Machine/server name | `MachineName` |
| `WithProcessId` | Current process ID | `ProcessId` |
| `WithThreadId` | Current thread ID | `ThreadId` |
| `WithEnvironmentUserName` | Environment user name | `EnvironmentUserName` |
| `WithCorrelationId` | HTTP correlation ID | `CorrelationId` |

### Log Level Mappings

| String Value | Serilog Level | Description |
|--------------|---------------|-------------|
| `"Verbose"` | `LogEventLevel.Verbose` | Highest level of detail |
| `"Debug"` | `LogEventLevel.Debug` | Debug information |
| `"Information"` | `LogEventLevel.Information` | General information |
| `"Warning"` | `LogEventLevel.Warning` | Warning conditions |
| `"Error"` | `LogEventLevel.Error` | Error conditions |
| `"Fatal"` | `LogEventLevel.Fatal` | Fatal errors |

## 🔍 Troubleshooting

### Common Issues

#### Configuration Not Loading

**Problem:** Serilog configuration not being applied from `appsettings.json`.

**Solutions:**
1. Verify `appsettings.json` is copied to output directory
2. Check section name is exactly `"Serilog"`
3. Ensure proper JSON syntax and structure
4. Verify `Using` array contains required package names

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    // ... rest of configuration
  }
}
```

#### Logs Not Appearing

**Problem:** No log output despite configuration.

**Solutions:**
1. Check minimum log levels are appropriate
2. Verify sinks are properly configured
3. Ensure file paths are accessible and writable
4. Check namespace-specific overrides aren't too restrictive

#### Performance Issues

**Problem:** Logging impacting application performance.

**Solutions:**
1. Use appropriate minimum log levels for production
2. Configure file size limits and rolling policies
3. Consider async sinks for high-throughput scenarios
4. Use structured logging instead of string concatenation

```csharp
// Good - structured logging
_logger.LogInformation("Processing request for user {UserId} with {RequestCount} items", userId, items.Count);

// Avoid - string concatenation
_logger.LogInformation($"Processing request for user {userId} with {items.Count} items");
```

#### HTTP Request Logs Missing Correlation ID

**Problem:** HTTP request logs don't contain correlation ID.

**Solutions:**
1. Ensure `UseCorrelationId()` is called before `UseSerilogRequestLogging()`
2. Verify `WithCorrelationId` enricher is configured
3. Check middleware order in pipeline

```csharp
// Correct order
app.UseCorrelationId();           // FIRST
app.UseSerilogRequestLogging();   // AFTER correlation ID
```

### Debug Configuration

For troubleshooting configuration issues:

```json
{
  "Serilog": {
    "MinimumLevel": "Verbose",
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "{Timestamp:HH:mm:ss.fff} [{Level:u3}] [{CorrelationId}] {SourceContext} {Message:lj}{NewLine}{Properties:j}{NewLine}{Exception}"
        }
      }
    ],
    "Properties": {
      "DebugMode": true
    }
  }
}
```

## 🎯 Best Practices

### 1. Environment-Specific Configuration

Use different log levels and outputs per environment:

```csharp
// Development: Verbose console logging
// Production: Warning+ file logging with compression
```

### 2. Structured Logging

Always use structured logging with named parameters:

```csharp
// Good
_logger.LogInformation("User {UserId} performed {Action} on {Resource}", userId, action, resource);

// Avoid
_logger.LogInformation($"User {userId} performed {action} on {resource}");
```

### 3. Correlation ID Integration

Always configure correlation ID for distributed tracing:

```csharp
builder.Services.AddCorrelationIdServices();
app.UseCorrelationId();  // FIRST in pipeline
```

### 4. Resource Management

Properly dispose of standalone loggers:

```csharp
var logger = SerilogExtensions.CreateSerilogLogger(options => { ... });
try
{
    // Use logger
}
finally
{
    logger.Dispose();
}
```

### 5. Security Considerations

Exclude sensitive information from logs:

```json
{
  "RequestResponseLogging": {
    "Requests": {
      "ExcludedHeaders": [
        "Authorization",
        "Cookie",
        "X-API-Key",
        "X-Auth-Token"
      ]
    }
  }
}
```

## 📄 Dependencies

- **.NET 8.0** - Target framework
- **Serilog** - Core logging framework
- **Serilog.AspNetCore** - ASP.NET Core integration
- **Serilog.Settings.Configuration** - Configuration-based setup
- **Microsoft.Extensions.Configuration** - Configuration abstractions
- **Microsoft.Extensions.DependencyInjection** - DI integration
- **Microsoft.Extensions.Hosting** - Host environment support

## 🤝 Contributing

Contributions are welcome! Please ensure:

1. All methods have comprehensive XML documentation
2. Include unit tests for new functionality
3. Follow existing code style and patterns
4. Update this README with new examples
5. Maintain backward compatibility

## 📜 License

This component is part of the SeriTrace library and follows the same MIT License.

---

**SerilogExtensions** - Production-ready Serilog integration for ASP.NET Core applications with advanced configuration and correlation ID support.
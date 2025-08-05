# SeriTrace

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Serilog](https://img.shields.io/badge/Serilog-Integrated-green.svg)](https://serilog.net/)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A powerful .NET 8 library that provides comprehensive HTTP request tracing and structured logging capabilities using Serilog. SeriTrace automatically manages correlation IDs, logs HTTP requests/responses, and enriches all logs with correlation context for easy request tracking across distributed systems.

## ✨ Features

### 🔗 Automatic Correlation ID Management
- **Header Reading**: Automatically extracts correlation ID from `X-Correlation-ID` request header
- **Auto-Generation**: Generates unique correlation ID when not provided in request
- **Response Headers**: Automatically adds correlation ID to response headers
- **Context Propagation**: Makes correlation ID available throughout the entire request pipeline

### 📊 Structured Logging with Serilog
- **Automatic Enrichment**: All logs automatically include correlation ID without manual intervention
- **Multiple Sinks**: Support for Console, File, Seq, ElasticSearch, and more
- **Environment-Aware**: Different configurations for Development, Staging, and Production
- **Performance Optimized**: Async logging with buffering and size limits

### 🌐 HTTP Request/Response Logging
- **Dedicated Loggers**: Separate log files for requests (`logs/requests/`) and responses (`logs/responses/`)
- **Complete Context**: Logs include method, path, headers, body, status codes, and processing time
- **Smart Filtering**: Automatically excludes binary content, sensitive headers, and health checks
- **Configurable**: Full control over what gets logged and where

### 📈 Response Metadata Enhancement
- **Automatic Envelope**: Adds metadata to API responses including:
  - Correlation ID for request tracking
  - Request timestamp
  - Processing duration in milliseconds
- **Easy Debugging**: Enables easy tracing of request processing paths and performance analysis

### ⚙️ Flexible Configuration
- **appsettings.json**: Complete configuration through standard .NET configuration
- **Environment Profiles**: Different settings for Development, Staging, Production
- **Runtime Configuration**: Programmatic configuration support
- **Sensible Defaults**: Works out-of-the-box with minimal configuration

## 🚀 Quick Start

### 1. Installation

Add the SeriTrace library to your project:

```bash
dotnet add reference path/to/SeriTrace/SeriTrace.csproj
```

### 2. Basic Setup

Configure SeriTrace in your `Program.cs`:

```csharp
using SeriTrace.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with correlation ID support
builder.Services.AddSerilogWithConfiguration(builder.Configuration, builder.Environment);

// Register correlation ID services
builder.Services.AddCorrelationIdServices();

// Register request/response logging
builder.Services.AddRequestResponseLogging(builder.Configuration);

// Add your other services
builder.Services.AddControllers();

var app = builder.Build();

// IMPORTANT: Add middleware in this order
app.UseCorrelationId();                              // 1. First - manages correlation ID
app.UseRequestResponseLogging(builder.Configuration); // 2. Second - logs HTTP traffic
app.UseSerilogRequestLogging(builder.Configuration);  // 3. Third - Serilog HTTP logging

// Add your other middleware
app.UseRouting();
app.MapControllers();

app.Run();
```

### 3. Minimal Configuration

Add this to your `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": "Information",
    "Enrich": ["FromLogContext", "WithCorrelationId"],
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "OutputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "Path": "logs/app-.log",
          "RollingInterval": "Day",
          "OutputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  },
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

## 🔧 Configuration

### Complete appsettings.json Example

<details>
<summary>Click to expand full configuration example</summary>

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": "Information",
    "Override": {
      "Microsoft": "Warning",
      "System": "Warning",
      "Microsoft.AspNetCore": "Warning"
    },
    "Properties": {
      "Application": "SeriTrace.DemoApi",
      "Environment": "Development"
    },
    "Enrich": [
      "FromLogContext",
      "WithMachineName",
      "WithProcessId",
      "WithCorrelationId"
    ],
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "OutputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}",
          "Theme": "Literate"
        }
      },
      {
        "Name": "File",
        "Args": {
          "Path": "logs/app-.log",
          "RollingInterval": "Day",
          "FileSizeLimitBytes": 1073741824,
          "RollOnFileSizeLimit": true,
          "RetainedFileCountLimit": 31,
          "OutputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ],
    "Destructure": {
      "MaxDepth": 10,
      "MaxStringLength": 1024,
      "MaxCollectionCount": 10
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
      ]
    }
  }
}
```
</details>

## 💻 Usage Examples

### Using Correlation ID in Controllers

```csharp
[ApiController]
[Route("[controller]")]
public class WeatherController : ControllerBase
{
    private readonly ILogger<WeatherController> _logger;
    private readonly ICorrelationIdService _correlationIdService;

    public WeatherController(
        ILogger<WeatherController> logger,
        ICorrelationIdService correlationIdService)
    {
        _logger = logger;
        _correlationIdService = correlationIdService;
    }

    [HttpGet]
    public async Task<IActionResult> GetWeather()
    {
        // Get correlation ID for the current request
        var correlationId = _correlationIdService.GetCorrelationId();
        var startTime = _correlationIdService.GetRequestStartTime();
        
        _logger.LogInformation("Processing weather request"); // Automatically includes CorrelationId
        
        // Your business logic here
        var weather = await GetWeatherData();
        
        _logger.LogInformation("Weather request completed successfully");
        
        return Ok(weather);
    }
    
    private async Task<object> GetWeatherData()
    {
        // Simulate some work
        await Task.Delay(100);
        
        _logger.LogDebug("Weather data retrieved from service"); // Also includes CorrelationId
        
        return new { Temperature = 22, Condition = "Sunny" };
    }
}
```

### Using in Services

```csharp
public class WeatherService
{
    private readonly ILogger<WeatherService> _logger;
    private readonly ICorrelationIdService _correlationIdService;

    public WeatherService(
        ILogger<WeatherService> logger,
        ICorrelationIdService correlationIdService)
    {
        _logger = logger;
        _correlationIdService = correlationIdService;
    }

    public async Task<WeatherData> GetWeatherAsync(string location)
    {
        var correlationId = _correlationIdService.GetCorrelationId();
        
        _logger.LogInformation("Fetching weather for location: {Location}", location);
        
        try
        {
            // External API call or database query
            var data = await FetchFromExternalApi(location);
            
            _logger.LogInformation("Successfully retrieved weather data for {Location}", location);
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve weather for {Location}", location);
            throw;
        }
    }
}
```

## 📊 Log Output Examples

### Standard Application Log
```
2024-01-15 10:30:45.123 +00:00 [INF] [abc123-def456-ghi789] Processing weather request
2024-01-15 10:30:45.145 +00:00 [DBG] [abc123-def456-ghi789] Weather data retrieved from service
2024-01-15 10:30:45.167 +00:00 [INF] [abc123-def456-ghi789] Weather request completed successfully
```

### HTTP Request Log (`logs/requests/requests-20240115.log`)
```
[2024-01-15 10:30:45.120 +00:00] [INF] [abc123-def456-ghi789] REQUEST: GET /weather {"Method":"GET","Path":"/weather","QueryString":"","Headers":{"User-Agent":"Mozilla/5.0...","Accept":"application/json"},"ContentLength":0}
```

### HTTP Response Log (`logs/responses/responses-20240115.log`)
```
[2024-01-15 10:30:45.170 +00:00] [INF] [abc123-def456-ghi789] RESPONSE: 200 /weather in 47ms {"StatusCode":200,"Headers":{"Content-Type":"application/json","X-Correlation-ID":"abc123-def456-ghi789"},"ContentLength":156,"Body":"{\"temperature\":22,\"condition\":\"Sunny\"}"}
```

### Serilog HTTP Request Log
```
2024-01-15 10:30:45.170 +00:00 [INF] [abc123-def456-ghi789] HTTP GET /weather responded 200 in 47.2534 ms
```

## 🏗️ Architecture

### Middleware Pipeline Order

The order of middleware registration is crucial for proper functionality:

```csharp
app.UseCorrelationId();                    // 1. FIRST - Extract/generate correlation ID
app.UseRequestResponseLogging();           // 2. SECOND - Log detailed HTTP traffic  
app.UseSerilogRequestLogging();           // 3. THIRD - Serilog's built-in HTTP logging
app.UseAuthentication();                  // 4. Your other middleware...
app.UseAuthorization();
app.UseRouting();
app.MapControllers();
```

### Components Overview

- **CorrelationIdMiddleware**: Manages correlation ID lifecycle
- **RequestResponseLoggingMiddleware**: Logs detailed HTTP traffic
- **CorrelationIdService**: Provides access to correlation context
- **CorrelationIdEnricher**: Enriches Serilog with correlation ID
- **SerilogExtensions**: Configures Serilog with all integrations

## 🔍 Troubleshooting

### Common Issues

#### Correlation ID Not Appearing in Logs
- Ensure `UseCorrelationId()` is called first in the middleware pipeline
- Verify `WithCorrelationId` is included in the `Enrich` array
- Check that `FromLogContext` enricher is also enabled

#### Request/Response Logs Not Generated
- Verify directories exist or can be created (`logs/requests/`, `logs/responses/`)
- Check file permissions for log directories
- Ensure `Enabled: true` in configuration
- Verify middleware order (RequestResponseLogging before routing)

#### Performance Issues
- Disable request/response body logging in production
- Increase `FileSizeLimitBytes` and enable `RollOnFileSizeLimit`
- Use async sinks for high-throughput scenarios
- Consider excluding health check endpoints

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- [Serilog](https://serilog.net/) - The fantastic structured logging library
- [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/) - The web framework that makes this possible
- Microsoft - For the excellent .NET ecosystem

---

**SeriTrace** - Making request tracing and structured logging effortless in .NET applications.

# CorrelationIdExtensions

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Serilog](https://img.shields.io/badge/Serilog-Integrated-green.svg)](https://serilog.net/)

Comprehensive extension methods for easy integration of correlation ID tracking in ASP.NET Core applications with automatic Serilog enrichment. This module provides seamless correlation ID management across HTTP requests and distributed logging systems.

## 📋 Overview

The `CorrelationIdExtensions` namespace contains two main extension classes:

- **`CorrelationIdExtensions`** - Core extension methods for registering correlation ID services and middleware
- **`CorrelationIdEnricherExtensions`** - Serilog-specific extensions for automatic log enrichment

## 🚀 Quick Start

### Basic Setup

```csharp
using SeriTrace.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register correlation ID services
builder.Services.AddCorrelationIdServices();

var app = builder.Build();

// Add correlation ID middleware (MUST BE FIRST!)
app.UseCorrelationId();

app.Run();
```

### With Serilog Integration

```csharp
using SeriTrace.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with correlation ID enrichment
Log.Logger = new LoggerConfiguration()
    .Enrich.WithCorrelationId()  // Automatic correlation ID in all logs
    .WriteTo.Console(outputTemplate: "{Timestamp} [{Level}] [{CorrelationId}] {Message}{NewLine}")
    .CreateLogger();

builder.Services.AddCorrelationIdServices();

var app = builder.Build();
app.UseCorrelationId();
app.Run();
```

## 📚 API Reference

### CorrelationIdExtensions Class

#### AddCorrelationIdServices(IServiceCollection)

Registers correlation ID services in the dependency injection container.

**Signature:**
```csharp
public static IServiceCollection AddCorrelationIdServices(this IServiceCollection services)
```

**Parameters:**
- `services` - The service collection to add services to

**Returns:**
- `IServiceCollection` - The service collection for fluent API usage

**Registered Services:**
- `IHttpContextAccessor` - For accessing current HTTP context
- `ICorrelationIdService` (Scoped) - For retrieving correlation information

**Example:**
```csharp
var builder = WebApplication.CreateBuilder(args);

// Registers IHttpContextAccessor and ICorrelationIdService
builder.Services.AddCorrelationIdServices();

// Now you can inject ICorrelationIdService in your controllers/services
```

**Usage in Controllers:**
```csharp
[ApiController]
public class WeatherController : ControllerBase
{
    private readonly ICorrelationIdService _correlationIdService;
    private readonly ILogger<WeatherController> _logger;

    public WeatherController(
        ICorrelationIdService correlationIdService,
        ILogger<WeatherController> logger)
    {
        _correlationIdService = correlationIdService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetWeather()
    {
        var correlationId = _correlationIdService.GetCorrelationId();
        var startTime = _correlationIdService.GetRequestStartTime();
        
        _logger.LogInformation("Processing weather request for {CorrelationId}", correlationId);
        
        return Ok(new { correlationId, startTime });
    }
}
```

#### UseCorrelationId(IApplicationBuilder)

Adds the CorrelationIdMiddleware to the HTTP request pipeline.

**Signature:**
```csharp
public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
```

**Parameters:**
- `app` - The application builder

**Returns:**
- `IApplicationBuilder` - The application builder for fluent API usage

**⚠️ CRITICAL: Pipeline Order**
This middleware **MUST** be registered first in the pipeline to ensure correlation ID is available for all subsequent middleware.

**Example:**
```csharp
var app = builder.Build();

// MUST BE FIRST - before any other custom middleware
app.UseCorrelationId();

// Other middleware follows
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

**What it does:**
1. **Extracts** correlation ID from `X-Correlation-ID` header if present
2. **Generates** unique correlation ID (format: `CID-YYMMDDHHMMSS-RandomHex`) if missing
3. **Stores** correlation ID and request start time in `HttpContext.Items`
4. **Enriches** JSON response metadata with correlation information
5. **Adds** correlation ID to response headers for backward compatibility
6. **Integrates** with Serilog LogContext for automatic log enrichment

### CorrelationIdEnricherExtensions Class

#### WithCorrelationId(LoggerEnrichmentConfiguration, IHttpContextAccessor?)

Adds correlation ID enricher to Serilog configuration.

**Signature:**
```csharp
public static LoggerConfiguration WithCorrelationId(
    this LoggerEnrichmentConfiguration enrichmentConfiguration,
    IHttpContextAccessor? httpContextAccessor = null)
```

**Parameters:**
- `enrichmentConfiguration` - Serilog enrichment configuration
- `httpContextAccessor` - Optional IHttpContextAccessor (uses default if not provided)

**Returns:**
- `LoggerConfiguration` - Logger configuration for fluent API usage

**Example:**
```csharp
// Basic usage - uses default HttpContextAccessor
Log.Logger = new LoggerConfiguration()
    .Enrich.WithCorrelationId()
    .WriteTo.Console(outputTemplate: "{Timestamp} [{Level}] [{CorrelationId}] {Message}{NewLine}")
    .CreateLogger();

// With custom HttpContextAccessor
var httpContextAccessor = new HttpContextAccessor();
Log.Logger = new LoggerConfiguration()
    .Enrich.WithCorrelationId(httpContextAccessor)
    .WriteTo.File("logs/app.log", 
        outputTemplate: "{Timestamp} [{Level}] [{CorrelationId}] {Message}{NewLine}")
    .CreateLogger();
```

**Log Output Example:**
```
2024-01-15 10:30:45.123 [INF] [CID-240115103045-abc123def456] Processing weather request
2024-01-15 10:30:45.145 [INF] [CID-240115103045-abc123def456] Weather data retrieved successfully
```

#### WithCorrelationIdFromDI(LoggerEnrichmentConfiguration, IServiceProvider)

Adds correlation ID enricher using dependency injection to retrieve IHttpContextAccessor.

**Signature:**
```csharp
public static LoggerConfiguration WithCorrelationIdFromDI(
    this LoggerEnrichmentConfiguration enrichmentConfiguration,
    IServiceProvider serviceProvider)
```

**Parameters:**
- `enrichmentConfiguration` - Serilog enrichment configuration
- `serviceProvider` - Service provider for dependency injection

**Returns:**
- `LoggerConfiguration` - Logger configuration for fluent API usage

**Exceptions:**
- `InvalidOperationException` - Thrown if IHttpContextAccessor is not registered in DI

**Example:**
```csharp
var builder = WebApplication.CreateBuilder(args);

// Register required services
builder.Services.AddHttpContextAccessor();

// Configure Serilog using DI
Log.Logger = new LoggerConfiguration()
    .Enrich.WithCorrelationIdFromDI(builder.Services.BuildServiceProvider())
    .WriteTo.Console()
    .CreateLogger();

builder.Services.AddSerilog();
```

## 🔧 Configuration Examples

### Complete Integration Example

```csharp
using SeriTrace.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with correlation ID enrichment
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithCorrelationId()  // Add correlation ID to all logs
    .WriteTo.Console(outputTemplate: 
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/app-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: 
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

// Register services
builder.Services.AddSerilog();
builder.Services.AddCorrelationIdServices();
builder.Services.AddControllers();

var app = builder.Build();

// Configure middleware pipeline (ORDER IS CRITICAL!)
app.UseCorrelationId();              // 1. FIRST - Correlation ID management
app.UseSerilogRequestLogging();      // 2. SECOND - Request logging with correlation
app.UseRouting();                    // 3. Standard routing
app.MapControllers();                // 4. Controller mapping

app.Run();
```

### appsettings.json Configuration

```json
{
  "Serilog": {
    "MinimumLevel": "Information",
    "Override": {
      "Microsoft": "Warning",
      "System": "Warning"
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
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/app-.log",
          "rollingInterval": "Day",
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  }
}
```

### Business Service Integration

```csharp
public interface IWeatherService
{
    Task<WeatherData> GetWeatherAsync(string location);
}

public class WeatherService : IWeatherService
{
    private readonly ILogger<WeatherService> _logger;
    private readonly ICorrelationIdService _correlationIdService;
    private readonly HttpClient _httpClient;

    public WeatherService(
        ILogger<WeatherService> logger,
        ICorrelationIdService correlationIdService,
        HttpClient httpClient)
    {
        _logger = logger;
        _correlationIdService = correlationIdService;
        _httpClient = httpClient;
    }

    public async Task<WeatherData> GetWeatherAsync(string location)
    {
        var correlationId = _correlationIdService.GetCorrelationId();
        
        _logger.LogInformation("Fetching weather for location: {Location}", location);
        
        try
        {
            // Add correlation ID to outgoing HTTP requests
            _httpClient.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
            
            var response = await _httpClient.GetAsync($"api/weather/{location}");
            var data = await response.Content.ReadFromJsonAsync<WeatherData>();
            
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

## 📊 Response Metadata Integration

The middleware automatically enhances JSON responses with correlation metadata:

### Standard Object Response

**Original Response:**
```json
{
  "temperature": 22,
  "condition": "Sunny"
}
```

**Enhanced Response:**
```json
{
  "temperature": 22,
  "condition": "Sunny",
  "metadata": {
    "correlationId": "CID-240115103045-abc123def456",
    "timestamp": "2024-01-15T10:30:45.167Z",
    "duration-ms": 47
  }
}
```

### Array Response Enhancement

**Original Response:**
```json
[
  { "id": 1, "name": "Item 1" },
  { "id": 2, "name": "Item 2" }
]
```

**Enhanced Response:**
```json
{
  "data": [
    { "id": 1, "name": "Item 1" },
    { "id": 2, "name": "Item 2" }
  ],
  "metadata": {
    "correlationId": "CID-240115103045-abc123def456",
    "timestamp": "2024-01-15T10:30:45.167Z",
    "duration-ms": 23
  }
}
```

## 🔍 Troubleshooting

### Common Issues

#### Correlation ID Not Appearing in Logs

**Problem:** Logs don't contain correlation ID even though middleware is registered.

**Solutions:**
1. Ensure `UseCorrelationId()` is called **first** in the middleware pipeline
2. Verify `WithCorrelationId()` is included in Serilog enrichment configuration
3. Check that output template includes `{CorrelationId}` placeholder
4. Ensure `FromLogContext` enricher is also enabled

```csharp
// Correct order
app.UseCorrelationId();        // FIRST!
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

#### Service Not Available in DI

**Problem:** `InvalidOperationException` when using `WithCorrelationIdFromDI`.

**Solution:** Register `IHttpContextAccessor` before configuring Serilog:

```csharp
builder.Services.AddHttpContextAccessor();  // Add this line
builder.Services.AddCorrelationIdServices();
```

#### Response Metadata Not Added

**Problem:** JSON responses don't contain correlation metadata.

**Solutions:**
1. Ensure responses are JSON format (`application/json`)
2. Verify middleware is registered before routing
3. Check that response is not empty or whitespace-only

### Debug Configuration

Add debug logging to troubleshoot correlation ID issues:

```json
{
  "Serilog": {
    "MinimumLevel": "Debug",
    "Override": {
      "SeriTrace": "Debug"
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "{Timestamp:HH:mm:ss.fff} [{Level:u3}] [{CorrelationId}] {SourceContext} {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  }
}
```

## 🎯 Best Practices

### 1. Middleware Order

Always register correlation ID middleware **first**:

```csharp
app.UseCorrelationId();           // 1. FIRST - Critical!
app.UseRequestResponseLogging();  // 2. After correlation ID
app.UseSerilogRequestLogging();   // 3. After detailed logging
app.UseAuthentication();          // 4. Standard middleware
app.UseAuthorization();
app.UseRouting();
app.MapControllers();
```

### 2. Output Templates

Include correlation ID in all output templates:

```csharp
var template = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}";
```

### 3. HTTP Client Integration

Propagate correlation ID to downstream services:

```csharp
public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly ICorrelationIdService _correlationIdService;

    public async Task<T> CallDownstreamAsync<T>(string endpoint)
    {
        var correlationId = _correlationIdService.GetCorrelationId();
        
        _httpClient.DefaultRequestHeaders.Remove("X-Correlation-ID");
        _httpClient.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
        
        var response = await _httpClient.GetAsync(endpoint);
        return await response.Content.ReadFromJsonAsync<T>();
    }
}
```

### 4. Error Handling

Always include correlation ID in error logs:

```csharp
try
{
    // Business logic
}
catch (Exception ex)
{
    _logger.LogError(ex, "Operation failed for correlation ID: {CorrelationId}", 
        _correlationIdService.GetCorrelationId());
    throw;
}
```

## 📄 Dependencies

- **.NET 8.0** - Target framework
- **Microsoft.AspNetCore.Http.Abstractions** - For HTTP context access
- **Microsoft.Extensions.DependencyInjection.Abstractions** - For DI integration
- **Serilog** - For log enrichment capabilities
- **Serilog.AspNetCore** - For ASP.NET Core integration

## 🤝 Contributing

Contributions are welcome! Please ensure:

1. All methods have comprehensive XML documentation
2. Include unit tests for new functionality
3. Follow existing code style and patterns
4. Update this README with new examples

## 📜 License

This component is part of the SeriTrace library and follows the same MIT License.

---

**CorrelationIdExtensions** - Seamless correlation ID integration for ASP.NET Core applications with automatic Serilog enrichment.
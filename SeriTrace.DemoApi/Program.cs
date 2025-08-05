using SeriTrace.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ✅ Konfiguracja Serilog jako głównego providera logowania
// Ładuje ustawienia z appsettings.json sekcji "Serilog"
// Konfiguruje inteligentne wartości domyślne per środowisko (Development: Debug+Console, Production: Async+File)
// Rejestruje Serilog jako główny provider logowania z enrichers (MachineName, ProcessId, RequestId)
// Dodatkowo konfiguruje dedykowane loggery dla Request/Response logowania
builder.Services.AddSerilogWithConfiguration(builder.Configuration, builder.Environment);

// ✅ Rejestracja serwisów CorrelationId
// Dodaje IHttpContextAccessor i ICorrelationIdService do DI container
// Umożliwia dostęp do CorrelationId w kontrolerach i serwisach biznesowych
builder.Services.AddCorrelationIdServices();

// ✅ Rejestracja serwisów Request/Response logowania
// Dodaje konfigurację dla dedykowanych loggerów requestów i response'ów HTTP
// Konfiguruje osobne pliki logów z pełną parametryzacją przez appsettings.json
builder.Services.AddRequestResponseLogging(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ✅ WAŻNE: CorrelationId middleware na początku pipeline
// Automatycznie pobiera/generuje CorrelationId z nagłówka X-Correlation-ID
// Zapisuje w HttpContext.Items dla dostępu w całej aplikacji
// Wypełnia envelope w odpowiedziach ServiceResponse (correlationId, timestamp, durationMs)
app.UseCorrelationId();

// ✅ Request/Response logging middleware - zaraz po CorrelationId
// Loguje przychodzące requesty HTTP z pełnym kontekstem (metoda, path, headers, body)
// Loguje wychodzące response'y HTTP z timing i kontekstem (status, headers, body, duration)
// Używa dedykowanych plików logów: logs/requests/ i logs/responses/
// Automatycznie wykluczające pliki binarne, wrażliwe nagłówki i health check'i
app.UseRequestResponseLogging(builder.Configuration);

// ✅ Strukturalne logowanie HTTP requests z Serilog
// Automatyczne logowanie wszystkich HTTP requests z timing, status codes, user info
// Template: "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms"
// Enrichers: ClientIP, UserAgent, CorrelationId, UserId (jeśli authenticated)
// UWAGA: Serilog będzie automatycznie używać CorrelationId z middleware powyżej
app.UseSerilogRequestLogging(builder.Configuration);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

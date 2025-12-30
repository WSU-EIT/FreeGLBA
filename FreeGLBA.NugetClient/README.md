# FreeGLBA.Client

A .NET client library for integrating with [FreeGLBA](https://github.com/WSU-EIT/FreeGLBA) - a GLBA Compliance Data Access Tracking System.

## Installation

```bash
dotnet add package FreeGLBA.Client
```

## Quick Start

### Simple Usage

```csharp
using FreeGLBA.Client;

var client = new GlbaClient("https://your-glba-server.com", "your-api-key");

var response = await client.LogAccessAsync(new GlbaEventRequest
{
    AccessedAt = DateTime.UtcNow,
    UserId = "jsmith",
    UserName = "John Smith",
    SubjectId = "S12345678",
    AccessType = "Export",
    Purpose = "Student requested transcript"
});

if (response.Status == "accepted")
{
    Console.WriteLine($"Event logged: {response.EventId}");
}
```

### With Dependency Injection (ASP.NET Core)

```csharp
// Program.cs
builder.Services.AddGlbaClient(options =>
{
    options.Endpoint = "https://your-glba-server.com";
    options.ApiKey = builder.Configuration["GlbaApiKey"];
});

// In your service
public class DataExportService
{
    private readonly IGlbaClient _glbaClient;

    public DataExportService(IGlbaClient glbaClient)
    {
        _glbaClient = glbaClient;
    }

    public async Task<byte[]> ExportStudentDataAsync(string userId, string studentId, string purpose)
    {
        await _glbaClient.LogAccessAsync(new GlbaEventRequest
        {
            AccessedAt = DateTime.UtcNow,
            UserId = userId,
            SubjectId = studentId,
            AccessType = "Export",
            Purpose = purpose
        });

        return await GenerateCsvAsync(studentId);
    }
}
```

### Simplified Methods

```csharp
// Log an export event
await client.LogExportAsync("jsmith", "S12345678", "Annual review");

// Log a view event
await client.LogViewAsync("jsmith", "S12345678");
```

### Try Pattern (No Exceptions)

```csharp
if (await client.TryLogAccessAsync(request))
{
    // Success - proceed with data access
}
else
{
    // Failed - handle accordingly
}
```

### Batch Processing

```csharp
var events = new List<GlbaEventRequest>
{
    new() { UserId = "user1", SubjectId = "S001", AccessType = "View", AccessedAt = DateTime.UtcNow },
    new() { UserId = "user2", SubjectId = "S002", AccessType = "Export", AccessedAt = DateTime.UtcNow }
};

var result = await client.LogAccessBatchAsync(events);
Console.WriteLine($"Accepted: {result.Accepted}, Rejected: {result.Rejected}");
```

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Endpoint` | string | (required) | Base URL of the FreeGLBA server |
| `ApiKey` | string | (required) | API key for authentication |
| `Timeout` | TimeSpan | 30 seconds | HTTP request timeout |
| `RetryCount` | int | 3 | Number of retry attempts for transient failures |
| `ThrowOnError` | bool | true | Whether to throw exceptions on API errors |

## Error Handling

The client throws specific exceptions for different error conditions:

- `GlbaAuthenticationException` - Invalid or expired API key (HTTP 401)
- `GlbaValidationException` - Invalid request data (HTTP 400)
- `GlbaDuplicateException` - Event already exists (HTTP 409)
- `GlbaException` - Base exception for other errors

```csharp
try
{
    await client.LogAccessAsync(request);
}
catch (GlbaAuthenticationException)
{
    // Handle invalid API key
}
catch (GlbaDuplicateException ex)
{
    // Event already logged - may be okay depending on your use case
    Console.WriteLine($"Duplicate event: {ex.EventId}");
}
catch (GlbaException ex)
{
    // Handle other errors
    Console.WriteLine($"Error: {ex.Message}");
}
```

## Requirements

- .NET 8.0, .NET 9.0, .NET 10.0, or .NET Standard 2.1

## License

MIT License - see [LICENSE](LICENSE) for details.

## Links

- [FreeGLBA GitHub Repository](https://github.com/WSU-EIT/FreeGLBA)
- [Report Issues](https://github.com/WSU-EIT/FreeGLBA/issues)

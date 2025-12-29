# FreeGLBA.Client NuGet Package - Project Plan

## Overview

Create a .NET NuGet package that provides a simple, strongly-typed client library for integrating with FreeGLBA's API. Developers can install the package, configure it with their API key and endpoint, and easily log GLBA access events from their applications.

---

## Project Type

**Class Library** targeting multiple .NET versions for maximum compatibility:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net8.0;net9.0;net10.0;netstandard2.1</TargetFrameworks>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    
    <!-- NuGet Package Properties -->
    <PackageId>FreeGLBA.Client</PackageId>
    <Version>1.0.0</Version>
    <Authors>WSU-EIT</Authors>
    <Company>Washington State University</Company>
    <Description>Client library for FreeGLBA - GLBA Compliance Data Access Tracking System</Description>
    <PackageTags>glba;compliance;audit;logging;ferpa;hipaa</PackageTags>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageProjectUrl>https://github.com/WSU-EIT/FreeGLBA</PackageProjectUrl>
    <RepositoryUrl>https://github.com/WSU-EIT/FreeGLBA</RepositoryUrl>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    
    <!-- Enable package generation on build -->
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
  </PropertyGroup>
</Project>
```

**Why these targets?**
- `net8.0` - Current LTS version
- `net9.0` - Latest stable
- `net10.0` - Preview/future
- `netstandard2.1` - Maximum compatibility with older projects

---

## Project Structure

```
FreeGLBA.Client.NuGet/
├── FreeGLBA.Client.NuGet.csproj
├── README.md                          # Package readme (shown on NuGet.org)
├── CHANGELOG.md                       # Version history
├── LICENSE                            # MIT license
│
├── GlbaClient.cs                      # Main client class
├── GlbaClientOptions.cs               # Configuration options
├── IGlbaClient.cs                     # Interface for DI/mocking
│
├── Models/
│   ├── GlbaEventRequest.cs            # Request DTOs
│   ├── GlbaEventResponse.cs           # Response DTOs
│   ├── GlbaBatchResponse.cs
│   └── GlbaException.cs               # Custom exceptions
│
├── Extensions/
│   └── ServiceCollectionExtensions.cs # DI registration helpers
│
└── Internal/
    └── HttpClientFactory.cs           # Internal HTTP handling
```

---

## Phase 1: Project Setup & Core Structure
**Duration: 1-2 hours**

### Task 1.1: Create Class Library Project
- [ ] Create new Class Library project `FreeGLBA.Client.NuGet`
- [ ] Configure multi-targeting (net8.0, net9.0, net10.0, netstandard2.1)
- [ ] Add NuGet package metadata to .csproj
- [ ] Create folder structure

### Task 1.2: Add Dependencies
- [ ] Add `System.Net.Http.Json` (for JSON serialization)
- [ ] Add `Microsoft.Extensions.DependencyInjection.Abstractions` (for DI support)
- [ ] Add `Microsoft.Extensions.Options` (for options pattern)
- [ ] Add `System.Text.Json` (for JSON handling)

### Task 1.3: Create Configuration Classes
- [ ] Create `GlbaClientOptions.cs` with:
  - `Endpoint` (string, required)
  - `ApiKey` (string, required)
  - `Timeout` (TimeSpan, default 30s)
  - `RetryCount` (int, default 3)
  - `ThrowOnError` (bool, default true)

---

## Phase 2: Core Models
**Duration: 1 hour**

### Task 2.1: Create Request Models
- [ ] `GlbaEventRequest` - matches API contract
- [ ] `GlbaBatchRequest` - for batch submissions

### Task 2.2: Create Response Models
- [ ] `GlbaEventResponse` - single event response
- [ ] `GlbaBatchResponse` - batch response with counts
- [ ] `GlbaBatchError` - individual error in batch

### Task 2.3: Create Exception Classes
- [ ] `GlbaException` - base exception
- [ ] `GlbaAuthenticationException` - 401 errors
- [ ] `GlbaValidationException` - 400 errors
- [ ] `GlbaDuplicateException` - 409 conflicts

---

## Phase 3: Client Implementation
**Duration: 2-3 hours**

### Task 3.1: Create Interface
```csharp
public interface IGlbaClient
{
    Task<GlbaEventResponse> LogAccessAsync(GlbaEventRequest request, CancellationToken ct = default);
    Task<GlbaBatchResponse> LogAccessBatchAsync(IEnumerable<GlbaEventRequest> requests, CancellationToken ct = default);
    Task<bool> TryLogAccessAsync(GlbaEventRequest request, CancellationToken ct = default);
}
```

### Task 3.2: Implement GlbaClient
- [ ] Constructor with HttpClient injection
- [ ] Constructor with options for simple usage
- [ ] `LogAccessAsync` - single event, throws on error
- [ ] `LogAccessBatchAsync` - batch events
- [ ] `TryLogAccessAsync` - returns bool, no throw
- [ ] Proper error handling and response parsing
- [ ] Retry logic with exponential backoff

### Task 3.3: Add Convenience Methods
- [ ] `LogExportAsync(userId, subjectId, purpose)` - simplified export logging
- [ ] `LogViewAsync(userId, subjectId)` - simplified view logging
- [ ] Fluent builder pattern for complex requests

---

## Phase 4: Dependency Injection Support
**Duration: 1 hour**

### Task 4.1: Create Extension Methods
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGlbaClient(
        this IServiceCollection services, 
        Action<GlbaClientOptions> configure);
    
    public static IServiceCollection AddGlbaClient(
        this IServiceCollection services,
        string endpoint,
        string apiKey);
}
```

### Task 4.2: Support IHttpClientFactory
- [ ] Register named HttpClient
- [ ] Support for custom HttpClient configuration
- [ ] Polly integration for resilience (optional)

---

## Phase 5: Documentation
**Duration: 1-2 hours**

### Task 5.1: Create README.md
- [ ] Installation instructions
- [ ] Quick start examples
- [ ] Configuration options
- [ ] DI registration examples
- [ ] Error handling guidance

### Task 5.2: XML Documentation
- [ ] Add XML comments to all public types
- [ ] Enable XML documentation generation
- [ ] Include in NuGet package

### Task 5.3: Create CHANGELOG.md
- [ ] Version 1.0.0 initial release notes

---

## Phase 6: Testing
**Duration: 2-3 hours**

### Task 6.1: Create Test Project
- [ ] Create `FreeGLBA.Client.NuGet.Tests` project
- [ ] Add xUnit, Moq, FluentAssertions

### Task 6.2: Unit Tests
- [ ] Test request serialization
- [ ] Test response deserialization
- [ ] Test error handling
- [ ] Test retry logic
- [ ] Test configuration validation

### Task 6.3: Integration Tests (Optional)
- [ ] Test against real FreeGLBA instance
- [ ] Test authentication flow
- [ ] Test batch processing

---

## Phase 7: Publishing
**Duration: 1 hour**

### Task 7.1: Prepare for Publishing
- [ ] Create NuGet.org account (if needed)
- [ ] Generate API key on NuGet.org
- [ ] Verify package metadata
- [ ] Test package locally with `dotnet pack`

### Task 7.2: Publish to NuGet.org
```bash
# Build and pack
dotnet pack -c Release

# Push to NuGet.org
dotnet nuget push bin/Release/FreeGLBA.Client.1.0.0.nupkg \
  --api-key YOUR_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

### Task 7.3: Post-Publish
- [ ] Verify package appears on NuGet.org
- [ ] Test installation in sample project
- [ ] Update FreeGLBA documentation with package reference

---

## Sample Usage (What Consumers Will See)

### Installation
```bash
dotnet add package FreeGLBA.Client
```

### Simple Usage
```csharp
using FreeGLBA.Client;

var client = new GlbaClient("https://glba.em.wsu.edu", "your-api-key");

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
    // Proceed with data export
}
```

### With Dependency Injection (ASP.NET Core)
```csharp
// Program.cs
builder.Services.AddGlbaClient(options =>
{
    options.Endpoint = "https://glba.em.wsu.edu";
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
        // Log access - throws if fails
        await _glbaClient.LogAccessAsync(new GlbaEventRequest
        {
            AccessedAt = DateTime.UtcNow,
            UserId = userId,
            SubjectId = studentId,
            AccessType = "Export",
            Purpose = purpose
        });
        
        // Only reached if logging succeeded
        return await GenerateCsvAsync(studentId);
    }
}
```

### Simplified Methods
```csharp
// Even simpler for common scenarios
await client.LogExportAsync(
    userId: "jsmith",
    subjectId: "S12345678",
    purpose: "Annual review"
);
```

### Try Pattern (No Exceptions)
```csharp
if (await client.TryLogAccessAsync(request))
{
    // Success - proceed with export
}
else
{
    // Failed - deny export, log locally
}
```

---

## Timeline Summary

| Phase | Description | Duration |
|-------|-------------|----------|
| 1 | Project Setup | 1-2 hours |
| 2 | Core Models | 1 hour |
| 3 | Client Implementation | 2-3 hours |
| 4 | DI Support | 1 hour |
| 5 | Documentation | 1-2 hours |
| 6 | Testing | 2-3 hours |
| 7 | Publishing | 1 hour |
| **Total** | | **9-13 hours** |

---

## Future Enhancements (v2.0+)

- [ ] Support for retrieving access events (GET endpoints)
- [ ] Caching for repeated requests
- [ ] Offline queue with automatic retry
- [ ] Telemetry/diagnostics integration
- [ ] Source generators for compile-time validation
- [ ] Health check integration
- [ ] OpenTelemetry tracing support

---

## NuGet Publishing Checklist

Before publishing:
- [ ] Version number updated
- [ ] README.md complete
- [ ] CHANGELOG.md updated
- [ ] All tests passing
- [ ] Package builds without warnings
- [ ] License file included
- [ ] Icon file included (optional but recommended)
- [ ] Package validated with `dotnet pack`
- [ ] Local installation tested

Commands:
```bash
# Pack
dotnet pack -c Release -o ./nupkg

# Test locally
dotnet nuget add source ./nupkg --name local
dotnet add package FreeGLBA.Client --source local

# Publish (when ready)
dotnet nuget push ./nupkg/FreeGLBA.Client.1.0.0.nupkg \
  --api-key $NUGET_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

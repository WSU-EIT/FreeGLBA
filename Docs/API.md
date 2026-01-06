# FreeGLBA API Documentation

FreeGLBA provides a REST API for logging access events to protected financial data (GLBA compliance) and a .NET client library for easy integration.

## Table of Contents

- [Quick Start](#quick-start)
- [Authentication](#authentication)
- [REST API Endpoints](#rest-api-endpoints)
  - [POST /api/glba/events](#post-apiglbaevents)
  - [POST /api/glba/events/batch](#post-apiglbaeventsbatch)
- [.NET Client Library](#net-client-library)
  - [Installation](#installation)
  - [Configuration](#configuration)
  - [Client Methods Reference](#client-methods-reference)
- [Integration Scenarios](#integration-scenarios)
- [Data Models](#data-models)
- [Error Handling](#error-handling)
- [Best Practices](#best-practices)

---

## Quick Start

### Using the REST API (curl)

```bash
curl -X POST https://your-server/FreeGLBA/api/glba/events \
  -H "Authorization: Bearer YOUR_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "jsmith",
    "subjectId": "STU-12345",
    "accessType": "View",
    "purpose": "Reviewing financial aid application"
  }'
```

### Using the .NET Client (NuGet)

```bash
dotnet add package FreeGLBA.Client
```

```csharp
using FreeGLBA.Client;

// Simple instantiation
using var client = new GlbaClient("https://your-server/FreeGLBA", "YOUR_API_KEY");

// Log an access event
await client.LogAccessAsync(new GlbaEventRequest
{
    UserId = "jsmith",
    SubjectId = "STU-12345",
    AccessType = "View",
    Purpose = "Reviewing financial aid application"
});
```

---

## Authentication

All external API requests require authentication via an API key in the `Authorization` header:

```http
Authorization: Bearer YOUR_API_KEY
```

### Obtaining an API Key

1. Log in to your FreeGLBA dashboard at `https://your-server/FreeGLBA`
2. Navigate to **Source Systems**
3. Create a new source system (e.g., "Banner", "Touchpoints", "PowerFAIDS")
4. Click **Generate API Key**
5. **Copy the key immediately** - it will only be shown once!

### API Key Security

> ⚠️ **Important Security Notes:**
> - Store API keys in secure configuration (User Secrets, Azure Key Vault, etc.)
> - Never commit API keys to source control
> - Each source system should have its own unique API key
> - Regenerate keys if they may have been compromised

---

## REST API Endpoints

### POST /api/glba/events

Log a single access event.

**Endpoint:** `POST /api/glba/events`

**Authentication:** API Key (Bearer token)

**Controller:** `GlbaController.PostEvent`

```csharp
// Server-side implementation
[HttpPost("events")]
public async Task<ActionResult<GlbaEventResponse>> PostEvent(
    [FromBody] GlbaEventRequest request)
```

#### Request Headers

| Header | Value | Required |
|--------|-------|----------|
| `Authorization` | `Bearer YOUR_API_KEY` | Yes |
| `Content-Type` | `application/json` | Yes |

#### Request Body

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `sourceEventId` | string | No | Your system's unique event ID (prevents duplicates) |
| `accessedAt` | datetime | No | When access occurred (defaults to server time) |
| `userId` | string | **Yes** | ID of user who accessed the data |
| `userName` | string | No | Display name of the user |
| `userEmail` | string | No | Email address of the user |
| `userDepartment` | string | No | Department (e.g., "Financial Aid", "Bursar") |
| `subjectId` | string | No* | ID of the person whose data was accessed |
| `subjectType` | string | No | Type of subject (e.g., "Student", "Employee") |
| `subjectIds` | string[] | No | Array of subject IDs for bulk operations |
| `dataCategory` | string | No | Category of data (e.g., "Financial", "Academic") |
| `accessType` | string | **Yes** | Type of access: View, Export, Print, Query, etc. |
| `purpose` | string | No | Business justification for the access |
| `ipAddress` | string | No | IP address of the client |
| `additionalData` | string | No | JSON string with custom metadata |
| `agreementText` | string | No | Privacy agreement text shown to user |
| `agreementAcknowledgedAt` | datetime | No | When user acknowledged agreement |

> *`subjectId` is optional. If omitted, defaults to "SYSTEM" for general audit logging.

#### Example Requests

**Standard GLBA Access Event:**
```json
{
  "sourceEventId": "BANNER-2024-001-VIEW",
  "accessedAt": "2024-01-15T10:30:00Z",
  "userId": "jsmith",
  "userName": "John Smith",
  "userEmail": "jsmith@university.edu",
  "userDepartment": "Financial Aid",
  "subjectId": "STU-12345",
  "subjectType": "Student",
  "dataCategory": "Financial Aid",
  "accessType": "View",
  "purpose": "Reviewing FAFSA application for 2024-25 academic year",
  "ipAddress": "192.168.1.100"
}
```

**General Audit Log (no data subject):**
```json
{
  "userId": "admin",
  "accessType": "Config",
  "purpose": "Updated email notification settings"
}
```

**Bulk Export with Privacy Agreement:**
```json
{
  "sourceEventId": "TP-EXPORT-20240115-001",
  "userId": "analyst",
  "userName": "Jane Analyst",
  "subjectId": "BULK",
  "subjectIds": ["STU-001", "STU-002", "STU-003", "STU-004", "STU-005"],
  "subjectType": "Student",
  "dataCategory": "Financial Aid",
  "accessType": "Export",
  "purpose": "Q4 Financial Aid Disbursement Report for Dean's Office",
  "agreementText": "I acknowledge that I am accessing protected financial information under GLBA. This data will only be used for legitimate university business purposes and will not be shared with unauthorized parties.",
  "agreementAcknowledgedAt": "2024-01-15T10:29:45Z"
}
```

#### Response

**Success (201 Created):**
```json
{
  "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "receivedAt": "2024-01-15T10:30:05.123Z",
  "status": "accepted",
  "message": null,
  "subjectCount": 1
}
```

**Duplicate (409 Conflict):**
```json
{
  "eventId": null,
  "receivedAt": "2024-01-15T10:30:05.123Z",
  "status": "duplicate",
  "message": "Event with this SourceEventId already exists",
  "subjectCount": 0
}
```

**Error (400 Bad Request):**
```json
{
  "eventId": null,
  "receivedAt": "2024-01-15T10:30:05.123Z",
  "status": "error",
  "message": "Missing required field: UserId",
  "subjectCount": 0
}
```

---

### POST /api/glba/events/batch

Log multiple access events in a single request.

**Endpoint:** `POST /api/glba/events/batch`

**Authentication:** API Key (Bearer token)

**Maximum:** 1000 events per request

```csharp
// Server-side implementation
[HttpPost("events/batch")]
public async Task<ActionResult<GlbaBatchResponse>> PostBatch(
    [FromBody] List<GlbaEventRequest> events)
```

#### Request Body

Array of event objects (same schema as single event):

```json
[
  {
    "sourceEventId": "BATCH-001",
    "userId": "jsmith",
    "subjectId": "STU-001",
    "accessType": "View",
    "accessedAt": "2024-01-15T09:00:00Z"
  },
  {
    "sourceEventId": "BATCH-002",
    "userId": "jsmith",
    "subjectId": "STU-002",
    "accessType": "View",
    "accessedAt": "2024-01-15T09:01:00Z"
  },
  {
    "sourceEventId": "BATCH-003",
    "userId": "jsmith",
    "subjectId": "STU-003",
    "accessType": "View",
    "accessedAt": "2024-01-15T09:02:00Z"
  }
]
```

#### Response

**Success:**
```json
{
  "accepted": 3,
  "rejected": 0,
  "duplicate": 0,
  "errors": []
}
```

**Partial Success:**
```json
{
  "accepted": 2,
  "rejected": 1,
  "duplicate": 0,
  "errors": [
    {
      "index": 1,
      "error": "Missing required field: UserId"
    }
  ]
}
```

---

## .NET Client Library

### Installation

```bash
# Via .NET CLI
dotnet add package FreeGLBA.Client

# Via Package Manager
Install-Package FreeGLBA.Client
```

### Configuration

#### Option 1: Direct Instantiation (Console Apps, Tests)

```csharp
using FreeGLBA.Client;

// Simple constructor
using var client = new GlbaClient(
    endpoint: "https://your-server/FreeGLBA",
    apiKey: "YOUR_API_KEY"
);

// With options
using var client = new GlbaClient(new GlbaClientOptions
{
    Endpoint = "https://your-server/FreeGLBA",
    ApiKey = "YOUR_API_KEY",
    TimeoutSeconds = 30,
    RetryCount = 3
});
```

#### Option 2: Dependency Injection (ASP.NET Core, Blazor)

```csharp
// In Program.cs
builder.Services.AddGlbaClient(options =>
{
    options.Endpoint = builder.Configuration["FreeGLBA:Endpoint"]!;
    options.ApiKey = builder.Configuration["FreeGLBA:ApiKey"]!;
    options.TimeoutSeconds = 30;
    options.RetryCount = 3;
});

// Or simplified
builder.Services.AddGlbaClient(
    endpoint: builder.Configuration["FreeGLBA:Endpoint"]!,
    apiKey: builder.Configuration["FreeGLBA:ApiKey"]!
);
```

```csharp
// In your service or controller
public class StudentService
{
    private readonly IGlbaClient _glba;

    public StudentService(IGlbaClient glba)
    {
        _glba = glba;
    }

    public async Task ViewStudentFinancialData(string userId, string studentId)
    {
        // Your business logic here...

        // Log the access
        await _glba.LogViewAsync(userId, studentId);
    }
}
```

#### appsettings.json Configuration

```json
{
  "FreeGLBA": {
    "Endpoint": "https://your-server/FreeGLBA",
    "ApiKey": ""  // Store in User Secrets or Azure Key Vault!
  }
}
```

#### User Secrets (Development)

```bash
cd YourProject
dotnet user-secrets init
dotnet user-secrets set "FreeGLBA:ApiKey" "your-actual-api-key-here"
```

### Client Methods Reference

#### LogAccessAsync - Full Control

```csharp
/// <summary>
/// Logs a single GLBA access event with full control over all fields.
/// </summary>
Task<GlbaEventResponse> LogAccessAsync(
    GlbaEventRequest request, 
    CancellationToken cancellationToken = default);
```

**Example:**
```csharp
var response = await client.LogAccessAsync(new GlbaEventRequest
{
    SourceEventId = $"MYAPP-{Guid.NewGuid()}",
    AccessedAt = DateTime.UtcNow,
    UserId = "jsmith",
    UserName = "John Smith",
    UserEmail = "jsmith@university.edu",
    UserDepartment = "Financial Aid",
    SubjectId = "STU-12345",
    SubjectType = "Student",
    DataCategory = "Financial Aid",
    AccessType = "View",
    Purpose = "Processing financial aid application",
    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
});

if (response.IsSuccess)
{
    Console.WriteLine($"Logged event: {response.EventId}");
}
```

#### TryLogAccessAsync - Fire-and-Forget

```csharp
/// <summary>
/// Attempts to log without throwing exceptions.
/// Returns true on success or duplicate; false on error.
/// </summary>
Task<bool> TryLogAccessAsync(
    GlbaEventRequest request, 
    CancellationToken cancellationToken = default);
```

**Example:**
```csharp
// Fire and forget - won't crash your app if logging fails
_ = client.TryLogAccessAsync(new GlbaEventRequest
{
    UserId = userId,
    SubjectId = studentId,
    AccessType = "View"
});
```

#### LogViewAsync - Simple View Logging

```csharp
/// <summary>
/// Logs a data view event with minimal parameters.
/// </summary>
Task<GlbaEventResponse> LogViewAsync(
    string userId,
    string subjectId,
    string? userName = null,
    CancellationToken cancellationToken = default);
```

**Example:**
```csharp
await client.LogViewAsync(
    userId: User.Identity.Name,
    subjectId: studentId,
    userName: User.FindFirst("name")?.Value
);
```

#### LogExportAsync - Simple Export Logging

```csharp
/// <summary>
/// Logs a data export event.
/// </summary>
Task<GlbaEventResponse> LogExportAsync(
    string userId,
    string subjectId,
    string? purpose = null,
    string? userName = null,
    CancellationToken cancellationToken = default);
```

**Example:**
```csharp
await client.LogExportAsync(
    userId: "analyst",
    subjectId: "STU-12345",
    purpose: "Generating financial aid verification letter",
    userName: "Jane Analyst"
);
```

#### LogBulkExportAsync - Bulk Export with Agreement

```csharp
/// <summary>
/// Logs a bulk export affecting multiple subjects.
/// </summary>
Task<GlbaEventResponse> LogBulkExportAsync(
    string userId,
    IEnumerable<string> subjectIds,
    string purpose,
    string? userName = null,
    string? dataCategory = null,
    string? agreementText = null,
    CancellationToken cancellationToken = default);
```

**Example:**
```csharp
var studentIds = await GetStudentsInReport(); // Returns List<string>

var response = await client.LogBulkExportAsync(
    userId: User.Identity.Name,
    subjectIds: studentIds,
    purpose: "Q4 Financial Aid Disbursement Report for Dean's Office",
    userName: User.FindFirst("name")?.Value,
    dataCategory: "Financial Aid",
    agreementText: @"I acknowledge that I am accessing protected financial 
        information under GLBA. This data will only be used for legitimate 
        university business purposes."
);

Console.WriteLine($"Logged export affecting {response.SubjectCount} students");
```

#### LogBulkViewAsync - Bulk View/Query

```csharp
/// <summary>
/// Logs a bulk view/query affecting multiple subjects.
/// </summary>
Task<GlbaEventResponse> LogBulkViewAsync(
    string userId,
    IEnumerable<string> subjectIds,
    string? purpose = null,
    string? userName = null,
    CancellationToken cancellationToken = default);
```

**Example:**
```csharp
// Log when user views search results containing multiple students
var searchResults = await SearchStudents(query);
var studentIds = searchResults.Select(s => s.StudentId).ToList();

await client.LogBulkViewAsync(
    userId: User.Identity.Name,
    subjectIds: studentIds,
    purpose: $"Search query: {query}"
);
```

#### LogAccessBatchAsync - Batch Processing

```csharp
/// <summary>
/// Logs multiple events in a single API call. Maximum 1000 events.
/// </summary>
Task<GlbaBatchResponse> LogAccessBatchAsync(
    IEnumerable<GlbaEventRequest> requests, 
    CancellationToken cancellationToken = default);
```

**Example:**
```csharp
// Process historical logs or migrate data
var events = historicalRecords.Select(r => new GlbaEventRequest
{
    SourceEventId = $"MIGRATE-{r.Id}",
    AccessedAt = r.Timestamp,
    UserId = r.UserId,
    SubjectId = r.StudentId,
    AccessType = r.ActionType,
    Purpose = "Historical data migration"
}).ToList();

// Process in batches of 1000
foreach (var batch in events.Chunk(1000))
{
    var response = await client.LogAccessBatchAsync(batch);
    Console.WriteLine($"Batch: {response.Accepted} accepted, {response.Rejected} rejected");
}
```

---

## Integration Scenarios

### Scenario 1: Banner Self-Service

Log when students/employees view their own financial data.

```csharp
public class BannerSelfServiceMiddleware
{
    private readonly IGlbaClient _glba;

    public async Task InvokeAsync(HttpContext context)
    {
        // After successful page load...
        if (context.Request.Path.StartsWithSegments("/StudentFinancialAid"))
        {
            var userId = context.User.Identity?.Name;
            var studentId = context.User.FindFirst("student_id")?.Value;

            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(studentId))
            {
                _ = _glba.TryLogAccessAsync(new GlbaEventRequest
                {
                    UserId = userId,
                    SubjectId = studentId,
                    AccessType = "View",
                    DataCategory = "Financial Aid",
                    Purpose = "Self-service portal access",
                    IpAddress = context.Connection.RemoteIpAddress?.ToString()
                });
            }
        }
    }
}
```

### Scenario 2: Touchpoints CRM Export

Log when advisors export student lists with financial data.

```csharp
public async Task<IActionResult> ExportStudentList(ExportRequest request)
{
    // Get students for export
    var students = await _studentService.GetStudentsForExport(request.Filters);
    var studentIds = students.Select(s => s.StudentId).ToList();

    // Log the bulk export BEFORE generating the file
    await _glba.LogBulkExportAsync(
        userId: User.Identity.Name!,
        subjectIds: studentIds,
        purpose: $"Export: {request.ReportName} - {request.Filters.Description}",
        userName: User.FindFirst("name")?.Value,
        dataCategory: "Financial Aid",
        agreementText: request.GlbaAcknowledgment
    );

    // Generate and return the export file
    var csv = GenerateCsv(students);
    return File(csv, "text/csv", $"export-{DateTime.Now:yyyyMMdd}.csv");
}
```

### Scenario 3: PowerFAIDS Nightly Batch

Log historical access from batch processing logs.

```csharp
public class PowerFaidsBatchProcessor
{
    private readonly IGlbaClient _glba;

    public async Task ProcessDailyLogs(DateTime date)
    {
        var logs = await LoadPowerFaidsLogs(date);

        var events = logs.Select(log => new GlbaEventRequest
        {
            SourceEventId = $"PFAIDS-{log.TransactionId}",
            AccessedAt = log.Timestamp,
            UserId = log.UserId,
            UserName = log.UserName,
            SubjectId = log.StudentId,
            SubjectType = "Student",
            DataCategory = "Financial Aid",
            AccessType = MapAccessType(log.Action),
            Purpose = log.Description
        }).ToList();

        // Process in batches
        foreach (var batch in events.Chunk(1000))
        {
            var response = await _glba.LogAccessBatchAsync(batch);
            
            _logger.LogInformation(
                "PowerFAIDS batch: {Accepted} accepted, {Duplicate} duplicate, {Rejected} rejected",
                response.Accepted, response.Duplicate, response.Rejected);
        }
    }
}
```

### Scenario 4: Blazor WebAssembly App

Log access from a Blazor WASM client-side app.

```csharp
// Program.cs
builder.Services.AddGlbaClient(options =>
{
    options.Endpoint = builder.Configuration["FreeGLBA:Endpoint"]!;
    options.ApiKey = builder.Configuration["FreeGLBA:ApiKey"]!;
});

// StudentDetail.razor
@inject IGlbaClient Glba
@inject AuthenticationStateProvider AuthState

@code {
    [Parameter] public string StudentId { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthState.GetAuthenticationStateAsync();
        var userId = authState.User.Identity?.Name;

        // Log the view
        _ = Glba.TryLogAccessAsync(new GlbaEventRequest
        {
            UserId = userId ?? "anonymous",
            SubjectId = StudentId,
            AccessType = "View",
            DataCategory = "Student Financial Record"
        });

        // Load student data...
    }
}
```

### Scenario 5: General Audit Logging (No Subject)

Log system events that don't involve specific individuals.

```csharp
// Configuration change
await client.LogAccessAsync(new GlbaEventRequest
{
    UserId = "admin",
    AccessType = "Config",
    Purpose = "Updated SMTP email settings"
});

// Report generation (aggregate data, no individuals)
await client.LogAccessAsync(new GlbaEventRequest
{
    UserId = "scheduler",
    AccessType = "Report",
    Purpose = "Generated monthly aggregate statistics report"
});

// Batch job completion
await client.LogAccessAsync(new GlbaEventRequest
{
    UserId = "SYSTEM",
    AccessType = "Execute",
    Purpose = "Nightly data sync completed - processed 15,234 records"
});
```

---

## Data Models

### GlbaEventRequest

```csharp
public class GlbaEventRequest
{
    /// <summary>Your system's unique event ID for deduplication.</summary>
    public string? SourceEventId { get; set; }
    
    /// <summary>When the access occurred. Defaults to now.</summary>
    public DateTime AccessedAt { get; set; }
    
    /// <summary>ID of the user who accessed the data. REQUIRED.</summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>Display name of the user.</summary>
    public string? UserName { get; set; }
    
    /// <summary>Email address of the user.</summary>
    public string? UserEmail { get; set; }
    
    /// <summary>Department of the user.</summary>
    public string? UserDepartment { get; set; }
    
    /// <summary>ID of the data subject. Optional - defaults to "SYSTEM".</summary>
    public string SubjectId { get; set; } = string.Empty;
    
    /// <summary>Type of subject (Student, Employee, etc.).</summary>
    public string? SubjectType { get; set; }
    
    /// <summary>List of subject IDs for bulk operations.</summary>
    public List<string>? SubjectIds { get; set; }
    
    /// <summary>Category of data accessed.</summary>
    public string? DataCategory { get; set; }
    
    /// <summary>Type of access (View, Export, etc.). REQUIRED.</summary>
    public string AccessType { get; set; } = string.Empty;
    
    /// <summary>Business purpose/justification.</summary>
    public string? Purpose { get; set; }
    
    /// <summary>Client IP address.</summary>
    public string? IpAddress { get; set; }
    
    /// <summary>Custom JSON metadata.</summary>
    public string? AdditionalData { get; set; }
    
    /// <summary>Privacy agreement text shown to user.</summary>
    public string? AgreementText { get; set; }
    
    /// <summary>When user acknowledged the agreement.</summary>
    public DateTime? AgreementAcknowledgedAt { get; set; }
}
```

### GlbaEventResponse

```csharp
public class GlbaEventResponse
{
    /// <summary>Server-assigned event ID.</summary>
    public Guid? EventId { get; set; }
    
    /// <summary>When the server received the event.</summary>
    public DateTime ReceivedAt { get; set; }
    
    /// <summary>Status: "accepted", "duplicate", or "error".</summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>Error message if status is "error".</summary>
    public string? Message { get; set; }
    
    /// <summary>Number of data subjects affected.</summary>
    public int SubjectCount { get; set; }
    
    // Convenience properties
    public bool IsSuccess => Status == "accepted";
    public bool IsDuplicate => Status == "duplicate";
    public bool IsError => Status == "error";
}
```

### GlbaBatchResponse

```csharp
public class GlbaBatchResponse
{
    /// <summary>Number of events successfully recorded.</summary>
    public int Accepted { get; set; }
    
    /// <summary>Number of events that failed validation.</summary>
    public int Rejected { get; set; }
    
    /// <summary>Number of duplicate events (already existed).</summary>
    public int Duplicate { get; set; }
    
    /// <summary>Details of rejected events.</summary>
    public List<GlbaBatchError> Errors { get; set; } = new();
}

public class GlbaBatchError
{
    /// <summary>Index of the failed event in the batch (0-based).</summary>
    public int Index { get; set; }
    
    /// <summary>Error message.</summary>
    public string Error { get; set; } = string.Empty;
}
```

### GlbaClientOptions

```csharp
public class GlbaClientOptions
{
    /// <summary>Base URL of the FreeGLBA server. REQUIRED.</summary>
    public string Endpoint { get; set; } = string.Empty;
    
    /// <summary>API key for authentication. REQUIRED.</summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>Request timeout in seconds. Default: 30.</summary>
    public int TimeoutSeconds { get; set; } = 30;
    
    /// <summary>Number of retry attempts for transient failures. Default: 3.</summary>
    public int RetryCount { get; set; } = 3;
}
```

---

## Error Handling

### HTTP Status Codes

| Code | Description | Action |
|------|-------------|--------|
| 201 | Created - Event recorded successfully | Success |
| 400 | Bad Request - Validation error | Check request fields |
| 401 | Unauthorized - Invalid API key | Verify API key |
| 409 | Conflict - Duplicate event | Already recorded (success) |
| 500 | Server Error - Internal error | Retry or contact admin |

### .NET Client Exceptions

```csharp
try
{
    var response = await client.LogAccessAsync(request);
}
catch (GlbaAuthenticationException ex)
{
    // API key is invalid or missing
    _logger.LogError("GLBA auth failed: {Message}", ex.Message);
}
catch (GlbaValidationException ex)
{
    // Request data is invalid
    _logger.LogWarning("GLBA validation error: {Message}", ex.Message);
}
catch (GlbaDuplicateException ex)
{
    // Event already exists (usually safe to ignore)
    _logger.LogDebug("GLBA duplicate event: {EventId}", ex.ExistingEventId);
}
catch (GlbaBatchTooLargeException)
{
    // Batch exceeds 1000 events
    _logger.LogWarning("GLBA batch too large - split into smaller batches");
}
catch (GlbaException ex)
{
    // Other API errors
    _logger.LogError("GLBA error: {Message}", ex.Message);
}
```

### Safe Logging Pattern

For non-critical logging where failures shouldn't crash your app:

```csharp
// Option 1: TryLogAccessAsync (recommended)
var success = await client.TryLogAccessAsync(request);
if (!success)
{
    _logger.LogWarning("Failed to log GLBA event");
}

// Option 2: Fire-and-forget with error handling
_ = Task.Run(async () =>
{
    try
    {
        await client.LogAccessAsync(request);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Failed to log GLBA event");
    }
});
```

---

## Best Practices

### 1. Always Use Source Event IDs

Prevents duplicate records if requests are retried:

```csharp
await client.LogAccessAsync(new GlbaEventRequest
{
    SourceEventId = $"{AppName}-{TransactionId}-{Action}",
    // ...
});
```

### 2. Include Purpose for Compliance

GLBA requires documenting the business reason:

```csharp
Purpose = "Processing 2024-25 financial aid application - reviewing EFC"
// NOT: "Looked at student"
```

### 3. Track Privacy Agreements

Document what users acknowledged:

```csharp
AgreementText = "I acknowledge that this data is protected under GLBA...",
AgreementAcknowledgedAt = DateTime.UtcNow
```

### 4. Use Appropriate Access Types

| Type | Use Case |
|------|----------|
| `View` | Viewing data on screen |
| `Export` | Downloading/exporting data |
| `Print` | Printing data |
| `Query` | Search/report that shows results |
| `Config` | System configuration changes |
| `Execute` | Batch job execution |

### 5. Handle Bulk Operations Correctly

Always use `SubjectIds` for operations affecting multiple people:

```csharp
// CORRECT - tracks each subject individually
await client.LogBulkExportAsync(
    userId: user,
    subjectIds: studentIds,  // List of all affected students
    purpose: "Export report"
);

// WRONG - doesn't track individual subjects
await client.LogAccessAsync(new GlbaEventRequest
{
    UserId = user,
    SubjectId = $"Report containing {studentIds.Count} students",  // Bad!
    AccessType = "Export"
});
```

### 6. Configure Retries for Resilience

```csharp
services.AddGlbaClient(options =>
{
    options.Endpoint = endpoint;
    options.ApiKey = apiKey;
    options.TimeoutSeconds = 30;
    options.RetryCount = 3;  // Exponential backoff: 1s, 2s, 4s
});
```

---

## Rate Limits & Performance

- **No enforced rate limits** currently
- **Batch API recommended** for high-volume scenarios (up to 1000 events/request)
- **Connection pooling** handled automatically by HttpClient
- **Retry with exponential backoff** built into the client

---

## Dashboard URLs

After logging events, view them in the FreeGLBA dashboard:

| Page | URL | Description |
|------|-----|-------------|
| Dashboard | `/FreeGLBA/GlbaDashboard` | Overview and statistics |
| Access Events | `/FreeGLBA/AccessEvents` | All logged events |
| Event Detail | `/FreeGLBA/AccessEvents/{eventId}` | Single event details |
| Data Subjects | `/FreeGLBA/DataSubjects` | All tracked individuals |
| Subject Detail | `/FreeGLBA/DataSubjects/{subjectId}` | Individual's access history |
| Source Systems | `/FreeGLBA/SourceSystems` | API key management |

---

## Support

- **Source Code**: [Azure DevOps](https://wsueit.visualstudio.com/FreeGLBA)
- **NuGet Package**: `FreeGLBA.Client`
- **Contact**: Your system administrator

---

*Documentation Version: 1.0 | Last Updated: January 2025*

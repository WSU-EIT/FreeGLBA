# 120 — Implementation Plan: Phase 2 - Logging Attribute & DataAccess

> **Document ID:** 120  
> **Category:** Implementation Plan  
> **Purpose:** Implement core logging logic in attribute and data access layer  
> **Phase:** 2 of 6  
> **Estimated Time:** 1.5 days  
> **Dependencies:** Phase 1 complete (entities and DTOs exist)

---

## Phase Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    PHASE 2: LOGGING ATTRIBUTE & DATA ACCESS                 │
└─────────────────────────────────────────────────────────────────────────────┘

  THIS IS THE CORE ENGINE
  ════════════════════════
  
         REQUEST                                           DATABASE
            │                                                  ▲
            ▼                                                  │
  ┌─────────────────┐                              ┌─────────────────┐
  │ OnActionExec    │                              │ DataAccess      │
  │ uting           │──────────────────────────────│ .CreateLogAsync │
  │                 │          Fire &              │                 │
  │ • Start timer   │          Forget              │ • Insert log    │
  │ • Capture req   │─────────────────────────────►│ • Handle errors │
  │ • Check body    │                              │                 │
  └─────────────────┘                              └─────────────────┘
            │                                                  
            ▼                                                  
  ┌─────────────────┐                              
  │ Controller      │                              
  │ Action          │                              
  │ Executes        │                              
  └─────────────────┘                              
            │                                      
            ▼                                      
  ┌─────────────────┐                              
  │ OnActionExec    │                              
  │ uted            │                              
  │                 │                              
  │ • Stop timer    │                              
  │ • Capture resp  │                              
  │ • Build log     │                              
  │ • Fire & forget │                              
  └─────────────────┘                              

  NOTE: Database cleanup is handled externally (see Task 2.7)
```

---

## Task 2.1: Implement ApiRequestLoggingAttribute

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 2.1: ApiRequestLoggingAttribute                                       │
│  ════════════════════════════════════                                       │
│                                                                             │
│           OnActionExecuting                    OnActionExecuted             │
│           ═════════════════                    ════════════════             │
│                                                                             │
│        ┌─────────────────────┐            ┌─────────────────────┐           │
│        │ 1. Check for        │            │ 1. Stop Stopwatch   │           │
│        │    [SkipApiLogging] │            │                     │           │
│        │    ▼ if present,    │            │ 2. Capture response │           │
│        │      SKIP           │            │    status code      │           │
│        └─────────┬───────────┘            │                     │           │
│                  │                        │ 3. Check body       │           │
│        ┌─────────▼───────────┐            │    config & capture │           │
│        │ 2. Start Stopwatch  │            │                     │           │
│        │    Stopwatch.       │            │ 4. Build log entry  │           │
│        │    StartNew()       │            │                     │           │
│        └─────────┬───────────┘            │ 5. FIRE & FORGET    │           │
│                  │                        │    _ = SaveAsync()  │           │
│        ┌─────────▼───────────┐            └─────────────────────┘           │
│        │ 3. Record           │                                              │
│        │    RequestedAt      │                                              │
│        │    = DateTime.      │                                              │
│        │      UtcNow         │                                              │
│        └─────────┬───────────┘                                              │
│                  │                                                          │
│        ┌─────────▼───────────┐                                              │
│        │ 4. Capture request  │                                              │
│        │    • Method, Path   │                                              │
│        │    • Headers (safe) │                                              │
│        │    • IP Address     │                                              │
│        │    • Source System  │                                              │
│        └─────────┬───────────┘                                              │
│                  │                                                          │
│        ┌─────────▼───────────┐                                              │
│        │ 5. Store in         │                                              │
│        │    HttpContext.Items│                                              │
│        │    for later        │                                              │
│        └─────────────────────┘                                              │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- ActionFilterAttribute that wraps controller actions
- Captures timing with Stopwatch
- Fire-and-forget async save (doesn't slow down request)

**Pseudo-code:**
```csharp
namespace FreeGLBA.Controllers;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiRequestLoggingAttribute : ActionFilterAttribute
{
    // Keys for HttpContext.Items storage
    private const string StopwatchKey = "ApiLogging_Stopwatch";
    private const string RequestDataKey = "ApiLogging_RequestData";
    
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // 1. Check for skip attribute
        var skipAttribute = context.ActionDescriptor.EndpointMetadata
            .OfType<SkipApiLoggingAttribute>()
            .FirstOrDefault();
        
        if (skipAttribute != null)
        {
            base.OnActionExecuting(context);
            return;
        }
        
        // 2. Start stopwatch
        var stopwatch = Stopwatch.StartNew();
        context.HttpContext.Items[StopwatchKey] = stopwatch;
        
        // 3. Record request time
        var requestData = new RequestCaptureData
        {
            RequestedAt = DateTime.UtcNow,
            HttpMethod = context.HttpContext.Request.Method,
            RequestPath = context.HttpContext.Request.Path,
            QueryString = context.HttpContext.Request.QueryString.ToString(),
            IpAddress = GetClientIpAddress(context.HttpContext),
            UserAgent = context.HttpContext.Request.Headers.UserAgent.ToString(),
            // ... more capture
        };
        
        // 4. Capture headers (filtered)
        requestData.RequestHeaders = CaptureHeaders(
            context.HttpContext.Request.Headers,
            _options.SensitiveHeaders
        );
        
        // 5. Store for OnActionExecuted
        context.HttpContext.Items[RequestDataKey] = requestData;
        
        base.OnActionExecuting(context);
    }
    
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        // 1. Get stopwatch and stop
        if (context.HttpContext.Items[StopwatchKey] is not Stopwatch stopwatch)
        {
            base.OnActionExecuted(context);
            return;
        }
        stopwatch.Stop();
        
        // 2. Get captured request data
        if (context.HttpContext.Items[RequestDataKey] is not RequestCaptureData requestData)
        {
            base.OnActionExecuted(context);
            return;
        }
        
        // 3. Build complete log entry
        var logEntry = new ApiRequestLogItem
        {
            ApiRequestLogId = Guid.NewGuid(),
            RequestedAt = requestData.RequestedAt,
            RespondedAt = DateTime.UtcNow,
            DurationMs = stopwatch.ElapsedMilliseconds,
            HttpMethod = requestData.HttpMethod,
            RequestPath = requestData.RequestPath,
            StatusCode = context.HttpContext.Response.StatusCode,
            IsSuccess = context.HttpContext.Response.StatusCode < 400,
            // ... all fields
        };
        
        // 4. Handle exceptions
        if (context.Exception != null)
        {
            logEntry.ErrorMessage = context.Exception.Message;
            logEntry.ExceptionType = context.Exception.GetType().Name;
        }
        
        // 5. FIRE AND FORGET - Don't await, don't block response
        _ = SaveLogAsync(logEntry);
        
        base.OnActionExecuted(context);
    }
    
    private async Task SaveLogAsync(ApiRequestLogItem log)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dataAccess = scope.ServiceProvider.GetRequiredService<IDataAccess>();
            await dataAccess.CreateApiLogAsync(log);
        }
        catch (Exception ex)
        {
            // Fallback to Serilog - never throw from logging
            Log.Error(ex, "Failed to save API request log: {Path}", log.RequestPath);
        }
    }
}
```

---

## Task 2.2: Implement SkipApiLoggingAttribute

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 2.2: SkipApiLoggingAttribute                                          │
│  ═════════════════════════════════                                          │
│                                                                             │
│   PURPOSE: Prevent infinite logging loops                                   │
│                                                                             │
│   ┌───────────────────────────────────────────────────────────────────┐     │
│   │                                                                   │     │
│   │   [ApiRequestLogging]           // Controller has logging         │     │
│   │   public class DataController                                     │     │
│   │   {                                                               │     │
│   │       [SkipApiLogging]          // ◄── This endpoint skipped     │     │
│   │       public async Task GetApiLogs() { ... }                      │     │
│   │                                                                   │     │
│   │       // This endpoint IS logged                                  │     │
│   │       public async Task CreateEvent() { ... }                     │     │
│   │   }                                                               │     │
│   │                                                                   │     │
│   └───────────────────────────────────────────────────────────────────┘     │
│                                                                             │
│   Without [SkipApiLogging]:                                                 │
│   ════════════════════════                                                  │
│                                                                             │
│   Request ──► Log ──► Save ──► Log ──► Save ──► Log ──► 💥 INFINITE        │
│                                                                             │
│   With [SkipApiLogging]:                                                    │
│   ═════════════════════                                                     │
│                                                                             │
│   Request ──► Check attr ──► SKIP ──► Return                                │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- Simple marker attribute (no logic)
- Applied to endpoints that shouldn't be logged
- Checked by ApiRequestLoggingAttribute

**Pseudo-code:**
```csharp
namespace FreeGLBA.Controllers;

/// <summary>
/// Marker attribute to skip API request logging for specific actions.
/// Use on endpoints that would cause infinite loops (e.g., log viewing endpoints)
/// or don't need logging (health checks, static content).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class SkipApiLoggingAttribute : Attribute
{
    /// <summary>
    /// Optional reason for skipping (for documentation purposes)
    /// </summary>
    public string? Reason { get; set; }
    
    public SkipApiLoggingAttribute() { }
    
    public SkipApiLoggingAttribute(string reason)
    {
        Reason = reason;
    }
}

// Usage examples:
// [SkipApiLogging]  // Simple
// [SkipApiLogging("Prevents infinite loop")]  // With reason
// [SkipApiLogging(Reason = "Health check endpoint")]  // Named parameter
```

---

## Task 2.3: Implement Body Logging Check

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 2.3: BODY LOGGING CHECK                                               │
│  ════════════════════════════                                               │
│                                                                             │
│        Is body logging enabled for this source system?                      │
│                                                                             │
│                         ┌──────────────┐                                    │
│                         │ Check Config │                                    │
│                         │   Table      │                                    │
│                         └──────┬───────┘                                    │
│                                │                                            │
│               ┌────────────────┼────────────────┐                           │
│               │                │                │                           │
│               ▼                ▼                ▼                           │
│        ┌────────────┐   ┌────────────┐   ┌────────────┐                     │
│        │ No Config  │   │ Config     │   │ Config     │                     │
│        │ Found      │   │ Expired    │   │ Active     │                     │
│        └─────┬──────┘   └─────┬──────┘   └─────┬──────┘                     │
│              │                │                │                            │
│              ▼                ▼                ▼                            │
│        ┌────────────┐   ┌────────────┐   ┌────────────┐                     │
│        │ Log        │   │ Auto-      │   │ Capture    │                     │
│        │ Metadata   │   │ Disable &  │   │ Body       │                     │
│        │ Only       │   │ Log Meta   │   │ (Truncated)│                     │
│        └────────────┘   └────────────┘   └────────────┘                     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- Checks BodyLoggingConfig table for source system
- Handles expiration automatically
- Truncates body to configured limit

**Pseudo-code:**
```csharp
// Inside ApiRequestLoggingAttribute

private async Task<(bool Enabled, string? Body)> CheckAndCaptureBodyAsync(
    HttpContext context, 
    Guid sourceSystemId,
    int bodyLimit)
{
    // Check if body logging is enabled for this source
    using var scope = _serviceProvider.CreateScope();
    var dataAccess = scope.ServiceProvider.GetRequiredService<IDataAccess>();
    
    var config = await dataAccess.GetBodyLoggingConfigAsync(sourceSystemId);
    
    // No config = not enabled
    if (config == null || !config.IsActive)
    {
        return (false, null);
    }
    
    // Check expiration
    if (config.ExpiresAt < DateTime.UtcNow)
    {
        // Auto-disable expired config
        config.IsActive = false;
        config.DisabledAt = DateTime.UtcNow;
        await dataAccess.UpdateBodyLoggingConfigAsync(config);
        return (false, null);
    }
    
    // Body logging is enabled - capture it
    context.Request.EnableBuffering();
    context.Request.Body.Position = 0;
    
    using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
    var body = await reader.ReadToEndAsync();
    context.Request.Body.Position = 0;
    
    // Truncate if necessary
    if (body.Length > bodyLimit)
    {
        body = body.Substring(0, bodyLimit) + "...[TRUNCATED]";
    }
    
    return (true, body);
}
```

---

## Task 2.4: Implement DataAccess Methods

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 2.4: DATA ACCESS METHODS                                              │
│  ═════════════════════════════                                              │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │                     DataAccess.ApiLogging                           │   │
│   ├─────────────────────────────────────────────────────────────────────┤   │
│   │                                                                     │   │
│   │   CreateApiLogAsync(log)                                            │   │
│   │   ├── Validate entity                                               │   │
│   │   ├── Insert into DbSet                                             │   │
│   │   └── SaveChangesAsync                                              │   │
│   │                                                                     │   │
│   │   GetBodyLoggingConfigAsync(sourceSystemId)                         │   │
│   │   ├── Query BodyLoggingConfigs                                      │   │
│   │   └── Return active config or null                                  │   │
│   │                                                                     │   │
│   │   NOTE: Database cleanup is handled externally via scheduled        │   │
│   │         SQL jobs (see Task 2.7 for recommended scripts)             │   │
│   │                                                                     │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- Partial class extending existing DataAccess
- CRUD operations for API logs
- Note: Cleanup is handled by external database maintenance jobs

**Pseudo-code:**
```csharp
namespace FreeGLBA.DataAccess;

public partial class DataAccess
{
    // === CREATE LOG ===
    public async Task<Guid> CreateApiLogAsync(ApiRequestLogItem log)
    {
        if (log.ApiRequestLogId == Guid.Empty)
        {
            log.ApiRequestLogId = Guid.NewGuid();
        }
        
        _context.ApiRequestLogs.Add(log);
        await _context.SaveChangesAsync();
        
        return log.ApiRequestLogId;
    }
    
    // === GET BODY LOGGING CONFIG ===
    public async Task<BodyLoggingConfigItem?> GetBodyLoggingConfigAsync(Guid sourceSystemId)
    {
        return await _context.BodyLoggingConfigs
            .Where(c => c.SourceSystemId == sourceSystemId && c.IsActive)
            .FirstOrDefaultAsync();
    }
    
    // === UPDATE BODY LOGGING CONFIG ===
    public async Task UpdateBodyLoggingConfigAsync(BodyLoggingConfigItem config)
    {
        _context.BodyLoggingConfigs.Update(config);
        await _context.SaveChangesAsync();
    }
    
    // NOTE: Database cleanup (deleting old logs) is handled externally
    // via scheduled SQL jobs. See Task 2.7 for recommended scripts.
}
```

---

## Task 2.5: Implement Header Filtering

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 2.5: HEADER FILTERING                                                 │
│  ══════════════════════════                                                 │
│                                                                             │
│   INCOMING HEADERS                  LOGGED HEADERS                          │
│   ════════════════                  ══════════════                          │
│                                                                             │
│   Authorization: Bearer xyz123      Authorization: [REDACTED]               │
│   X-Api-Key: secret-key-here   ──►  X-Api-Key: [REDACTED]                   │
│   Content-Type: application/json    Content-Type: application/json          │
│   Cookie: session=abc123            Cookie: [REDACTED]                      │
│   Host: api.example.com             Host: api.example.com                   │
│   User-Agent: Mozilla/5.0           User-Agent: Mozilla/5.0                 │
│                                                                             │
│   SENSITIVE HEADERS (configurable):                                         │
│   ═════════════════════════════════                                         │
│   • Authorization      • Cookie                                             │
│   • X-Api-Key         • Set-Cookie                                         │
│   • X-Auth-Token      • X-Forwarded-Authorization                          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- Filters sensitive headers before logging
- Configurable list of headers to redact
- Returns JSON string of filtered headers

**Pseudo-code:**
```csharp
// Inside ApiRequestLoggingAttribute

private string CaptureHeaders(
    IHeaderDictionary headers, 
    List<string> sensitiveHeaders)
{
    var filteredHeaders = new Dictionary<string, string>();
    
    foreach (var header in headers)
    {
        var headerName = header.Key;
        var headerValue = header.Value.ToString();
        
        // Check if sensitive (case-insensitive)
        var isSensitive = sensitiveHeaders
            .Any(s => s.Equals(headerName, StringComparison.OrdinalIgnoreCase));
        
        filteredHeaders[headerName] = isSensitive 
            ? "[REDACTED]" 
            : headerValue;
    }
    
    return JsonSerializer.Serialize(filteredHeaders);
}
```

---

## Task 2.6: Register Attribute in GlbaController

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 2.6: APPLY ATTRIBUTE TO CONTROLLER                                    │
│  ═══════════════════════════════════════                                    │
│                                                                             │
│   BEFORE                              AFTER                                 │
│   ══════                              ═════                                 │
│                                                                             │
│   [ApiController]                     [ApiController]                       │
│   [Route("api/glba")]                 [Route("api/glba")]                   │
│   public class GlbaController         [ApiRequestLogging]  ◄── ADD THIS     │
│   {                                   public class GlbaController           │
│       ...                             {                                     │
│   }                                       ...                               │
│                                       }                                     │
│                                                                             │
│                                                                             │
│   ENDPOINTS WITH [SkipApiLogging]:                                          │
│   ════════════════════════════════                                          │
│                                                                             │
│   • GET /api/data/api-logs           (list view data)                       │
│   • GET /api/data/api-logs/{id}      (detail view data)                     │
│   • GET /api/data/api-logs/stats     (dashboard data)                       │
│   • GET /api/data/api-logs/export    (export data)                          │
│   • Health check endpoints                                                  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- Apply `[ApiRequestLogging]` to controllers that should be logged
- Apply `[SkipApiLogging]` to log-viewing endpoints
- Prevents infinite logging loops

**Pseudo-code:**
```csharp
// GlbaController.cs - Add attribute
[ApiController]
[Route("api/glba")]
[ApiRequestLogging]  // ◄── ADD THIS
public class GlbaController : ControllerBase
{
    // All actions in this controller will be logged
}

// DataController.cs - Add skip to log endpoints (when created)
[ApiController]
[Route("api/data")]
public partial class DataController : ControllerBase
{
    [HttpGet("api-logs")]
    [SkipApiLogging(Reason = "Prevents infinite loop")]
    public async Task<IActionResult> GetApiLogs([FromQuery] ApiLogFilterParams filters)
    {
        // ... implementation
    }
    
    [HttpGet("api-logs/{id}")]
    [SkipApiLogging(Reason = "Prevents infinite loop")]
    public async Task<IActionResult> GetApiLog(Guid id)
    {
        // ... implementation
    }
}
```

---

## Task 2.7: External Database Cleanup (Recommended SQL)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 2.7: EXTERNAL DATABASE CLEANUP (RECOMMENDED SQL)                      │
│  ═════════════════════════════════════════════════════                      │
│                                                                             │
│   Database cleanup is handled externally by your DBA team or                │
│   scheduled database maintenance jobs. The application does NOT             │
│   manage log retention - this is by design to allow flexibility             │
│   in retention policies and maintenance windows.                            │
│                                                                             │
│   RECOMMENDED: Schedule these scripts to run daily during off-hours         │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- SQL scripts for external database maintenance
- Run via SQL Agent Job, Azure Automation, or other scheduler
- Allows DBA control over retention policies and execution timing

**Recommended SQL Scripts:**

```sql
-- ═══════════════════════════════════════════════════════════════════════════
-- SCRIPT 1: Delete old API request logs
-- Recommended: Run daily during maintenance window
-- Adjust @RetentionDays based on your compliance requirements
-- ═══════════════════════════════════════════════════════════════════════════

DECLARE @RetentionDays INT = 90;  -- Adjust as needed
DECLARE @CutoffDate DATETIME2 = DATEADD(DAY, -@RetentionDays, GETUTCDATE());
DECLARE @BatchSize INT = 10000;   -- Delete in batches to avoid lock escalation
DECLARE @DeletedCount INT = 1;
DECLARE @TotalDeleted INT = 0;

WHILE @DeletedCount > 0
BEGIN
    DELETE TOP (@BatchSize) 
    FROM [dbo].[ApiRequestLogs]
    WHERE [RequestedAt] < @CutoffDate;
    
    SET @DeletedCount = @@ROWCOUNT;
    SET @TotalDeleted = @TotalDeleted + @DeletedCount;
    
    -- Brief pause to reduce lock contention
    IF @DeletedCount > 0
        WAITFOR DELAY '00:00:01';
END

PRINT 'Deleted ' + CAST(@TotalDeleted AS VARCHAR(20)) + ' API request logs older than ' 
      + CAST(@RetentionDays AS VARCHAR(10)) + ' days.';
GO


-- ═══════════════════════════════════════════════════════════════════════════
-- SCRIPT 2: Auto-disable expired body logging configs
-- Recommended: Run daily (can be combined with Script 1)
-- ═══════════════════════════════════════════════════════════════════════════

UPDATE [dbo].[BodyLoggingConfigs]
SET [IsActive] = 0,
    [DisabledAt] = GETUTCDATE()
WHERE [IsActive] = 1 
  AND [ExpiresAt] < GETUTCDATE();

PRINT 'Disabled ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' expired body logging configs.';
GO


-- ═══════════════════════════════════════════════════════════════════════════
-- SCRIPT 3: Optional - Get storage statistics before cleanup
-- Useful for monitoring and capacity planning
-- ═══════════════════════════════════════════════════════════════════════════

SELECT 
    COUNT(*) AS TotalLogCount,
    MIN(RequestedAt) AS OldestLog,
    MAX(RequestedAt) AS NewestLog,
    SUM(CASE WHEN IsSuccess = 0 THEN 1 ELSE 0 END) AS ErrorCount,
    COUNT(DISTINCT SourceSystemId) AS UniqueSourceSystems
FROM [dbo].[ApiRequestLogs];

-- Size estimate
SELECT 
    t.name AS TableName,
    p.rows AS RowCounts,
    CAST(ROUND((SUM(a.total_pages) * 8) / 1024.00, 2) AS NUMERIC(36, 2)) AS TotalSpaceMB,
    CAST(ROUND((SUM(a.used_pages) * 8) / 1024.00, 2) AS NUMERIC(36, 2)) AS UsedSpaceMB
FROM sys.tables t
INNER JOIN sys.indexes i ON t.object_id = i.object_id
INNER JOIN sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
WHERE t.name = 'ApiRequestLogs'
GROUP BY t.name, p.rows;
GO
```

**SQL Agent Job Setup (Example):**

```sql
-- Create a SQL Agent Job to run cleanup daily at 2:00 AM
-- This is a template - adjust for your environment

USE msdb;
GO

EXEC sp_add_job
    @job_name = N'FreeGLBA_ApiLogCleanup',
    @description = N'Deletes API request logs older than retention period';

EXEC sp_add_jobstep
    @job_name = N'FreeGLBA_ApiLogCleanup',
    @step_name = N'Delete Old Logs',
    @subsystem = N'TSQL',
    @command = N'
        DECLARE @RetentionDays INT = 90;
        DELETE FROM [dbo].[ApiRequestLogs]
        WHERE [RequestedAt] < DATEADD(DAY, -@RetentionDays, GETUTCDATE());
    ',
    @database_name = N'YourDatabaseName';

EXEC sp_add_schedule
    @schedule_name = N'Daily_2AM',
    @freq_type = 4,  -- Daily
    @freq_interval = 1,
    @active_start_time = 020000;  -- 2:00 AM

EXEC sp_attach_schedule
    @job_name = N'FreeGLBA_ApiLogCleanup',
    @schedule_name = N'Daily_2AM';

EXEC sp_add_jobserver
    @job_name = N'FreeGLBA_ApiLogCleanup';
GO
```

---

## Phase 2 Files Summary

| File | Action | Key Changes |
|------|--------|-------------|
| `FreeGLBA.App.ApiRequestLoggingAttribute.cs` | MODIFY | Full implementation |
| `FreeGLBA.App.SkipApiLoggingAttribute.cs` | MODIFY | Add Reason property |
| `FreeGLBA.App.DataAccess.ApiLogging.cs` | MODIFY | Implement CreateLog, GetConfig |
| `FreeGLBA.App.GlbaController.cs` | MODIFY | Add [ApiRequestLogging] attribute |
| N/A (SQL Scripts) | DOCUMENT | Provide to DBA for scheduled execution |

---

## Verification Checklist

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         PHASE 2 VERIFICATION                                │
└─────────────────────────────────────────────────────────────────────────────┘

  □ ApiRequestLoggingAttribute captures request data in OnActionExecuting
  □ ApiRequestLoggingAttribute saves log in OnActionExecuted
  □ Fire-and-forget pattern used (response not delayed)
  □ SkipApiLoggingAttribute prevents logging when applied
  □ Sensitive headers are filtered
  □ Body logging respects configuration
  □ DataAccess.CreateApiLogAsync works
  □ GlbaController has [ApiRequestLogging] attribute
  □ Solution builds without errors
  □ SQL cleanup scripts provided to DBA team
  
  MANUAL TEST:
  ════════════
  1. POST /api/glba/events
  2. Check database for ApiRequestLogs entry
  3. Verify all fields populated correctly
  4. Verify sensitive headers are [REDACTED]
```

---

## Phase 2 Summary

| Metric | Value |
|--------|-------|
| Files Modified | 4 |
| Lines of Code | ~300 |
| Key Feature | Core logging engine |
| Estimated Time | 1.5 days |
| Dependencies | Phase 1 |
| Deliverable | Working logging |
| DB Cleanup | External (SQL scripts provided) |

---

*Previous: [119 — Phase 1: Entity & DTO Implementation](119_impl_phase1_entities.md)*  
*Next: [121 — Phase 3: Dashboard View](121_impl_phase3_dashboard.md)*

# 119 — Implementation Plan: Phase 1 - Entity & DTO Implementation

> **Document ID:** 119  
> **Category:** Implementation Plan  
> **Purpose:** Fully implement entity classes and DTOs  
> **Phase:** 1 of 6  
> **Estimated Time:** 1.0 days  
> **Dependencies:** Phase 0 complete (files exist)

---

## Phase Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     PHASE 1: ENTITY & DTO IMPLEMENTATION                    │
└─────────────────────────────────────────────────────────────────────────────┘

  INPUT                              OUTPUT
  ═════                              ══════
  
  ┌─────────────────┐                ┌─────────────────────────────────────┐
  │  Empty entity   │                │  Complete entities with:            │
  │  skeletons from │───────────────►│  • All 25+ properties               │
  │  Phase 0        │                │  • Validation attributes            │
  │                 │                │  • Proper data types                │
  └─────────────────┘                │  • Database annotations             │
                                     └─────────────────────────────────────┘
  
  ┌─────────────────┐                ┌─────────────────────────────────────┐
  │  Empty DTO      │                │  Complete DTOs with:                │
  │  skeletons from │───────────────►│  • Matching properties              │
  │  Phase 0        │                │  • Mapping methods                  │
  │                 │                │  • Configuration options            │
  └─────────────────┘                └─────────────────────────────────────┘
```

---

## Task 1.1: Complete ApiRequestLogItem Entity

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 1.1: ApiRequestLogItem ENTITY                                         │
│  ═══════════════════════════════════                                        │
│                                                                             │
│                    ┌─────────────────────────────────────┐                  │
│                    │         ApiRequestLogItem           │                  │
│                    ├─────────────────────────────────────┤                  │
│                    │                                     │                  │
│                    │  WHO Section                        │                  │
│                    │  ├── SourceSystemId                 │                  │
│                    │  ├── SourceSystemName               │                  │
│                    │  ├── UserId                         │                  │
│                    │  ├── UserName                       │                  │
│                    │  └── TenantId                       │                  │
│                    │                                     │                  │
│                    │  WHAT Section                       │                  │
│                    │  ├── HttpMethod                     │                  │
│                    │  ├── RequestPath                    │                  │
│                    │  ├── QueryString                    │                  │
│                    │  ├── RequestHeaders                 │                  │
│                    │  ├── RequestBody                    │                  │
│                    │  └── RequestBodySize                │                  │
│                    │                                     │                  │
│                    │  WHEN Section                       │                  │
│                    │  ├── RequestedAt                    │                  │
│                    │  ├── RespondedAt                    │                  │
│                    │  └── DurationMs                     │                  │
│                    │                                     │                  │
│                    │  WHERE Section                      │                  │
│                    │  ├── IpAddress                      │                  │
│                    │  ├── UserAgent                      │                  │
│                    │  └── ForwardedFor                   │                  │
│                    │                                     │                  │
│                    │  RESULT Section                     │                  │
│                    │  ├── StatusCode                     │                  │
│                    │  ├── IsSuccess                      │                  │
│                    │  ├── ResponseBody                   │                  │
│                    │  ├── ResponseBodySize               │                  │
│                    │  ├── ErrorMessage                   │                  │
│                    │  └── ExceptionType                  │                  │
│                    │                                     │                  │
│                    │  TRACING Section                    │                  │
│                    │  ├── CorrelationId                  │                  │
│                    │  ├── AuthType                       │                  │
│                    │  ├── RelatedEntityId                │                  │
│                    │  ├── RelatedEntityType              │                  │
│                    │  └── BodyLoggingEnabled             │                  │
│                    │                                     │                  │
│                    └─────────────────────────────────────┘                  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- Complete entity with 25+ properties
- Proper `[MaxLength]` and `[Required]` attributes
- Indexed columns for query performance

**Pseudo-code:**
```csharp
namespace FreeGLBA.EFModels.EFModels;

[Table("ApiRequestLogs")]
[Index(nameof(SourceSystemId), nameof(RequestedAt))]
[Index(nameof(RequestedAt))]
[Index(nameof(StatusCode))]
[Index(nameof(CorrelationId))]
public partial class ApiRequestLogItem
{
    [Key]
    public Guid ApiRequestLogId { get; set; }
    
    // === WHO ===
    public Guid SourceSystemId { get; set; }
    
    [MaxLength(200)]
    public string SourceSystemName { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string UserId { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string UserName { get; set; } = string.Empty;
    
    public Guid? TenantId { get; set; }
    
    // === WHAT ===
    [MaxLength(10)]
    public string HttpMethod { get; set; } = string.Empty;  // GET, POST, etc.
    
    [MaxLength(500)]
    public string RequestPath { get; set; } = string.Empty;
    
    [MaxLength(2000)]
    public string QueryString { get; set; } = string.Empty;
    
    public string RequestHeaders { get; set; } = string.Empty;  // JSON
    
    public string RequestBody { get; set; } = string.Empty;  // Truncated to 4KB
    
    public int RequestBodySize { get; set; }  // Actual size before truncation
    
    // === WHEN ===
    public DateTime RequestedAt { get; set; }
    
    public DateTime RespondedAt { get; set; }
    
    public long DurationMs { get; set; }
    
    // === WHERE ===
    [MaxLength(50)]
    public string IpAddress { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string UserAgent { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string ForwardedFor { get; set; } = string.Empty;
    
    // === RESULT ===
    public int StatusCode { get; set; }
    
    public bool IsSuccess { get; set; }
    
    public string ResponseBody { get; set; } = string.Empty;  // Truncated
    
    public int ResponseBodySize { get; set; }
    
    [MaxLength(1000)]
    public string ErrorMessage { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string ExceptionType { get; set; } = string.Empty;
    
    // === TRACING ===
    [MaxLength(100)]
    public string CorrelationId { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string AuthType { get; set; } = string.Empty;  // ApiKey, JWT, etc.
    
    [MaxLength(100)]
    public string RelatedEntityId { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string RelatedEntityType { get; set; } = string.Empty;
    
    public bool BodyLoggingEnabled { get; set; }
}
```

---

## Task 1.2: Complete BodyLoggingConfigItem Entity

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 1.2: BodyLoggingConfigItem ENTITY                                     │
│  ═══════════════════════════════════════                                    │
│                                                                             │
│                    ┌─────────────────────────────────────┐                  │
│                    │       BodyLoggingConfigItem         │                  │
│                    ├─────────────────────────────────────┤                  │
│                    │                                     │                  │
│                    │  ┌───────────────────────────────┐  │                  │
│                    │  │     WHO ENABLED IT?           │  │                  │
│                    │  │     EnabledByUserId           │  │                  │
│                    │  │     EnabledByUserName         │  │                  │
│                    │  └───────────────────────────────┘  │                  │
│                    │                │                    │                  │
│                    │                ▼                    │                  │
│                    │  ┌───────────────────────────────┐  │                  │
│                    │  │     FOR WHICH SOURCE?         │  │                  │
│                    │  │     SourceSystemId            │  │                  │
│                    │  └───────────────────────────────┘  │                  │
│                    │                │                    │                  │
│                    │                ▼                    │                  │
│                    │  ┌───────────────────────────────┐  │                  │
│                    │  │     WHEN?                     │  │                  │
│                    │  │     EnabledAt                 │  │                  │
│                    │  │     ExpiresAt                 │  │                  │
│                    │  │     DisabledAt                │  │                  │
│                    │  └───────────────────────────────┘  │                  │
│                    │                │                    │                  │
│                    │                ▼                    │                  │
│                    │  ┌───────────────────────────────┐  │                  │
│                    │  │     WHY?                      │  │                  │
│                    │  │     Reason                    │  │                  │
│                    │  └───────────────────────────────┘  │                  │
│                    │                                     │                  │
│                    └─────────────────────────────────────┘                  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- Audit trail for body logging configuration changes
- Time-limited enabling with auto-expire
- Links to source system

**Pseudo-code:**
```csharp
namespace FreeGLBA.EFModels.EFModels;

[Table("BodyLoggingConfigs")]
[Index(nameof(SourceSystemId), nameof(IsActive))]
public partial class BodyLoggingConfigItem
{
    [Key]
    public Guid BodyLoggingConfigId { get; set; }
    
    // Which source system
    public Guid SourceSystemId { get; set; }
    
    // Who enabled it
    public Guid EnabledByUserId { get; set; }
    
    [MaxLength(200)]
    public string EnabledByUserName { get; set; } = string.Empty;
    
    // When
    public DateTime EnabledAt { get; set; }
    
    public DateTime ExpiresAt { get; set; }
    
    public DateTime? DisabledAt { get; set; }  // Null until disabled
    
    // Status
    public bool IsActive { get; set; }
    
    // Why
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}
```

---

## Task 1.3: Complete DTOs

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 1.3: DATA TRANSFER OBJECTS                                            │
│  ═══════════════════════════════                                            │
│                                                                             │
│        ENTITY                            DTO                                │
│        ══════                            ═══                                │
│                                                                             │
│   ┌─────────────────┐              ┌─────────────────┐                      │
│   │ ApiRequestLog   │              │ ApiRequestLog   │                      │
│   │ Item            │─────────────►│ Dto             │ (full details)       │
│   └─────────────────┘              └─────────────────┘                      │
│           │                                                                 │
│           │                        ┌─────────────────┐                      │
│           └───────────────────────►│ ApiRequestLog   │ (list/summary)       │
│                                    │ ListDto         │                      │
│                                    └─────────────────┘                      │
│                                                                             │
│   ┌─────────────────┐              ┌─────────────────┐                      │
│   │ BodyLogging     │              │ BodyLogging     │                      │
│   │ ConfigItem      │─────────────►│ ConfigDto       │                      │
│   └─────────────────┘              └─────────────────┘                      │
│                                                                             │
│   (No entity)                      ┌─────────────────┐                      │
│                                    │ DashboardStats  │ (computed)           │
│                                    │ Dto             │                      │
│                                    └─────────────────┘                      │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- DTOs for API responses (not exposing entities directly)
- List DTO with fewer fields for table display
- Dashboard DTO for aggregated statistics

**Pseudo-code:**
```csharp
namespace FreeGLBA.DataObjects;

/// <summary>
/// Full details DTO for single log view
/// </summary>
public class ApiRequestLogDto
{
    public Guid ApiRequestLogId { get; set; }
    public Guid SourceSystemId { get; set; }
    public string SourceSystemName { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string RequestPath { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public long DurationMs { get; set; }
    public int StatusCode { get; set; }
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    // ... all fields ...
    
    public static ApiRequestLogDto FromEntity(ApiRequestLogItem item) => new()
    {
        ApiRequestLogId = item.ApiRequestLogId,
        // ... map all fields ...
    };
}

/// <summary>
/// Summary DTO for list/table view
/// </summary>
public class ApiRequestLogListDto
{
    public Guid ApiRequestLogId { get; set; }
    public string SourceSystemName { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string RequestPath { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public long DurationMs { get; set; }
    public int StatusCode { get; set; }
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Dashboard statistics DTO
/// </summary>
public class DashboardStatsDto
{
    public int TotalRequests { get; set; }
    public int TotalErrors { get; set; }
    public double ErrorRate { get; set; }
    public double AvgDurationMs { get; set; }
    
    public List<SourceSystemStats> BySourceSystem { get; set; } = new();
    public List<StatusCodeStats> ByStatusCode { get; set; } = new();
    public List<TimeSeriesPoint> RequestsOverTime { get; set; } = new();
    public List<ApiRequestLogListDto> RecentErrors { get; set; } = new();
}

public class SourceSystemStats
{
    public string SourceSystemName { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class StatusCodeStats
{
    public int StatusCode { get; set; }
    public string Category { get; set; } = string.Empty;  // 2xx, 4xx, 5xx
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class TimeSeriesPoint
{
    public DateTime Timestamp { get; set; }
    public int Count { get; set; }
}
```

---

## Task 1.4: Complete Configuration Options

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 1.4: CONFIGURATION OPTIONS                                            │
│  ═══════════════════════════════                                            │
│                                                                             │
│                  appsettings.json                                           │
│                        │                                                    │
│                        ▼                                                    │
│         ┌─────────────────────────────┐                                     │
│         │  "ApiLogging": {            │                                     │
│         │    "BodyLogLimit": 4096,    │                                     │
│         │    "SensitiveHeaders": [...] │                                    │
│         │    ...                      │                                     │
│         │  }                          │                                     │
│         └──────────────┬──────────────┘                                     │
│                        │                                                    │
│                        ▼                                                    │
│         ┌─────────────────────────────┐                                     │
│         │     ApiLoggingOptions       │                                     │
│         │                             │                                     │
│         │  builder.Services.Configure │                                     │
│         │  <ApiLoggingOptions>(...)   │                                     │
│         └─────────────────────────────┘                                     │
│                        │                                                    │
│                        ▼                                                    │
│         ┌─────────────────────────────┐                                     │
│         │  IOptions<ApiLoggingOptions>│                                     │
│         │                             │                                     │
│         │  Injected into attribute    │                                     │
│         │  and data access layer      │                                     │
│         └─────────────────────────────┘                                     │
│                                                                             │
│   NOTE: Log retention is handled externally via SQL jobs (see doc 123)      │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- Strongly-typed configuration class
- Default values for all settings
- Bound from `appsettings.json`

**Pseudo-code:**
```csharp
namespace FreeGLBA.DataObjects;

public class ApiLoggingOptions
{
    /// <summary>
    /// Maximum size of request/response body to log (default: 4096 bytes)
    /// </summary>
    public int BodyLogLimit { get; set; } = 4096;
    
    /// <summary>
    /// Headers to strip from logs (contain sensitive data)
    /// </summary>
    public List<string> SensitiveHeaders { get; set; } = new()
    {
        "Authorization",
        "X-Api-Key",
        "Cookie",
        "Set-Cookie"
    };
    
    /// <summary>
    /// Default duration for body logging when enabled (hours)
    /// </summary>
    public int DefaultBodyLoggingDurationHours { get; set; } = 24;
    
    /// <summary>
    /// Maximum duration for body logging (hours)
    /// </summary>
    public int MaxBodyLoggingDurationHours { get; set; } = 72;
    
    /// <summary>
    /// Maximum rows for immediate export
    /// </summary>
    public int MaxExportRows { get; set; } = 10000;
    
    /// <summary>
    /// Dashboard auto-refresh interval (seconds), 0 = disabled
    /// </summary>
    public int DashboardRefreshSeconds { get; set; } = 30;
    
    // NOTE: Log retention (cleanup) is handled externally via scheduled
    // SQL jobs managed by your DBA team. See doc 123 for scripts.
}
```

---

## Task 1.5: Complete Filter Parameters

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 1.5: FILTER PARAMETERS                                                │
│  ═══════════════════════════                                                │
│                                                                             │
│   USER SELECTS FILTERS              FILTER OBJECT                           │
│   ═════════════════════              ═════════════                          │
│                                                                             │
│   ┌─────────────────────┐          ┌─────────────────────────────────┐      │
│   │ Time: Last 24h  ▼   │          │ ApiLogFilterParams              │      │
│   ├─────────────────────┤          │                                 │      │
│   │ Source: Banner  ▼   │────────► │  FromDate: 2025-01-26           │      │
│   ├─────────────────────┤          │  ToDate: 2025-01-27             │      │
│   │ Status: Errors  ▼   │          │  SourceSystemId: <guid>         │      │
│   ├─────────────────────┤          │  StatusCodes: [400,401,500,502] │      │
│   │ Duration: >1s   ▼   │          │  MinDurationMs: 1000            │      │
│   └─────────────────────┘          │  Page: 1                        │      │
│                                    │  PageSize: 50                   │      │
│                                    └─────────────────────────────────┘      │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- Query parameter class for filtering logs
- Supports all filter types from focus group
- Pagination support

**Pseudo-code:**
```csharp
namespace FreeGLBA.DataObjects;

public class ApiLogFilterParams
{
    // Time range
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    
    // Source filter
    public Guid? SourceSystemId { get; set; }
    
    // Status filter
    public List<int>? StatusCodes { get; set; }
    public bool? SuccessOnly { get; set; }
    public bool? ErrorsOnly { get; set; }
    
    // Duration filter (focus group request)
    public long? MinDurationMs { get; set; }
    public long? MaxDurationMs { get; set; }
    
    // Search
    public string? SearchTerm { get; set; }  // Searches path, error message
    
    // Correlation
    public string? CorrelationId { get; set; }
    
    // Pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    
    // Sorting
    public string SortBy { get; set; } = "RequestedAt";
    public bool SortDescending { get; set; } = true;
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
```

---

## Task 1.6: Update EFDataModel

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 1.6: UPDATE DBCONTEXT                                                 │
│  ══════════════════════════                                                 │
│                                                                             │
│   BEFORE                              AFTER                                 │
│   ══════                              ═════                                 │
│                                                                             │
│   public partial class                public partial class                  │
│   EFDataModel : DbContext             EFDataModel : DbContext               │
│   {                                   {                                     │
│     DbSet<SourceSystemItem>             DbSet<SourceSystemItem>             │
│     DbSet<AccessEventItem>              DbSet<AccessEventItem>              │
│     DbSet<DataSubjectItem>              DbSet<DataSubjectItem>              │
│     DbSet<ComplianceReportItem>         DbSet<ComplianceReportItem>         │
│   }                               +     DbSet<ApiRequestLogItem>            │
│                                   +     DbSet<BodyLoggingConfigItem>        │
│                                     }                                       │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- Add new DbSet properties
- Entity Framework will now track these entities

**Pseudo-code (addition to existing file):**
```csharp
// Add to FreeGLBA.App.EFDataModel.cs

public partial class EFDataModel : DbContext
{
    // ...existing DbSets...
    
    // API Request Logging
    public virtual DbSet<ApiRequestLogItem> ApiRequestLogs { get; set; } = null!;
    public virtual DbSet<BodyLoggingConfigItem> BodyLoggingConfigs { get; set; } = null!;
}
```

---

## Phase 1 Files Summary

| File | Action | Key Changes |
|------|--------|-------------|
| `FreeGLBA.App.ApiRequestLog.cs` | MODIFY | Add all 25+ properties with attributes |
| `FreeGLBA.App.BodyLoggingConfig.cs` | MODIFY | Add all 9 properties with attributes |
| `FreeGLBA.App.DataObjects.ApiLogging.cs` | MODIFY | Add complete DTOs and options |
| `FreeGLBA.App.EFDataModel.cs` | MODIFY | Add 2 DbSet properties |

---

## Verification Checklist

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         PHASE 1 VERIFICATION                                │
└─────────────────────────────────────────────────────────────────────────────┘

  □ ApiRequestLogItem has 25+ properties with proper attributes
  □ BodyLoggingConfigItem has 9 properties with proper attributes  
  □ Both entities have [Index] attributes for query performance
  □ All DTOs have FromEntity() mapping methods
  □ ApiLoggingOptions has all configuration properties with defaults
  □ ApiLogFilterParams supports all filter types
  □ EFDataModel has new DbSet properties
  □ Solution builds without errors
  
  BUILD CHECK:
  ════════════
  $ dotnet build FreeGLBA.sln
  
  Expected: Build succeeded. 0 Errors.
```

---

## Phase 1 Summary

| Metric | Value |
|--------|-------|
| Files Modified | 4 |
| Properties Added | ~50 |
| DTOs Created | 7 |
| Estimated Time | 1.0 days |
| Dependencies | Phase 0 |
| Deliverable | Complete data model |

---

*Previous: [118 — Phase 0: File Scaffolding](118_impl_phase0_scaffolding.md)*  
*Next: [120 — Phase 2: Logging Attribute & DataAccess](120_impl_phase2_attribute.md)*

# 118 — Implementation Plan: Phase 0 - File Scaffolding

> **Document ID:** 118  
> **Category:** Implementation Plan  
> **Purpose:** Create all empty/skeleton files before implementation begins  
> **Phase:** 0 of 6  
> **Estimated Time:** 0.5 days  
> **Dependencies:** None (this is the starting point)

---

## Phase Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         PHASE 0: FILE SCAFFOLDING                           │
└─────────────────────────────────────────────────────────────────────────────┘

  PURPOSE: Create all files with skeleton structure before any implementation
  
  WHY DO THIS FIRST?
  ══════════════════
  
  ┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
  │   Prevents      │     │   Enables       │     │   Allows        │
  │   Merge         │────►│   Parallel      │────►│   Build         │
  │   Conflicts     │     │   Development   │     │   Verification  │
  └─────────────────┘     └─────────────────┘     └─────────────────┘
  
  All files exist         Team can work on        Solution compiles
  in correct locations    different phases        at every step
  from day 1              simultaneously          (even if empty)
```

---

## Complete File Inventory

### Files to CREATE (New Files)

| # | Project | File Path | Purpose |
|---|---------|-----------|---------|
| 1 | FreeGLBA.EFModels | `EFModels\FreeGLBA.App.ApiRequestLog.cs` | Main logging entity |
| 2 | FreeGLBA.EFModels | `EFModels\FreeGLBA.App.BodyLoggingConfig.cs` | Body logging audit entity |
| 3 | FreeGLBA.DataObjects | `FreeGLBA.App.DataObjects.ApiLogging.cs` | DTOs + configuration options |
| 4 | FreeGLBA.DataAccess | `FreeGLBA.App.DataAccess.ApiLogging.cs` | CRUD + dashboard stats |
| 5 | FreeGLBA | `Controllers\FreeGLBA.App.ApiRequestLoggingAttribute.cs` | Action filter attribute |
| 6 | FreeGLBA | `Controllers\FreeGLBA.App.SkipApiLoggingAttribute.cs` | Skip marker attribute |
| 7 | FreeGLBA.Client | `Pages\FreeGLBA.App.ApiLogDashboard.razor` | Dashboard view |
| 8 | FreeGLBA.Client | `Pages\FreeGLBA.App.ApiRequestLogs.razor` | List view |
| 9 | FreeGLBA.Client | `Pages\FreeGLBA.App.ViewApiRequestLog.razor` | Detail view |
| 10 | FreeGLBA.Client | `Pages\FreeGLBA.App.BodyLoggingSettings.razor` | Config UI |

### Files to MODIFY (Existing Files)

| # | Project | File Path | Modification |
|---|---------|-----------|--------------|
| 1 | FreeGLBA.EFModels | `EFModels\FreeGLBA.App.EFDataModel.cs` | Add DbSet properties |
| 2 | FreeGLBA.DataAccess | `FreeGLBA.App.IDataAccess.cs` | Add interface methods |
| 3 | FreeGLBA.DataAccess | `FreeGLBA.App.DataAccess.cs` | Add partial class reference |
| 4 | FreeGLBA | `Controllers\FreeGLBA.App.GlbaController.cs` | Add [ApiRequestLogging] attribute |
| 5 | FreeGLBA | `Program.cs` or startup | Register services + options |

---

## Task 0.1: Create Entity Files

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 0.1: ENTITY SKELETONS                                                 │
│  ════════════════════════════                                               │
│                                                                             │
│       ┌─────────────────────────────────────────────────────┐               │
│       │              FreeGLBA.EFModels                      │               │
│       │                    │                                │               │
│       │    ┌───────────────┴───────────────┐                │               │
│       │    │                               │                │               │
│       │    ▼                               ▼                │               │
│       │  ┌─────────────────┐   ┌─────────────────────┐      │               │
│       │  │ ApiRequestLog   │   │ BodyLoggingConfig   │      │               │
│       │  │ Item.cs         │   │ Item.cs             │      │               │
│       │  └─────────────────┘   └─────────────────────┘      │               │
│       └─────────────────────────────────────────────────────┘               │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- Empty entity classes with `[Key]` attribute only
- Proper namespace and using statements
- `[Table]` attribute for database mapping

**Pseudo-code for `FreeGLBA.App.ApiRequestLog.cs`:**
```csharp
namespace FreeGLBA.EFModels.EFModels;

[Table("ApiRequestLogs")]
public partial class ApiRequestLogItem
{
    [Key]
    public Guid ApiRequestLogId { get; set; }
    
    // TODO: Phase 1 - Add all properties
}
```

**Pseudo-code for `FreeGLBA.App.BodyLoggingConfig.cs`:**
```csharp
namespace FreeGLBA.EFModels.EFModels;

[Table("BodyLoggingConfigs")]
public partial class BodyLoggingConfigItem
{
    [Key]
    public Guid BodyLoggingConfigId { get; set; }
    
    // TODO: Phase 1 - Add all properties
}
```

---

## Task 0.2: Create DataObjects File

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 0.2: DTO SKELETON                                                     │
│  ══════════════════════                                                     │
│                                                                             │
│       ┌─────────────────────────────────────────────────────┐               │
│       │              FreeGLBA.DataObjects                   │               │
│       │                         │                           │               │
│       │                         ▼                           │               │
│       │          ┌─────────────────────────────┐            │               │
│       │          │ FreeGLBA.App.DataObjects    │            │               │
│       │          │ .ApiLogging.cs              │            │               │
│       │          │                             │            │               │
│       │          │  • ApiRequestLogDto         │            │               │
│       │          │  • ApiRequestLogListDto     │            │               │
│       │          │  • ApiLoggingOptions        │            │               │
│       │          │  • BodyLoggingConfigDto     │            │               │
│       │          │  • DashboardStatsDto        │            │               │
│       │          └─────────────────────────────┘            │               │
│       └─────────────────────────────────────────────────────┘               │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- Empty DTO classes for API responses
- Configuration options class for `IOptions<T>`
- Filter/query parameter classes

**Pseudo-code:**
```csharp
namespace FreeGLBA.DataObjects;

// === DTOs ===
public class ApiRequestLogDto { /* TODO: Phase 1 */ }
public class ApiRequestLogListDto { /* TODO: Phase 1 */ }
public class BodyLoggingConfigDto { /* TODO: Phase 1 */ }
public class DashboardStatsDto { /* TODO: Phase 3 */ }

// === Configuration ===
public class ApiLoggingOptions
{
    public int BodyLogLimit { get; set; } = 4096;
    // TODO: Phase 1 - Add remaining options
    // NOTE: Log retention handled externally via SQL (see doc 123)
}

// === Filters ===
public class ApiLogFilterParams { /* TODO: Phase 4 */ }
```

---

## Task 0.3: Create DataAccess File

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 0.3: DATA ACCESS SKELETON                                             │
│  ══════════════════════════════                                             │
│                                                                             │
│       ┌─────────────────────────────────────────────────────┐               │
│       │              FreeGLBA.DataAccess                    │               │
│       │                         │                           │               │
│       │                         ▼                           │               │
│       │          ┌─────────────────────────────┐            │               │
│       │          │ FreeGLBA.App.DataAccess     │            │               │
│       │          │ .ApiLogging.cs              │            │               │
│       │          │                             │            │               │
│       │          │  + CreateLogAsync()         │            │               │
│       │          │  + GetLogsAsync()           │            │               │
│       │          │  + GetLogByIdAsync()        │            │               │
│       │          │  + GetDashboardStats()      │            │               │
│       │          │  + GetBodyLoggingConfig()   │            │               │
│       │          │  + SetBodyLoggingConfig()   │            │               │
│       │          └─────────────────────────────┘            │               │
│       └─────────────────────────────────────────────────────┘               │
│                                                                             │
│   NOTE: Database cleanup is handled externally via scheduled SQL jobs.      │
│         See doc 123 for recommended cleanup scripts for your DBA team.      │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- Partial class extending existing DataAccess
- Method stubs that throw `NotImplementedException`
- Proper async signatures

**Pseudo-code:**
```csharp
namespace FreeGLBA.DataAccess;

public partial class DataAccess
{
    // === API Logging ===
    public async Task<Guid> CreateApiLogAsync(ApiRequestLogItem log)
        => throw new NotImplementedException("Phase 2");
    
    public async Task<List<ApiRequestLogListDto>> GetApiLogsAsync(ApiLogFilterParams filters)
        => throw new NotImplementedException("Phase 4");
    
    public async Task<DashboardStatsDto> GetApiLogDashboardStatsAsync(DateTime from, DateTime to)
        => throw new NotImplementedException("Phase 3");
    
    // === Body Logging Config ===
    public async Task<BodyLoggingConfigItem?> GetBodyLoggingConfigAsync(Guid sourceSystemId)
        => throw new NotImplementedException("Phase 2");
    
    public async Task SetBodyLoggingConfigAsync(BodyLoggingConfigItem config)
        => throw new NotImplementedException("Phase 5");
    
    // NOTE: Database cleanup (log retention) is handled externally
    // via scheduled SQL jobs managed by your DBA team.
    // See doc 123 for recommended SQL scripts.
}
```

---

## Task 0.4: Create Attribute Files

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 0.4: ATTRIBUTE SKELETONS                                              │
│  ═════════════════════════════                                              │
│                                                                             │
│       ┌─────────────────────────────────────────────────────┐               │
│       │                   FreeGLBA                          │               │
│       │                      │                              │               │
│       │     ┌────────────────┴────────────────┐             │               │
│       │     │                                 │             │               │
│       │     ▼                                 ▼             │               │
│       │  ┌──────────────────┐   ┌──────────────────────┐    │               │
│       │  │ ApiRequestLogging│   │ SkipApiLogging       │    │               │
│       │  │ Attribute.cs     │   │ Attribute.cs         │    │               │
│       │  │                  │   │                      │    │               │
│       │  │ ActionFilter     │   │ Marker Attribute     │    │               │
│       │  │ (does the work)  │   │ (just exists)        │    │               │
│       │  └──────────────────┘   └──────────────────────┘    │               │
│       └─────────────────────────────────────────────────────┘               │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- `ApiRequestLoggingAttribute` extends `ActionFilterAttribute`
- `SkipApiLoggingAttribute` is just a marker (empty class)
- Both compile but don't do anything yet

**Pseudo-code for `ApiRequestLoggingAttribute`:**
```csharp
namespace FreeGLBA.Controllers;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiRequestLoggingAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // TODO: Phase 2 - Implement
        base.OnActionExecuting(context);
    }
    
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        // TODO: Phase 2 - Implement
        base.OnActionExecuted(context);
    }
}
```

**Pseudo-code for `SkipApiLoggingAttribute`:**
```csharp
namespace FreeGLBA.Controllers;

[AttributeUsage(AttributeTargets.Method)]
public class SkipApiLoggingAttribute : Attribute
{
    // Marker attribute - no implementation needed
}
```

---

## Task 0.5: Create Razor Page Files

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 0.5: BLAZOR PAGE SKELETONS                                            │
│  ═══════════════════════════════                                            │
│                                                                             │
│       ┌─────────────────────────────────────────────────────────────┐       │
│       │                    FreeGLBA.Client                          │       │
│       │                          │                                  │       │
│       │    ┌─────────────────────┼─────────────────────┐            │       │
│       │    │                     │                     │            │       │
│       │    ▼                     ▼                     ▼            │       │
│       │  ┌──────────┐    ┌──────────────┐    ┌────────────────┐     │       │
│       │  │Dashboard │    │ List View    │    │ Detail View    │     │       │
│       │  │ .razor   │    │ .razor       │    │ .razor         │     │       │
│       │  └──────────┘    └──────────────┘    └────────────────┘     │       │
│       │        │                                                    │       │
│       │        ▼                                                    │       │
│       │  ┌──────────────────┐                                       │       │
│       │  │ Body Logging     │                                       │       │
│       │  │ Settings.razor   │                                       │       │
│       │  └──────────────────┘                                       │       │
│       └─────────────────────────────────────────────────────────────┘       │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- Minimal Razor pages with `@page` directive
- Basic layout structure
- "Coming Soon" placeholder content

**Pseudo-code for each page:**
```razor
@page "/api-logs/dashboard"
@* FreeGLBA.App.ApiLogDashboard.razor *@

<h1>API Log Dashboard</h1>
<p>🚧 Coming in Phase 3</p>

@code {
    // TODO: Phase 3 - Implement dashboard
}
```

```razor
@page "/api-logs"
@* FreeGLBA.App.ApiRequestLogs.razor *@

<h1>API Request Logs</h1>
<p>🚧 Coming in Phase 4</p>

@code {
    // TODO: Phase 4 - Implement list view
}
```

```razor
@page "/api-logs/{Id:guid}"
@* FreeGLBA.App.ViewApiRequestLog.razor *@

<h1>View API Request Log</h1>
<p>🚧 Coming in Phase 4</p>

@code {
    [Parameter] public Guid Id { get; set; }
    // TODO: Phase 4 - Implement detail view
}
```

```razor
@page "/api-logs/settings"
@* FreeGLBA.App.BodyLoggingSettings.razor *@

<h1>Body Logging Settings</h1>
<p>🚧 Coming in Phase 5</p>

@code {
    // TODO: Phase 5 - Implement settings
}
```

---

## Task 0.6: Update Existing Files

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 0.6: MODIFY EXISTING FILES                                            │
│  ═══════════════════════════════                                            │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                                                                     │    │
│  │   EFDataModel.cs ──► Add DbSet<ApiRequestLogItem>                   │    │
│  │                  ──► Add DbSet<BodyLoggingConfigItem>               │    │
│  │                                                                     │    │
│  │   IDataAccess.cs ──► Add interface method signatures               │    │
│  │                                                                     │    │
│  │   DataAccess.cs  ──► Already partial, just ensure it compiles      │    │
│  │                                                                     │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- Add new DbSet properties for new entities
- Add interface definitions for new methods
- Keep everything compiling

**Pseudo-code for `EFDataModel.cs` changes:**
```csharp
public partial class EFDataModel : DbContext
{
    // ...existing DbSets...
    
    // API Logging (Phase 0)
    public virtual DbSet<ApiRequestLogItem> ApiRequestLogs { get; set; } = null!;
    public virtual DbSet<BodyLoggingConfigItem> BodyLoggingConfigs { get; set; } = null!;
}
```

---

## Verification Checklist

After Phase 0, verify:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         PHASE 0 VERIFICATION                                │
└─────────────────────────────────────────────────────────────────────────────┘

  □ All 10 new files created in correct locations
  □ Solution builds without errors (dotnet build)
  □ All placeholder pages render (even if just "Coming Soon")
  □ DbSet properties added to EFDataModel
  □ No TODO comments without phase numbers
  
  BUILD CHECK:
  ════════════
  $ dotnet build FreeGLBA.sln
  
  Expected: Build succeeded. 0 Errors.
```

---

## Phase 0 Summary

| Metric | Value |
|--------|-------|
| Files Created | 10 |
| Files Modified | 3-5 |
| Estimated Time | 0.5 days |
| Dependencies | None |
| Deliverable | Compiling skeleton |

---

*Next Phase: [119 — Phase 1: Entity & DTO Implementation](119_impl_phase1_entities.md)*

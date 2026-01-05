# 123 — Implementation Plan: Phase 5 - Body Logging Settings & Database Maintenance

> **Document ID:** 123  
> **Category:** Implementation Plan  
> **Purpose:** Implement body logging settings UI and document database maintenance  
> **Phase:** 5 of 6  
> **Estimated Time:** 1.0 days  
> **Dependencies:** Phase 4 complete (list/detail views working)

---

## Phase Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│              PHASE 5: SETTINGS & DATABASE MAINTENANCE                       │
└─────────────────────────────────────────────────────────────────────────────┘

  TWO COMPONENTS
  ══════════════
  
  ┌─────────────────────────────────────┐    ┌─────────────────────────────────┐
  │     BODY LOGGING SETTINGS           │    │    DATABASE MAINTENANCE         │
  │                                     │    │    (External - SQL Scripts)     │
  │  ┌───────────────────────────────┐  │    │                                 │
  │  │ Per-source toggle             │  │    │  ┌───────────────────────────┐  │
  │  │ Time-limited (max 72h)        │  │    │  │ Scheduled SQL jobs        │  │
  │  │ Reason required               │  │    │  │ Delete logs > X days      │  │
  │  │ PII warning modal             │  │    │  │ DBA-managed retention     │  │
  │  │ Audit trail                   │  │    │  │ Flexible maintenance      │  │
  │  └───────────────────────────────┘  │    │  └───────────────────────────┘  │
  │                                     │    │  Design: External management   │
  └─────────────────────────────────────┘    └─────────────────────────────────┘
```

---

## Task 5.1: Create Body Logging Settings API

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 5.1: BODY LOGGING SETTINGS API                                        │
│  ═══════════════════════════════════                                        │
│                                                                             │
│   ENDPOINTS                                                                 │
│   ═════════                                                                 │
│                                                                             │
│   GET  /api/data/body-logging-configs                                       │
│   ├── Returns list of all configs (active + expired)                        │
│   └── For settings page table                                               │
│                                                                             │
│   GET  /api/data/body-logging-configs/{sourceSystemId}                      │
│   ├── Returns current config for source                                     │
│   └── Or null if not configured                                             │
│                                                                             │
│   POST /api/data/body-logging-configs                                       │
│   ├── Creates new body logging config                                       │
│   ├── Requires: sourceSystemId, durationHours, reason                       │
│   ├── Enforces max duration from config                                     │
│   └── Creates audit trail entry                                             │
│                                                                             │
│   DELETE /api/data/body-logging-configs/{id}                                │
│   ├── Disables (doesn't delete) config                                      │
│   └── Sets DisabledAt timestamp                                             │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- CRUD endpoints for body logging configuration
- Enforces time limits from options
- Creates audit trail automatically

**Pseudo-code:**
```csharp
// In DataController.ApiLogs.cs

[HttpGet("body-logging-configs")]
[SkipApiLogging]
public async Task<IActionResult> GetBodyLoggingConfigs()
{
    var configs = await _dataAccess.GetAllBodyLoggingConfigsAsync();
    return Ok(configs);
}

[HttpGet("body-logging-configs/{sourceSystemId:guid}")]
[SkipApiLogging]
public async Task<IActionResult> GetBodyLoggingConfig(Guid sourceSystemId)
{
    var config = await _dataAccess.GetBodyLoggingConfigAsync(sourceSystemId);
    return Ok(config); // Returns null if not found, which is OK
}

[HttpPost("body-logging-configs")]
public async Task<IActionResult> CreateBodyLoggingConfig(
    [FromBody] CreateBodyLoggingConfigRequest request)
{
    // Validate duration doesn't exceed max
    var maxHours = _options.MaxBodyLoggingDurationHours;
    if (request.DurationHours > maxHours)
    {
        return BadRequest($"Duration cannot exceed {maxHours} hours");
    }
    
    if (string.IsNullOrWhiteSpace(request.Reason))
    {
        return BadRequest("Reason is required for compliance");
    }
    
    // Get current user info
    var userId = User.GetUserId();
    var userName = User.GetUserName();
    
    var config = new BodyLoggingConfigItem
    {
        BodyLoggingConfigId = Guid.NewGuid(),
        SourceSystemId = request.SourceSystemId,
        EnabledByUserId = userId,
        EnabledByUserName = userName,
        EnabledAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddHours(request.DurationHours),
        IsActive = true,
        Reason = request.Reason
    };
    
    await _dataAccess.CreateBodyLoggingConfigAsync(config);
    
    return Ok(BodyLoggingConfigDto.FromEntity(config));
}

[HttpDelete("body-logging-configs/{id:guid}")]
public async Task<IActionResult> DisableBodyLoggingConfig(Guid id)
{
    var config = await _dataAccess.GetBodyLoggingConfigByIdAsync(id);
    
    if (config == null)
        return NotFound();
    
    config.IsActive = false;
    config.DisabledAt = DateTime.UtcNow;
    
    await _dataAccess.UpdateBodyLoggingConfigAsync(config);
    
    return Ok();
}

// Request model
public class CreateBodyLoggingConfigRequest
{
    public Guid SourceSystemId { get; set; }
    public int DurationHours { get; set; }
    public string Reason { get; set; } = string.Empty;
}
```

---

## Task 5.2: Create Body Logging Settings Page

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 5.2: BODY LOGGING SETTINGS PAGE                                       │
│  ════════════════════════════════════                                       │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │                    BODY LOGGING SETTINGS                            │   │
│   ├─────────────────────────────────────────────────────────────────────┤   │
│   │                                                                     │   │
│   │   ⚠️ WARNING: Body logging may capture personally identifiable      │   │
│   │   information (PII). Enable only when necessary for debugging.      │   │
│   │   All changes are logged for compliance audit.                      │   │
│   │                                                                     │   │
│   │   CURRENT CONFIGURATIONS                                            │   │
│   │   ══════════════════════                                            │   │
│   │   ┌──────────┬──────────────┬────────────┬──────────────┬────────┐  │   │
│   │   │ Source   │ Enabled By   │ Expires    │ Reason       │ Action │  │   │
│   │   ├──────────┼──────────────┼────────────┼──────────────┼────────┤  │   │
│   │   │ Banner   │ John D.      │ 2h 15m     │ Debug #1234  │[Disable]│ │   │
│   │   │ HR       │ Jane S.      │ EXPIRED    │ Issue #5678  │[Remove] │  │   │
│   │   └──────────┴──────────────┴────────────┴──────────────┴────────┘  │   │
│   │                                                                     │   │
│   │   ENABLE BODY LOGGING                                               │   │
│   │   ═══════════════════                                               │   │
│   │   ┌────────────────────────────────────────────────────────────┐    │   │
│   │   │ Source System:  [Banner                              ▼]    │    │   │
│   │   │ Duration:       [24 hours                            ▼]    │    │   │
│   │   │ Reason:         [________________________________________________] │   │
│   │   │                 (Required - ticket # or justification)     │    │   │
│   │   │                                                            │    │   │
│   │   │                                    [Enable Body Logging]   │    │   │
│   │   └────────────────────────────────────────────────────────────┘    │   │
│   │                                                                     │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- Settings page for body logging configs
- PII warning prominently displayed
- Shows audit trail (who enabled, when)

**Pseudo-code:**
```razor
@page "/api-logs/settings"
@* FreeGLBA.App.BodyLoggingSettings.razor *@

@inject HttpClient Http
@inject IJSRuntime JS

<PageTitle>Body Logging Settings</PageTitle>

<h1>Body Logging Settings</h1>

<!-- PII Warning Banner -->
<div class="alert alert-warning" role="alert">
    <h4 class="alert-heading">
        <i class="bi bi-exclamation-triangle"></i> Privacy Notice
    </h4>
    <p>
        Body logging captures the full request and response content, which may include 
        <strong>personally identifiable information (PII)</strong>. Enable only when 
        necessary for debugging specific issues.
    </p>
    <hr>
    <p class="mb-0">
        All body logging configuration changes are recorded for compliance audit. 
        You must provide a reason (ticket # or justification) when enabling.
    </p>
</div>

<!-- Current Configurations -->
<div class="card mb-4">
    <div class="card-header">
        <i class="bi bi-list-check"></i> Current Body Logging Configurations
    </div>
    <div class="card-body">
        @if (_configs.Any())
        {
            <table class="table">
                <thead>
                    <tr>
                        <th>Source System</th>
                        <th>Enabled By</th>
                        <th>Enabled At</th>
                        <th>Expires</th>
                        <th>Reason</th>
                        <th>Status</th>
                        <th>Action</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var config in _configs)
                    {
                        <tr class="@(config.IsActive ? "" : "table-secondary")">
                            <td>@GetSourceName(config.SourceSystemId)</td>
                            <td>@config.EnabledByUserName</td>
                            <td>@config.EnabledAt.ToString("MM/dd HH:mm")</td>
                            <td>
                                @if (config.IsActive && config.ExpiresAt > DateTime.UtcNow)
                                {
                                    <span class="badge bg-success">
                                        @GetTimeRemaining(config.ExpiresAt)
                                    </span>
                                }
                                else
                                {
                                    <span class="badge bg-secondary">EXPIRED</span>
                                }
                            </td>
                            <td>@config.Reason</td>
                            <td>
                                @if (config.IsActive)
                                {
                                    <span class="badge bg-success">Active</span>
                                }
                                else
                                {
                                    <span class="badge bg-secondary">Disabled</span>
                                }
                            </td>
                            <td>
                                @if (config.IsActive)
                                {
                                    <button class="btn btn-sm btn-outline-danger"
                                            @onclick="() => DisableConfig(config.BodyLoggingConfigId)">
                                        Disable
                                    </button>
                                }
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        }
        else
        {
            <p class="text-muted">No body logging configurations. Body content is not being captured.</p>
        }
    </div>
</div>

<!-- Enable Form -->
<div class="card">
    <div class="card-header">
        <i class="bi bi-plus-circle"></i> Enable Body Logging
    </div>
    <div class="card-body">
        <EditForm Model="_newConfig" OnValidSubmit="EnableBodyLogging">
            <DataAnnotationsValidator />
            
            <div class="row mb-3">
                <div class="col-md-4">
                    <label class="form-label">Source System</label>
                    <InputSelect class="form-select" @bind-Value="_newConfig.SourceSystemId">
                        <option value="">-- Select Source --</option>
                        @foreach (var source in _sourceSystems)
                        {
                            <option value="@source.SourceSystemId">@source.Name</option>
                        }
                    </InputSelect>
                    <ValidationMessage For="() => _newConfig.SourceSystemId" />
                </div>
                
                <div class="col-md-4">
                    <label class="form-label">Duration</label>
                    <InputSelect class="form-select" @bind-Value="_newConfig.DurationHours">
                        <option value="1">1 hour</option>
                        <option value="4">4 hours</option>
                        <option value="8">8 hours</option>
                        <option value="24">24 hours</option>
                        <option value="48">48 hours</option>
                        <option value="72">72 hours (maximum)</option>
                    </InputSelect>
                </div>
            </div>
            
            <div class="mb-3">
                <label class="form-label">Reason (Required)</label>
                <InputText class="form-control" @bind-Value="_newConfig.Reason" 
                           placeholder="Ticket #, issue description, or justification" />
                <ValidationMessage For="() => _newConfig.Reason" />
                <div class="form-text">
                    This will be recorded in the compliance audit trail.
                </div>
            </div>
            
            <button type="submit" class="btn btn-warning" disabled="@_submitting">
                @if (_submitting)
                {
                    <span class="spinner-border spinner-border-sm me-2"></span>
                }
                <i class="bi bi-exclamation-triangle me-1"></i>
                Enable Body Logging
            </button>
        </EditForm>
    </div>
</div>

<!-- Confirmation Modal -->
@if (_showConfirmModal)
{
    <div class="modal show d-block" tabindex="-1" style="background: rgba(0,0,0,0.5)">
        <div class="modal-dialog">
            <div class="modal-content">
                <div class="modal-header bg-warning">
                    <h5 class="modal-title">
                        <i class="bi bi-exclamation-triangle"></i> Confirm Body Logging
                    </h5>
                </div>
                <div class="modal-body">
                    <p>You are about to enable body logging for:</p>
                    <ul>
                        <li><strong>Source:</strong> @GetSourceName(_newConfig.SourceSystemId)</li>
                        <li><strong>Duration:</strong> @_newConfig.DurationHours hours</li>
                        <li><strong>Reason:</strong> @_newConfig.Reason</li>
                    </ul>
                    <div class="alert alert-danger">
                        <strong>Warning:</strong> Request and response bodies may contain 
                        PII (names, IDs, financial data). Ensure this is necessary.
                    </div>
                </div>
                <div class="modal-footer">
                    <button class="btn btn-secondary" @onclick="() => _showConfirmModal = false">
                        Cancel
                    </button>
                    <button class="btn btn-warning" @onclick="ConfirmEnable">
                        Yes, Enable Body Logging
                    </button>
                </div>
            </div>
        </div>
    </div>
}

@code {
    private List<BodyLoggingConfigDto> _configs = new();
    private List<SourceSystemItem> _sourceSystems = new();
    private CreateBodyLoggingConfigRequest _newConfig = new() { DurationHours = 24 };
    private bool _submitting = false;
    private bool _showConfirmModal = false;
    
    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }
    
    private async Task LoadData()
    {
        _configs = await Http.GetFromJsonAsync<List<BodyLoggingConfigDto>>(
            "api/data/body-logging-configs") ?? new();
        _sourceSystems = await Http.GetFromJsonAsync<List<SourceSystemItem>>(
            "api/data/source-systems") ?? new();
    }
    
    private void EnableBodyLogging()
    {
        // Validate first
        if (_newConfig.SourceSystemId == Guid.Empty || 
            string.IsNullOrWhiteSpace(_newConfig.Reason))
        {
            return;
        }
        
        // Show confirmation modal
        _showConfirmModal = true;
    }
    
    private async Task ConfirmEnable()
    {
        _showConfirmModal = false;
        _submitting = true;
        
        try
        {
            await Http.PostAsJsonAsync("api/data/body-logging-configs", _newConfig);
            _newConfig = new() { DurationHours = 24 };
            await LoadData();
        }
        finally
        {
            _submitting = false;
        }
    }
    
    private async Task DisableConfig(Guid id)
    {
        await Http.DeleteAsync($"api/data/body-logging-configs/{id}");
        await LoadData();
    }
    
    private string GetSourceName(Guid id) =>
        _sourceSystems.FirstOrDefault(s => s.SourceSystemId == id)?.Name ?? "Unknown";
    
    private string GetTimeRemaining(DateTime expiresAt)
    {
        var remaining = expiresAt - DateTime.UtcNow;
        if (remaining.TotalHours >= 1)
            return $"{remaining.Hours}h {remaining.Minutes}m remaining";
        return $"{remaining.Minutes}m remaining";
    }
}
```

---

## Task 5.3: Database Maintenance Recommendations

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 5.3: DATABASE MAINTENANCE RECOMMENDATIONS                             │
│  ══════════════════════════════════════════════                             │
│                                                                             │
│   Database cleanup is handled externally by your DBA team or                │
│   scheduled database maintenance jobs. This approach provides:              │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │                                                                     │   │
│   │   ✓ Flexibility in retention policies per environment              │   │
│   │   ✓ Control over maintenance windows                                │   │
│   │   ✓ Integration with existing DBA monitoring                        │   │
│   │   ✓ No application-level resource consumption                       │   │
│   │   ✓ Batch processing to avoid lock contention                       │   │
│   │                                                                     │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│   RECOMMENDED ACTIONS:                                                      │
│   ════════════════════                                                      │
│                                                                             │
│   1. Run log cleanup daily (preferably during off-hours)                    │
│   2. Auto-disable expired BodyLoggingConfigs daily                          │
│   3. Monitor table size and adjust retention as needed                      │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- SQL scripts for external database maintenance
- Run via SQL Agent Job, Azure Automation, or other scheduler
- DBA team manages retention policies and execution timing

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
-- SCRIPT 3: Get storage statistics (for monitoring/capacity planning)
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
        DECLARE @CutoffDate DATETIME2 = DATEADD(DAY, -@RetentionDays, GETUTCDATE());
        DECLARE @BatchSize INT = 10000;
        DECLARE @DeletedCount INT = 1;
        
        WHILE @DeletedCount > 0
        BEGIN
            DELETE TOP (@BatchSize) FROM [dbo].[ApiRequestLogs]
            WHERE [RequestedAt] < @CutoffDate;
            SET @DeletedCount = @@ROWCOUNT;
            IF @DeletedCount > 0 WAITFOR DELAY ''00:00:01'';
        END
        
        -- Also disable expired body logging configs
        UPDATE [dbo].[BodyLoggingConfigs]
        SET [IsActive] = 0, [DisabledAt] = GETUTCDATE()
        WHERE [IsActive] = 1 AND [ExpiresAt] < GETUTCDATE();
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

## Task 5.4: Register Services in Program.cs

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 5.4: SERVICE REGISTRATION                                             │
│  ══════════════════════════════                                             │
│                                                                             │
│   Program.cs additions:                                                     │
│   ═════════════════════                                                     │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │                                                                     │   │
│   │  // Configuration binding                                           │   │
│   │  builder.Services.Configure<ApiLoggingOptions>(                     │   │
│   │      builder.Configuration.GetSection("ApiLogging"));               │   │
│   │                                                                     │   │
│   │  // NOTE: No background cleanup service - handled externally        │   │
│   │                                                                     │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│   appsettings.json additions:                                               │
│   ═══════════════════════════                                               │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │  {                                                                  │   │
│   │    "ApiLogging": {                                                  │   │
│   │      "BodyLogLimit": 4096,                                          │   │
│   │      "MaxBodyLoggingDurationHours": 72,                             │   │
│   │      "DefaultBodyLoggingDurationHours": 24,                         │   │
│   │      "MaxExportRows": 10000,                                        │   │
│   │      "DashboardRefreshSeconds": 30,                                 │   │
│   │      "SensitiveHeaders": [                                          │   │
│   │        "Authorization",                                             │   │
│   │        "X-Api-Key",                                                 │   │
│   │        "Cookie",                                                    │   │
│   │        "Set-Cookie"                                                 │   │
│   │      ]                                                              │   │
│   │    }                                                                │   │
│   │  }                                                                  │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│   NOTE: RetentionDays removed - managed externally via SQL scripts          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- Register configuration options
- Note: No background cleanup service (handled externally)
- Configuration in appsettings.json

**Pseudo-code:**
```csharp
// In Program.cs

// === API LOGGING CONFIGURATION ===
builder.Services.Configure<ApiLoggingOptions>(
    builder.Configuration.GetSection("ApiLogging"));

// NOTE: Database cleanup (log retention) is handled externally
// via scheduled SQL jobs managed by your DBA team.
// See doc 123 for recommended SQL scripts.
```

```json
// In appsettings.json
{
  "ApiLogging": {
    "BodyLogLimit": 4096,
    "MaxBodyLoggingDurationHours": 72,
    "DefaultBodyLoggingDurationHours": 24,
    "MaxExportRows": 10000,
    "DashboardRefreshSeconds": 30,
    "SensitiveHeaders": [
      "Authorization",
      "X-Api-Key",
      "Cookie",
      "Set-Cookie",
      "X-Auth-Token"
    ]
  }
}
```

---

## Phase 5 Files Summary

| File | Action | Key Changes |
|------|--------|-------------|
| `DataController.ApiLogs.cs` | MODIFY | Add body logging config endpoints |
| `FreeGLBA.App.DataAccess.ApiLogging.cs` | MODIFY | Add config CRUD methods |
| `FreeGLBA.App.BodyLoggingSettings.razor` | MODIFY | Full settings page implementation |
| `Program.cs` | MODIFY | Register options |
| `appsettings.json` | MODIFY | Add ApiLogging configuration |
| N/A (SQL Scripts) | DOCUMENT | Provide to DBA for scheduled execution |

---

## Verification Checklist

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         PHASE 5 VERIFICATION                                │
└─────────────────────────────────────────────────────────────────────────────┘

  SETTINGS PAGE:
  ══════════════
  □ Settings page loads at /api-logs/settings
  □ PII warning is prominently displayed
  □ Current configurations table shows active configs
  □ Can enable body logging for a source system
  □ Confirmation modal appears before enabling
  □ Reason is required and validated
  □ Duration is limited to max (72 hours)
  □ Can disable active body logging config
  □ Audit trail info displayed (who, when, why)
  
  DATABASE MAINTENANCE (External):
  ════════════════════════════════
  □ SQL cleanup scripts provided to DBA team
  □ Scripts tested in dev environment
  □ SQL Agent Job (or equivalent) scheduled
  □ DBA confirms retention policy (e.g., 90 days)
  
  CONFIGURATION:
  ═══════════════
  □ ApiLogging section in appsettings.json
  □ Options bound correctly
  □ Defaults work if section missing
  
  MANUAL TEST:
  ════════════
  1. Go to /api-logs/settings
  2. Enable body logging for Banner (4 hours, reason: "Testing")
  3. Confirm modal appears and enable
  4. Make API request, verify body is captured
  5. Disable the config
  6. Make another request, verify body NOT captured
```

---

## Phase 5 Summary

| Metric | Value |
|--------|-------|
| Files Modified | 4 |
| Files Created | 0 |
| SQL Scripts | Documented for DBA |
| Lines of Code | ~350 |
| Key Features | Settings UI |
| DB Cleanup | External (SQL scripts) |
| Estimated Time | 1.0 days |
| Dependencies | Phase 4 |
| Deliverable | Complete settings |

---

*Previous: [122 — Phase 4: List & Detail Views](122_impl_phase4_listdetail.md)*  
*Next: [124 — Phase 6: Testing & Wrap-up](124_impl_phase6_testing.md)*

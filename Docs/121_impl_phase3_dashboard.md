# 121 — Implementation Plan: Phase 3 - Dashboard View

> **Document ID:** 121  
> **Category:** Implementation Plan  
> **Purpose:** Implement the API Log Dashboard with statistics and visualizations  
> **Phase:** 3 of 6  
> **Estimated Time:** 1.5 days  
> **Dependencies:** Phase 2 complete (logging is working)

---

## Phase Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       PHASE 3: DASHBOARD VIEW                               │
└─────────────────────────────────────────────────────────────────────────────┘

  FOCUS GROUP PRIORITY #1
  ═══════════════════════
  
  "We need to see at a glance if something is wrong"
                                    - Focus Group Participant

  ┌─────────────────────────────────────────────────────────────────────────┐
  │                                                                         │
  │   ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐               │
  │   │  8,421   │  │   127    │  │  1.5%    │  │  234ms   │               │
  │   │ REQUESTS │  │  ERRORS  │  │ERROR RATE│  │AVG TIME  │               │
  │   └──────────┘  └──────────┘  └──────────┘  └──────────┘               │
  │                                                                         │
  │   ┌─────────────────────────────────────────────────────────────────┐   │
  │   │          REQUEST RATE OVER TIME (Line Chart)                    │   │
  │   └─────────────────────────────────────────────────────────────────┘   │
  │                                                                         │
  │   ┌─────────────────────┐  ┌────────────────────────────────────────┐   │
  │   │ BY SOURCE SYSTEM    │  │ BY STATUS CODE                        │   │
  │   │ (Bar Chart)         │  │ (Pie Chart)                           │   │
  │   └─────────────────────┘  └────────────────────────────────────────┘   │
  │                                                                         │
  │   ┌─────────────────────────────────────────────────────────────────┐   │
  │   │          RECENT ERRORS (Table)                                  │   │
  │   └─────────────────────────────────────────────────────────────────┘   │
  │                                                                         │
  └─────────────────────────────────────────────────────────────────────────┘
```

---

## Task 3.1: Implement Dashboard Stats Query

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 3.1: DASHBOARD STATS QUERY                                            │
│  ═══════════════════════════════                                            │
│                                                                             │
│   DATABASE                              AGGREGATED STATS                    │
│   ════════                              ════════════════                    │
│                                                                             │
│   ┌─────────────────┐                  ┌─────────────────────────────┐      │
│   │ ApiRequestLogs  │                  │ DashboardStatsDto           │      │
│   │                 │                  │                             │      │
│   │ 50,000 rows     │──── GROUP BY ───►│ TotalRequests: 8421        │      │
│   │ (last 24 hrs)   │     AGGREGATE    │ TotalErrors: 127           │      │
│   │                 │                  │ ErrorRate: 1.5%            │      │
│   └─────────────────┘                  │ AvgDurationMs: 234         │      │
│                                        │                             │      │
│                                        │ BySourceSystem: [...]       │      │
│                                        │ ByStatusCode: [...]         │      │
│                                        │ RequestsOverTime: [...]     │      │
│                                        │ RecentErrors: [5 items]     │      │
│                                        └─────────────────────────────┘      │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- Single efficient query to get all dashboard data
- Aggregations done at database level
- Returns DTO with all stats

**Pseudo-code:**
```csharp
// In DataAccess.ApiLogging.cs

public async Task<DashboardStatsDto> GetApiLogDashboardStatsAsync(
    DateTime fromDate, 
    DateTime toDate)
{
    var stats = new DashboardStatsDto();
    
    // Base query for time range
    var logsQuery = _context.ApiRequestLogs
        .Where(l => l.RequestedAt >= fromDate && l.RequestedAt <= toDate);
    
    // === SUMMARY STATS ===
    stats.TotalRequests = await logsQuery.CountAsync();
    stats.TotalErrors = await logsQuery.CountAsync(l => !l.IsSuccess);
    stats.ErrorRate = stats.TotalRequests > 0 
        ? (double)stats.TotalErrors / stats.TotalRequests * 100 
        : 0;
    stats.AvgDurationMs = stats.TotalRequests > 0
        ? await logsQuery.AverageAsync(l => (double)l.DurationMs)
        : 0;
    
    // === BY SOURCE SYSTEM ===
    stats.BySourceSystem = await logsQuery
        .GroupBy(l => l.SourceSystemName)
        .Select(g => new SourceSystemStats
        {
            SourceSystemName = g.Key,
            Count = g.Count(),
            Percentage = 0 // Calculate after
        })
        .OrderByDescending(s => s.Count)
        .Take(10)
        .ToListAsync();
    
    // Calculate percentages
    foreach (var source in stats.BySourceSystem)
    {
        source.Percentage = (double)source.Count / stats.TotalRequests * 100;
    }
    
    // === BY STATUS CODE ===
    stats.ByStatusCode = await logsQuery
        .GroupBy(l => l.StatusCode / 100)  // Group by 2xx, 4xx, 5xx
        .Select(g => new StatusCodeStats
        {
            Category = g.Key + "xx",
            Count = g.Count(),
            Percentage = 0
        })
        .ToListAsync();
    
    // === REQUESTS OVER TIME ===
    // Group by hour for 24h, by day for longer ranges
    var timeSpan = toDate - fromDate;
    var groupByMinutes = timeSpan.TotalHours <= 24 ? 60 : 1440; // Hour or day
    
    stats.RequestsOverTime = await logsQuery
        .GroupBy(l => new 
        { 
            Bucket = EF.Functions.DateDiffMinute(DateTime.MinValue, l.RequestedAt) / groupByMinutes 
        })
        .Select(g => new TimeSeriesPoint
        {
            Timestamp = g.Min(l => l.RequestedAt),
            Count = g.Count()
        })
        .OrderBy(p => p.Timestamp)
        .ToListAsync();
    
    // === RECENT ERRORS ===
    stats.RecentErrors = await logsQuery
        .Where(l => !l.IsSuccess)
        .OrderByDescending(l => l.RequestedAt)
        .Take(10)
        .Select(l => new ApiRequestLogListDto
        {
            ApiRequestLogId = l.ApiRequestLogId,
            SourceSystemName = l.SourceSystemName,
            HttpMethod = l.HttpMethod,
            RequestPath = l.RequestPath,
            RequestedAt = l.RequestedAt,
            StatusCode = l.StatusCode,
            ErrorMessage = l.ErrorMessage
        })
        .ToListAsync();
    
    return stats;
}
```

---

## Task 3.2: Create Dashboard API Endpoint

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 3.2: DASHBOARD API ENDPOINT                                           │
│  ════════════════════════════════                                           │
│                                                                             │
│   BLAZOR                  API                         DATABASE              │
│   ══════                  ═══                         ════════              │
│                                                                             │
│   ┌─────────────┐      ┌─────────────┐             ┌─────────────┐          │
│   │ Dashboard   │      │ GET /api/   │             │ Aggregate   │          │
│   │ .razor      │─────►│ data/api-   │────────────►│ Query       │          │
│   │             │      │ logs/stats  │             │             │          │
│   └─────────────┘      └──────┬──────┘             └─────────────┘          │
│         ▲                     │                                             │
│         │                     ▼                                             │
│         │              ┌─────────────┐                                      │
│         └──────────────│ Dashboard   │                                      │
│                        │ StatsDto    │                                      │
│          JSON          │ (JSON)      │                                      │
│                        └─────────────┘                                      │
│                                                                             │
│   [SkipApiLogging] - Don't log requests to this endpoint                    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- API endpoint to return dashboard statistics
- Accepts time range parameters
- Marked with `[SkipApiLogging]`

**Pseudo-code:**
```csharp
// In DataController (new partial file: DataController.ApiLogs.cs)

public partial class DataController
{
    [HttpGet("api-logs/stats")]
    [SkipApiLogging(Reason = "Dashboard stats endpoint")]
    public async Task<IActionResult> GetApiLogStats(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? timeRange = "24h")
    {
        // Parse time range
        var to = toDate ?? DateTime.UtcNow;
        var from = fromDate ?? timeRange switch
        {
            "1h" => to.AddHours(-1),
            "24h" => to.AddHours(-24),
            "7d" => to.AddDays(-7),
            "30d" => to.AddDays(-30),
            _ => to.AddHours(-24)
        };
        
        var stats = await _dataAccess.GetApiLogDashboardStatsAsync(from, to);
        return Ok(stats);
    }
}
```

---

## Task 3.3: Create Dashboard Blazor Page

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 3.3: DASHBOARD BLAZOR PAGE                                            │
│  ═══════════════════════════════                                            │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │  @page "/api-logs/dashboard"                                        │   │
│   │                                                                     │   │
│   │  ┌───────────────────────────────────────────────────────────────┐  │   │
│   │  │  [Auto-refresh: OFF ▼]          Time Range: [Last 24h ▼]     │  │   │
│   │  └───────────────────────────────────────────────────────────────┘  │   │
│   │                                                                     │   │
│   │  @if (_loading)                                                     │   │
│   │  {                                                                  │   │
│   │      <LoadingSpinner />                                             │   │
│   │  }                                                                  │   │
│   │  else                                                               │   │
│   │  {                                                                  │   │
│   │      <SummaryCards Stats="@_stats" />                               │   │
│   │      <RequestRateChart Data="@_stats.RequestsOverTime" />           │   │
│   │      <SourceSystemChart Data="@_stats.BySourceSystem" />            │   │
│   │      <StatusCodeChart Data="@_stats.ByStatusCode" />                │   │
│   │      <RecentErrorsTable Errors="@_stats.RecentErrors" />            │   │
│   │  }                                                                  │   │
│   │                                                                     │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- Blazor page at `/api-logs/dashboard`
- Fetches stats from API on load
- Auto-refresh toggle (SysAdmin request)

**Pseudo-code:**
```razor
@page "/api-logs/dashboard"
@* FreeGLBA.App.ApiLogDashboard.razor *@

@inject HttpClient Http
@inject NavigationManager Nav

<PageTitle>API Log Dashboard</PageTitle>

<div class="dashboard-header">
    <h1>API Request Log Dashboard</h1>
    
    <div class="dashboard-controls">
        <!-- Auto-refresh toggle -->
        <div class="form-check form-switch">
            <input class="form-check-input" type="checkbox" 
                   @bind="_autoRefresh" @bind:after="OnAutoRefreshChanged" />
            <label class="form-check-label">Auto-refresh</label>
        </div>
        
        <!-- Time range selector -->
        <select class="form-select" @bind="_timeRange" @bind:after="LoadStats">
            <option value="1h">Last Hour</option>
            <option value="24h">Last 24 Hours</option>
            <option value="7d">Last 7 Days</option>
            <option value="30d">Last 30 Days</option>
        </select>
    </div>
</div>

@if (_loading)
{
    <div class="loading-spinner">
        <div class="spinner-border" role="status">
            <span class="visually-hidden">Loading...</span>
        </div>
    </div>
}
else if (_stats != null)
{
    <!-- Summary Cards -->
    <div class="row summary-cards">
        <div class="col-md-3">
            <div class="card text-center">
                <div class="card-body">
                    <h2 class="display-4">@_stats.TotalRequests.ToString("N0")</h2>
                    <p class="text-muted">Total Requests</p>
                </div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="card text-center @(_stats.TotalErrors > 0 ? "border-warning" : "")">
                <div class="card-body">
                    <h2 class="display-4 @(_stats.TotalErrors > 0 ? "text-warning" : "")">
                        @_stats.TotalErrors.ToString("N0")
                    </h2>
                    <p class="text-muted">Errors</p>
                </div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="card text-center">
                <div class="card-body">
                    <h2 class="display-4">@_stats.ErrorRate.ToString("F1")%</h2>
                    <p class="text-muted">Error Rate</p>
                </div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="card text-center">
                <div class="card-body">
                    <h2 class="display-4">@_stats.AvgDurationMs.ToString("F0")ms</h2>
                    <p class="text-muted">Avg Duration</p>
                </div>
            </div>
        </div>
    </div>
    
    <!-- Charts would go here -->
    <!-- Using simple tables/bars for MVP, could add Chart.js later -->
    
    <!-- Recent Errors Table -->
    <div class="card mt-4">
        <div class="card-header d-flex justify-content-between">
            <span>Recent Errors</span>
            <a href="/api-logs?errorsOnly=true">View All →</a>
        </div>
        <div class="card-body">
            <table class="table table-hover">
                <thead>
                    <tr>
                        <th>Time</th>
                        <th>Source</th>
                        <th>Method</th>
                        <th>Path</th>
                        <th>Status</th>
                        <th>Error</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var error in _stats.RecentErrors)
                    {
                        <tr @onclick="() => ViewLog(error.ApiRequestLogId)" style="cursor: pointer">
                            <td>@error.RequestedAt.ToString("HH:mm:ss")</td>
                            <td>@error.SourceSystemName</td>
                            <td>@error.HttpMethod</td>
                            <td>@TruncatePath(error.RequestPath)</td>
                            <td><span class="badge bg-danger">@error.StatusCode</span></td>
                            <td>@TruncateError(error.ErrorMessage)</td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
    </div>
}

@code {
    private DashboardStatsDto? _stats;
    private bool _loading = true;
    private string _timeRange = "24h";
    private bool _autoRefresh = false;
    private Timer? _refreshTimer;
    
    protected override async Task OnInitializedAsync()
    {
        await LoadStats();
    }
    
    private async Task LoadStats()
    {
        _loading = true;
        StateHasChanged();
        
        try
        {
            _stats = await Http.GetFromJsonAsync<DashboardStatsDto>(
                $"api/data/api-logs/stats?timeRange={_timeRange}");
        }
        catch (Exception ex)
        {
            // Handle error
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }
    
    private void OnAutoRefreshChanged()
    {
        if (_autoRefresh)
        {
            _refreshTimer = new Timer(async _ => 
            {
                await LoadStats();
            }, null, 30000, 30000); // 30 seconds
        }
        else
        {
            _refreshTimer?.Dispose();
            _refreshTimer = null;
        }
    }
    
    private void ViewLog(Guid id)
    {
        Nav.NavigateTo($"/api-logs/{id}");
    }
    
    private string TruncatePath(string path) => 
        path.Length > 30 ? path[..30] + "..." : path;
    
    private string TruncateError(string error) => 
        error.Length > 40 ? error[..40] + "..." : error;
    
    public void Dispose()
    {
        _refreshTimer?.Dispose();
    }
}
```

---

## Task 3.4: Add Simple Charts

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 3.4: SIMPLE CHART COMPONENTS                                          │
│  ═════════════════════════════════                                          │
│                                                                             │
│   For MVP: Use CSS-based progress bars instead of Chart.js                  │
│   (Can upgrade to Chart.js in V2 if needed)                                 │
│                                                                             │
│   BY SOURCE SYSTEM                     BY STATUS CODE                       │
│   ════════════════                     ══════════════                       │
│                                                                             │
│   Banner   ████████████████ 50%        ✓ 2xx ████████████████████ 92%      │
│   HR       ████████ 25%                ⚠ 4xx ███ 5%                        │
│   FinAid   ██████ 15%                  ✗ 5xx █ 2%                          │
│   Other    ████ 10%                    ⚡ Other █ 1%                        │
│                                                                             │
│   Implementation: Bootstrap progress bars with percentages                  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- CSS-based progress bar charts
- Bootstrap styling
- Simple but effective visualization

**Pseudo-code:**
```razor
<!-- Source System Chart Component -->
<div class="card">
    <div class="card-header">Requests by Source System</div>
    <div class="card-body">
        @foreach (var source in Stats.BySourceSystem)
        {
            <div class="mb-2">
                <div class="d-flex justify-content-between">
                    <span>@source.SourceSystemName</span>
                    <span>@source.Percentage.ToString("F1")%</span>
                </div>
                <div class="progress" style="height: 20px;">
                    <div class="progress-bar" 
                         role="progressbar" 
                         style="width: @source.Percentage%"
                         aria-valuenow="@source.Percentage">
                        @source.Count.ToString("N0")
                    </div>
                </div>
            </div>
        }
    </div>
</div>

<!-- Status Code Chart Component -->
<div class="card">
    <div class="card-header">Requests by Status</div>
    <div class="card-body">
        @foreach (var status in Stats.ByStatusCode)
        {
            var color = status.Category switch
            {
                "2xx" => "bg-success",
                "3xx" => "bg-info",
                "4xx" => "bg-warning",
                "5xx" => "bg-danger",
                _ => "bg-secondary"
            };
            
            <div class="mb-2">
                <div class="d-flex justify-content-between">
                    <span>@status.Category</span>
                    <span>@status.Percentage.ToString("F1")%</span>
                </div>
                <div class="progress" style="height: 20px;">
                    <div class="progress-bar @color" 
                         role="progressbar" 
                         style="width: @status.Percentage%">
                        @status.Count.ToString("N0")
                    </div>
                </div>
            </div>
        }
    </div>
</div>
```

---

## Phase 3 Files Summary

| File | Action | Key Changes |
|------|--------|-------------|
| `FreeGLBA.App.DataAccess.ApiLogging.cs` | MODIFY | Add GetDashboardStats method |
| `DataController.ApiLogs.cs` | CREATE | New partial file for API log endpoints |
| `FreeGLBA.App.ApiLogDashboard.razor` | MODIFY | Full dashboard implementation |

---

## Verification Checklist

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         PHASE 3 VERIFICATION                                │
└─────────────────────────────────────────────────────────────────────────────┘

  □ Dashboard loads at /api-logs/dashboard
  □ Summary cards show correct counts
  □ Time range selector filters data
  □ Auto-refresh toggle works (30 second interval)
  □ Source system breakdown displays
  □ Status code breakdown displays  
  □ Recent errors table populates
  □ Clicking error row navigates to detail view
  □ API endpoint has [SkipApiLogging]
  □ Dashboard performs well (< 1 second load)
  
  MANUAL TEST:
  ════════════
  1. Generate some API traffic (POST /api/glba/events)
  2. Generate some errors (invalid requests)
  3. Navigate to /api-logs/dashboard
  4. Verify all sections populate correctly
  5. Change time range, verify data updates
  6. Enable auto-refresh, wait 30s, verify refresh
```

---

## Phase 3 Summary

| Metric | Value |
|--------|-------|
| Files Modified | 2 |
| Files Created | 1 |
| Lines of Code | ~400 |
| Key Feature | Visual dashboard |
| Estimated Time | 1.5 days |
| Dependencies | Phase 2 |
| Deliverable | Working dashboard |

---

*Previous: [120 — Phase 2: Logging Attribute & DataAccess](120_impl_phase2_attribute.md)*  
*Next: [122 — Phase 4: List & Detail Views](122_impl_phase4_listdetail.md)*

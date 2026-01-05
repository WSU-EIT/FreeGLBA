# 122 — Implementation Plan: Phase 4 - List & Detail Views

> **Document ID:** 122  
> **Category:** Implementation Plan  
> **Purpose:** Implement the list view with filters and detail view  
> **Phase:** 4 of 6  
> **Estimated Time:** 1.5 days  
> **Dependencies:** Phase 3 complete (dashboard working)

---

## Phase Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     PHASE 4: LIST & DETAIL VIEWS                            │
└─────────────────────────────────────────────────────────────────────────────┘

  USER FLOW
  ═════════
  
  ┌─────────────┐     ┌─────────────────┐     ┌─────────────────┐
  │  Dashboard  │     │   List View     │     │  Detail View    │
  │             │────►│                 │────►│                 │
  │  "127       │     │  Filter & Find  │     │  Full Request   │
  │   errors"   │     │  the one you    │     │  Details        │
  │             │     │  want           │     │                 │
  └─────────────┘     └─────────────────┘     └─────────────────┘
       Click              Click row              Investigate
       error                                     problem
```

---

## Task 4.1: Implement List Query with Filters

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 4.1: LIST QUERY WITH FILTERS                                          │
│  ═════════════════════════════════                                          │
│                                                                             │
│   FILTER PARAMS                      QUERY BUILDER                          │
│   ═════════════                      ═════════════                          │
│                                                                             │
│   FromDate: 2025-01-26      ┌────────────────────────────────────┐          │
│   ToDate: 2025-01-27        │                                    │          │
│   SourceSystemId: <guid> ───┼──► WHERE SourceSystemId = @id      │          │
│   ErrorsOnly: true      ────┼──► AND IsSuccess = false           │          │
│   MinDurationMs: 1000   ────┼──► AND DurationMs >= 1000          │          │
│   Page: 1               ────┼──► OFFSET 0                        │          │
│   PageSize: 50          ────┼──► FETCH NEXT 50                   │          │
│   SortBy: RequestedAt   ────┼──► ORDER BY RequestedAt DESC       │          │
│                             │                                    │          │
│                             └────────────────────────────────────┘          │
│                                          │                                  │
│                                          ▼                                  │
│                             ┌────────────────────────────────────┐          │
│                             │ PagedResult<ApiRequestLogListDto>  │          │
│                             │                                    │          │
│                             │ Items: [50 logs]                   │          │
│                             │ TotalCount: 127                    │          │
│                             │ Page: 1                            │          │
│                             │ TotalPages: 3                      │          │
│                             └────────────────────────────────────┘          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- Flexible query builder with all filter options
- Server-side pagination
- Efficient projection (only needed columns)

**Pseudo-code:**
```csharp
// In DataAccess.ApiLogging.cs

public async Task<PagedResult<ApiRequestLogListDto>> GetApiLogsAsync(
    ApiLogFilterParams filters)
{
    // Start with base query
    IQueryable<ApiRequestLogItem> query = _context.ApiRequestLogs;
    
    // === APPLY FILTERS ===
    
    // Time range
    if (filters.FromDate.HasValue)
        query = query.Where(l => l.RequestedAt >= filters.FromDate.Value);
    
    if (filters.ToDate.HasValue)
        query = query.Where(l => l.RequestedAt <= filters.ToDate.Value);
    
    // Source system
    if (filters.SourceSystemId.HasValue)
        query = query.Where(l => l.SourceSystemId == filters.SourceSystemId.Value);
    
    // Status/Success filter
    if (filters.ErrorsOnly == true)
        query = query.Where(l => !l.IsSuccess);
    else if (filters.SuccessOnly == true)
        query = query.Where(l => l.IsSuccess);
    else if (filters.StatusCodes?.Any() == true)
        query = query.Where(l => filters.StatusCodes.Contains(l.StatusCode));
    
    // Duration filter (DBA request from focus group)
    if (filters.MinDurationMs.HasValue)
        query = query.Where(l => l.DurationMs >= filters.MinDurationMs.Value);
    
    if (filters.MaxDurationMs.HasValue)
        query = query.Where(l => l.DurationMs <= filters.MaxDurationMs.Value);
    
    // Search term (path or error message)
    if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
    {
        var search = filters.SearchTerm.ToLower();
        query = query.Where(l => 
            l.RequestPath.ToLower().Contains(search) ||
            l.ErrorMessage.ToLower().Contains(search));
    }
    
    // Correlation ID exact match
    if (!string.IsNullOrWhiteSpace(filters.CorrelationId))
        query = query.Where(l => l.CorrelationId == filters.CorrelationId);
    
    // === GET TOTAL COUNT ===
    var totalCount = await query.CountAsync();
    
    // === APPLY SORTING ===
    query = filters.SortBy?.ToLower() switch
    {
        "duration" => filters.SortDescending 
            ? query.OrderByDescending(l => l.DurationMs)
            : query.OrderBy(l => l.DurationMs),
        "status" => filters.SortDescending
            ? query.OrderByDescending(l => l.StatusCode)
            : query.OrderBy(l => l.StatusCode),
        "source" => filters.SortDescending
            ? query.OrderByDescending(l => l.SourceSystemName)
            : query.OrderBy(l => l.SourceSystemName),
        _ => filters.SortDescending
            ? query.OrderByDescending(l => l.RequestedAt)
            : query.OrderBy(l => l.RequestedAt)
    };
    
    // === APPLY PAGINATION ===
    var skip = (filters.Page - 1) * filters.PageSize;
    
    // === PROJECT TO DTO ===
    var items = await query
        .Skip(skip)
        .Take(filters.PageSize)
        .Select(l => new ApiRequestLogListDto
        {
            ApiRequestLogId = l.ApiRequestLogId,
            SourceSystemName = l.SourceSystemName,
            HttpMethod = l.HttpMethod,
            RequestPath = l.RequestPath,
            RequestedAt = l.RequestedAt,
            DurationMs = l.DurationMs,
            StatusCode = l.StatusCode,
            IsSuccess = l.IsSuccess,
            ErrorMessage = l.ErrorMessage
        })
        .ToListAsync();
    
    return new PagedResult<ApiRequestLogListDto>
    {
        Items = items,
        TotalCount = totalCount,
        Page = filters.Page,
        PageSize = filters.PageSize
    };
}
```

---

## Task 4.2: Create List View API Endpoint

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 4.2: LIST VIEW API ENDPOINT                                           │
│  ════════════════════════════════                                           │
│                                                                             │
│   GET /api/data/api-logs?fromDate=...&errorsOnly=true&page=1                │
│                                                                             │
│   Query Parameters:                                                         │
│   ─────────────────                                                         │
│   • fromDate, toDate     - Time range                                       │
│   • sourceSystemId       - Filter by source                                 │
│   • errorsOnly           - Only show errors                                 │
│   • successOnly          - Only show successes                              │
│   • statusCodes          - Specific status codes (comma-separated)          │
│   • minDurationMs        - Minimum duration (slow queries)                  │
│   • maxDurationMs        - Maximum duration                                 │
│   • searchTerm           - Search in path/error                             │
│   • correlationId        - Exact correlation match                          │
│   • page, pageSize       - Pagination                                       │
│   • sortBy, sortDesc     - Sorting                                          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- API endpoint for list data
- Accepts all filter parameters
- Returns paged results

**Pseudo-code:**
```csharp
// In DataController.ApiLogs.cs

[HttpGet("api-logs")]
[SkipApiLogging(Reason = "List view endpoint")]
public async Task<IActionResult> GetApiLogs([FromQuery] ApiLogFilterParams filters)
{
    // Validate pagination
    if (filters.Page < 1) filters.Page = 1;
    if (filters.PageSize < 1) filters.PageSize = 50;
    if (filters.PageSize > 100) filters.PageSize = 100; // Max limit
    
    var result = await _dataAccess.GetApiLogsAsync(filters);
    return Ok(result);
}

[HttpGet("api-logs/{id:guid}")]
[SkipApiLogging(Reason = "Detail view endpoint")]
public async Task<IActionResult> GetApiLog(Guid id)
{
    var log = await _dataAccess.GetApiLogByIdAsync(id);
    
    if (log == null)
        return NotFound();
    
    return Ok(ApiRequestLogDto.FromEntity(log));
}
```

---

## Task 4.3: Create List View Blazor Page

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 4.3: LIST VIEW BLAZOR PAGE                                            │
│  ═══════════════════════════════                                            │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │                      API REQUEST LOGS                               │   │
│   ├─────────────────────────────────────────────────────────────────────┤   │
│   │                                                                     │   │
│   │   FILTERS                                                           │   │
│   │   ═══════                                                           │   │
│   │   ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌───────────┐  │   │
│   │   │ Time Range ▼ │ │ Source    ▼  │ │ Status    ▼  │ │Duration ▼ │  │   │
│   │   └──────────────┘ └──────────────┘ └──────────────┘ └───────────┘  │   │
│   │                                                                     │   │
│   │   ┌────────────────────────────────────────────┐  ┌──────────────┐  │   │
│   │   │ 🔍 Search path or error...                 │  │   Export ⬇   │  │   │
│   │   └────────────────────────────────────────────┘  └──────────────┘  │   │
│   │                                                                     │   │
│   │   TABLE                                         Showing 1-50 of 127 │   │
│   │   ═════                                                             │   │
│   │   ┌─────┬────────┬──────┬────────────────┬──────┬────────┬───────┐  │   │
│   │   │Time │Source  │Method│Path            │Status│Duration│Error  │  │   │
│   │   ├─────┼────────┼──────┼────────────────┼──────┼────────┼───────┤  │   │
│   │   │14:32│Banner  │POST  │/api/events     │ 200  │ 234ms  │       │  │   │
│   │   │14:28│HR      │POST  │/api/events     │ 500  │ 1234ms │DB err │  │   │
│   │   │14:15│Banner  │POST  │/api/batch      │ 400  │ 45ms   │Missing│  │   │
│   │   │...  │...     │...   │...             │ ...  │ ...    │...    │  │   │
│   │   └─────┴────────┴──────┴────────────────┴──────┴────────┴───────┘  │   │
│   │                                                                     │   │
│   │   PAGINATION                                                        │   │
│   │   ══════════                                                        │   │
│   │   [◀ Prev] [1] [2] [3] [Next ▶]                                    │   │
│   │                                                                     │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- Full filtering UI with dropdowns
- Sortable columns
- Pagination
- Export button

**Pseudo-code:**
```razor
@page "/api-logs"
@* FreeGLBA.App.ApiRequestLogs.razor *@

@inject HttpClient Http
@inject NavigationManager Nav
@inject IJSRuntime JS

<PageTitle>API Request Logs</PageTitle>

<h1>API Request Logs</h1>

<!-- Filters Row -->
<div class="row mb-3 filters">
    <!-- Time Range -->
    <div class="col-md-2">
        <label>Time Range</label>
        <select class="form-select" @bind="_filters.TimeRange" @bind:after="ApplyFilters">
            <option value="1h">Last Hour</option>
            <option value="24h">Last 24 Hours</option>
            <option value="7d">Last 7 Days</option>
            <option value="30d">Last 30 Days</option>
            <option value="custom">Custom...</option>
        </select>
    </div>
    
    <!-- Source System -->
    <div class="col-md-2">
        <label>Source System</label>
        <select class="form-select" @bind="_filters.SourceSystemId" @bind:after="ApplyFilters">
            <option value="">All Sources</option>
            @foreach (var source in _sourceSystems)
            {
                <option value="@source.SourceSystemId">@source.Name</option>
            }
        </select>
    </div>
    
    <!-- Status Filter -->
    <div class="col-md-2">
        <label>Status</label>
        <select class="form-select" @bind="_statusFilter" @bind:after="ApplyFilters">
            <option value="all">All</option>
            <option value="success">Success (2xx)</option>
            <option value="errors">Errors Only</option>
            <option value="4xx">Client Errors (4xx)</option>
            <option value="5xx">Server Errors (5xx)</option>
        </select>
    </div>
    
    <!-- Duration Filter (DBA request) -->
    <div class="col-md-2">
        <label>Duration</label>
        <select class="form-select" @bind="_durationFilter" @bind:after="ApplyFilters">
            <option value="">Any</option>
            <option value="100">< 100ms</option>
            <option value="500">> 500ms</option>
            <option value="1000">> 1 second</option>
            <option value="5000">> 5 seconds</option>
        </select>
    </div>
    
    <!-- Search -->
    <div class="col-md-3">
        <label>Search</label>
        <input type="text" class="form-control" placeholder="Search path or error..."
               @bind="_filters.SearchTerm" @bind:after="ApplyFilters" />
    </div>
    
    <!-- Export -->
    <div class="col-md-1 d-flex align-items-end">
        <button class="btn btn-outline-secondary" @onclick="ExportCsv">
            <i class="bi bi-download"></i> Export
        </button>
    </div>
</div>

<!-- Results Table -->
@if (_loading)
{
    <div class="text-center p-5">
        <div class="spinner-border" role="status"></div>
    </div>
}
else if (_result != null)
{
    <div class="d-flex justify-content-between mb-2">
        <span class="text-muted">
            Showing @((_result.Page - 1) * _result.PageSize + 1)-@Math.Min(_result.Page * _result.PageSize, _result.TotalCount) 
            of @_result.TotalCount.ToString("N0") results
        </span>
    </div>
    
    <table class="table table-hover table-striped">
        <thead>
            <tr>
                <th @onclick='() => SortBy("requestedAt")' style="cursor: pointer">
                    Time @SortIndicator("requestedAt")
                </th>
                <th @onclick='() => SortBy("source")' style="cursor: pointer">
                    Source @SortIndicator("source")
                </th>
                <th>Method</th>
                <th>Path</th>
                <th @onclick='() => SortBy("status")' style="cursor: pointer">
                    Status @SortIndicator("status")
                </th>
                <th @onclick='() => SortBy("duration")' style="cursor: pointer">
                    Duration @SortIndicator("duration")
                </th>
                <th>Error</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var log in _result.Items)
            {
                <tr @onclick="() => ViewDetail(log.ApiRequestLogId)" 
                    class="@(log.IsSuccess ? "" : "table-warning")"
                    style="cursor: pointer">
                    <td>@log.RequestedAt.ToLocalTime().ToString("MM/dd HH:mm:ss")</td>
                    <td>@log.SourceSystemName</td>
                    <td><code>@log.HttpMethod</code></td>
                    <td title="@log.RequestPath">@Truncate(log.RequestPath, 40)</td>
                    <td>
                        <span class="badge @StatusBadgeClass(log.StatusCode)">
                            @log.StatusCode
                        </span>
                    </td>
                    <td class="@(log.DurationMs > 1000 ? "text-warning fw-bold" : "")">
                        @log.DurationMs.ToString("N0")ms
                    </td>
                    <td title="@log.ErrorMessage">@Truncate(log.ErrorMessage, 30)</td>
                </tr>
            }
        </tbody>
    </table>
    
    <!-- Pagination -->
    <nav aria-label="Page navigation">
        <ul class="pagination justify-content-center">
            <li class="page-item @(_result.Page <= 1 ? "disabled" : "")">
                <a class="page-link" @onclick="PrevPage">Previous</a>
            </li>
            @for (var i = 1; i <= _result.TotalPages && i <= 10; i++)
            {
                var pageNum = i;
                <li class="page-item @(pageNum == _result.Page ? "active" : "")">
                    <a class="page-link" @onclick="() => GoToPage(pageNum)">@pageNum</a>
                </li>
            }
            <li class="page-item @(_result.Page >= _result.TotalPages ? "disabled" : "")">
                <a class="page-link" @onclick="NextPage">Next</a>
            </li>
        </ul>
    </nav>
}

@code {
    private ApiLogFilterParams _filters = new();
    private PagedResult<ApiRequestLogListDto>? _result;
    private List<SourceSystemItem> _sourceSystems = new();
    private bool _loading = true;
    private string _statusFilter = "all";
    private string _durationFilter = "";
    
    protected override async Task OnInitializedAsync()
    {
        // Load source systems for filter dropdown
        _sourceSystems = await Http.GetFromJsonAsync<List<SourceSystemItem>>(
            "api/data/source-systems") ?? new();
        
        await ApplyFilters();
    }
    
    private async Task ApplyFilters()
    {
        _loading = true;
        _filters.Page = 1; // Reset to first page
        
        // Map status filter
        _filters.ErrorsOnly = _statusFilter == "errors";
        _filters.SuccessOnly = _statusFilter == "success";
        // ... handle other status filters
        
        // Map duration filter
        if (int.TryParse(_durationFilter, out var duration))
            _filters.MinDurationMs = duration;
        else
            _filters.MinDurationMs = null;
        
        await LoadData();
    }
    
    private async Task LoadData()
    {
        _loading = true;
        StateHasChanged();
        
        var queryString = BuildQueryString();
        _result = await Http.GetFromJsonAsync<PagedResult<ApiRequestLogListDto>>(
            $"api/data/api-logs?{queryString}");
        
        _loading = false;
        StateHasChanged();
    }
    
    private void ViewDetail(Guid id)
    {
        Nav.NavigateTo($"/api-logs/{id}");
    }
    
    private async Task ExportCsv()
    {
        // Trigger CSV download
        var url = $"api/data/api-logs/export?{BuildQueryString()}";
        await JS.InvokeVoidAsync("downloadFile", url, "api-logs.csv");
    }
    
    private string StatusBadgeClass(int statusCode) => statusCode switch
    {
        >= 200 and < 300 => "bg-success",
        >= 300 and < 400 => "bg-info",
        >= 400 and < 500 => "bg-warning",
        >= 500 => "bg-danger",
        _ => "bg-secondary"
    };
    
    private string Truncate(string text, int maxLength) =>
        string.IsNullOrEmpty(text) ? "" :
        text.Length <= maxLength ? text : text[..maxLength] + "...";
}
```

---

## Task 4.4: Create Detail View Blazor Page

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 4.4: DETAIL VIEW BLAZOR PAGE                                          │
│  ═════════════════════════════════                                          │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │                    API REQUEST LOG DETAIL                           │   │
│   │                                                                     │   │
│   │   [← Back to List]                                                  │   │
│   │                                                                     │   │
│   │   ┌─────────────────────────────────────────────────────────────┐   │   │
│   │   │  REQUEST                                                    │   │   │
│   │   ├─────────────────────────────────────────────────────────────┤   │   │
│   │   │  Method:        POST                                        │   │   │
│   │   │  Path:          /api/glba/events                            │   │   │
│   │   │  Query String:  ?version=2                                  │   │   │
│   │   │  Source System: Banner (Student Information)                │   │   │
│   │   │  Correlation ID: abc-123-def                                │   │   │
│   │   │  IP Address:    192.168.1.100                               │   │   │
│   │   │  User Agent:    Banner/9.0                                  │   │   │
│   │   └─────────────────────────────────────────────────────────────┘   │   │
│   │                                                                     │   │
│   │   ┌─────────────────────────────────────────────────────────────┐   │   │
│   │   │  TIMING                                                     │   │   │
│   │   ├─────────────────────────────────────────────────────────────┤   │   │
│   │   │  Requested At:  2025-01-27 14:32:01.234                     │   │   │
│   │   │  Responded At:  2025-01-27 14:32:01.567                     │   │   │
│   │   │  Duration:      333ms                                       │   │   │
│   │   └─────────────────────────────────────────────────────────────┘   │   │
│   │                                                                     │   │
│   │   ┌─────────────────────────────────────────────────────────────┐   │   │
│   │   │  RESPONSE                                                   │   │   │
│   │   ├─────────────────────────────────────────────────────────────┤   │   │
│   │   │  Status Code:   500 (Internal Server Error)                 │   │   │
│   │   │  Error:         Database connection timeout                 │   │   │
│   │   │  Exception:     SqlException                                │   │   │
│   │   └─────────────────────────────────────────────────────────────┘   │   │
│   │                                                                     │   │
│   │   ┌─────────────────────────────────────────────────────────────┐   │   │
│   │   │  HEADERS (Filtered)                                         │   │   │
│   │   ├─────────────────────────────────────────────────────────────┤   │   │
│   │   │  Content-Type:  application/json                            │   │   │
│   │   │  Authorization: [REDACTED]                                  │   │   │
│   │   │  Host:          api.freeglba.edu                            │   │   │
│   │   └─────────────────────────────────────────────────────────────┘   │   │
│   │                                                                     │   │
│   │   ┌─────────────────────────────────────────────────────────────┐   │   │
│   │   │  REQUEST BODY (if captured)                                 │   │   │
│   │   ├─────────────────────────────────────────────────────────────┤   │   │
│   │   │  {                                                          │   │   │
│   │   │    "eventType": "Access",                                   │   │   │
│   │   │    "subjectId": "123456",                                   │   │   │
│   │   │    ...                                                      │   │   │
│   │   │  }                                                          │   │   │
│   │   └─────────────────────────────────────────────────────────────┘   │   │
│   │                                                                     │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- Full details of a single log entry
- Formatted JSON display for bodies
- All captured information organized

**Pseudo-code:**
```razor
@page "/api-logs/{Id:guid}"
@* FreeGLBA.App.ViewApiRequestLog.razor *@

@inject HttpClient Http
@inject NavigationManager Nav

<PageTitle>View API Request Log</PageTitle>

<div class="mb-3">
    <a href="/api-logs" class="btn btn-outline-secondary">
        <i class="bi bi-arrow-left"></i> Back to List
    </a>
</div>

@if (_loading)
{
    <div class="text-center p-5">
        <div class="spinner-border" role="status"></div>
    </div>
}
else if (_log == null)
{
    <div class="alert alert-warning">Log not found</div>
}
else
{
    <h1>
        <span class="badge @StatusBadgeClass(_log.StatusCode) me-2">@_log.StatusCode</span>
        @_log.HttpMethod @_log.RequestPath
    </h1>
    
    <!-- Request Info -->
    <div class="card mb-3">
        <div class="card-header">
            <i class="bi bi-arrow-up-right"></i> Request
        </div>
        <div class="card-body">
            <dl class="row mb-0">
                <dt class="col-sm-3">Method</dt>
                <dd class="col-sm-9"><code>@_log.HttpMethod</code></dd>
                
                <dt class="col-sm-3">Path</dt>
                <dd class="col-sm-9"><code>@_log.RequestPath</code></dd>
                
                @if (!string.IsNullOrEmpty(_log.QueryString))
                {
                    <dt class="col-sm-3">Query String</dt>
                    <dd class="col-sm-9"><code>@_log.QueryString</code></dd>
                }
                
                <dt class="col-sm-3">Source System</dt>
                <dd class="col-sm-9">@_log.SourceSystemName</dd>
                
                <dt class="col-sm-3">Correlation ID</dt>
                <dd class="col-sm-9"><code>@_log.CorrelationId</code></dd>
                
                <dt class="col-sm-3">IP Address</dt>
                <dd class="col-sm-9">@_log.IpAddress</dd>
                
                <dt class="col-sm-3">User Agent</dt>
                <dd class="col-sm-9 text-truncate" title="@_log.UserAgent">
                    @_log.UserAgent
                </dd>
            </dl>
        </div>
    </div>
    
    <!-- Timing -->
    <div class="card mb-3">
        <div class="card-header">
            <i class="bi bi-clock"></i> Timing
        </div>
        <div class="card-body">
            <dl class="row mb-0">
                <dt class="col-sm-3">Requested At</dt>
                <dd class="col-sm-9">@_log.RequestedAt.ToString("yyyy-MM-dd HH:mm:ss.fff")</dd>
                
                <dt class="col-sm-3">Responded At</dt>
                <dd class="col-sm-9">@_log.RespondedAt.ToString("yyyy-MM-dd HH:mm:ss.fff")</dd>
                
                <dt class="col-sm-3">Duration</dt>
                <dd class="col-sm-9">
                    <span class="@(_log.DurationMs > 1000 ? "text-warning fw-bold" : "")">
                        @_log.DurationMs.ToString("N0")ms
                    </span>
                </dd>
            </dl>
        </div>
    </div>
    
    <!-- Response -->
    <div class="card mb-3 @(!_log.IsSuccess ? "border-danger" : "")">
        <div class="card-header @(!_log.IsSuccess ? "bg-danger text-white" : "")">
            <i class="bi bi-arrow-down-left"></i> Response
        </div>
        <div class="card-body">
            <dl class="row mb-0">
                <dt class="col-sm-3">Status Code</dt>
                <dd class="col-sm-9">
                    <span class="badge @StatusBadgeClass(_log.StatusCode)">@_log.StatusCode</span>
                    @StatusDescription(_log.StatusCode)
                </dd>
                
                @if (!string.IsNullOrEmpty(_log.ErrorMessage))
                {
                    <dt class="col-sm-3">Error Message</dt>
                    <dd class="col-sm-9 text-danger">@_log.ErrorMessage</dd>
                }
                
                @if (!string.IsNullOrEmpty(_log.ExceptionType))
                {
                    <dt class="col-sm-3">Exception Type</dt>
                    <dd class="col-sm-9"><code>@_log.ExceptionType</code></dd>
                }
            </dl>
        </div>
    </div>
    
    <!-- Headers -->
    <div class="card mb-3">
        <div class="card-header">
            <i class="bi bi-list"></i> Request Headers (Filtered)
        </div>
        <div class="card-body">
            <pre class="mb-0"><code>@FormatJson(_log.RequestHeaders)</code></pre>
        </div>
    </div>
    
    <!-- Request Body (if captured) -->
    @if (_log.BodyLoggingEnabled && !string.IsNullOrEmpty(_log.RequestBody))
    {
        <div class="card mb-3">
            <div class="card-header">
                <i class="bi bi-file-text"></i> Request Body
                @if (_log.RequestBodySize > 4096)
                {
                    <span class="badge bg-warning ms-2">Truncated (@_log.RequestBodySize bytes original)</span>
                }
            </div>
            <div class="card-body">
                <pre class="mb-0"><code>@FormatJson(_log.RequestBody)</code></pre>
            </div>
        </div>
    }
    
    <!-- Response Body (if captured) -->
    @if (_log.BodyLoggingEnabled && !string.IsNullOrEmpty(_log.ResponseBody))
    {
        <div class="card mb-3">
            <div class="card-header">
                <i class="bi bi-file-text"></i> Response Body
                @if (_log.ResponseBodySize > 4096)
                {
                    <span class="badge bg-warning ms-2">Truncated (@_log.ResponseBodySize bytes original)</span>
                }
            </div>
            <div class="card-body">
                <pre class="mb-0"><code>@FormatJson(_log.ResponseBody)</code></pre>
            </div>
        </div>
    }
    
    @if (!_log.BodyLoggingEnabled)
    {
        <div class="alert alert-info">
            <i class="bi bi-info-circle"></i> 
            Request/response bodies were not captured. Body logging must be enabled for the source system.
        </div>
    }
}

@code {
    [Parameter] public Guid Id { get; set; }
    
    private ApiRequestLogDto? _log;
    private bool _loading = true;
    
    protected override async Task OnInitializedAsync()
    {
        await LoadLog();
    }
    
    private async Task LoadLog()
    {
        try
        {
            _log = await Http.GetFromJsonAsync<ApiRequestLogDto>($"api/data/api-logs/{Id}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _log = null;
        }
        finally
        {
            _loading = false;
        }
    }
    
    private string FormatJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return "";
        
        try
        {
            var obj = JsonSerializer.Deserialize<JsonElement>(json);
            return JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return json; // Return as-is if not valid JSON
        }
    }
    
    private string StatusBadgeClass(int statusCode) => statusCode switch
    {
        >= 200 and < 300 => "bg-success",
        >= 300 and < 400 => "bg-info",
        >= 400 and < 500 => "bg-warning",
        >= 500 => "bg-danger",
        _ => "bg-secondary"
    };
    
    private string StatusDescription(int statusCode) => statusCode switch
    {
        200 => "OK",
        201 => "Created",
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        500 => "Internal Server Error",
        502 => "Bad Gateway",
        503 => "Service Unavailable",
        _ => ""
    };
}
```

---

## Task 4.5: Implement Export Feature

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 4.5: CSV EXPORT                                                       │
│  ════════════════════                                                       │
│                                                                             │
│   ┌─────────────────┐                                                       │
│   │  Export Button  │                                                       │
│   │  [⬇ Export]     │                                                       │
│   └────────┬────────┘                                                       │
│            │                                                                │
│            ▼                                                                │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │  Apply same filters as current view                                 │   │
│   │  Limit to MaxExportRows (10,000 from config)                        │   │
│   │  Generate CSV                                                       │   │
│   │  Return as file download                                            │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│            │                                                                │
│            ▼                                                                │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │  api-logs-2025-01-27.csv                                            │   │
│   │  ─────────────────────────────────────────────────────────────────  │   │
│   │  RequestedAt,Source,Method,Path,Status,Duration,Error              │   │
│   │  2025-01-27 14:32,Banner,POST,/api/events,200,234,                  │   │
│   │  2025-01-27 14:28,HR,POST,/api/events,500,1234,DB timeout          │   │
│   │  ...                                                                │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- CSV export with current filters
- Row limit to prevent huge exports
- Compliance officer request from focus group

**Pseudo-code:**
```csharp
// In DataController.ApiLogs.cs

[HttpGet("api-logs/export")]
[SkipApiLogging(Reason = "Export endpoint")]
public async Task<IActionResult> ExportApiLogs([FromQuery] ApiLogFilterParams filters)
{
    // Override pagination for export
    filters.Page = 1;
    filters.PageSize = _options.MaxExportRows; // Default 10,000
    
    var result = await _dataAccess.GetApiLogsAsync(filters);
    
    // Build CSV
    var csv = new StringBuilder();
    csv.AppendLine("RequestedAt,SourceSystem,Method,Path,StatusCode,DurationMs,IsSuccess,ErrorMessage");
    
    foreach (var log in result.Items)
    {
        csv.AppendLine(string.Join(",",
            log.RequestedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            EscapeCsv(log.SourceSystemName),
            log.HttpMethod,
            EscapeCsv(log.RequestPath),
            log.StatusCode,
            log.DurationMs,
            log.IsSuccess,
            EscapeCsv(log.ErrorMessage)
        ));
    }
    
    var bytes = Encoding.UTF8.GetBytes(csv.ToString());
    var fileName = $"api-logs-{DateTime.Now:yyyy-MM-dd}.csv";
    
    return File(bytes, "text/csv", fileName);
}

private string EscapeCsv(string value)
{
    if (string.IsNullOrEmpty(value)) return "";
    if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
    {
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
    return value;
}
```

---

## Phase 4 Files Summary

| File | Action | Key Changes |
|------|--------|-------------|
| `FreeGLBA.App.DataAccess.ApiLogging.cs` | MODIFY | Add GetApiLogsAsync, GetApiLogByIdAsync |
| `DataController.ApiLogs.cs` | MODIFY | Add list, detail, export endpoints |
| `FreeGLBA.App.ApiRequestLogs.razor` | MODIFY | Full list view implementation |
| `FreeGLBA.App.ViewApiRequestLog.razor` | MODIFY | Full detail view implementation |

---

## Verification Checklist

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         PHASE 4 VERIFICATION                                │
└─────────────────────────────────────────────────────────────────────────────┘

  □ List view loads at /api-logs
  □ All filters work (time, source, status, duration)
  □ Search filters by path and error message
  □ Sorting works on all sortable columns
  □ Pagination works correctly
  □ Clicking row navigates to detail view
  □ Detail view shows all captured information
  □ Headers are formatted correctly
  □ Bodies display with proper JSON formatting
  □ Export generates valid CSV file
  □ Export respects row limit (10,000)
  □ All API endpoints have [SkipApiLogging]
  
  MANUAL TEST:
  ════════════
  1. Generate varied traffic (success, errors, different durations)
  2. Navigate to /api-logs
  3. Test each filter type
  4. Click through to detail view
  5. Export to CSV and open in Excel
```

---

## Phase 4 Summary

| Metric | Value |
|--------|-------|
| Files Modified | 4 |
| Lines of Code | ~600 |
| Key Feature | Browsing & searching logs |
| Estimated Time | 1.5 days |
| Dependencies | Phase 3 |
| Deliverable | Complete log viewer |

---

*Previous: [121 — Phase 3: Dashboard View](121_impl_phase3_dashboard.md)*  
*Next: [123 — Phase 5: Body Logging Settings & Cleanup](123_impl_phase5_settings.md)*

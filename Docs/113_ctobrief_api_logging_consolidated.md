# 113 — CTO Brief: API Request Logging - Final Consolidated Design

> **Document ID:** 113  
> **Category:** CTO Brief  
> **Purpose:** Comprehensive summary of all discussions and final implementation proposal  
> **Source Documents:** 108, 109, 110, 111, 112  
> **Date:** 2025-01-27  
> **Status:** ⏳ Ready for final CTO approval

---

## Executive Summary

This document consolidates four team discussions and incorporates CTO feedback to present the final API request logging design for FreeGLBA.

**The Ask:** Comprehensive API request logging for GLBA compliance auditing and debugging.

**CTO Requirements:**
1. Attribute-based (`[ApiRequestLogging]` on controllers)
2. Mandatory (this is critical compliance infrastructure)
3. No infinite loops (don't log the log endpoint)
4. Track both directions (request IN, response OUT)
5. Timing for debugging (Stopwatch-based duration)

---

## Document Summary

### Doc 108 — Initial Design Meeting

**Focus:** Entity design, middleware vs filter approach, what fields to capture.

**Key Decisions:**
- Created comprehensive `ApiRequestLogItem` entity with WHO/WHAT/WHEN/WHERE/RESULT categories
- Decided on async fire-and-forget writes
- Denormalize SourceSystemName/UserName for queryability
- Add `RelatedEntityId` to link logs to created entities

**Open Questions:** Body logging, retention, UI — deferred to CTO.

---

### Doc 110 — Industry Deep Dive

**Focus:** How do others solve this? What should we adopt?

**Solutions Analyzed:**
- ASP.NET Core HTTP Logging Middleware
- Serilog Request Logging
- Azure Application Insights
- Azure API Management
- Dataverse/SQL Server Auditing
- Moesif/Seq dedicated services

**Key Learnings Adopted:**
| From | Learning |
|------|----------|
| ASP.NET Core | 4KB body truncation default, opt-in bodies |
| Azure APIM | Strip sensitive headers (Authorization, cookies) |
| Dataverse | Mandatory cleanup job for retention |
| All | Logging without UI is useless |

---

### Doc 111 — CTO Brief v1

**Focus:** Present industry-validated recommendations for CTO decisions.

**Recommendations Made:**
1. Bodies: Metadata default, opt-in truncated (4KB)
2. Retention: 90 days with cleanup job
3. UI: Build in V1
4. Headers: Strip Authorization, cookies, API keys

---

### Doc 112 — CTO Feedback Standup

**Focus:** Incorporate CTO's specific requirements.

**CTO Direction:**
- Switch from middleware to `[ApiRequestLogging]` attribute
- Make logging mandatory, not optional
- Add `[SkipApiLogging]` to prevent infinite loops
- Use Stopwatch for timing both directions
- This is compliance infrastructure—treat it seriously

---

## Industry Comparison: What Others Do

| Aspect | ASP.NET Core | App Insights | Azure APIM | Our Design |
|--------|--------------|--------------|------------|------------|
| **Activation** | Middleware | Auto-collect | Policy | **Attribute** ✓ |
| **Scope** | Global/Endpoint | All requests | Per-API | **Per-controller** ✓ |
| **Body logging** | Opt-in | Off | Opt-in (8KB) | **Opt-in (4KB)** ✓ |
| **Timing** | Yes | Yes | Yes | **Yes (Stopwatch)** ✓ |
| **Loop prevention** | N/A | N/A | N/A | **[SkipApiLogging]** ✓ |
| **Storage** | Log files | Cloud | Event Hubs | **Database** ✓ |
| **Queryable** | No | KQL | KQL | **SQL** ✓ |
| **Self-hosted** | Yes | No | No | **Yes** ✓ |

**Our approach combines the best of each:**
- Attribute-based like ASP.NET Core's `[HttpLogging]`
- Structured data like App Insights
- Header filtering like APIM
- Database storage for self-hosted queryability

---

## Final Proposed Design

### Implementation Approach: Action Filter Attribute

```csharp
// Apply to entire controller — all actions logged
[ApiController]
[Route("api/glba")]
[ApiRequestLogging]
public class GlbaController : ControllerBase
{
    [HttpPost("events")]
    public async Task<IActionResult> SubmitEvent([FromBody] GlbaEventRequest request)
    {
        // This action is logged automatically
    }
}

// Prevent infinite loop on log management endpoints
[ApiController]
[Route("api/logs")]
[ApiRequestLogging]
public class ApiLogsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetLogs() { }  // Logged
    
    [HttpPost]
    [SkipApiLogging]  // NOT logged — prevents infinite loop
    public async Task<IActionResult> CreateLog([FromBody] ApiRequestLog log) { }
}
```

### Timing Implementation

```csharp
public class ApiRequestLoggingAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // Check for skip
        if (context.ActionDescriptor.EndpointMetadata.OfType<SkipApiLoggingAttribute>().Any())
        {
            context.HttpContext.Items["ApiLog_Skip"] = true;
            return;
        }
        
        // Start timing
        var stopwatch = Stopwatch.StartNew();
        context.HttpContext.Items["ApiLog_Stopwatch"] = stopwatch;
        context.HttpContext.Items["ApiLog_RequestedAt"] = DateTime.UtcNow;
        
        // Capture request details
        CaptureRequest(context);
    }
    
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.HttpContext.Items.ContainsKey("ApiLog_Skip")) return;
        
        // Stop timing
        var stopwatch = context.HttpContext.Items["ApiLog_Stopwatch"] as Stopwatch;
        stopwatch?.Stop();
        
        // Build and save log
        var log = BuildLogEntry(context, stopwatch.ElapsedMilliseconds);
        _ = SaveLogAsync(log);  // Fire-and-forget
    }
}
```

### Entity Design (Final)

```csharp
public partial class ApiRequestLogItem
{
    [Key]
    public Guid ApiRequestLogId { get; set; }

    // === WHO ===
    public Guid? SourceSystemId { get; set; }
    public string SourceSystemName { get; set; } = "";
    public Guid? UserId { get; set; }
    public string UserName { get; set; } = "";
    public Guid? TenantId { get; set; }

    // === WHAT (Request) ===
    public string HttpMethod { get; set; } = "";
    public string RequestPath { get; set; } = "";
    public string QueryString { get; set; } = "";
    public string RequestHeaders { get; set; } = "";      // Filtered JSON
    public string RequestBody { get; set; } = "";         // Optional, truncated
    public long RequestBodySize { get; set; }

    // === WHEN ===
    public DateTime RequestedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public long DurationMs { get; set; }                  // Stopwatch timing

    // === WHERE ===
    public string IpAddress { get; set; } = "";
    public string UserAgent { get; set; } = "";
    public string ForwardedFor { get; set; } = "";

    // === RESULT (Response) ===
    public int StatusCode { get; set; }
    public bool IsSuccess { get; set; }
    public string ResponseBody { get; set; } = "";        // Optional, truncated
    public long ResponseBodySize { get; set; }
    public string ErrorMessage { get; set; } = "";
    public string ExceptionType { get; set; } = "";

    // === CONTEXT ===
    public string CorrelationId { get; set; } = "";
    public string AuthType { get; set; } = "";            // ApiKey, Bearer, etc.
    public Guid? RelatedEntityId { get; set; }            // Link to created entity
    public string RelatedEntityType { get; set; } = "";   // e.g., "AccessEvent"
}
```

---

## What We Will Have

| Feature | Status | Notes |
|---------|--------|-------|
| `[ApiRequestLogging]` attribute | ✅ Build | Apply to controllers |
| `[SkipApiLogging]` attribute | ✅ Build | Prevent infinite loops |
| Stopwatch timing | ✅ Build | DurationMs with ms precision |
| Request capture | ✅ Build | Method, path, headers, optional body |
| Response capture | ✅ Build | Status, optional body, errors |
| Header filtering | ✅ Build | Strip Authorization, cookies, API keys |
| 4KB body truncation | ✅ Build | Industry standard |
| Async fire-and-forget | ✅ Build | Don't block requests |
| Fallback to Serilog | ✅ Build | If DB write fails |
| 90-day retention | ✅ Build | With cleanup job |
| Admin UI | ✅ Build | Filter by source, date, status, duration |
| Duration indicators | ✅ Build | Color-coded (🟢🟡🟠🔴) |

## What We Will NOT Have (V1)

| Feature | Reason | Future? |
|---------|--------|---------|
| Automatic PII redaction | Complexity | V2 |
| Request replay | Scope creep | V2 |
| Alerting on patterns | Separate feature | V2 |
| Export to SIEM | Enterprise feature | V2 |
| Per-tenant retention | Complexity | V2 |

---

## Files to Create

| File | Project | Purpose |
|------|---------|---------|
| `FreeGLBA.App.ApiRequestLog.cs` | EFModels | EF entity |
| `FreeGLBA.App.DataObjects.ApiRequestLog.cs` | DataObjects | DTOs + options |
| `FreeGLBA.App.ApiRequestLoggingAttribute.cs` | FreeGLBA | Main filter attribute |
| `FreeGLBA.App.SkipApiLoggingAttribute.cs` | FreeGLBA | Skip marker attribute |
| `FreeGLBA.App.DataAccess.ApiRequestLog.cs` | DataAccess | CRUD + cleanup |
| `FreeGLBA.App.ApiRequestLogs.razor` | Client | List page with filters |
| `FreeGLBA.App.ViewApiRequestLog.razor` | Client | Detail page |

---

## Configuration

```json
// appsettings.json
{
  "ApiLogging": {
    "IncludeRequestBody": false,
    "IncludeResponseBody": false,
    "BodyLogLimit": 4096,
    "RetentionDays": 90,
    "SensitiveHeaders": [
      "Authorization",
      "X-Api-Key", 
      "Cookie",
      "Set-Cookie"
    ]
  }
}
```

---

## Risk Summary

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Infinite loop | Low | High | `[SkipApiLogging]` + self-detection |
| Storage growth | Medium | Medium | 90-day retention + cleanup job |
| Performance impact | Low | Low | Async writes + truncation |
| PII in logs | Medium | Medium | Opt-in bodies, header filtering |
| Log write failures | Low | Low | Fallback to Serilog |

---

## Your Approval Needed

### Confirm Design Decisions

| Decision | Proposed | Your Call |
|----------|----------|-----------|
| **Approach** | Action filter attribute | ☐ Approve / ☐ Change |
| **Body logging** | Opt-in, 4KB truncation | ☐ Approve / ☐ Change |
| **Retention** | 90 days + cleanup job | ☐ Approve / ☐ Change: ___ days |
| **UI** | Build in V1 | ☐ Approve / ☐ Defer |
| **Header filtering** | Strip auth/cookies/keys | ☐ Approve / ☐ Modify |

### Confirm Scope

| In Scope (V1) | Out of Scope (V2+) |
|---------------|-------------------|
| ☐ Attribute-based logging | ☐ PII auto-redaction |
| ☐ Stopwatch timing | ☐ Request replay |
| ☐ Both directions | ☐ SIEM export |
| ☐ Infinite loop prevention | ☐ Alerting |
| ☐ Admin UI with filters | ☐ Per-tenant retention |
| ☐ 90-day retention | |

---

## Response Format

```
DESIGN APPROVAL:
☐ Approved as proposed
☐ Approved with changes: ___

SCOPE CONFIRMATION:
☐ V1 scope confirmed
☐ Add to V1: ___
☐ Remove from V1: ___

RETENTION PERIOD:
☐ 90 days (recommended)
☐ Other: ___ days

READY TO IMPLEMENT:
☐ Yes, proceed
☐ No, discuss: ___
```

---

*Consolidated from: 108, 109, 110, 111, 112*  
*Ready for implementation upon approval*

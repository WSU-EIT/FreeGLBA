# 111 — CTO Brief: API Request Logging - Industry Validated Design

> **Document ID:** 111  
> **Category:** CTO Brief  
> **Purpose:** Final decision summary after industry deep dive  
> **Source:** 108_meeting_api_request_logging.md, 110_meeting_api_request_logging_deep_dive.md  
> **Date:** 2025-01-27  
> **Status:** ⏳ Awaiting CTO final approval

---

## Executive Summary

Following the initial design meeting (doc 108), the team conducted an industry deep dive (doc 110) comparing our approach against:

- **ASP.NET Core** HTTP Logging & W3CLogger
- **Serilog** Request Logging
- **Azure Application Insights** Request Telemetry
- **Azure API Management** Event Hub Logging
- **Dataverse/SQL Server** Audit Logging
- **Moesif/Seq** Dedicated API Logging Services

**Conclusion:** Our design aligns with industry best practices. The deep dive identified several improvements to adopt.

---

## What Industry Leaders Do

| Solution | Body Logging | Truncation | Retention | Header Filtering |
|----------|--------------|------------|-----------|------------------|
| **ASP.NET Core** | Opt-in | 4KB default | N/A (log files) | ❌ |
| **App Insights** | Off by default | N/A | 90 days | N/A |
| **Azure APIM** | Yes | 8KB | Configurable | ✅ Strips auth headers |
| **Dataverse** | N/A | N/A | Cleanup job | N/A |
| **Moesif** | Yes | Configurable | Tiered pricing | ✅ |
| **Our Design** | Opt-in | 4KB default | 90 days + cleanup | ✅ |

**Key insight:** Every major solution warns about PII and performance. All recommend opt-in body logging with truncation.

---

## Updated Design Based on Research

### Added from Industry Best Practices

| Addition | Source | Why |
|----------|--------|-----|
| **Opt-in body logging** | ASP.NET Core | Default off, explicitly enable |
| **4KB truncation** | ASP.NET Core | Battle-tested default |
| **Sensitive header stripping** | Azure APIM | Don't log Authorization, cookies, API keys |
| **Retention cleanup job** | Dataverse | "Must have" — logs grow forever otherwise |
| **Store actual size with truncated body** | APIM | Know if data was truncated |

### Configuration Model (New)

```csharp
public class ApiLoggingOptions
{
    public ApiLoggingScope Scope { get; set; } = ApiLoggingScope.ExternalOnly;
    
    public bool LogRequestBody { get; set; } = false;   // OFF by default
    public bool LogResponseBody { get; set; } = false;  // OFF by default
    public int BodyLogLimit { get; set; } = 4096;       // 4KB
    
    public List<string> SensitiveHeaders { get; set; } = new() {
        "Authorization", "X-Api-Key", "Cookie", "Set-Cookie"
    };
    
    public int RetentionDays { get; set; } = 90;
}
```

---

## Your Original Questions — Updated Recommendations

### Decision 1: Body Logging

| Option | Before Deep Dive | After Deep Dive |
|--------|------------------|-----------------|
| A) Full bodies | "Maximum debugging" | ❌ **Not recommended** — MSFT warns against |
| B) Truncated 4KB | "Team recommended" | ✅ **Confirmed** — Industry standard |
| C) Metadata only | "Acceptable" | ⚠️ **Default** — Enable B when needed |

**Updated Recommendation:** **Start with C (metadata only), allow B (truncated) via config.**

This matches ASP.NET Core's approach: metadata by default, opt-in for bodies.

```json
// appsettings.json - Default (metadata only)
"ApiLogging": {
    "Scope": "ExternalOnly"
}

// appsettings.json - Enable body logging when debugging
"ApiLogging": {
    "Scope": "ExternalOnly",
    "LogRequestBody": true,
    "LogResponseBody": true,
    "BodyLogLimit": 4096
}
```

**Your call:** Accept updated recommendation? Or force a specific option?

---

### Decision 2: Retention

| Option | Before Deep Dive | After Deep Dive |
|--------|------------------|-----------------|
| A) Forever | "Full audit trail" | ❌ **Not recommended** — Dataverse explicitly warns |
| B) 90 days | "Team recommended" | ✅ **Confirmed** — App Insights default |
| C) Configurable | "More complex" | ⚠️ Good for V2 |

**Updated Recommendation:** **90 days with mandatory cleanup job in V1.**

Dataverse documentation explicitly states: "Create a plan for how long you'll keep logged data and use the included cleanup job."

**Your call:** Accept 90 days? Or specify different value: ___ days?

---

### Decision 3: Admin UI

| Option | Before Deep Dive | After Deep Dive |
|--------|------------------|-----------------|
| A) Build now | "2-3 days effort" | ✅ **Confirmed** — Industry expectation |
| B) Database only | "Query via SSMS" | ❌ Not user-friendly |
| C) Later | "Acceptable" | ⚠️ Data without visibility is waste |

**Updated Recommendation:** **Build UI in V1.**

Every logging solution (App Insights, Seq, Moesif, APIM) has a UI. Logging without visibility is pointless for admins.

**Your call:** A / B / C

---

## New Decision: Header Filtering

Industry research revealed we need to strip sensitive headers. Azure APIM does this by default.

**Proposed sensitive headers to exclude from logs:**
- `Authorization` (tokens, basic auth)
- `X-Api-Key` (our API keys)
- `Cookie` / `Set-Cookie` (session data)
- `X-CSRF-Token`
- `X-Auth-Token`

**Your call:** Accept list? Add/remove any?

---

## Implementation Impact

### Files to Create (Updated List)

| File | Project | Notes |
|------|---------|-------|
| `FreeGLBA.App.ApiRequestLog.cs` | EFModels | Entity with updated fields |
| `FreeGLBA.App.DataObjects.ApiRequestLog.cs` | DataObjects | DTOs + `ApiLoggingOptions` |
| `FreeGLBA.App.ApiRequestLoggingMiddleware.cs` | Server | With header filtering |
| `FreeGLBA.App.DataAccess.ApiRequestLog.cs` | DataAccess | CRUD + cleanup job |
| `FreeGLBA.App.ApiRequestLogs.razor` | Client | List page with filters |
| `FreeGLBA.App.ViewApiRequestLog.razor` | Client | Detail page |

### Background Job (Required for V1)

```csharp
// Runs daily, deletes logs older than RetentionDays
public class ApiLogCleanupJob : IHostedService
{
    public async Task ExecuteAsync()
    {
        var cutoff = DateTime.UtcNow.AddDays(-options.RetentionDays);
        await dataAccess.DeleteApiLogsOlderThan(cutoff);
    }
}
```

---

## Risk Mitigation (Updated)

| Risk | Mitigation from Industry |
|------|-------------------------|
| **PII in logs** | Opt-in body logging (off by default) |
| **Storage growth** | 90-day retention + cleanup job |
| **Performance** | Async writes + truncation |
| **Security headers** | Strip sensitive headers |
| **Log injection** | HTML encode in UI |

---

## Your Response Format

```
Decision 1 (Bodies): 
  [ ] Accept: Metadata default, opt-in truncated bodies
  [ ] Override: Always log bodies
  [ ] Override: Never log bodies

Decision 2 (Retention):
  [ ] Accept: 90 days with cleanup job
  [ ] Override: ___ days
  [ ] Override: Forever (team advises against)

Decision 3 (UI):
  [ ] A - Build now (recommended)
  [ ] B - Database only
  [ ] C - Later

Decision 4 (Sensitive Headers):
  [ ] Accept proposed list
  [ ] Modify: Add ___, Remove ___

Additional notes:
```

---

## Summary Comparison

| Aspect | Our Original Design | After Industry Deep Dive |
|--------|---------------------|-------------------------|
| Body logging | Always (truncated) | **Opt-in** (truncated when enabled) |
| Default truncation | "Configurable" | **4KB** (ASP.NET standard) |
| Header logging | All headers | **Filtered** (strip auth/cookies) |
| Retention | "TBD" | **90 days + cleanup job** |
| UI | "Optional" | **Required** |

The deep dive validated our architecture while adding important security and operational refinements.

---

*Created: 2025-01-27*  
*Source discussions: 108, 110*

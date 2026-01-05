# 116 — Post-Focus Group: Team Analysis

> **Document ID:** 116  
> **Category:** Analysis  
> **Purpose:** Team review and prioritization of focus group feedback  
> **Attendees:** [Architect], [Backend], [Frontend], [Quality], [Sanity]  
> **Date:** 2025-01-27  
> **Outcome:** Prioritized action items for updated design

---

## Meeting Purpose

The team reviews the focus group session (Doc 115) and decides what to incorporate into our final design.

---

## Focus Group Feedback Summary

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    FOCUS GROUP FEEDBACK CATEGORIZATION                      │
└─────────────────────────────────────────────────────────────────────────────┘

                    ┌─────────────────────────────────────┐
                    │         ALL FEEDBACK (27 items)     │
                    └──────────────────┬──────────────────┘
                                       │
           ┌───────────────────────────┼───────────────────────────┐
           │                           │                           │
           ▼                           ▼                           ▼
  ┌─────────────────┐         ┌─────────────────┐         ┌─────────────────┐
  │   VALIDATED     │         │  NEW MUST-HAVE  │         │    NICE-TO-HAVE │
  │   (5 items)     │         │   (9 items)     │         │    (13 items)   │
  │                 │         │                 │         │                 │
  │ Things we       │         │ Things we       │         │ V2 features     │
  │ already had     │         │ need to add     │         │ and enhancements│
  │ right           │         │ for V1          │         │                 │
  └─────────────────┘         └─────────────────┘         └─────────────────┘
```

---

## Detailed Analysis

### What We Got Right (Validated)

**[Architect]:** Good news first. These items were validated by the focus group:

| Item | Validation Quote | Status |
|------|------------------|--------|
| Attribute-based logging | "That's exactly how authorization works, makes sense" | ✅ Keep |
| 90-day hot retention | "90 days hot, then archive is fine" | ✅ Keep |
| Opt-in body logging | "Bodies should never be on by default" | ✅ Keep |
| Correlation ID | "Essential for end-to-end tracing" | ✅ Keep |
| Strip sensitive headers | "Don't log Authorization headers" | ✅ Keep |

**[Quality]:** That's 5 out of 5 core decisions validated. Our industry research was on point.

---

### New Must-Have Requirements

**[Backend]:** These items came out of the focus group that we MUST add:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     NEW MUST-HAVE REQUIREMENTS                              │
└─────────────────────────────────────────────────────────────────────────────┘

  ┌───────────────────────────────────────────────────────────────────────────┐
  │  #1: TIME-LIMITED BODY LOGGING                                            │
  ├───────────────────────────────────────────────────────────────────────────┤
  │                                                                           │
  │  CURRENT:                           PROPOSED:                             │
  │  ════════                           ═════════                             │
  │  Body logging ON/OFF                Body logging with EXPIRATION          │
  │                                                                           │
  │  ┌─────────────┐                    ┌─────────────────────────────────┐   │
  │  │ Bodies: ON  │                    │ Bodies: ON for Banner           │   │
  │  │ (forever)   │      ═══►          │ Expires: 2025-01-28 14:00       │   │
  │  └─────────────┘                    │ Enabled by: Admin               │   │
  │                                     │ Reason: Debugging issue #1234   │   │
  │                                     └─────────────────────────────────┘   │
  │                                                                           │
  │  • Auto-expires after duration (default 24h)                              │
  │  • Logs WHO enabled it and WHEN                                           │
  │  • Logs the REASON for enabling                                           │
  │  • Notification before expiry                                             │
  └───────────────────────────────────────────────────────────────────────────┘

  ┌───────────────────────────────────────────────────────────────────────────┐
  │  #2: TIERED RETENTION                                                     │
  ├───────────────────────────────────────────────────────────────────────────┤
  │                                                                           │
  │  DAY 0          DAY 90              DAY 365             FOREVER           │
  │  │               │                   │                   │                │
  │  │◄─── HOT ─────►│◄──── WARM ───────►│◄──── COLD ───────►│                │
  │  │               │                   │                   │                │
  │  │  Database     │  Blob Storage     │  Archive/Delete   │                │
  │  │  Full UI      │  Bulk Export      │  On Request       │                │
  │  │  Fast Query   │  Slower Query     │  Manual Restore   │                │
  │  │               │                   │                   │                |
  │                                                                           │
  │  V1: Implement hot tier + cleanup job                                     │
  │  V2: Add warm tier archival                                               │
  └───────────────────────────────────────────────────────────────────────────┘

  ┌───────────────────────────────────────────────────────────────────────────┐
  │  #3: DASHBOARD VIEW                                                       │
  ├───────────────────────────────────────────────────────────────────────────┤
  │                                                                           │
  │  Focus group was unanimous: "The dashboard mockup is exactly what we      │
  │  need. Please build that."                                                │
  │                                                                           │
  │  REQUIRED ELEMENTS:                                                       │
  │  ┌───────────────┐  ┌───────────────┐  ┌───────────────┐                  │
  │  │ Total Requests│  │ Total Errors  │  │  Error Rate   │                  │
  │  │    Today      │  │    Today      │  │      %        │                  │
  │  └───────────────┘  └───────────────┘  └───────────────┘                  │
  │                                                                           │
  │  ┌───────────────────────────────────────────────────────┐                │
  │  │             REQUEST RATE OVER TIME CHART              │                │
  │  └───────────────────────────────────────────────────────┘                │
  │                                                                           │
  │  ┌─────────────────────┐  ┌─────────────────────────────┐                 │
  │  │  BY SOURCE SYSTEM   │  │     RECENT ERRORS          │                 │
  │  │  (pie or bar)       │  │     (table, 5-10 rows)     │                 │
  │  └─────────────────────┘  └─────────────────────────────┘                 │
  │                                                                           │
  └───────────────────────────────────────────────────────────────────────────┘

  ┌───────────────────────────────────────────────────────────────────────────┐
  │  #4: EXPORT WITH LIMITS                                                   │
  ├───────────────────────────────────────────────────────────────────────────┤
  │                                                                           │
  │  • Export current view to CSV/Excel                                       │
  │  • Limit: 10,000 rows in immediate download                               │
  │  • Larger exports: "Request via email" queued job                         │
  │  • Export includes current filters applied                                │
  │                                                                           │
  └───────────────────────────────────────────────────────────────────────────┘

  ┌───────────────────────────────────────────────────────────────────────────┐
  │  #5: DURATION FILTER                                                      │
  ├───────────────────────────────────────────────────────────────────────────┤
  │                                                                           │
  │  "Show requests where DurationMs > 1000"                                  │
  │                                                                           │
  │  UI: Dropdown or slider                                                   │
  │  ┌────────────────────────────────────────────┐                           │
  │  │ Duration: [ All ▼ ]                        │                           │
  │  │           [ All               ]            │                           │
  │  │           [ > 100ms           ]            │                           │
  │  │           [ > 500ms           ]            │                           │
  │  │           [ > 1 second        ]  ◄─────────┼── DBA wants this          │
  │  │           [ > 5 seconds       ]            │                           │
  │  └────────────────────────────────────────────┘                           │
  │                                                                           │
  └───────────────────────────────────────────────────────────────────────────┘

  ┌───────────────────────────────────────────────────────────────────────────┐
  │  #6: SEPARATE LOG DATABASE - ❌ REJECTED BY CTO                           │
  ├───────────────────────────────────────────────────────────────────────────┤
  │                                                                           │
  │  DBA recommended: "Don't log to the same database as transactions"        │
  │                                                                           │
  │  CTO DECISION: Use single database                                        │
  │  • Simplicity over complexity                                             │
  │  • Expected volume: few thousand requests/day                             │
  │  • Performance impact acceptable for reduced complexity                   │
  │  • Only one GLBA system, not expecting massive scale                      │
  │                                                                           │
  │  ❌ NOT IMPLEMENTING separate LoggingConnectionString                     │
  │                                                                           │
  └───────────────────────────────────────────────────────────────────────────┘

  ┌───────────────────────────────────────────────────────────────────────────┐
  │  #7: BODY LOGGING AUDIT TRAIL                                             │
  ├───────────────────────────────────────────────────────────────────────────┤
  │                                                                           │
  │  When body logging is enabled, log:                                       │
  │  • WHO enabled it (admin user)                                            │
  │  • WHEN it was enabled                                                    │
  │  • WHY (reason/ticket number)                                             │
  │  • WHICH source system                                                    │
  │  • WHEN it will expire                                                    │
  │                                                                           │
  │  New entity: BodyLoggingAudit                                             │
  │  ├── BodyLoggingAuditId                                                   │
  │  ├── SourceSystemId                                                       │
  │  ├── EnabledByUserId                                                      │
  │  ├── EnabledAt                                                            │
  │  ├── ExpiresAt                                                            │
  │  ├── Reason                                                               │
  │  └── DisabledAt (null until manually disabled or expired)                 │
  │                                                                           │
  └───────────────────────────────────────────────────────────────────────────┘

  ┌───────────────────────────────────────────────────────────────────────────┐
  │  #8: PII WARNING IN UI                                                    │
  ├───────────────────────────────────────────────────────────────────────────┤
  │                                                                           │
  │  When enabling body logging, show prominent warning:                      │
  │                                                                           │
  │  ┌────────────────────────────────────────────────────────────────────┐   │
  │  │  ⚠️  WARNING: Body Logging Contains PII                           │   │
  │  │                                                                    │   │
  │  │  Request bodies may contain personally identifiable information   │   │
  │  │  (PII) including student IDs and financial data. This data        │   │
  │  │  becomes subject to GLBA and FERPA regulations.                   │   │
  │  │                                                                    │   │
  │  │  • Use only for active debugging                                  │   │
  │  │  • Limit duration to what's necessary                             │   │
  │  │  • Do not export body data unnecessarily                          │   │
  │  │                                                                    │   │
  │  │  [ ] I understand the compliance implications                     │   │
  │  │                                                                    │   │
  │  │  [Cancel]                            [Enable for 24 hours]        │   │
  │  └────────────────────────────────────────────────────────────────────┘   │
  │                                                                           │
  └───────────────────────────────────────────────────────────────────────────┘

  ┌───────────────────────────────────────────────────────────────────────────┐
  │  #9: AUTO-REFRESH TOGGLE                                                  │
  ├───────────────────────────────────────────────────────────────────────────┤
  │                                                                           │
  │  ┌──────────────────────────────────────────────────────────────────┐     │
  │  │  [🔄 Auto-refresh: OFF ▼]                                        │     │
  │  │                 [ OFF          ]                                 │     │
  │  │                 [ Every 30 sec ]  ◄── For active debugging       │     │
  │  │                 [ Every 1 min  ]                                 │     │
  │  │                 [ Every 5 min  ]                                 │     │
  │  └──────────────────────────────────────────────────────────────────┘     │
  │                                                                           │
  │  SysAdmin: "When debugging actively, I want to see requests as they       │
  │             happen. When analyzing history, I don't want it jumping."     │
  │                                                                           │
  └───────────────────────────────────────────────────────────────────────────┘
```

---

### Nice-to-Have (V2 Backlog)

**[Sanity]:** These are great ideas but not critical for launch:

| Feature | Source | Priority | V2 Notes |
|---------|--------|----------|----------|
| Copy as cURL | APIDev | Medium | Debugging convenience |
| Alerting on thresholds | SecurityAnalyst | Medium | Requires notification system |
| API access to logs | DBA | Medium | REST endpoint for log queries |
| Scheduled reports | ComplianceOfficer | Low | Email automation |
| Week-over-week comparison | SysAdmin | Low | Trending/analytics |
| Request replay (careful) | APIDev | Low | Dangerous, needs safeguards |
| Warm tier archival | DBA | Medium | Azure Blob integration |
| Log integrity proof | ComplianceOfficer | Low | Tamper-evident logging |

---

## Impact on Entity Design

**[Backend]:** Based on feedback, here's the entity changes:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      ENTITY DESIGN CHANGES                                  │
└─────────────────────────────────────────────────────────────────────────────┘

  BEFORE (from Doc 113)                  AFTER (with focus group feedback)
  ═════════════════════                  ═════════════════════════════════

  ApiRequestLogItem                      ApiRequestLogItem
  ├── ApiRequestLogId                    ├── ApiRequestLogId
  ├── SourceSystemId                     ├── SourceSystemId
  ├── SourceSystemName                   ├── SourceSystemName
  ├── UserId                             ├── UserId
  ├── UserName                           ├── UserName
  ├── TenantId                           ├── TenantId
  ├── HttpMethod                         ├── HttpMethod
  ├── RequestPath                        ├── RequestPath
  ├── QueryString                        ├── QueryString
  ├── RequestHeaders                     ├── RequestHeaders
  ├── RequestBody                        ├── RequestBody
  ├── RequestBodySize                    ├── RequestBodySize
  ├── RequestedAt                        ├── RequestedAt
  ├── RespondedAt                        ├── RespondedAt
  ├── DurationMs                         ├── DurationMs
  ├── IpAddress                          ├── IpAddress
  ├── UserAgent                          ├── UserAgent
  ├── ForwardedFor                       ├── ForwardedFor
  ├── StatusCode                         ├── StatusCode
  ├── IsSuccess                          ├── IsSuccess
  ├── ResponseBody                       ├── ResponseBody
  ├── ResponseBodySize                   ├── ResponseBodySize
  ├── ErrorMessage                       ├── ErrorMessage
  ├── ExceptionType                      ├── ExceptionType
  ├── CorrelationId                      ├── CorrelationId
  ├── AuthType                           ├── AuthType
  ├── RelatedEntityId                    ├── RelatedEntityId
  └── RelatedEntityType                  ├── RelatedEntityType
                                         └── BodyLoggingEnabled ◄── NEW
                                             (bool - was body captured?)

  NEW ENTITY:                            
  ══════════                             
                                         
  BodyLoggingConfig                       
  ├── BodyLoggingConfigId                 
  ├── SourceSystemId                      
  ├── EnabledByUserId                     
  ├── EnabledAt                           
  ├── ExpiresAt                           
  ├── Reason                              
  ├── IsActive                            
  └── DisabledAt                          
```

---

## Updated Configuration Model

**[Backend]:** Configuration changes:

```csharp
public class ApiLoggingOptions
{
    // Core settings (unchanged)
    public int BodyLogLimit { get; set; } = 4096;
    public int RetentionDays { get; set; } = 90;
    public List<string> SensitiveHeaders { get; set; } = new() { ... };
    
    // NEW: Body logging defaults
    public int DefaultBodyLoggingDurationHours { get; set; } = 24;
    public int MaxBodyLoggingDurationHours { get; set; } = 72;
    
    // NEW: Export limits
    public int MaxExportRows { get; set; } = 10000;
    
    // NEW: Dashboard refresh
    public int DashboardRefreshSeconds { get; set; } = 30;
}
```

---

## Updated File List

**[Architect]:** Revised files to create:

| File | Project | Purpose | Status |
|------|---------|---------|--------|
| `FreeGLBA.App.ApiRequestLog.cs` | EFModels | Main entity | Updated |
| `FreeGLBA.App.BodyLoggingConfig.cs` | EFModels | **NEW** - Body logging audit | New |
| `FreeGLBA.App.DataObjects.ApiRequestLog.cs` | DataObjects | DTOs + options | Updated |
| `FreeGLBA.App.ApiRequestLoggingAttribute.cs` | FreeGLBA | Filter attribute | Same |
| `FreeGLBA.App.SkipApiLoggingAttribute.cs` | FreeGLBA | Skip marker | Same |
| `FreeGLBA.App.DataAccess.ApiRequestLog.cs` | DataAccess | CRUD + cleanup | Updated |
| `FreeGLBA.App.ApiLogDashboard.razor` | Client | **NEW** - Dashboard view | New |
| `FreeGLBA.App.ApiRequestLogs.razor` | Client | List page | Updated |
| `FreeGLBA.App.ViewApiRequestLog.razor` | Client | Detail page | Same |
| `FreeGLBA.App.BodyLoggingSettings.razor` | Client | **NEW** - Body logging config | New |

---

## Effort Estimate Update

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      EFFORT ESTIMATE COMPARISON                             │
└─────────────────────────────────────────────────────────────────────────────┘

  COMPONENT                    BEFORE FOCUS GROUP    AFTER FOCUS GROUP
  ═════════                    ══════════════════    ═════════════════

  EF Entity + Migrations            0.5 days             1 day (+body config)
  DataAccess Layer                  0.5 days             0.5 days
  Logging Attribute                 1 day                1 day
  List Page                         1 day                1 day
  Detail Page                       0.5 days             0.5 days
  Dashboard (NEW)                   —                    1.5 days
  Body Logging Config UI (NEW)      —                    0.5 days
  Export Feature (NEW)              —                    0.5 days
  Auto-refresh (NEW)                —                    0.25 days
  Testing                           1 day                1.5 days
  ─────────────────────────────────────────────────────────────────────
  TOTAL                             4.5 days             8.25 days

  INCREASE: +3.75 days (~83% more)
  REASON: Dashboard + body logging config + export are significant additions
```

**[Sanity]:** Is the extra scope worth it?

**[Architect]:** Yes. The focus group was clear: without the dashboard, we're building a feature people won't use. It's the first thing everyone looks at.

**[Frontend]:** The dashboard is the most visible value-add. Worth the investment.

---

## Decision: What's In V1?

**[Architect]:** Let's confirm V1 scope with the new requirements:

### V1 Scope (8.25 days)

| Feature | Priority | Include |
|---------|----------|---------|
| Core logging attribute | P0 | ✅ |
| Entity + migrations | P0 | ✅ |
| List page with filters | P0 | ✅ |
| Detail page | P0 | ✅ |
| **Dashboard** | P1 | ✅ (focus group priority) |
| **Export with limits** | P1 | ✅ (compliance need) |
| **Duration filter** | P1 | ✅ (debugging need) |
| **Auto-refresh toggle** | P2 | ✅ (low effort) |
| Time-limited body logging | P1 | ✅ |
| Body logging audit trail | P1 | ✅ |
| **PII warning dialog** | P1 | ✅ (compliance) |
| ~~Separate log database~~ | — | ❌ Rejected by CTO (simplicity) |
| 90-day cleanup job | P0 | ✅ |

### V2 Backlog

| Feature | Effort |
|---------|--------|
| Warm tier archival (Azure Blob) | Medium |
| Copy as cURL | Low |
| Alerting on thresholds | Medium |
| API endpoint for log queries | Medium |
| Scheduled reports | Low |
| Week-over-week comparison | Low |

---

## Next Steps

| Action | Owner | Due |
|--------|-------|-----|
| Update CTO proposal with focus group findings | [Architect] | Doc 117 |
| Get CTO approval on expanded scope | [CTO] | Before dev |
| Begin implementation | [Backend] | After approval |

---

**[Architect]:** The focus group was extremely valuable. We're adding ~4 days of work, but we're building something people will actually use.

**[Quality]:** The compliance officer and security analyst feedback especially valuable. We would have missed the body logging audit trail.

**[Frontend]:** That dashboard is going to be the star of the show. Glad we're prioritizing it.

---

*Analysis completed: 2025-01-27*  
*Next: CTO Proposal (Doc 117)*

# 117 — CTO Proposal: API Request Logging - Final Design with Focus Group Input

> **Document ID:** 117  
> **Category:** CTO Proposal  
> **Purpose:** Final comprehensive proposal incorporating all research and focus group validation  
> **Source Documents:** 108-116 (full design journey)  
> **Date:** 2025-01-27  
> **Status:** ⏳ Ready for CTO final approval

---

## 📋 Executive Summary

This proposal presents our API request logging feature after extensive internal design, industry research, CTO feedback, and external focus group validation.

**The Ask:** Build comprehensive API request logging for GLBA compliance and debugging.

**Timeline:** 8.25 days (expanded from 4.5 days based on focus group feedback)

**Key Addition:** Dashboard view — focus group unanimous that this is essential.

---

## 🗺️ Design Journey

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         DESIGN JOURNEY OVERVIEW                             │
└─────────────────────────────────────────────────────────────────────────────┘

  DOC 108                DOC 110               DOC 112              DOC 115
  Initial Design         Industry Research     CTO Feedback         Focus Group
  ═══════════════        ════════════════      ════════════         ═══════════
       │                       │                    │                    │
       ▼                       ▼                    ▼                    ▼
  ┌─────────┐            ┌─────────┐          ┌─────────┐          ┌─────────┐
  │ Entity  │            │ Compare │          │ Switch  │          │ Validate│
  │ Design  │───────────►│ to ASP  │─────────►│ to      │─────────►│ with    │
  │         │            │ NET/APIM│          │ Attrib  │          │ 5 Users │
  └─────────┘            └─────────┘          └─────────┘          └─────────┘
       │                       │                    │                    │
       │                       │                    │                    │
       ▼                       ▼                    ▼                    ▼
  • WHO/WHAT/WHEN        • 4KB truncation      • [ApiRequest         • Dashboard
  • Fire-and-forget      • Strip headers         Logging]            • Time-limit
  • Denormalize          • 90-day retention    • [SkipApiLogging]      body log
  • CorrelationId        • Cleanup job         • Stopwatch timing    • Export
                         • Opt-in bodies       • Mandatory           • Audit trail
                                                                     
                                 │
                                 ▼
                    ┌─────────────────────────────┐
                    │   DOC 117 - THIS PROPOSAL   │
                    │   Final Consolidated Design │
                    └─────────────────────────────┘
```

---

## 🏗️ System Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    COMPLETE API LOGGING ARCHITECTURE                        │
└─────────────────────────────────────────────────────────────────────────────┘


  EXTERNAL SYSTEMS                    FREEGLBA SERVER
  ════════════════                    ══════════════════════════════════════

  ┌─────────────┐                    ┌────────────────────────────────────────┐
  │   Banner    │                    │                                        │
  │  (Student)  │──┐                 │   ┌──────────────────────────────────┐ │
  └─────────────┘  │                 │   │         GlbaController           │ │
                   │   HTTPS         │   │    [ApiRequestLogging]           │ │
  ┌─────────────┐  │   POST          │   │                                  │ │
  │     HR      │──┼────────────────►│   │  ┌──────────────────────────┐    │ │
  │   System    │  │                 │   │  │  OnActionExecuting       │    │ │
  └─────────────┘  │                 │   │  │  ════════════════════    │    │ │
                   │                 │   │  │  1. Check [SkipApiLog]   │    │ │
  ┌─────────────┐  │                 │   │  │  2. Start Stopwatch      │    │ │
  │  Financial  │──┘                 │   │  │  3. Capture Request      │    │ │
  │    Aid      │                    │   │  │  4. Check Body Config    │    │ │
  └─────────────┘                    │   │  └───────────┬──────────────┘    │ │
                                     │   │              │                   │ │
                                     │   │              ▼                   │ │
                                     │   │  ┌──────────────────────────┐    │ │
                                     │   │  │  Controller Action       │    │ │
                                     │   │  │  (Business Logic)        │    │ │
                                     │   │  └───────────┬──────────────┘    │ │
                                     │   │              │                   │ │
                                     │   │              ▼                   │ │
                                     │   │  ┌──────────────────────────┐    │ │
                                     │   │  │  OnActionExecuted        │    │ │
                                     │   │  │  ════════════════════    │    │ │
                                     │   │  │  1. Stop Stopwatch       │    │ │
                                     │   │  │  2. Capture Response     │    │ │
                                     │   │  │  3. Build Log Entry      │    │ │
                                     │   │  │  4. Fire-and-forget save │    │ │
                                     │   │  └───────────┬──────────────┘    │ │
                                     │   └──────────────┼──────────────────┘ │
                                     │                  │                    │
                                     └──────────────────┼────────────────────┘
                                                        │
                                                        │ Async Write
                                                        ▼
  ┌─────────────────────────────────────────────────────────────────────────────┐
  │                              DATABASE LAYER                                  │
  ├─────────────────────────────────────────────────────────────────────────────┤
  │                                                                              │
  │  ┌─────────────────────────────┐    ┌──────────────────────────────────┐    │
  │  │      ApiRequestLogs         │    │      BodyLoggingConfigs          │    │
  │  │  ┌──────┬──────┬──────┐     │    │  ┌──────────┬───────────────┐    │    │
  │  │  │ WHO  │ WHAT │RESULT│     │    │  │SourceSys │ Enabled Until │    │    │
  │  │  │      │      │      │     │    │  │ ID       │ Timestamp     │    │    │
  │  │  └──────┴──────┴──────┘     │    │  └──────────┴───────────────┘    │    │
  │  │   • 90 days hot             │    │   • Audit trail for body log     │    │
  │  │   • Cleanup job daily       │    │   • Auto-expires                 │    │
  │  └─────────────────────────────┘    └──────────────────────────────────┘    │
  │                                                                              │
  │  Single database - simplicity over separate logging DB (CTO decision)       │
  │  Expected volume: ~few thousand requests/day - no performance concern       │
  │                                                                              │
  └─────────────────────────────────────────────────────────────────────────────┘
```

---

## 📊 User Interface Components

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         UI COMPONENT HIERARCHY                              │
└─────────────────────────────────────────────────────────────────────────────┘


                    ┌─────────────────────────────────────┐
                    │        API Logs Navigation          │
                    │  [Dashboard] [List] [Settings]      │
                    └─────────────────┬───────────────────┘
                                      │
            ┌─────────────────────────┼─────────────────────────┐
            │                         │                         │
            ▼                         ▼                         ▼
  ┌─────────────────────┐   ┌─────────────────────┐   ┌─────────────────────┐
  │     DASHBOARD       │   │      LIST VIEW      │   │     SETTINGS        │
  │  ApiLogDashboard    │   │   ApiRequestLogs    │   │  BodyLoggingSettings│
  │     .razor          │   │      .razor         │   │      .razor         │
  └─────────────────────┘   └─────────────────────┘   └─────────────────────┘
            │                         │                         │
            │                         │                         │
            ▼                         ▼                         ▼
  ┌─────────────────────┐   ┌─────────────────────┐   ┌─────────────────────┐
  │ • Total Requests    │   │ • Filterable table  │   │ • Per-source toggle │
  │ • Error Count       │   │ • Time range picker │   │ • Duration selector │
  │ • Request Rate Graph│   │ • Source filter     │   │ • Reason field      │
  │ • By Source Chart   │   │ • Status filter     │   │ • Expiry display    │
  │ • Recent Errors     │   │ • Duration filter   │   │ • PII Warning       │
  │ • Auto-refresh      │   │ • Export button     │   │ • Audit trail       │
  └─────────────────────┘   │ • Click → Detail    │   └─────────────────────┘
                            └──────────┬──────────┘
                                       │
                                       ▼
                            ┌─────────────────────┐
                            │    DETAIL VIEW      │
                            │  ViewApiRequestLog  │
                            │      .razor         │
                            ├─────────────────────┤
                            │ • Full request info │
                            │ • Headers (filtered)│
                            │ • Body (if enabled) │
                            │ • Response details  │
                            │ • Timing breakdown  │
                            └─────────────────────┘
```

---

## 📋 Dashboard Wireframe

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        API REQUEST LOG DASHBOARD                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  [🔄 Auto-refresh: OFF ▼]              Time Range: [Last 24 Hours ▼]       │
│                                                                             │
│  ╔═══════════════════════════════════════════════════════════════════════╗ │
│  ║                          SUMMARY CARDS                                ║ │
│  ╠═════════════════╦═════════════════╦═════════════════╦═════════════════╣ │
│  ║                 ║                 ║                 ║                 ║ │
│  ║     8,421       ║       127       ║      1.5%       ║      234ms      ║ │
│  ║    REQUESTS     ║     ERRORS      ║   ERROR RATE    ║   AVG DURATION  ║ │
│  ║       ✓         ║       ⚠         ║                 ║                 ║ │
│  ║                 ║                 ║                 ║                 ║ │
│  ╚═════════════════╩═════════════════╩═════════════════╩═════════════════╝ │
│                                                                             │
│  ╔═════════════════════════════════════════════════════════════════════╗   │
│  ║                     REQUEST RATE (PER MINUTE)                       ║   │
│  ╠═════════════════════════════════════════════════════════════════════╣   │
│  ║                                                                     ║   │
│  ║  25│                                    ╭───╮                       ║   │
│  ║    │       ╭────╮    ╭────────╮        │   │                       ║   │
│  ║  20│      ╭╯    ╰╮  ╭╯        ╰╮      ╭╯   ╰╮         ╭───╮        ║   │
│  ║    │     ╭╯      ╰──╯          ╰──────╯     ╰────╮   ╭╯   │        ║   │
│  ║  15│    ╭╯                                       ╰───╯    │        ║   │
│  ║    │───╯                                                  ╰────    ║   │
│  ║  10│                                                               ║   │
│  ║    └─────────────────────────────────────────────────────────────  ║   │
│  ║     8am      10am      12pm       2pm       4pm      NOW           ║   │
│  ║                                                                     ║   │
│  ╚═════════════════════════════════════════════════════════════════════╝   │
│                                                                             │
│  ╔═══════════════════════════╗  ╔═══════════════════════════════════════╗  │
│  ║   BY SOURCE SYSTEM        ║  ║         BY STATUS CODE                ║  │
│  ╠═══════════════════════════╣  ╠═══════════════════════════════════════╣  │
│  ║                           ║  ║                                       ║  │
│  ║  Banner   ██████████ 50%  ║  ║  ✓ 2xx ████████████████████████ 92%  ║  │
│  ║  HR       ██████ 25%      ║  ║  ⚠ 4xx ███ 5%                        ║  │
│  ║  FinAid   ████ 15%        ║  ║  ✗ 5xx █ 2%                          ║  │
│  ║  Other    ██ 10%          ║  ║  ⚡ Timeout █ 1%                      ║  │
│  ║                           ║  ║                                       ║  │
│  ╚═══════════════════════════╝  ╚═══════════════════════════════════════╝  │
│                                                                             │
│  ╔═══════════════════════════════════════════════════════════════════════╗ │
│  ║                          RECENT ERRORS                                ║ │
│  ╠═══════════════════════════════════════════════════════════════════════╣ │
│  ║  TIME      SOURCE    METHOD  PATH           STATUS  ERROR            ║ │
│  ║  ──────────────────────────────────────────────────────────────────  ║ │
│  ║  14:32:01  Banner    POST    /api/events    400     Invalid SubjectId║ │
│  ║  14:28:45  HR Sys    POST    /api/events    500     DB timeout       ║ │
│  ║  14:15:22  Banner    POST    /api/batch     400     Missing Purpose  ║ │
│  ║  13:45:11  FinAid    POST    /api/events    401     Expired API key  ║ │
│  ║  13:22:08  Banner    POST    /api/events    500     Null reference   ║ │
│  ║                                                                       ║ │
│  ║                            [View All Errors →]                       ║ │
│  ╚═══════════════════════════════════════════════════════════════════════╝ │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔒 Body Logging Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     BODY LOGGING DECISION FLOW                              │
└─────────────────────────────────────────────────────────────────────────────┘


  REQUEST ARRIVES                      
        │                              
        ▼                              
  ┌─────────────────┐                 
  │ Check Body      │                 
  │ Logging Config  │                 
  └────────┬────────┘                 
           │                          
           ▼                          
  ┌─────────────────────┐             ┌─────────────────────────────────────┐
  │ Is body logging     │───No───────►│ Log metadata only                   │
  │ enabled for this    │             │ • Method, Path, Status              │
  │ source system?      │             │ • Headers (filtered)                │
  └─────────┬───────────┘             │ • NO request/response body          │
            │                         │ • BodyLoggingEnabled = false        │
            │ Yes                     └─────────────────────────────────────┘
            ▼                         
  ┌─────────────────────┐             
  │ Is the config       │───No───────►┌─────────────────────────────────────┐
  │ expired?            │             │ Auto-disable expired config         │
  │ (ExpiresAt < Now)   │             │ Update BodyLoggingConfig.IsActive   │
  └─────────┬───────────┘             │ Then log metadata only              │
            │                         └─────────────────────────────────────┘
            │ No (still valid)        
            ▼                         
  ┌─────────────────────────────────────────────────────────────────────────┐
  │                    LOG WITH BODY                                        │
  ├─────────────────────────────────────────────────────────────────────────┤
  │                                                                         │
  │  • Capture request body (truncated to 4KB)                              │
  │  • Capture response body (truncated to 4KB)                             │
  │  • BodyLoggingEnabled = true                                            │
  │  • Store actual sizes (RequestBodySize, ResponseBodySize)               │
  │                                                                         │
  │  ⚠️  PII WARNING: Bodies may contain sensitive data!                    │
  │                                                                         │
  └─────────────────────────────────────────────────────────────────────────┘
```

---

## ⏱️ Timing Capture Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         STOPWATCH TIMING FLOW                               │
└─────────────────────────────────────────────────────────────────────────────┘


  OnActionExecuting                    OnActionExecuted
  ═════════════════                    ════════════════

  ┌─────────────────┐                  ┌─────────────────┐
  │ var stopwatch = │                  │ stopwatch.Stop()│
  │ Stopwatch.Start │                  │                 │
  │ New()           │                  │ DurationMs =    │
  │                 │                  │ stopwatch       │
  │ RequestedAt =   │                  │ .ElapsedMillis  │
  │ DateTime.UtcNow │                  │                 │
  │                 │                  │ RespondedAt =   │
  │ DateTime.UtcNow │                  │ DateTime.UtcNow │
  │                 │                  │                 │
  │ Store in        │                  │                 │
  │ HttpContext     │                  │                 │
  │ .Items          │                  │                 │
  └────────┬────────┘                  └────────┬────────┘
           │                                    │
           │         ┌──────────────────┐       │
           │         │                  │       │
           └────────►│ Controller       │◄──────┘
                     │ Action           │
                     │ Executes         │
                     │                  │
                     │ (timed)          │
                     │                  │
                     └──────────────────┘

  RESULT IN LOG:
  ┌─────────────────────────────────────────────────────────────────────────┐
  │  RequestedAt: 2025-01-27 14:32:01.234                                   │
  │  RespondedAt: 2025-01-27 14:32:01.567                                   │
  │  DurationMs: 333                                                        │
  └─────────────────────────────────────────────────────────────────────────┘
```

---

## 📁 Implementation Files

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        FILE STRUCTURE                                       │
└─────────────────────────────────────────────────────────────────────────────┘


  FreeGLBA/
  ├── FreeGLBA.EFModels/
  │   ├── FreeGLBA.App.ApiRequestLog.cs           ◄── Main entity
  │   └── FreeGLBA.App.BodyLoggingConfig.cs       ◄── Body log audit (NEW)
  │
  ├── FreeGLBA.DataObjects/
  │   └── FreeGLBA.App.DataObjects.ApiRequestLog.cs
  │       ├── ApiRequestLogDto
  │       ├── ApiRequestLogListDto
  │       ├── ApiLoggingOptions                   ◄── Configuration
  │       └── BodyLoggingConfigDto
  │
  ├── FreeGLBA.DataAccess/
  │   └── FreeGLBA.App.DataAccess.ApiRequestLog.cs
  │       ├── CreateLog()
  │       ├── GetLogs(filters)
  │       ├── GetDashboardStats()                 ◄── Dashboard data (NEW)
  │       ├── CleanupOldLogs()
  │       ├── GetBodyLoggingConfig()
  │       └── SetBodyLoggingConfig()
  │
  ├── FreeGLBA/
  │   ├── FreeGLBA.App.ApiRequestLoggingAttribute.cs
  │   │   ├── OnActionExecuting()
  │   │   └── OnActionExecuted()
  │   │
  │   └── FreeGLBA.App.SkipApiLoggingAttribute.cs  ◄── Marker attribute
  │
  └── FreeGLBA.Client/
      ├── FreeGLBA.App.ApiLogDashboard.razor       ◄── Dashboard (NEW)
      ├── FreeGLBA.App.ApiRequestLogs.razor        ◄── List view
      ├── FreeGLBA.App.ViewApiRequestLog.razor     ◄── Detail view
      └── FreeGLBA.App.BodyLoggingSettings.razor   ◄── Config UI (NEW)
```

---

## ⏰ Effort Breakdown

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         EFFORT ESTIMATE: 8.25 DAYS                          │
└─────────────────────────────────────────────────────────────────────────────┘


  COMPONENT                                              DAYS    CUMULATIVE
  ═════════                                              ════    ══════════

  ┌─────────────────────────────────────────────────┐
  │ EF Entities + Migrations                        │    1.0   ████
  │ • ApiRequestLog entity                          │
  │ • BodyLoggingConfig entity                      │
  │ • Database migrations                           │
  └─────────────────────────────────────────────────┘

  ┌─────────────────────────────────────────────────┐
  │ DataAccess Layer                                │    0.5   ██
  │ • CRUD operations                               │
  │ • Dashboard stats query                         │
  │ • Cleanup job                                   │
  └─────────────────────────────────────────────────┘

  ┌─────────────────────────────────────────────────┐
  │ Logging Attribute                               │    1.0   ████
  │ • ApiRequestLoggingAttribute                    │
  │ • SkipApiLoggingAttribute                       │
  │ • Stopwatch timing                              │
  │ • Body logging check                            │
  └─────────────────────────────────────────────────┘

  ┌─────────────────────────────────────────────────┐
  │ Dashboard View                                  │    1.5   ██████
  │ • Summary cards                                 │
  │ • Request rate chart                            │
  │ • By source/status charts                       │
  │ • Recent errors table                           │
  │ • Auto-refresh                                  │
  └─────────────────────────────────────────────────┘

  ┌─────────────────────────────────────────────────┐
  │ List View                                       │    1.0   ████
  │ • Filterable table                              │
  │ • Time/source/status/duration filters           │
  │ • Export button                                 │
  └─────────────────────────────────────────────────┘

  ┌─────────────────────────────────────────────────┐
  │ Detail View                                     │    0.5   ██
  │ • Full request details                          │
  │ • Body display (if available)                   │
  └─────────────────────────────────────────────────┘

  ┌─────────────────────────────────────────────────┐
  │ Body Logging Settings UI                        │    0.5   ██
  │ • Per-source toggle                             │
  │ • Duration picker                               │
  │ • PII warning modal                             │
  └─────────────────────────────────────────────────┘

  ┌─────────────────────────────────────────────────┐
  │ Export Feature                                  │    0.5   ██
  │ • CSV generation                                │
  │ • Row limits                                    │
  └─────────────────────────────────────────────────┘

  ┌─────────────────────────────────────────────────┐
  │ Testing                                         │    1.5   ██████
  │ • Unit tests                                    │
  │ • Integration tests                             │
  │ • Manual testing                                │
  └─────────────────────────────────────────────────┘

  ═══════════════════════════════════════════════════════════════════
  TOTAL                                              8.25 days
  ═══════════════════════════════════════════════════════════════════
```

---

## ✅ What's IN V1

| Feature | Source | Priority |
|---------|--------|----------|
| `[ApiRequestLogging]` attribute | CTO requirement | P0 |
| `[SkipApiLogging]` attribute | CTO requirement | P0 |
| Stopwatch timing | CTO requirement | P0 |
| Entity + migrations | Core | P0 |
| 90-day cleanup job | Industry research | P0 |
| List view with filters | Core | P0 |
| Detail view | Core | P0 |
| **Dashboard** | Focus group | P1 |
| **Duration filter** | Focus group (DBA) | P1 |
| **Export with limits** | Focus group (Compliance) | P1 |
| **Auto-refresh toggle** | Focus group (SysAdmin) | P2 |
| **Time-limited body logging** | Focus group (all) | P1 |
| **Body logging audit trail** | Focus group (Compliance) | P1 |
| **PII warning modal** | Focus group (Compliance) | P1 |

---

## ❌ What's NOT in V1 (V2 Backlog)

| Feature | Reason | Effort |
|---------|--------|--------|
| Warm tier archival | Requires Azure Blob | Medium |
| Copy as cURL | Convenience feature | Low |
| Alerting | Requires notification system | Medium |
| API for logs | Nice-to-have | Medium |
| Scheduled reports | Nice-to-have | Low |
| Week-over-week | Analytics feature | Low |

---

## 🔐 Risk Mitigation

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         RISK MITIGATION MATRIX                              │
└─────────────────────────────────────────────────────────────────────────────┘

  RISK                    LIKELIHOOD   IMPACT    MITIGATION
  ════                    ══════════   ══════    ══════════

  Infinite log loop         Low        High      [SkipApiLogging] attribute
  ─────────────────────────────────────────────────────────────────────────

  Storage explosion         Medium     Medium    90-day retention + cleanup
  ─────────────────────────────────────────────────────────────────────────

  PII in body logs          Medium     High      Opt-in + audit + warning
  ─────────────────────────────────────────────────────────────────────────

  Performance impact        Low        Medium    Async writes + truncation
  ─────────────────────────────────────────────────────────────────────────

  Log write failure         Low        Low       Fallback to Serilog
  ─────────────────────────────────────────────────────────────────────────

  Scope creep               Medium     Medium    Clear V1/V2 boundary
  ─────────────────────────────────────────────────────────────────────────
```

---

## 📝 CTO Approval Checklist

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         CTO APPROVAL FORM                                   │
└─────────────────────────────────────────────────────────────────────────────┘

  DESIGN VALIDATION:
  ══════════════════

  ☐ Attribute-based approach confirmed
  ☐ Stopwatch timing confirmed
  ☐ Infinite loop prevention confirmed
  ☐ Both directions (request + response) confirmed

  SCOPE APPROVAL:
  ═══════════════

  ☐ Dashboard included in V1 (focus group priority)
  ☐ Body logging audit trail included
  ☐ Export feature included
  ☐ V2 items deferred as listed

  EFFORT APPROVAL:
  ═════════════════

  ☐ 8.25 days approved
    (expanded from 4.5 days based on focus group)

  CONFIGURATION:
  ══════════════

  Retention period: _____ days (default: 90)
  Max body logging duration: _____ hours (default: 72)
  Export row limit: _____ rows (default: 10,000)

  NOTES:
  ══════

  _______________________________________________________
  _______________________________________________________
  _______________________________________________________

  APPROVAL:
  ═════════

  ☐ APPROVED - Proceed with implementation
  ☐ APPROVED WITH CHANGES - See notes
  ☐ NOT APPROVED - Discuss further

  Signature: ________________________
  Date: ____________________________

```

---

*Document consolidates: 108, 109, 110, 111, 112, 113, 114, 115, 116*  
*Ready for CTO final approval*

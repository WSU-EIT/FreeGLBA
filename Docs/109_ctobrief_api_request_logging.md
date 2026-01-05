# 109 — CTO Brief: API Request Logging

> **Document ID:** 109  
> **Category:** CTO Brief  
> **Purpose:** Decision summary for API request logging feature  
> **Source:** 108_meeting_api_request_logging.md  
> **Date:** 2025-01-27  
> **Status:** ⏳ Awaiting CTO decisions

---

## Executive Summary

The team proposes adding comprehensive API request logging to track all external (and optionally internal) API calls. This provides audit trails, debugging capability, and abuse detection for the FreeGLBA system.

**Why now:** Currently, when source systems submit events via the external API, we have no record of the raw requests. If something fails or is disputed, we can't verify what was actually sent.

---

## What the Team Agreed On

| Decision | Details |
|----------|---------|
| **Implementation** | Middleware-based logging (cleanest approach) |
| **Storage** | New `ApiRequestLogs` database table |
| **Scope default** | External API only (`/api/glba/*` POST routes) |
| **Async writes** | Fire-and-forget to avoid blocking responses |
| **Fallback** | Log to Serilog if database write fails |
| **Denormalization** | Store SourceSystemName/UserName directly in log |
| **Entity linking** | Add `RelatedEntityId` to link logs to created entities |

---

## Decisions Needed From You

### Decision 1: Request/Response Body Logging

Should we store the actual request and response bodies?

| Option | Storage Impact | Debug Value | Risk |
|--------|----------------|-------------|------|
| **A) Full bodies** | High (~2-10KB per log) | Maximum | PII stored in another table |
| **B) Truncated (4KB)** | Medium | Good for most cases | Large payloads cut off |
| **C) Metadata only** | Low (~500B per log) | Limited | Can't see what was sent |

**Team recommendation:** Option B (truncated) — balances debugging needs with storage concerns. We already store the processed data in AccessEvents; logs are for debugging, not archival.

**Your call:** A / B / C

---

### Decision 2: Retention Policy

How long do we keep API request logs?

| Option | Storage | Compliance | Maintenance |
|--------|---------|------------|-------------|
| **A) Forever** | Grows indefinitely | Full audit trail | None |
| **B) 90 days rolling** | Bounded | Recent history only | Auto-cleanup job |
| **C) Configurable** | Varies | Flexible per tenant | More complexity |

**Team recommendation:** Option B (90 days) — provides sufficient debugging window without unbounded growth. AccessEvents are the permanent record; these logs are operational.

**Your call:** A / B / C (or specify different window: ___ days)

---

### Decision 3: Admin UI

Do we build a UI for viewing logs now?

| Option | Effort | Immediate Value | Notes |
|--------|--------|-----------------|-------|
| **A) Build UI now** | ~2-3 days | High | Filter by source, date, status; drill into details |
| **B) Database only** | 0 days | Low | Query via SSMS/pgAdmin for now |
| **C) Build later** | 0 now | Deferred | Start collecting, view when needed |

**Team recommendation:** Option A (build now) — if we're logging this data, we should be able to use it without writing SQL. The UI is straightforward (table + filters + detail view).

**Your call:** A / B / C

---

## Proposed Entity Structure

```
ApiRequestLogItem
├── ApiRequestLogId (PK)
│
├── WHO
│   ├── SourceSystemId, SourceSystemName
│   ├── UserId, UserName
│   └── TenantId
│
├── WHAT
│   ├── HttpMethod, RequestPath, QueryString
│   ├── RequestBody (if approved), RequestBodySize
│   └── ContentType
│
├── WHEN
│   ├── RequestedAt, RespondedAt
│   └── DurationMs
│
├── WHERE
│   ├── IpAddress, UserAgent
│   ├── ForwardedFor, Origin, Referer
│
├── RESULT
│   ├── StatusCode, IsSuccess
│   ├── ResponseBody (if approved), ResponseBodySize
│   ├── ErrorMessage, ExceptionType
│
└── CONTEXT
    ├── CorrelationId, RequestId
    ├── AuthType, ApiVersion
    ├── RelatedEntityId, RelatedEntityType
    └── Metadata (JSON)
```

---

## Files To Be Created

Following the `{ProjectName}.App.{Feature}` naming convention:

| File | Project | Purpose |
|------|---------|---------|
| `FreeGLBA.App.ApiRequestLog.cs` | EFModels | Database entity |
| `FreeGLBA.App.DataObjects.ApiRequestLog.cs` | DataObjects | DTOs |
| `FreeGLBA.App.ApiRequestLoggingMiddleware.cs` | FreeGLBA (Server) | Request capture |
| `FreeGLBA.App.DataAccess.ApiRequestLog.cs` | DataAccess | CRUD operations |
| `FreeGLBA.App.ApiRequestLogs.razor` | Client | List page (if UI) |
| `FreeGLBA.App.ViewApiRequestLog.razor` | Client | Detail page (if UI) |

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Storage growth | Medium | Medium | Retention policy + truncation |
| Performance impact | Low | Low | Async writes, fire-and-forget |
| PII in logs | Medium | Medium | Admin-only access, HTML encoding |
| Log injection attacks | Low | Low | HTML encode in UI display |

---

## Your Response Format

Please respond with your decisions:

```
Decision 1 (Bodies): A / B / C
Decision 2 (Retention): A / B / C (or ___ days)
Decision 3 (UI): A / B / C

Additional notes: [any other guidance]
```

Once we have your decisions, [Backend] will proceed with implementation.

---

*Created: 2025-01-27*  
*Source discussion: 108_meeting_api_request_logging.md*

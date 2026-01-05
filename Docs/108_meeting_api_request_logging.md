# 108 — Meeting: API Request Logging Design

> **Document ID:** 108  
> **Category:** Meeting  
> **Purpose:** Design discussion for adding comprehensive API request logging to FreeGLBA  
> **Attendees:** [Architect], [Backend], [Frontend], [Quality], [Sanity], [JrDev]  
> **Date:** 2025-01-27  
> **Predicted Outcome:** Agreed design for ApiRequestLog entity, middleware, and UI  
> **Actual Outcome:** *(to be updated)*  
> **Resolution:** *(to be updated with PR link)*

---

## Context

FreeGLBA currently generates API keys for source systems to submit GLBA access events, but there's no logging of the API requests themselves. The CTO wants to track the "who, what, when, where, and why" of every API call for auditing and debugging purposes.

---

## Discussion

**[Architect]:** Let's frame this. We have two categories of API endpoints: external (API key auth via middleware) and internal (user auth via `[Authorize]`). The question is: do we log both, or just external?

**[Backend]:** From a compliance perspective, we should log everything. But from a practical standpoint, internal endpoints are already tied to authenticated users—we have that context. The external API is where we're flying blind. We only know the source system from the API key.

**[JrDev]:** Wait, so right now if an external system calls our API and something goes wrong, we have no record of what they sent?

**[Backend]:** Correct. We process the event, but we don't keep a log of the raw request. If they claim they sent something and we didn't process it, we have no way to verify.

**[Architect]:** So the primary use case is: audit trail for external API calls, debugging failed requests, and potentially rate limiting or abuse detection down the road.

**[Quality]:** Security concern—if we're logging request bodies, we might capture sensitive data. The `GlbaEventRequest` contains `UserId`, `SubjectId`, `Purpose`... that's PII-adjacent.

**[Sanity]:** Mid-check—are we overcomplicating this? Could we just use standard ASP.NET Core logging or Serilog request logging?

**[Backend]:** Standard logging gives us timestamps and status codes, but not the structured data we need. We want to query things like "show me all failed requests from SourceSystem X in the last 24 hours" or "what was the request body for this specific failed call?"

**[Architect]:** Agreed. We need a database table, not just log files. Let me propose the entity structure.

---

## Proposed Entity: ApiRequestLogItem

**[Backend]:** Here's what I'm thinking for the table:

```
WHO (Identity)
├── SourceSystemId (Guid?) — for external API calls
├── SourceSystemName (string) — denormalized for quick reference
├── UserId (Guid?) — for internal API calls
├── UserName (string) — denormalized
└── TenantId (Guid?) — if multi-tenant

WHAT (Request Details)
├── HttpMethod (string) — GET, POST, etc.
├── RequestPath (string) — /api/glba/events
├── QueryString (string) — any query params
├── RequestBody (string) — JSON body (may truncate)
├── RequestBodySize (long) — actual size in bytes
└── ContentType (string)

WHEN (Timing)
├── RequestedAt (DateTime)
├── RespondedAt (DateTime?)
└── DurationMs (long)

WHERE (Origin)
├── IpAddress (string)
├── UserAgent (string)
├── Referer (string)
├── ForwardedFor (string) — for proxied requests
└── Origin (string) — CORS requests

RESULT (Outcome)
├── StatusCode (int)
├── IsSuccess (bool)
├── ResponseBody (string) — may truncate
├── ResponseBodySize (long)
├── ErrorMessage (string)
└── ExceptionType (string)

CONTEXT (Metadata)
├── CorrelationId (string)
├── RequestId (string)
├── AuthType (string) — ApiKey, Bearer, Cookie, None
├── ApiVersion (string)
└── Metadata (string) — JSON for extras
```

**[Frontend]:** That's a lot of columns. Do we really need all of them?

**[Backend]:** We don't have to populate all of them. The entity is designed to capture what's available. If there's no `ForwardedFor` header, it stays empty.

**[Quality]:** I like denormalizing `SourceSystemName` and `UserName`. If we delete a source system, the logs still make sense.

---

## Implementation Approach

**[Architect]:** For implementation, I see three options:

1. **Middleware** — Sits in the pipeline, logs every request
2. **Action Filter** — Attribute-based, more targeted
3. **Manual logging** — Call a log method in each controller action

**[Backend]:** Middleware is cleanest. We already have `ApiKeyMiddleware` that validates API keys. We could add `ApiRequestLoggingMiddleware` right after it.

**[JrDev]:** Wouldn't middleware log even requests that fail authentication?

**[Backend]:** Yes, and that's actually valuable! We'd see failed auth attempts, invalid API keys, etc.

**[Architect]:** Let's use middleware. The flow would be:

```
Request In
    → ApiRequestLoggingMiddleware (start timer, capture request)
    → ApiKeyMiddleware (for /api/glba/* POST routes)
    → Controller action
    → ApiRequestLoggingMiddleware (capture response, save log)
Response Out
```

**[Quality]:** How do we handle the request body? We can only read it once in ASP.NET Core.

**[Backend]:** We enable request buffering with `EnableBuffering()`, read the body, then reset the stream position. Same pattern used by many logging middlewares.

---

## Scope: Which Endpoints?

**[Sanity]:** Do we log ALL endpoints or just the external API?

**[Architect]:** Good question. Options:

| Option | Pros | Cons |
|--------|------|------|
| **All endpoints** | Complete audit trail | High volume, noise from internal calls |
| **External only** (`/api/glba/*` POST) | Focused on the problem | Miss internal API abuse |
| **Configurable** | Flexible | More complex |

**[Backend]:** I'd vote for configurable with a sensible default. Start with external API only, but allow expanding later.

**[Frontend]:** From a UI perspective, if we're logging internal endpoints, the log viewer gets much busier. We'd need good filtering.

**[Quality]:** Could we have a setting like `ApiLogging:Scope` with values `External`, `All`, or `None`?

**[Architect]:** Yes. Let's make it configurable. Default to external only.

---

## ⏸️ **CTO Input Needed**

**Question 1:** Should we log request/response bodies?

**Options:**
1. **Yes, full bodies** — Maximum debugging info, but storage and PII concerns
2. **Yes, truncated** (e.g., first 4KB) — Balance of info vs storage
3. **No bodies, just metadata** — Safest, but less useful for debugging

**Question 2:** What's the retention policy?

**Options:**
1. **Keep forever** — Full audit trail
2. **Rolling window** (e.g., 90 days) — Balance storage vs history
3. **Configurable per tenant** — Maximum flexibility

**Question 3:** Do we need a UI to view logs, or is database access sufficient for now?

**Options:**
1. **Admin UI page** — Table with filters, drill-down to details
2. **Database only** — Query directly, build UI later
3. **Export only** — No live view, but can export to CSV

@CTO — Which way on each?

---

## Security Considerations

**[Quality]:** A few security items to address:

1. **PII in bodies** — Request bodies contain `UserId`, `SubjectId`, etc. If we store them, that's sensitive data in another table.

2. **Log injection** — Malicious actors could craft requests with payloads designed to exploit log viewers (XSS if we render logs in UI).

3. **Access control** — Who can view API logs? Admin only? Or source system owners can see their own?

**[Backend]:** For PII, we could offer a "redact sensitive fields" option that masks things like `SubjectId` in stored logs.

**[Architect]:** Let's keep it simple for V1: Admin-only access, store bodies as-is (they're already in our AccessEvents table anyway), and HTML-encode when displaying.

**[Quality]:** Agreed. We can add redaction later if needed.

---

## Performance Considerations

**[Backend]:** Logging adds overhead. A few things to consider:

1. **Async writes** — Don't block the response waiting for the log to save
2. **Batching** — Could batch log writes, but adds complexity
3. **Sampling** — For high-volume endpoints, log every Nth request

**[Sanity]:** For FreeGLBA's scale, I don't think we need batching or sampling. This isn't a high-traffic consumer API. It's enterprise source systems sending events.

**[Backend]:** Agreed. Let's do fire-and-forget async writes. If a log fails to save, we don't fail the request—we just lose that log entry.

**[Quality]:** Should we at least log to Serilog if the database write fails?

**[Backend]:** Yes, good catch. Fallback to Serilog on database failure.

---

## File Naming

**[Architect]:** Following the naming convention:

| File | Purpose |
|------|---------|
| `FreeGLBA.App.ApiRequestLog.cs` | EF entity in EFModels |
| `FreeGLBA.App.DataObjects.ApiRequestLog.cs` | DTO in DataObjects |
| `FreeGLBA.App.ApiRequestLoggingMiddleware.cs` | Middleware in Server |
| `FreeGLBA.App.DataAccess.ApiRequestLog.cs` | CRUD methods in DataAccess |
| `FreeGLBA.App.ApiRequestLogs.razor` | List page in Client (if UI) |
| `FreeGLBA.App.ViewApiRequestLog.razor` | Detail page in Client (if UI) |

---

## Sanity Final Check

**[Sanity]:** Final check—did we miss anything obvious?

1. ✅ We know what to log (request metadata + optionally bodies)
2. ✅ We know where to log (new database table)
3. ✅ We know how to log (middleware)
4. ⚠️ We need CTO input on bodies, retention, and UI
5. ✅ We've considered security and performance
6. ✅ File naming follows convention

**[JrDev]:** One thing—how do we correlate an API request log with the AccessEvent it created?

**[Backend]:** Great catch! We should add the `AccessEventId` (or list of IDs for batch) to the log's `Metadata` field. Or add a dedicated `RelatedEntityId` column.

**[Architect]:** Let's add `RelatedEntityId` (Guid?) and `RelatedEntityType` (string). That way we can link to AccessEvents, or any other entity type in the future.

---

## Decisions

1. **Use middleware** for logging (not action filters or manual)
2. **Configurable scope** — Default to external API only, allow expansion
3. **Denormalize names** — Store SourceSystemName/UserName directly
4. **Async fire-and-forget** — Don't block responses for logging
5. **Fallback logging** — Use Serilog if database write fails
6. **Add RelatedEntityId** — Link logs to the entities they created

## Open Questions (For CTO)

1. Log request/response bodies? (Full / Truncated / No)
2. Retention policy? (Forever / Rolling window / Configurable)
3. Build UI now? (Yes / Database only / Later)

## Next Steps

| Action | Owner | Priority |
|--------|-------|----------|
| Get CTO decisions on open questions | [CTO] | P0 |
| Create EF entity `FreeGLBA.App.ApiRequestLog.cs` | [Backend] | P1 |
| Create DTO `FreeGLBA.App.DataObjects.ApiRequestLog.cs` | [Backend] | P1 |
| Implement middleware | [Backend] | P1 |
| Add DataAccess methods | [Backend] | P1 |
| Build UI (if approved) | [Frontend] | P2 |
| Add integration tests | [Quality] | P2 |

---

*Created: 2025-01-27*  
*Maintained by: [Quality]*

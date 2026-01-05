# 110 — Meeting: API Request Logging Deep Dive - Industry Comparison

> **Document ID:** 110  
> **Category:** Meeting  
> **Purpose:** Deep dive comparing our API logging design against industry solutions  
> **Attendees:** [Architect], [Backend], [Frontend], [Quality], [Sanity], [JrDev]  
> **Date:** 2025-01-27  
> **Predicted Outcome:** Validated design with industry best practices incorporated  
> **Actual Outcome:** *(to be updated)*  
> **Resolution:** *(to be updated with PR link)*

---

## Context

Following our initial design meeting (doc 108), we're doing a deep dive to compare our proposed `ApiRequestLog` design against how the industry handles API request logging. We'll examine ASP.NET Core's built-in options, third-party solutions, and cloud services.

---

## Industry Solutions Analyzed

**[Architect]:** Let's systematically compare our approach against what's out there. I've researched six major categories:

| Category | Solutions Analyzed |
|----------|-------------------|
| **Built-in .NET** | ASP.NET Core HTTP Logging Middleware, W3CLogger |
| **Third-Party Logging** | Serilog RequestLogging, NLog |
| **APM/Telemetry** | Application Insights, OpenTelemetry |
| **API Gateways** | Azure API Management, AWS API Gateway |
| **Database Audit** | Dataverse Auditing, SQL Server Audit, pgAudit |
| **Dedicated Services** | Seq, Datadog, Moesif |

---

## Comparison: ASP.NET Core Built-in HTTP Logging

**[Backend]:** ASP.NET Core 8+ has built-in HTTP logging middleware. Let's compare:

### ASP.NET Core `UseHttpLogging()`

```csharp
builder.Services.AddHttpLogging(options => {
    options.LoggingFields = HttpLoggingFields.All;
    options.RequestBodyLogLimit = 4096;
    options.ResponseBodyLogLimit = 4096;
    options.CombineLogs = true;
});
```

| Feature | ASP.NET Built-in | Our Design |
|---------|------------------|------------|
| **Storage** | Log files (ILogger) | Database table |
| **Queryable** | No (grep/search) | Yes (SQL queries) |
| **Request body** | ✅ Configurable | ✅ Configurable |
| **Response body** | ✅ Configurable | ✅ Configurable |
| **Body truncation** | ✅ 4KB default | ✅ Configurable |
| **Sensitive data redaction** | ✅ Built-in | ❌ Manual (V2) |
| **Duration tracking** | ✅ Yes | ✅ Yes |
| **Correlation ID** | ✅ Yes | ✅ Yes |
| **Source system context** | ❌ No | ✅ Yes (our custom) |
| **User context** | ❌ Claims only | ✅ Full user object |
| **Tenant awareness** | ❌ No | ✅ Yes |
| **Endpoint filtering** | ✅ Attribute-based | ✅ Configurable |
| **Performance impact** | ⚠️ "Can reduce performance" | ⚠️ Similar |

**[JrDev]:** Why don't we just use the built-in middleware?

**[Backend]:** Three reasons:
1. **Queryability** — We need to search "show failed requests from SourceSystem X last 24 hours." Log files don't support that without shipping to Elasticsearch or similar.
2. **Context** — Built-in logging doesn't know about our SourceSystem or authenticated user. It only sees raw HTTP.
3. **Retention control** — With a database, we control retention policies per-tenant if needed.

**[Quality]:** The built-in middleware explicitly warns about PII and performance. We should heed that.

**[Architect]:** Agreed. Let's adopt their defaults: 4KB body truncation, explicit opt-in for body logging.

---

## Comparison: W3C Logger

**[Backend]:** ASP.NET Core also has W3CLogger for W3C standard format logging:

| Feature | W3CLogger | Our Design |
|---------|-----------|------------|
| **Format** | W3C extended log file | JSON/Database |
| **Standardized** | ✅ Industry standard | ❌ Custom |
| **Tooling** | Log analyzers (AWStats, etc.) | Custom UI |
| **Body logging** | ❌ No | ✅ Yes |
| **Real-time query** | ❌ No | ✅ Yes |

**[Sanity]:** W3C format is for web server logs. We're doing API audit logging—different use case.

**[Architect]:** Correct. W3C is great for traffic analysis but not API debugging.

---

## Comparison: Serilog Request Logging

**[Backend]:** Serilog is the most popular .NET logging library. Their request logging enricher:

```csharp
app.UseSerilogRequestLogging(options => {
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) => {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"]);
    };
});
```

| Feature | Serilog | Our Design |
|---------|---------|------------|
| **Structured logging** | ✅ Excellent | ✅ Yes (JSON metadata) |
| **Sinks (destinations)** | ✅ 100+ (Seq, SQL, Elastic) | Database only |
| **Request body** | ⚠️ Custom code needed | ✅ Built-in |
| **Enrichment** | ✅ Flexible | ✅ Context-aware |
| **Query language** | Depends on sink | SQL |

**[Architect]:** Serilog is fantastic for general logging. But for API audit trails, we want:
1. Guaranteed database persistence (not dependent on sink configuration)
2. Tight integration with our SourceSystem/User models
3. Queryable UI without external tools

**[Quality]:** We should still log to Serilog as fallback if database write fails. That was decided in doc 108.

---

## Comparison: Application Insights

**[Backend]:** Azure Application Insights auto-collects request telemetry:

| Feature | App Insights | Our Design |
|---------|--------------|------------|
| **Auto-collection** | ✅ Zero config | ❌ Middleware needed |
| **Request tracking** | ✅ Built-in | ✅ Custom |
| **Body logging** | ❌ Not by default | ✅ Yes |
| **Dependency tracking** | ✅ Yes | ❌ No (scope limited) |
| **Distributed tracing** | ✅ Excellent | ⚠️ Correlation ID only |
| **Query language** | KQL | SQL |
| **Cost** | 💰 Per-GB ingestion | Included (our database) |
| **Data sovereignty** | ⚠️ Azure regions | ✅ Our database |
| **Retention** | 90 days (default) | Configurable |

**[JrDev]:** Why not just use App Insights?

**[Frontend]:** Cost and control. App Insights charges per GB ingested. For a compliance system that logs every request with bodies, that adds up. Plus, some customers want data on-premises.

**[Architect]:** App Insights is complementary, not replacement. Use it for APM (Application Performance Monitoring). Use our table for API audit trail.

---

## Comparison: Azure API Management Logging

**[Backend]:** Azure APIM has sophisticated logging to Event Hubs:

| Feature | APIM | Our Design |
|---------|------|------------|
| **Request/Response bodies** | ✅ Yes (truncated to 8KB) | ✅ Yes (configurable) |
| **Policy-based** | ✅ XML policies | N/A (code-based) |
| **Event Hubs integration** | ✅ Native | ❌ Not needed |
| **Message correlation** | ✅ Custom message-id | ✅ Correlation ID |
| **Security header stripping** | ✅ Built-in | ⚠️ Should add |

**[Quality]:** APIM explicitly strips security-sensitive headers. We should do the same.

**[Architect]:** Good catch. Let's add a list of headers to exclude:
- `Authorization`
- `X-Api-Key`
- `Cookie`
- `Set-Cookie`

**[Backend]:** I'll add a `SensitiveHeaders` configuration list.

---

## Comparison: Database Audit Logging (Dataverse/SQL)

**[Backend]:** Enterprise platforms like Dataverse have audit logging:

| Feature | Dataverse Audit | Our Design |
|---------|-----------------|------------|
| **What's logged** | Field-level changes | Full API request |
| **Who** | ✅ User context | ✅ User + SourceSystem |
| **What** | Field changes | HTTP request |
| **When** | ✅ Timestamp | ✅ Timestamp + duration |
| **Old/New values** | ✅ Yes | N/A (request/response) |
| **Cleanup job** | ✅ Built-in | ⚠️ Need to add |

**[Quality]:** Dataverse recommends: "Create a plan for how long you'll keep logged data and use the included cleanup job." We need that.

**[Architect]:** Yes. Let's add a background job for retention cleanup. Task for V1 or V2?

**[Sanity]:** V1. If we don't have cleanup, the table grows forever. That's a time bomb.

---

## Comparison: Dedicated API Logging Services (Moesif, Seq)

**[Backend]:** Dedicated API analytics services like Moesif capture:

| Field Category | Moesif | Seq | Our Design |
|----------------|--------|-----|------------|
| **Request metadata** | ✅ | ✅ | ✅ |
| **Request body** | ✅ | ✅ | ✅ |
| **Response body** | ✅ | ✅ | ✅ |
| **User identification** | ✅ | ⚠️ | ✅ |
| **Company/Tenant** | ✅ | ❌ | ✅ |
| **Geo-location** | ✅ | ❌ | ❌ |
| **API versioning** | ✅ | ❌ | ✅ |
| **Error analysis** | ✅ | ✅ | ✅ |
| **Self-hosted** | ❌ | ✅ | ✅ |
| **Cost** | 💰💰 | 💰 | Included |

**[Frontend]:** Moesif and similar services are great but expensive and cloud-dependent. Our customers want self-hosted.

---

## Key Learnings from Industry

**[Architect]:** Let me summarize what we should adopt from industry:

### Adopt from ASP.NET Core HTTP Logging:
1. ✅ **4KB default truncation** — Their default is battle-tested
2. ✅ **Opt-in body logging** — Don't log bodies by default
3. ✅ **Duration tracking** — Essential for performance analysis
4. ⚠️ **Redaction** — Consider for V2

### Adopt from Azure API Management:
1. ✅ **Strip sensitive headers** — Authorization, cookies, API keys
2. ✅ **Message correlation** — Already have CorrelationId
3. ✅ **Truncation with size tracking** — Store actual size even if truncated

### Adopt from Application Insights:
1. ✅ **Request telemetry model** — Their fields are well-designed
2. ✅ **operation_Id concept** — Our CorrelationId serves this purpose

### Adopt from Dataverse Auditing:
1. ✅ **Cleanup job** — Must have for retention
2. ✅ **Security on log table** — Admin-only access

### Adopt from Enterprise Patterns:
1. ✅ **Denormalization** — Store names, not just IDs
2. ✅ **Async writes** — Don't block requests
3. ✅ **Fallback logging** — Serilog if DB fails

---

## Revised Entity Design

**[Backend]:** Based on industry comparison, here's the revised entity:

```csharp
public partial class ApiRequestLogItem
{
    [Key]
    public Guid ApiRequestLogId { get; set; }

    // WHO (Identity)
    public Guid? SourceSystemId { get; set; }
    [MaxLength(200)]
    public string SourceSystemName { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    [MaxLength(200)]
    public string UserName { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }

    // WHAT (Request)
    [MaxLength(10)]
    public string HttpMethod { get; set; } = string.Empty;
    [MaxLength(500)]
    public string RequestPath { get; set; } = string.Empty;
    [MaxLength(2000)]
    public string QueryString { get; set; } = string.Empty;
    public string RequestBody { get; set; } = string.Empty;      // Truncated
    public long RequestBodySize { get; set; }                     // Actual size
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;
    [MaxLength(2000)]
    public string RequestHeaders { get; set; } = string.Empty;    // NEW: Filtered headers as JSON

    // WHEN (Timing)
    public DateTime RequestedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public long DurationMs { get; set; }

    // WHERE (Origin)
    [MaxLength(50)]
    public string IpAddress { get; set; } = string.Empty;
    [MaxLength(500)]
    public string UserAgent { get; set; } = string.Empty;
    [MaxLength(200)]
    public string ForwardedFor { get; set; } = string.Empty;

    // RESULT (Response)
    public int StatusCode { get; set; }
    public bool IsSuccess { get; set; }
    public string ResponseBody { get; set; } = string.Empty;      // Truncated
    public long ResponseBodySize { get; set; }                    // Actual size
    [MaxLength(2000)]
    public string ErrorMessage { get; set; } = string.Empty;
    [MaxLength(200)]
    public string ExceptionType { get; set; } = string.Empty;

    // CONTEXT (Correlation)
    [MaxLength(50)]
    public string CorrelationId { get; set; } = string.Empty;
    [MaxLength(50)]
    public string AuthType { get; set; } = string.Empty;
    public Guid? RelatedEntityId { get; set; }                    // Links to created entity
    [MaxLength(100)]
    public string RelatedEntityType { get; set; } = string.Empty; // e.g., "AccessEvent"
    public string Metadata { get; set; } = string.Empty;          // Extra JSON
}
```

### Changes from Original Design:
| Change | Reason |
|--------|--------|
| Added `RequestHeaders` | Industry logs filtered headers |
| Removed `Referer`, `Origin` | Moved to `RequestHeaders` JSON |
| Removed `ApiVersion` | Can derive from path or metadata |
| Added `RelatedEntityId` + `RelatedEntityType` | Per doc 108 decision |
| Clarified body truncation | Store size separately |

---

## Configuration Model

**[Backend]:** Based on industry patterns, here's the configuration:

```csharp
public class ApiLoggingOptions
{
    // Scope control
    public ApiLoggingScope Scope { get; set; } = ApiLoggingScope.ExternalOnly;
    
    // Body logging
    public bool LogRequestBody { get; set; } = false;           // Opt-in per MSFT guidance
    public bool LogResponseBody { get; set; } = false;          // Opt-in per MSFT guidance
    public int RequestBodyLogLimit { get; set; } = 4096;        // 4KB per ASP.NET Core default
    public int ResponseBodyLogLimit { get; set; } = 4096;
    
    // Security
    public List<string> SensitiveHeaders { get; set; } = new() {
        "Authorization", "X-Api-Key", "Cookie", "Set-Cookie", 
        "X-CSRF-Token", "X-Auth-Token"
    };
    
    // Retention
    public int RetentionDays { get; set; } = 90;
    
    // Filtering
    public List<string> ExcludedPaths { get; set; } = new() {
        "/health", "/metrics", "/swagger"
    };
}

public enum ApiLoggingScope
{
    None,
    ExternalOnly,      // Only /api/glba/* POST routes
    AuthenticatedOnly, // All [Authorize] endpoints
    All                // Everything
}
```

---

## ⏸️ **CTO Input Needed — Updated Questions**

Based on industry research, the original questions from doc 109 remain, but with clearer recommendations:

### Decision 1: Request/Response Body Logging

| Option | Industry Precedent | Recommendation |
|--------|-------------------|----------------|
| **A) Full bodies** | Not recommended by MSFT | ❌ |
| **B) Truncated (4KB)** | ASP.NET Core default | ✅ Recommended |
| **C) Metadata only** | App Insights default | ⚠️ Acceptable |

**Updated recommendation:** Option B with opt-in (bodies logged only when explicitly enabled in config).

### Decision 2: Retention Policy

| Option | Industry Precedent |
|--------|-------------------|
| **A) Forever** | Not recommended (Dataverse warns against) |
| **B) 90 days** | App Insights default |
| **C) Configurable** | Azure APIM approach |

**Updated recommendation:** Option B (90 days) with cleanup job in V1.

### Decision 3: Admin UI

| Option | Industry Precedent |
|--------|-------------------|
| **A) Build UI** | Moesif, Seq, App Insights all have UIs |
| **B) Database only** | Not user-friendly |
| **C) Later** | Acceptable |

**Updated recommendation:** Option A — Industry consensus is that logging without a UI is not useful.

@CTO — Confirm or override?

---

## Decisions (Updated)

1. **Use middleware** — Confirmed
2. **4KB body truncation** — Adopted from ASP.NET Core
3. **Opt-in body logging** — Default to metadata only
4. **Strip sensitive headers** — Adopted from APIM
5. **Add cleanup job** — Required for V1
6. **Denormalize names** — Confirmed
7. **Async fire-and-forget** — Confirmed
8. **Fallback to Serilog** — Confirmed

## Next Steps

| Action | Owner | Priority |
|--------|-------|----------|
| Get CTO confirmation on updated recommendations | [CTO] | P0 |
| Add `SensitiveHeaders` filtering to middleware | [Backend] | P1 |
| Implement retention cleanup background job | [Backend] | P1 |
| Create configuration model `ApiLoggingOptions` | [Backend] | P1 |

---

*Created: 2025-01-27*  
*Maintained by: [Quality]*

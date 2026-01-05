# 115 — Focus Group: API Request Logging Validation

> **Document ID:** 115  
> **Category:** Focus Group  
> **Purpose:** External stakeholder validation of API logging design  
> **Facilitator:** [Architect]  
> **Participants:** [ComplianceOfficer], [SysAdmin], [SecurityAnalyst], [APIDev], [DBA]  
> **Date:** 2025-01-27  
> **Duration:** 2 hours  
> **Outcome:** Detailed feedback on logging requirements from real users

---

## Session Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    FOCUS GROUP PARTICIPANT SEATING                          │
└─────────────────────────────────────────────────────────────────────────────┘

                    ┌───────────────────────────────────┐
                    │         PRESENTATION SCREEN       │
                    │  ┌─────────────────────────────┐  │
                    │  │  API REQUEST LOGGING DESIGN │  │
                    │  └─────────────────────────────┘  │
                    └───────────────────────────────────┘
                                    │
          ┌─────────────────────────┼─────────────────────────┐
          │                         │                         │
    ┌─────┴─────┐             ┌─────┴─────┐             ┌─────┴─────┐
    │ Compliance │             │ [Arch]    │             │ Security  │
    │  Officer   │             │ Facilitator│            │ Analyst   │
    └───────────┘             └───────────┘             └───────────┘
          │                                                   │
          │         ┌───────────────────────────┐             │
          │         │                           │             │
    ┌─────┴─────┐   │                     ┌─────┴─────┐
    │   API     │   │                     │   Sys     │
    │   Dev     │   │                     │  Admin    │
    └───────────┘   │                     └───────────┘
                    │
              ┌─────┴─────┐
              │    DBA    │
              └───────────┘
```

---

## Welcome & Ground Rules (5 min)

**[Architect]:** Welcome everyone. Today we're validating our API request logging design for the GLBA compliance system. Ground rules:

1. There are no wrong answers — we want honest feedback
2. Speak from your experience — what do YOU need?
3. Challenge our assumptions — that's why you're here
4. We're taking notes, not names — speak freely

Let me briefly introduce our current design...

---

## System Overview & Current Design (10 min)

**[Architect]:** Here's what we're building:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                   PROPOSED API LOGGING FLOW                                 │
└─────────────────────────────────────────────────────────────────────────────┘

  EXTERNAL REQUEST                      CONTROLLER                  DATABASE
  ════════════════                      ══════════                  ════════

  ┌─────────────┐      ┌─────────────────────────────────────┐    ┌─────────┐
  │   Source    │      │  [ApiRequestLogging] Attribute      │    │  API    │
  │   System    │─────►│                                     │───►│ Request │
  │   (Banner)  │      │  ┌─────────────────────────────┐    │    │  Logs   │
  └─────────────┘      │  │ OnActionExecuting           │    │    │  Table  │
        │              │  │  • Start Stopwatch          │    │    └─────────┘
        │              │  │  • Capture Request Details  │    │         │
        │              │  └─────────────┬───────────────┘    │         │
        │              │                │                    │         │
        │              │                ▼                    │         │
        │              │  ┌─────────────────────────────┐    │         │
        │              │  │     Controller Action       │    │         │
        │              │  │   (Process GLBA Event)      │    │         │
        │              │  └─────────────┬───────────────┘    │         │
        │              │                │                    │         │
        │              │                ▼                    │         │
        │              │  ┌─────────────────────────────┐    │         │
        │              │  │ OnActionExecuted            │    │         │
        │              │  │  • Stop Stopwatch           │────┼────────►│
        │              │  │  • Capture Response         │    │         │
        │              │  │  • Save Log (async)         │    │         │
        │              │  └─────────────────────────────┘    │         │
        ◄──────────────┼──│  • RequestBodySize           │    │         │
  Response             │  └─────────────────────────────┘    │         │
                       └─────────────────────────────────────┘         │
                                                                       │
                       ┌───────────────────────────────────────────────┘
                       │
                       ▼
              ┌─────────────────────────────────────────────────────────┐
              │                    LOG ENTRY CONTAINS                   │
              ├─────────────────────────────────────────────────────────┤
              │  WHO: SourceSystemId, SourceSystemName, UserId, Tenant  │
              │  WHAT: Method, Path, Headers, Body (optional)           │
              │  WHEN: RequestedAt, RespondedAt, DurationMs             │
              │  WHERE: IpAddress, UserAgent, ForwardedFor              │
              │  RESULT: StatusCode, IsSuccess, ErrorMessage            │
              └─────────────────────────────────────────────────────────┘
```

**[Architect]:** Questions so far?

**[ComplianceOfficer]:** When you say "optional body" — who decides if it's logged?

**[Architect]:** System administrator via configuration. Default is OFF.

**[SecurityAnalyst]:** Good. Bodies can contain sensitive data.

---

## Topic 1: What Should Be Logged? (25 min)

### 1.1 Legal Requirements for GLBA

**[Architect]:** Let's start with compliance. What does GLBA actually require for access logging?

**[ComplianceOfficer]:** GLBA doesn't specify logging formats, but auditors expect:
- WHO accessed data (user identity)
- WHAT data was accessed (subject of the record)
- WHEN (timestamp)
- WHY (stated purpose)
- Was access authorized?

**[ComplianceOfficer]:** The "stated purpose" is critical. Every access must have a legitimate business reason. If Banner says "I accessed student X's financial data" — we need to know WHY.

**[APIDev]:** That's interesting. Our current `GlbaEventRequest` has a `Purpose` field. But that's in the ACCESS EVENT, not the API log.

**[ComplianceOfficer]:** Right. The API log tells you the SYSTEM called the API. The ACCESS EVENT tells you WHY a person looked at data. They're different.

**[DBA]:** So the API log is "Banner called our API at 2:00 PM" and the Access Event is "John looked at Mary's record because she's in his caseload."

**[ComplianceOfficer]:** Exactly. Don't conflate them. They serve different audit purposes.

**[Architect]:** Great clarification. Our API logs are system-to-system audit trails, not user-level access logs.

---

### 1.2 What Helps Trace a Failed Request?

**[Architect]:** When an API call fails, what do you need to debug it?

**[APIDev]:** First thing I look for: the exact error message and status code. 400 means I sent bad data. 500 means the server broke.

**[APIDev]:** Second: the request body. If I sent malformed JSON, I need to see WHAT I sent to fix it.

**[SysAdmin]:** For me, it's timing. If requests are slow, I need to know WHERE the time went. Was it our code or the database?

**[APIDev]:** Headers are underrated. I've debugged issues where the Content-Type was wrong, or Authorization was malformed.

**[SecurityAnalyst]:** Request ID or correlation ID is essential. If something goes wrong across multiple systems, I need to trace it end-to-end.

**[APIDev]:** Yes! A correlation ID that follows the request through our system AND back to the calling system. Banner logs should have the same ID.

**[Architect]:** We have `CorrelationId` in our design. Good to confirm it's valuable.

---

### 1.3 What Indicates a Security Threat?

**[Architect]:** From a security perspective, what patterns in API logs indicate a problem?

**[SecurityAnalyst]:** Velocity. Too many requests too fast from one source.

**[SecurityAnalyst]:** Failed authentication attempts. If I see 50 failed API key validations from the same IP, that's a brute force attack.

**[SecurityAnalyst]:** Unusual hours. If Banner normally calls during business hours, a 3 AM burst is suspicious.

**[SecurityAnalyst]:** Error patterns. If 80% of requests from one source are failing, either they're misconfigured or probing.

**[SysAdmin]:** Geographic anomalies. If our systems are in Michigan but requests come from overseas IPs, that's a flag.

**[APIDev]:** Parameter tampering. If someone's modifying request paths or query strings in unusual ways.

**[SecurityAnalyst]:** One more: payload size anomalies. If normal requests are 500 bytes but someone sends 50KB, what's in there?

**[Architect]:** Great list. We capture `RequestBodySize` which helps with the payload anomaly detection.

---

### 1.4 What's Overkill / Noise?

**[Architect]:** What do you NOT want in API logs? What's just noise?

**[DBA]:** Health check endpoints. If your load balancer pings `/health` every 30 seconds, don't log it. You'll drown in noise.

**[APIDev]:** Static file requests, if any. CSS, JS, images — irrelevant.

**[SysAdmin]:** Successful authentication details. I don't need every token validated logged — just failures.

**[ComplianceOfficer]:** But for compliance, I DO need successful access logged. It depends on the log's purpose.

**[SecurityAnalyst]:** The key is having BOTH. But for real-time monitoring, filter to anomalies. For audit, keep everything.

**[DBA]:** Storage is cheap. Log everything, but make the UI filterable. Don't filter at collection time.

**[Architect]:** Good principle: log everything, filter at query time.

---

## Topic 2: Body Logging Trade-offs (15 min)

### 2.1 Do You Need to See Request/Response Bodies?

**[Architect]:** Our current design makes body logging opt-in. Is that right?

**[APIDev]:** For debugging? Yes, I need bodies. If my JSON is malformed, I need to see exactly what I sent.

**[APIDev]:** But for production? I don't want bodies logged by default. Too much sensitive data.

**[ComplianceOfficer]:** Bodies contain PII. Student IDs, SSNs potentially, financial data. That's a liability if logged and exposed.

**[SecurityAnalyst]:** Bodies are great for forensics AFTER an incident. But logging them routinely creates a honeypot.

**[SysAdmin]:** I'd want body logging off normally, but the ability to turn it on for a specific source system when debugging.

**[APIDev]:** Can we have per-source-system toggles? Like "log bodies for Banner integration for the next 24 hours."

**[Architect]:** That's a good idea. Time-limited body logging for specific sources.

---

### 2.2 PII Risk in Bodies

**[ComplianceOfficer]:** Let me be clear about the risk. If you log bodies containing student data, that log table becomes subject to FERPA as well as GLBA.

**[ComplianceOfficer]:** Now you have TWO sets of audit requirements on one table.

**[SecurityAnalyst]:** And if that log table is breached, you've exposed not just metadata but actual personal data.

**[DBA]:** Can we hash or redact sensitive fields before logging?

**[APIDev]:** That adds complexity. You'd have to know which fields are sensitive.

**[ComplianceOfficer]:** The `SubjectId` field — that's a student ID or SSN equivalent. That MUST be protected.

**[Architect]:** Option: log bodies but mask known sensitive fields. `SubjectId: "***REDACTED***"`

**[SecurityAnalyst]:** Better option: don't log bodies by default. When you enable it, warn the admin prominently.

---

### 2.3 Bodies: Opt-in, Always-on, or Never?

**[Architect]:** Recommendation check: opt-in with warning, or never?

**[ComplianceOfficer]:** Opt-in with audit trail. Log WHO enabled body logging and WHEN.

**[SecurityAnalyst]:** Agreed. And auto-expire. Body logging turns off after 24-48 hours automatically.

**[APIDev]:** That's actually really good. "Enable body logging for Banner for 24 hours" — then it self-disables.

**[DBA]:** Include the body logging duration in the config. Don't just leave it on forever.

**[SysAdmin]:** And notify admins when it's about to expire. "Body logging for Banner expires in 2 hours."

**[Architect]:** I'm hearing: opt-in, time-limited, auto-expire, audit who enabled it. All great additions.

---

## Topic 3: Retention & Storage (15 min)

### 3.1 How Long for Audits?

**[Architect]:** Our proposal is 90 days retention. Is that enough for audits?

**[ComplianceOfficer]:** GLBA audits are typically annual. You need at least 1 year of logs available.

**[ComplianceOfficer]:** But "available" doesn't mean "online." Archive after 90 days to cold storage is fine.

**[SysAdmin]:** We archive to Azure blob storage after 90 days. Accessible but not instant.

**[ComplianceOfficer]:** That works. Hot/warm/cold tiering. 90 days hot, 1 year warm, then archive or delete.

**[SecurityAnalyst]:** For incident forensics, I need 6-12 months. Breaches often aren't discovered for months.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     SUGGESTED RETENTION TIERS                               │
└─────────────────────────────────────────────────────────────────────────────┘

  TIME ──────────────────────────────────────────────────────────────────────►

  │         90 DAYS              │        9 MONTHS          │   ARCHIVE      │
  │◄────────────────────────────►│◄────────────────────────►│◄─────────────► │
  │                              │                          │                │
  │  ┌─────────────────────┐     │   ┌──────────────────┐   │  ┌──────────┐  │
  │  │      HOT TIER       │     │   │    WARM TIER     │   │  │  COLD    │  │
  │  │  • In Database      │     │   │  • Blob Storage  │   │  │  TIER    │  │
  │  │  • Full Query       │ ───►│   │  • Bulk Query    │───►│  │ Offline  │  │
  │  │  • UI Access        │     │   │  • Export/Search │   │  │ Tape/S3  │  │
  │  │  • Fast             │     │   │  • Slower        │   │  │ Glacier  │  │
  │  └─────────────────────┘     │   └──────────────────┘   │  └──────────┘  │
  │                              │                          │                │
  └──────────────────────────────┴──────────────────────────┴────────────────┘
```

**[DBA]:** I like this model. The database stays lean. Historical data is still available but doesn't bloat the primary store.

---

### 3.2 How Long for Debugging?

**[APIDev]:** For debugging? I need maybe 7-14 days. After that, if there's a bug, we've already fixed it or given up.

**[SysAdmin]:** Agreed. Real-time debugging needs 7-30 days max. After that, it's forensics or audit.

**[APIDev]:** The key is that 90 days of hot storage gives us plenty of runway.

---

### 3.3 Storage/Performance Impact

**[DBA]:** What's the expected volume? Requests per day?

**[Architect]:** Based on current access events: 5,000-10,000 API calls per day.

**[DBA]:** That's manageable. At ~2KB per log entry, that's 15-20 MB per day. About 1.5-2 GB for 90 days.

**[DBA]:** My concerns:
1. Index the right columns (SourceSystemId, RequestedAt, StatusCode)
2. Partition by date if volume grows
3. Don't log to the same database as transactions — use a separate log database

**[SysAdmin]:** Separate database is good practice. Log storms shouldn't impact application performance.

**[Architect]:** Good point. We can configure a separate connection string for the log database.

---

## Topic 4: User Interface Needs (20 min)

### 4.1 First Action When Something Breaks

**[Architect]:** When an API integration breaks, what's your FIRST action?

**[SysAdmin]:** Check if the service is up. Then check recent errors.

**[APIDev]:** Filter by my source system, sort by newest, look for red (errors).

**[SecurityAnalyst]:** Check if it's ONE source failing or ALL sources. Localized vs systemic.

**[APIDev]:** Show me the last 5-10 requests from Banner, with status codes. That tells me immediately if it's working.

**[SysAdmin]:** A dashboard showing "requests per minute" with a big red spike when things break.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                  IDEAL FIRST-GLANCE DASHBOARD                               │
└─────────────────────────────────────────────────────────────────────────────┘

  ┌─────────────────────────────────────────────────────────────────────────┐
  │  API REQUEST LOG DASHBOARD                                   [REFRESH]  │
  ├─────────────────────────────────────────────────────────────────────────┤
  │                                                                         │
  │  LAST 24 HOURS                    REQUEST RATE (PER MINUTE)             │
  │  ┌──────────┐ ┌──────────┐       ═════════════════════════════          │
  │  │   8,421  │ │    127   │       │    ╭─╮      ╭─╮                      │
  │  │ Requests │ │  Errors  │       │ ╭──╯ ╰──╮╭──╯ ╰─╮ ←─ Normal          │
  │  │   ✓      │ │    ⚠     │       │─╯        ╰╯      ╰────────           │
  │  └──────────┘ └──────────┘       └─────────────────────────────         │
  │                                  8am   10am   12pm   2pm   NOW          │
  │                                                                         │
  │  BY SOURCE SYSTEM                BY STATUS                              │
  │  ═══════════════════             ══════════════                         │
  │  Banner     ████████████ 4,231   ✓ 200 OK     ██████████████████ 92%   │
  │  HR System  ███████ 2,100        ⚠ 400 Bad    ██ 5%                     │
  │  Financial  █████ 1,800          ✗ 500 Error  █ 2%                      │
  │  Other      ██ 290               ⚡ Timeout   █ 1%                       │
  │                                                                         │
  │  ┌─ RECENT ERRORS ──────────────────────────────────────────────────┐   │
  │  │ 14:32:01  Banner      POST /events    400  Invalid SubjectId     │   │
  │  │ 14:28:45  HR System   POST /events    500  Database timeout      │   │
  │  │ 14:15:22  Banner      POST /batch     400  Missing Purpose field │   │
  │  └──────────────────────────────────────────────────────────────────┘   │
  └─────────────────────────────────────────────────────────────────────────┘
```

**[Everyone]:** *[Positive reactions to mockup]*

---

### 4.2 What Filters Matter Most?

**[Architect]:** Priority filters for the list view?

**[SysAdmin]:** Time range is #1. "Last hour," "Last 24 hours," "Custom range."

**[APIDev]:** Source system. I only care about MY integration.

**[SecurityAnalyst]:** Status code groups. "All errors" (4xx + 5xx), "Client errors" (4xx), "Server errors" (5xx).

**[ComplianceOfficer]:** For audits, I need to filter by specific entities accessed. "Show me all requests that touched student ID X."

**[APIDev]:** That would require searching the body... which we might not have.

**[ComplianceOfficer]:** True. For entity-level audit, I should use the Access Events table, not API logs.

**[DBA]:** Duration filter. "Show requests > 1 second." Good for finding slow queries.

**[SecurityAnalyst]:** IP address filter. If I suspect an IP, I want to see all its activity.

---

### 4.3 Export/Download Capability?

**[ComplianceOfficer]:** Yes, absolutely. Auditors often want logs in Excel or CSV.

**[SecurityAnalyst]:** For incident reports, I need to export a subset and attach it to a ticket.

**[APIDev]:** For support tickets, I want to export "last 24 hours from Banner" and send to their team.

**[SysAdmin]:** Export with the current filters applied. What I see is what I export.

**[DBA]:** Limit export size. Don't let someone export 10 million rows and crash the server.

**[Architect]:** Good point. Export limit of 10,000 rows, with a "request full export via email" for larger sets.

---

### 4.4 Real-time vs Historical?

**[SysAdmin]:** For troubleshooting, I want near-real-time. Auto-refresh every 30 seconds.

**[APIDev]:** When debugging actively, I want to see requests as they happen.

**[SecurityAnalyst]:** For threat detection, batch analysis is fine. I don't need real-time for forensics.

**[ComplianceOfficer]:** Historical only for audits. I'm looking at last quarter, not right now.

**[SysAdmin]:** Auto-refresh toggle. On when debugging, off when analyzing history.

---

## Topic 5: What Are We Missing? (15 min)

### 5.1 Features You Wish You Had

**[APIDev]:** **Request replay**. "This request failed — let me re-send it."

**[SysAdmin]:** That's dangerous. You could accidentally re-process data.

**[APIDev]:** Fair. Maybe "copy as cURL" so I can test in Postman.

**[SecurityAnalyst]:** **Alerting**. Notify me when error rate exceeds threshold.

**[DBA]:** **API for the logs**. Let me query logs programmatically, not just via UI.

**[ComplianceOfficer]:** **Scheduled reports**. Email me a weekly summary of API activity.

**[SysAdmin]:** **Compare to baseline**. "This week vs last week" — are we seeing more errors?

---

### 5.2 Mistakes in Other Systems

**[APIDev]:** Logs that only show "Error" with no details. Useless.

**[SysAdmin]:** Logs with no timestamps in a useful timezone. Everything in UTC with no conversion.

**[SecurityAnalyst]:** Logs that get overwritten too quickly. "The evidence is gone."

**[DBA]:** Logs in the same database as application data. Log growth killed the app.

**[ComplianceOfficer]:** No way to prove logs haven't been tampered with. Auditors question integrity.

**[APIDev]:** Pagination that loses your place. You're on page 50, something refreshes, back to page 1.

---

### 5.3 Regulatory Changes Coming?

**[ComplianceOfficer]:** There's always talk of stricter data retention requirements. Keep your retention configurable.

**[ComplianceOfficer]:** CMMC (Cybersecurity Maturity Model) is becoming relevant for government-adjacent institutions. It has stricter logging requirements.

**[SecurityAnalyst]:** Zero-trust architecture is coming. Every access should be logged, verified, and challengeable.

**[ComplianceOfficer]:** Right to be forgotten under various privacy laws could complicate log retention. If a student requests deletion, do logs count?

**[Architect]:** Good question. Our logs contain SourceSystem IDs, not student IDs directly. The access event might need deletion, but the API log shows "a system called us" — that's operational data.

---

## Wrap-Up & Summary (10 min)

**[Architect]:** Let me summarize what I heard:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    KEY TAKEAWAYS FROM FOCUS GROUP                           │
└─────────────────────────────────────────────────────────────────────────────┘

  ✅ VALIDATED                           ⚡ NEW REQUIREMENTS
  ══════════                             ══════════════════

  • Attribute-based logging ✓            • Time-limited body logging
  • 90-day hot retention ✓               • Auto-expire body logging
  • Opt-in body logging ✓                • Audit trail for config changes
  • Correlation ID ✓                     • Tiered retention (hot/warm/cold)
  • Strip sensitive headers ✓            • Dashboard with at-a-glance health
                                         • Export with row limits
                                         • Duration-based filtering
                                         • Separate log database *

  * CTO later rejected separate DB for simplicity (see Doc 116)

  ⚠️ RECONSIDER                          🚀 FUTURE FEATURES (V2)
  ════════════                           ════════════════════════

  • Consider 1-year warm tier            • "Copy as cURL" for debugging
  • Add WHO enabled body logging         • Alerting on error thresholds
  • Add prominent PII warning            • API access to logs
                                         • Scheduled email reports
                                         • Week-over-week comparison
```

**[Architect]:** Thank you all for your time and insights. We'll incorporate this feedback and share the updated design.

**[ComplianceOfficer]:** Happy to help. This is going to make audits much easier.

**[APIDev]:** Looking forward to having real visibility into what's happening.

**[SecurityAnalyst]:** The dashboard mock-up is exactly what we need. Please build that.

---

*Focus Group concluded: 2025-01-27*  
*Next: Team Analysis (Doc 116)*

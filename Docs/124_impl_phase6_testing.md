# 124 — Implementation Plan: Phase 6 - Manual Testing & Wrap-up

> **Document ID:** 124  
> **Category:** Implementation Plan  
> **Purpose:** Final manual testing, documentation, and deployment preparation  
> **Phase:** 6 of 6 (Final)  
> **Estimated Time:** 1.0 days  
> **Dependencies:** Phase 5 complete (all features implemented)

---

## Phase Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      PHASE 6: MANUAL TESTING & WRAP-UP                      │
└─────────────────────────────────────────────────────────────────────────────┘

  FINAL PHASE - ENSURING QUALITY
  ═══════════════════════════════
  
  ┌─────────────────┐   ┌─────────────────┐   ┌─────────────────┐
  │   Manual        │   │   Deploy        │   │   Document      │
  │   Testing       │──►│   Prep          │──►│   Updates       │
  │                 │   │                 │   │                 │
  │ All pages       │   │ Migrations      │   │ Quickstart      │
  │ Edge cases      │   │ Config          │   │ DBA scripts     │
  │ Dev site        │   │ SQL scripts     │   │                 │
  └─────────────────┘   └─────────────────┘   └─────────────────┘
  
  NOTE: No automated unit/integration tests - manual testing on dev site,
        customers report production issues quickly.
```

---

## Task 6.1: Manual Testing Checklist

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 6.1: MANUAL TESTING CHECKLIST                                         │
│  ═══════════════════════════════════                                        │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │                     FUNCTIONAL TESTING                              │   │
│   ├─────────────────────────────────────────────────────────────────────┤   │
│   │                                                                     │   │
│   │   LOGGING ENGINE                                                    │   │
│   │   ══════════════                                                    │   │
│   │   □ POST to /api/glba/events creates log entry                      │   │
│   │   □ GET to /api/glba endpoints creates log entry                    │   │
│   │   □ Log contains correct method, path, status code                  │   │
│   │   □ DurationMs is realistic (not 0, not huge)                       │   │
│   │   □ Headers are captured with sensitive ones redacted               │   │
│   │   □ CorrelationId is captured when present                          │   │
│   │   □ Error responses have ErrorMessage populated                     │   │
│   │   □ [SkipApiLogging] endpoints don't create logs                    │   │
│   │                                                                     │   │
│   │   DASHBOARD                                                         │   │
│   │   ═════════                                                         │   │
│   │   □ Dashboard loads at /api-logs/dashboard                          │   │
│   │   □ Summary cards show correct totals                               │   │
│   │   □ Time range selector changes data                                │   │
│   │   □ Auto-refresh toggle works                                       │   │
│   │   □ Source system breakdown is accurate                             │   │
│   │   □ Status code breakdown is accurate                               │   │
│   │   □ Recent errors table shows actual errors                         │   │
│   │   □ Clicking error navigates to detail view                         │   │
│   │   □ Dashboard with zero logs - shows zeros, not errors              │   │
│   │   □ Total log count displays correctly                              │   │
│   │   □ Logs older than 7 years count displays (for compliance)         │   │
│   │                                                                     │   │
│   │   LIST VIEW                                                         │   │
│   │   ═════════                                                         │   │
│   │   □ List loads at /api-logs                                         │   │
│   │   □ Time range filter works                                         │   │
│   │   □ Source system filter works                                      │   │
│   │   □ Status filter (errors only) works                               │   │
│   │   □ Duration filter (slow queries) works                            │   │
│   │   □ Search filters by path                                          │   │
│   │   □ Search filters by error message                                 │   │
│   │   □ Pagination works correctly                                      │   │
│   │   □ Sorting by each column works                                    │   │
│   │   □ Row click navigates to detail                                   │   │
│   │   □ Export generates valid CSV                                      │   │
│   │   □ Export respects row limit                                       │   │
│   │                                                                     │   │
│   │   DETAIL VIEW                                                       │   │
│   │   ═══════════                                                       │   │
│   │   □ Detail page loads at /api-logs/{id}                             │   │
│   │   □ All request info displayed correctly                            │   │
│   │   □ Timing info shows request/response/duration                     │   │
│   │   □ Headers displayed (filtered)                                    │   │
│   │   □ Error details shown for failed requests                         │   │
│   │   □ Body shown when body logging enabled                            │   │
│   │   □ "Body not captured" message when disabled                       │   │
│   │   □ Back button returns to list                                     │   │
│   │                                                                     │   │
│   │   BODY LOGGING SETTINGS                                             │   │
│   │   ═════════════════════                                             │   │
│   │   □ Settings page loads at /api-logs/settings                       │   │
│   │   □ PII warning is prominently displayed                            │   │
│   │   □ Can enable body logging for source system                       │   │
│   │   □ Confirmation modal appears before enabling                      │   │
│   │   □ Reason is required                                              │   │
│   │   □ Duration limited to max (72 hours)                              │   │
│   │   □ Can disable active config                                       │   │
│   │   □ Expired configs show as expired                                 │   │
│   │   □ After enabling, body IS captured in logs                        │   │
│   │   □ After disabling, body NOT captured                              │   │
│   │                                                                     │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │                     EDGE CASE TESTING                               │   │
│   ├─────────────────────────────────────────────────────────────────────┤   │
│   │                                                                     │   │
│   │   □ Very large request body (> 4KB) - verify truncation             │   │
│   │   □ Very long URL path (> 500 chars) - verify truncation            │   │
│   │   □ Request with binary body - doesn't crash                        │   │
│   │   □ Concurrent requests - all logged correctly                      │   │
│   │   □ Slow request (> 5 seconds) - duration accurate                  │   │
│   │   □ No source system header - logged with default                   │   │
│   │   □ Export with zero results - generates empty CSV header           │   │
│   │                                                                     │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Task 6.2: Database Migration

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 6.2: DATABASE MIGRATION                                               │
│  ════════════════════════════                                               │
│                                                                             │
│   CREATE MIGRATION                                                          │
│   ════════════════                                                          │
│                                                                             │
│   $ dotnet ef migrations add AddApiRequestLogging                           │
│                                                                             │
│   MIGRATION CREATES:                                                        │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │                                                                     │   │
│   │  CREATE TABLE [ApiRequestLogs] (                                    │   │
│   │      [ApiRequestLogId] uniqueidentifier PRIMARY KEY,                │   │
│   │      [SourceSystemId] uniqueidentifier NOT NULL,                    │   │
│   │      [SourceSystemName] nvarchar(200),                              │   │
│   │      [HttpMethod] nvarchar(10),                                     │   │
│   │      [RequestPath] nvarchar(500),                                   │   │
│   │      [StatusCode] int,                                              │   │
│   │      [DurationMs] bigint,                                           │   │
│   │      [RequestedAt] datetime2,                                       │   │
│   │      ...                                                            │   │
│   │  );                                                                 │   │
│   │                                                                     │   │
│   │  CREATE INDEX [IX_ApiRequestLogs_SourceSystemId_RequestedAt]        │   │
│   │      ON [ApiRequestLogs] ([SourceSystemId], [RequestedAt]);         │   │
│   │                                                                     │   │
│   │  CREATE INDEX [IX_ApiRequestLogs_RequestedAt]                       │   │
│   │      ON [ApiRequestLogs] ([RequestedAt]);                           │   │
│   │                                                                     │   │
│   │  CREATE TABLE [BodyLoggingConfigs] (                                │   │
│   │      [BodyLoggingConfigId] uniqueidentifier PRIMARY KEY,            │   │
│   │      [SourceSystemId] uniqueidentifier NOT NULL,                    │   │
│   │      [EnabledByUserId] uniqueidentifier,                            │   │
│   │      ...                                                            │   │
│   │  );                                                                 │   │
│   │                                                                     │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│   APPLY MIGRATION                                                           │
│   ═══════════════                                                           │
│                                                                             │
│   $ dotnet ef database update                                               │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**What it is:**
- Generate EF Core migration for new tables
- Verify indexes are created for performance
- Test migration on dev database

**Pseudo-code:**
```bash
# In FreeGLBA.EFModels project directory

# Generate migration
dotnet ef migrations add AddApiRequestLogging --project FreeGLBA.EFModels --startup-project FreeGLBA

# Review generated migration
# Check: Tables created, indexes created, no data loss

# Apply to dev database
dotnet ef database update --project FreeGLBA.EFModels --startup-project FreeGLBA
```

---

## Task 6.3: Documentation Updates

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 6.3: DOCUMENTATION                                                    │
│  ═══════════════════════                                                    │
│                                                                             │
│   UPDATE QUICKSTART (000_quickstart.md)                                     │
│   ═════════════════════════════════════                                     │
│                                                                             │
│   Add section:                                                              │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │                                                                     │   │
│   │  ## API Request Logging                                             │   │
│   │                                                                     │   │
│   │  FreeGLBA includes comprehensive API request logging for GLBA       │   │
│   │  compliance and debugging.                                          │   │
│   │                                                                     │   │
│   │  ### Accessing Logs                                                 │   │
│   │  - Dashboard: `/api-logs/dashboard`                                 │   │
│   │  - List View: `/api-logs`                                           │   │
│   │  - Settings: `/api-logs/settings`                                   │   │
│   │                                                                     │   │
│   │  ### Configuration                                                  │   │
│   │  Configure in `appsettings.json`:                                   │   │
│   │  ```json                                                            │   │
│   │  {                                                                  │   │
│   │    "ApiLogging": {                                                  │   │
│   │      "BodyLogLimit": 4096,                                          │   │
│   │      "MaxBodyLoggingDurationHours": 72                              │   │
│   │    }                                                                │   │
│   │  }                                                                  │   │
│   │  ```                                                                │   │
│   │                                                                     │   │
│   │  ### Database Maintenance                                           │   │
│   │  Log retention is managed externally via scheduled SQL jobs.        │   │
│   │  GLBA requires 7-year retention. See doc 123 for recommended        │   │
│   │  cleanup scripts to provide to your DBA team.                       │   │
│   │                                                                     │   │
│   │  The dashboard displays total log count and logs older than         │   │
│   │  7 years for compliance monitoring.                                 │   │
│   │                                                                     │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Task 6.4: Provide SQL Scripts to DBA

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TASK 6.4: DBA HANDOFF                                                      │
│  ══════════════════════                                                     │
│                                                                             │
│   Ensure DBA team has received the SQL scripts from doc 123:                │
│                                                                             │
│   □ Delete old API logs script (with configurable retention)                │
│   □ Auto-disable expired body logging configs script                        │
│   □ Storage statistics query for monitoring                                 │
│   □ SQL Agent Job template (if applicable)                                  │
│                                                                             │
│   RETENTION NOTE:                                                           │
│   ════════════════                                                          │
│   GLBA typically requires 7-year retention. The cleanup scripts             │
│   default to 90 days but DBA should adjust based on compliance              │
│   requirements. When space becomes an issue:                                │
│                                                                             │
│   1. Export old logs to disk/archive storage                                │
│   2. Backup the exports multiple times                                      │
│   3. Then manually delete from database                                     │
│                                                                             │
│   The dashboard shows "Logs older than 7 years" count to help               │
│   monitor what's eligible for archival.                                     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Phase 6 Files Summary

| File | Action | Key Changes |
|------|--------|-------------|
| `Migrations/AddApiRequestLogging.cs` | CREATE | Database migration |
| `Docs/000_quickstart.md` | MODIFY | Add API logging section |

---

## Final Verification Checklist

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    FINAL VERIFICATION CHECKLIST                             │
└─────────────────────────────────────────────────────────────────────────────┘

  CODE QUALITY
  ════════════
  □ No compiler warnings
  □ Solution builds in Release mode
  
  FUNCTIONALITY (Manual Testing on Dev Site)
  ═══════════════════════════════════════════
  □ All manual test cases pass
  □ Edge cases handled gracefully
  □ Performance acceptable (< 1s dashboard load)
  □ Push to dev, test thoroughly
  
  DATABASE
  ════════
  □ Migration generated and reviewed
  □ Migration applied to dev database
  □ Indexes verified for performance
  □ SQL cleanup scripts provided to DBA team
  
  CONFIGURATION
  ═════════════
  □ appsettings.json has ApiLogging section
  □ Default values work correctly
  □ All options configurable
  
  DOCUMENTATION
  ═════════════
  □ Quickstart guide updated
  □ Implementation docs complete (118-124)
  □ SQL maintenance scripts documented (doc 123)
  
  DEPLOYMENT READY
  ════════════════
  □ All changes committed
  □ Migration script prepared for production
  □ DBA notified of maintenance scripts needed
  □ Deploy to prod when dev testing complete
```

---

## Phase 6 Summary

| Metric | Value |
|--------|-------|
| Files Created | 1 |
| Files Modified | 1 |
| Estimated Time | 1.0 days |
| Dependencies | Phase 5 |
| Deliverable | Production-ready feature |
| DB Maintenance | External (SQL scripts in doc 123) |
| Testing | Manual on dev site |

---

## Complete Implementation Summary

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    IMPLEMENTATION COMPLETE SUMMARY                          │
└─────────────────────────────────────────────────────────────────────────────┘

  PHASE     DAYS    FOCUS                           STATUS
  ═════     ════    ═════                           ══════
  
  Phase 0   0.5     File Scaffolding                □
  Phase 1   1.0     Entities & DTOs                 □
  Phase 2   1.5     Logging Attribute               □
  Phase 3   1.5     Dashboard View                  □
  Phase 4   1.5     List & Detail Views             □
  Phase 5   1.0     Settings                        □
  Phase 6   1.0     Manual Testing & Wrap-up        □
  ─────────────────────────────────────────────────────
  TOTAL     8.0 days
  
  
  FILES CREATED: 11
  ═════════════════
  • 2 Entity files (EFModels)
  • 1 DataObjects file
  • 1 DataAccess file
  • 2 Attribute files
  • 4 Blazor pages
  • 1 Controller partial
  
  FILES MODIFIED: 5+
  ═════════════════
  • EFDataModel.cs
  • IDataAccess.cs
  • DataAccess.cs
  • GlbaController.cs
  • Program.cs
  • appsettings.json
  • 000_quickstart.md
  
  DATABASE MAINTENANCE
  ════════════════════
  • SQL cleanup scripts documented in doc 123
  • DBA team handles retention policies
  • 7-year retention for GLBA compliance
  • Manual archive/delete when space needed
  
  TESTING APPROACH
  ════════════════
  • Manual testing on dev site
  • Push to prod when dev looks good
  • Customers report issues quickly
```

---

*Previous: [123 — Phase 5: Body Logging Settings & Database Maintenance](123_impl_phase5_settings.md)*  
*This is the final implementation phase document*

---

**Ready for Implementation! 🚀**

The API Request Logging feature is now fully designed. Manual testing on dev, then push to prod. Database maintenance handled externally by DBA with 7-year GLBA retention in mind.

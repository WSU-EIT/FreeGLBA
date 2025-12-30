# 102 — CTO Brief: FreeGLBA Style Guide Compliance Project Plan

> **Document ID:** 102  
> **Category:** Decision  
> **Purpose:** Project plan for reviewing and fixing FreeGLBA.App.* files to match FreeCRM style guide  
> **Audience:** CTO, Dev Team, AI Agents  
> **Outcome:** 🔄 In Progress

---

## Executive Summary

**Objective:** Audit all 25 FreeGLBA.App.* files for compliance with the FreeCRM style guide (docs 004, 005).

**Why:** The FreeCRM style guide was created by analyzing 4-5 projects written by the framework author (FreeCRM-main, nForm, Helpdesk4, TrusselBuilder). Maintaining consistency ensures:
- Code is readable by anyone familiar with FreeCRM
- Framework updates integrate smoothly
- New developers onboard faster

**Scope:** Style compliance only. No functional changes.

---

## Style Guide Reference (Quick Card)

### C# Rules (from doc 004)

| Rule | Correct | Incorrect |
|------|---------|-----------|
| **Namespace** | `namespace FreeGLBA;` (file-scoped) | `namespace FreeGLBA { }` |
| **Class braces** | New line | Same line |
| **Method braces** | New line | Same line |
| **if/for/while braces** | Same line | New line |
| **Variable types** | `List<string> items = new();` | `var items = new List<string>();` |
| **Private fields** | `_camelCase` | `camelCase` or `m_` |
| **Local variables** | `camelCase` | `_prefixed` |
| **Strings** | `$"Hello {name}"` | `"Hello " + name` |
| **Null checks** | `if (x == null)` | `if (x is null)` |
| **Async methods** | `GetUsers()` | `GetUsersAsync()` |
| **Guard clauses** | `if (x == null) return;` | `if (x == null) { return; }` |
| **Control flow** | `if (cond) { ... }` | `if (cond) ...` |

### Section Separators

```csharp
// ============================================================================
// FILE HEADER - 76 chars total
// ============================================================================

// ------------------------------------------------------------
// Section header - 60 chars total
// ------------------------------------------------------------
```

### Razor Rules

| Rule | Pattern |
|------|---------|
| Inject statements | Top of file, before markup |
| @code block | Bottom of file, after markup |
| Component class | Dots → underscores (`FreeGLBA_App_Dashboard`) |
| Loading state | `_loading` bool, spinner while true |
| Save state | `_saving` bool, disable buttons while true |
| Focus | `await Helpers.DelayedFocus("element-id")` |
| Validation | `Helpers.MissingValue()`, `Helpers.MissingRequiredField()` |
| Feedback | `Model.Message_Saving()`, `Model.Message_Saved()` |

### File Size Limits

| Threshold | Lines | Action |
|-----------|-------|--------|
| Target | ≤300 | Ideal |
| Soft max | 500 | Note in review |
| Hard max | 600 | Must split |

---

## Project Phases

### Phase 1: Foundation (8 files) — P1

**Goal:** Ensure data contracts are compliant. These define the shapes used everywhere else.

| # | File | Location | Check |
|---|------|----------|-------|
| 1.1 | FreeGLBA.App.SourceSystem.cs | EFModels/EFModels/ | ⬜ |
| 1.2 | FreeGLBA.App.AccessEvent.cs | EFModels/EFModels/ | ⬜ |
| 1.3 | FreeGLBA.App.DataSubject.cs | EFModels/EFModels/ | ⬜ |
| 1.4 | FreeGLBA.App.ComplianceReport.cs | EFModels/EFModels/ | ⬜ |
| 1.5 | FreeGLBA.App.EFDataModel.cs | EFModels/EFModels/ | ⬜ |
| 1.6 | FreeGLBA.App.DataObjects.cs | DataObjects/ | ⬜ |
| 1.7 | FreeGLBA.App.DataObjects.ExternalApi.cs | DataObjects/ | ⬜ |
| 1.8 | FreeGLBA.App.Endpoints.cs | DataObjects/ | ⬜ |

**Checklist per file:**
- [ ] File-scoped namespace
- [ ] Class braces on new line
- [ ] Properties use explicit types
- [ ] `string.Empty` over `""`
- [ ] XML doc comments on public members
- [ ] File size ≤300 lines

---

### Phase 2: Business Logic (7 files) — P2

**Goal:** Ensure backend logic follows style. Most C# rules apply here.

| # | File | Location | Check |
|---|------|----------|-------|
| 2.1 | FreeGLBA.App.IDataAccess.cs | DataAccess/ | ⬜ |
| 2.2 | FreeGLBA.App.DataAccess.cs | DataAccess/ | ⬜ |
| 2.3 | FreeGLBA.App.DataAccess.ApiKey.cs | DataAccess/ | ⬜ |
| 2.4 | FreeGLBA.App.DataAccess.ExternalApi.cs | DataAccess/ | ⬜ |
| 2.5 | FreeGLBA.App.DataController.cs | Controllers/ | ⬜ |
| 2.6 | FreeGLBA.App.GlbaController.cs | Controllers/ | ⬜ |
| 2.7 | FreeGLBA.App.ApiKeyMiddleware.cs | Controllers/ | ⬜ |

**Checklist per file:**
- [ ] File-scoped namespace
- [ ] Class/method braces on new line
- [ ] if/for/while braces on same line
- [ ] Explicit types (no var)
- [ ] Private fields use `_camelCase`
- [ ] Local variables use `camelCase`
- [ ] Guard clauses: single line, no braces
- [ ] Regular control flow: always braces
- [ ] No `Async` suffix on method names
- [ ] LINQ uses fluent syntax
- [ ] Section separators present
- [ ] File size ≤300 (flag if >500)

**Special attention:**
- `FreeGLBA.App.DataAccess.cs` is ~500 lines — review for potential split
- Recently refactored to remove ExecuteUpdateAsync — verify style intact

---

### Phase 3: UI Layer (10 files) — P3

**Goal:** Ensure Razor components follow patterns. Mix of C# and Razor rules.

| # | File | Location | Check |
|---|------|----------|-------|
| 3.1 | FreeGLBA.App.Helpers.cs | Client/ | ⬜ |
| 3.2 | FreeGLBA.App.GlbaDashboard.razor | Client/Pages/ | ⬜ |
| 3.3 | FreeGLBA.App.SourceSystemsPage.razor | Client/Pages/ | ⬜ |
| 3.4 | FreeGLBA.App.AccessEventsPage.razor | Client/Pages/ | ⬜ |
| 3.5 | FreeGLBA.App.DataSubjectsPage.razor | Client/Pages/ | ⬜ |
| 3.6 | FreeGLBA.App.ComplianceReportsPage.razor | Client/Pages/ | ⬜ |
| 3.7 | FreeGLBA.App.EditSourceSystem.razor | Client/Shared/AppComponents/ | ⬜ |
| 3.8 | FreeGLBA.App.EditAccessEvent.razor | Client/Shared/AppComponents/ | ⬜ |
| 3.9 | FreeGLBA.App.EditDataSubject.razor | Client/Shared/AppComponents/ | ⬜ |
| 3.10 | FreeGLBA.App.EditComplianceReport.razor | Client/Shared/AppComponents/ | ⬜ |

**Checklist per file:**
- [ ] @inject statements at top
- [ ] @code block at bottom
- [ ] C# in @code follows style guide
- [ ] Uses `_loading` / `_saving` state pattern
- [ ] Uses `Helpers.DelayedFocus()` for auto-focus
- [ ] Uses `Helpers.MissingValue()` for validation styling
- [ ] Uses `Model.Message_Saving()` / `Model.Message_Saved()`
- [ ] Save buttons disabled during save
- [ ] File size ≤300 lines

**Note:** Edit components were recently updated for UX polish. Verify they follow all patterns.

---

## Review Process

### For Each File

1. **Open file** and note line count
2. **Scan for obvious issues** (braces, var usage, naming)
3. **Check each item** on the phase checklist
4. **Document issues** in this format:

```markdown
### File: {filename}
**Lines:** {count}
**Status:** ✅ Compliant | ⚠️ Minor Issues | ❌ Needs Work

**Issues:**
- [ ] Line X: {description}
- [ ] Line Y: {description}

**Notes:** {context}
```

5. **Fix issues** or note for batch fix
6. **Mark checkbox** in phase table above

### Batch Fixes

Some issues can be fixed in batch:
- `var` → explicit type (search/replace with care)
- Brace placement (EditorConfig + format document)
- Missing `_` prefix on private fields

---

## Acceptance Criteria

- [ ] All 25 files reviewed
- [ ] All checkboxes in phase tables marked
- [ ] No files exceed 600 lines
- [ ] Files >500 lines have split plan documented
- [ ] Build succeeds after changes
- [ ] Tests pass after changes

---

## Files Excluded

| File | Reason |
|------|--------|
| FreeGLBA.App.EditSourceSystem.ApiKey.snippet | Template file, not runtime code |
| FreeGLBA.App.Program.snippet | Template file, not runtime code |

---

## Timeline

| Phase | Files | Est. Effort | Status |
|-------|-------|-------------|--------|
| Phase 1 | 8 | 1 hour | ⬜ Not Started |
| Phase 2 | 7 | 2 hours | ⬜ Not Started |
| Phase 3 | 10 | 2 hours | ⬜ Not Started |
| **Total** | **25** | **5 hours** | |

---

## Related Documents

- [004_styleguide.md](004_styleguide.md) — Full C# style guide
- [005_style.md](005_style.md) — Style guide index
- [101_meeting_styleguide_compliance_review.md](101_meeting_styleguide_compliance_review.md) — Team standup transcript

---

*Created: 2025-01-15*  
*Maintained by: [CTO]*

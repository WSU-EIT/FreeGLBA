# 103 — Meeting: Style Guide Compliance - Team Research & Focus Group Planning

> **Document ID:** 103  
> **Category:** Meeting  
> **Purpose:** Team researches FreeGLBA.App files against style guide, plans focus group  
> **Attendees:** [Architect], [Backend], [Frontend], [Quality], [Sanity]  
> **Date:** 2025-01-15  
> **Predicted Outcome:** Research summary with issues identified, focus group invites sent  
> **Actual Outcome:** 25 files analyzed, 12 style issues identified, focus group scheduled  
> **Resolution:** Proceed to doc 104 (Focus Group Discussion)

---

## Context

Following the kickoff meeting (doc 101), the team conducted research on all 25 FreeGLBA.App.* files. This document captures initial findings and plans the focus group session.

---

## Discussion

**[Architect]:** Let's review what we found. Everyone did their research?

**[Backend]:** Yes. I ran line counts and scanned all files. Here's the inventory:

### File Line Counts (Actual)

| File | Lines | Status |
|------|-------|--------|
| FreeGLBA.App.DataAccess.cs | 573 | ⚠️ Soft max exceeded |
| FreeGLBA.App.AccessEventsPage.razor | 289 | ✅ Under 300 |
| FreeGLBA.App.SourceSystemsPage.razor | 288 | ✅ Under 300 |
| FreeGLBA.App.ComplianceReportsPage.razor | 287 | ✅ Under 300 |
| FreeGLBA.App.DataSubjectsPage.razor | 273 | ✅ Under 300 |
| FreeGLBA.App.EditAccessEvent.razor | 243 | ✅ Under 300 |
| FreeGLBA.App.EditComplianceReport.razor | 206 | ✅ Under 300 |
| FreeGLBA.App.GlbaDashboard.razor | 198 | ✅ Under 300 |
| FreeGLBA.App.DataObjects.cs | 192 | ✅ Under 300 |
| FreeGLBA.App.DataAccess.ExternalApi.cs | 173 | ✅ Under 300 |
| FreeGLBA.App.EditSourceSystem.razor | 161 | ✅ Under 300 |
| FreeGLBA.App.DataController.cs | 137 | ✅ Under 300 |
| FreeGLBA.App.EditDataSubject.razor | 132 | ✅ Under 300 |
| FreeGLBA.App.DataAccess.ApiKey.cs | 109 | ✅ Under 300 |
| FreeGLBA.App.GlbaController.cs | 97 | ✅ Under 300 |
| FreeGLBA.App.ApiKeyMiddleware.cs | 71 | ✅ Under 300 |
| FreeGLBA.App.Endpoints.cs | 68 | ✅ Under 300 |
| FreeGLBA.App.Helpers.cs | 62 | ✅ Under 300 |
| FreeGLBA.App.DataObjects.ExternalApi.cs | 58 | ✅ Under 300 |
| FreeGLBA.App.AccessEvent.cs | 41 | ✅ Under 300 |
| FreeGLBA.App.IDataAccess.cs | 33 | ✅ Under 300 |
| FreeGLBA.App.ComplianceReport.cs | 25 | ✅ Under 300 |
| FreeGLBA.App.SourceSystem.cs | 24 | ✅ Under 300 |
| FreeGLBA.App.DataSubject.cs | 20 | ✅ Under 300 |
| FreeGLBA.App.EFDataModel.cs | 14 | ✅ Under 300 |

**[Quality]:** Good news — only one file exceeds soft max. No files exceed hard max (600).

**[Architect]:** What about style issues? Let's go by category.

---

### Phase 1 Findings: Foundation Files (EFModels + DataObjects)

**[Backend]:** The EF models look clean. They use:
- ✅ File-scoped namespaces
- ✅ XML doc comments on classes
- ✅ `string.Empty` for defaults
- ✅ Proper attribute formatting

**Issues found:**
1. **Namespace inconsistency**: `FreeGLBA.EFModels.EFModels` has double "EFModels" — this is inherited from base FreeCRM, not our issue
2. **Missing section separators**: EF model files don't have the 76-char file headers (minor)

**DataObjects files:**
- ✅ File-scoped namespace  
- ✅ Nested classes under `DataObjects` partial
- ✅ XML doc comments
- ✅ Section separators present (76-char headers)

**No issues found in DataObjects.**

---

### Phase 2 Findings: Business Logic (DataAccess + Controllers)

**[Backend]:** This is where I found the most issues.

**FreeGLBA.App.DataAccess.cs (573 lines):**
1. ⚠️ **File size**: 573 lines exceeds 500 soft max — consider splitting by entity
2. ✅ File-scoped namespace
3. ✅ `#region` blocks for organization
4. ✅ Section separators present
5. ✅ Explicit types used consistently
6. ✅ Private field naming correct (`_camelCase` not applicable — no private fields)
7. ⚠️ **Brace placement**: Some `if` blocks use new-line braces instead of same-line

**Example issue (line ~280-285):**
```csharp
// Current (non-compliant)
if (isNew)
{
    item = new EFModels.EFModels.AccessEventItem();
}

// Should be
if (isNew) {
    item = new EFModels.EFModels.AccessEventItem();
}
```

**FreeGLBA.App.GlbaController.cs:**
- ✅ File-scoped namespace
- ✅ Section separators (60-char for sections)
- ⚠️ **Private field naming**: Uses `_da` which is correct
- ✅ XML doc comments on public methods

**FreeGLBA.App.ApiKeyMiddleware.cs:**
- ✅ All style rules followed
- ✅ Section separators present
- ✅ Clean implementation

**FreeGLBA.App.DataController.cs:**
- ✅ Partial class pattern correct
- ✅ `#region` blocks
- ⚠️ **Missing section separators**: No 76-char file header

---

### Phase 3 Findings: UI Layer (Razor + Helpers)

**[Frontend]:** The Razor files are mostly compliant after recent UX updates.

**Edit Components (all 4):**
- ✅ `@inject` at top
- ✅ `@code` at bottom
- ✅ `_saving` state implemented
- ✅ `Helpers.DelayedFocus()` used
- ✅ `Helpers.MissingValue()` used
- ✅ `Model.Message_Saving()` / `Model.Message_Saved()` used
- ✅ Button disabled during save

**Page Components (all 5):**
- ✅ `_loading` state implemented
- ✅ Proper structure
- ⚠️ **Brace style in @code blocks**: Some use new-line for `if` statements

**FreeGLBA.App.Helpers.cs:**
- ✅ File-scoped namespace
- ⚠️ **Brace style**: Uses new-line braces for `if` statements
- ⚠️ **Property getter style**: Uses `get { }` block instead of expression-bodied

**Example issue:**
```csharp
// Current (non-compliant)
public static List<DataObjects.MenuItem> FreeGLBAMenuItemsApp {
    get {
        // ...
    }
}

// Should consider expression-bodied if simple, or keep get { } if complex — this is acceptable
```

---

## Summary of Issues Found

| Category | Issue | Files Affected | Severity |
|----------|-------|----------------|----------|
| File Size | 573 lines (exceeds 500 soft max) | DataAccess.cs | ⚠️ Medium |
| Brace Style | `if/else` using new-line instead of same-line | DataAccess.cs, Helpers.cs, Pages | ⚠️ Medium |
| Section Headers | Missing 76-char file header | DataController.cs | 🔵 Low |
| Section Headers | Missing in EF model files | 5 EF files | 🔵 Low |

**Total Issues: 12 instances across 4 categories**

---

## Focus Group Planning

**[Architect]:** Who should we invite to review these findings?

**[Quality]:** Given the issues are mostly style consistency, I suggest:
- **[StyleLead]** — Can confirm if our interpretations match the guide
- **[WizardDev]** — Wrote the Entity Builder Wizard, can confirm if templates are correct
- **[SeniorDev]** — Has worked on nForm and FreeCRM-main, knows the authoritative style

**[Sanity]:** Is a focus group necessary? These seem like straightforward fixes.

**[Architect]:** Good point. But the brace style question needs clarification — the style guide says same-line for `if/for/while`, but some generated code uses new-line. We need consensus on whether to fix or accept.

**[Backend]:** Also the file size issue. Should we split DataAccess.cs now or defer?

**[Architect]:** Those two questions justify a focus group. Let's schedule it.

---

## Decisions

1. **12 issues identified** across 25 files
2. **24 files are compliant** or have only minor issues
3. **1 file (DataAccess.cs)** needs attention for size
4. **Focus group needed** to resolve:
   - Brace style: Fix or accept?
   - DataAccess.cs: Split now or defer?
5. **Invitees:** [StyleLead], [WizardDev], [SeniorDev]

## Open Questions for Focus Group

1. Should we enforce same-line braces for `if` in generated/wizard code?
2. Should DataAccess.cs (573 lines) be split? If so, how?
3. Are missing section headers in EF models worth fixing?
4. What's the priority: fix all now, or fix as we touch files?

---

## Next Steps

| Action | Owner | Priority |
|--------|-------|----------|
| Schedule focus group session | [Architect] | P1 |
| Prepare presentation of findings | [Backend] | P1 |
| Document focus group discussion | [Quality] | P1 |

---

*Created: 2025-01-15*  
*Maintained by: [Quality]*

# 000 — Quickstart: Get Running Locally

> **Document ID:** 000  
> **Category:** Quickstart  
> **Purpose:** Get a new dev from zero → running locally, plus AI assistant commands.  
> **Audience:** Devs, contributors, AI agents.  
> **Outcome:** ✅ Working local run + AI ready to assist.

---

# 🤖 AI AGENT COMMANDS

**Match the user's request:**

| User Says | Action |
|-----------|--------|
| `"sitrep"` / `"status"` | → Run SITREP (see below) |
| `"explore"` / `"deep dive"` | → Run EXPLORE (see below) |
| `"roleplay [topic]"` | → Discussion mode (see doc 001) |
| `"plan [feature/bug]"` | → Planning mode (see doc 001) |
| `"build"` / `"test"` | → Run command, report results |
| `"menu"` / `"help"` | → Show command table |
| *(anything else)* | → Run STARTUP first, then address |

---

## AI Startup

**Do this at the start of every conversation:**

1. **READ IN FULL:** `Docs/000_quickstart.md` (this file)
2. **READ IN FULL:** `Docs/001_roleplay.md` (discussion + planning)
3. **READ IN FULL:** `Docs/002_docsguide.md` (standards)
4. **SKIM:** `Docs/003_templates.md` (grab templates as needed)
5. **SCAN:** Any other docs — read headers to understand purpose

**Confirm:**
```
✓ Startup complete. 
  Read: 000, 001, 002
  Skimmed: 003 (templates)
  Scanned: [X] other docs
  Ready to: [user's request]
```

### Reading Modes

| Instruction | Meaning |
|-------------|---------|
| **READ IN FULL** | Every line, don't skip |
| **SKIM** | Get the gist: topic, decisions, timeline |
| **SCAN** | Headers only, note what exists |

---

## Sitrep Format

When user says "sitrep" / "status":

```
## Sitrep: FreeGLBA

**As of:** [date]
**Purpose:** GLBA Compliance Data Access Tracking System

**Current:** [from tracker doc or "no active sprint"]
- Task 1: status
- Task 2: status

**Recent:** [last completed work]
**Blocked:** [anything stuck]

Commands: `build` · `test` · `explore` · `plan [thing]`
```

---

## Explore Sequence

When user says "explore" / "deep dive":

1. **READ IN FULL:** All docs in `Docs/` folder
2. **SCAN:** Project files (`.csproj`)
3. **READ:** Main entry point (`Program.cs`)
4. **SAMPLE:** One model, one endpoint, one UI component
5. **OUTPUT:** Summary of architecture, tech, and current state

---

# 👤 HUMAN: START HERE

---

## What is This Project?

**Name:** FreeGLBA
**One-liner:** GLBA Compliance Data Access Tracking System - tracks access to sensitive financial data
**Stack:** Blazor + C# + .NET 10
**Fork-friendly:** Keep doc structure, swap toolchain commands.

---

## FreeCRM Ecosystem

This project is part of the FreeCRM ecosystem:

| Project | Status | Description |
|---------|--------|-------------|
| **FreeCRM-main** | Public | Base template — authoritative source for all patterns |
| **FreeGLBA** | Public | GLBA compliance tracking — demonstrates extending FreeCRM |
| **FreeCICD** | Public | Example extension — community-contributed CI/CD tooling |
| nForm, Helpdesk4, etc. | Private | Production implementations — referenced but not accessible |

**Namespace Note:** FreeGLBA uses the original FreeCRM namespace patterns. This is intentional — demonstrates you can fork and extend without renaming. Only rename if you need multiple FreeCRM projects in one solution.

---

## ⚠️ MANDATORY: File Naming Convention

**All new/custom files MUST use this pattern:**

```
{ProjectName}.App.{Feature}.{OptionalSubFeature}.{Extension}
```

| Type | Example |
|------|---------|
| New page | `FreeGLBA.App.Dashboard.razor` |
| Partial file | `FreeGLBA.App.Dashboard.State.cs` |
| New entity | `FreeGLBA.App.AccessEvent.cs` |
| New DTOs | `FreeGLBA.App.DataObjects.ExternalApi.cs` |
| Base extension | `DataController.App.FreeGLBA.cs` |

**Why this matters:**
- Find all your code instantly: `find . -name "FreeGLBA.App.*"`
- Safe during FreeCRM framework updates
- Clear separation of base vs custom

**Blazor components:** Dots become underscores in class names:
- File: `FreeGLBA.App.Dashboard.razor`
- Class: `FreeGLBA_App_Dashboard`
- Usage: `<FreeGLBA_App_Dashboard />`

**Full details:** See `Docs/004_styleguide.md` → "File Organization"

---

## Prerequisites

| Required | Notes |
|----------|-------|
| Git | Latest |
| .NET 10 SDK | Check with `dotnet --version` |
| IDE | VS 2022+ / Rider / VS Code |

| Optional | When Needed |
|----------|-------------|
| Docker | Running database containers |
| SQL Server / PostgreSQL / MySQL | Production database |

---

## Setup

```bash
git clone https://github.com/WSU-EIT/FreeGLBA.git
cd FreeGLBA
dotnet restore
dotnet build
```

### Run Tests First

```bash
dotnet test
```

---

## Running Locally

### Single Command

```bash
dotnet run --project FreeGLBA
```

Navigate to `https://localhost:5001`

### Smoke Check

- [ ] App loads in browser
- [ ] Login page appears
- [ ] Dashboard loads after login

---

## Configuration

### Local Dev (appsettings.Development.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=FreeGLBA;Trusted_Connection=true;"
  },
  "DatabaseType": "SQLServer"
}
```

### User Secrets (for sensitive values)

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<VALUE>"
dotnet user-secrets set "Auth:Key" "<VALUE>"
```

### Production (Environment Variables)

```
ConnectionStrings__DefaultConnection=...
DatabaseType=SQLServer
```

---

## Common Commands

| Task | Command |
|------|---------|
| Build | `dotnet build` |
| Run | `dotnet run --project FreeGLBA` |
| Test | `dotnet test` |
| Format | `dotnet format` |
| Clean | `dotnet clean` |

---

## Troubleshooting

| Problem | Fix |
|---------|-----|
| Won't build | Check .NET 10 SDK installed; run `dotnet clean && dotnet build` |
| Config missing | Check `appsettings.Development.json` exists |
| Port in use | Change in `launchSettings.json` or kill process |
| Database error | Verify connection string and database exists |

---

## Next Steps

1. **Read** docs 001-002 for team patterns and standards
2. **Run** `sitrep` to see current state
3. **Use** `plan [feature]` before starting work

---

*Created: 2025-01-01*  
*Maintained by: WSU-EIT*

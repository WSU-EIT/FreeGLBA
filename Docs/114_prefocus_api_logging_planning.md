# 114 — Pre-Focus Group: Planning & Participant Selection

> **Document ID:** 114  
> **Category:** Planning  
> **Purpose:** Plan focus group for API request logging feature validation  
> **Attendees:** [Architect], [Backend], [Frontend], [Quality]  
> **Date:** 2025-01-27  
> **Outcome:** Focus group plan with topics, participants, and discussion guide

---

## Meeting Purpose

Before implementing our API request logging feature, we need external validation from people who would actually USE this system. The team is planning a focus group to gather real-world requirements and identify blind spots.

---

## Current System Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         FREEGLBA SYSTEM OVERVIEW                            │
└─────────────────────────────────────────────────────────────────────────────┘

    EXTERNAL SYSTEMS                         FREEGLBA SERVER
    ═══════════════                          ══════════════

  ┌─────────────────┐                    ┌───────────────────────────────────┐
  │  Banner System  │───┐                │                                   │
  │  (Student Info) │   │                │   ┌─────────────────────────────┐ │
  └─────────────────┘   │    HTTPS       │   │      GlbaController        │ │
                        │    POST        │   │   [ApiRequestLogging] ◄────┼─┼──── NEW!
  ┌─────────────────┐   │    ════►       │   │                             │ │
  │   HR System     │───┼───────────────►│   │  POST /api/glba/events     │ │
  │   (Employee)    │   │                │   │  POST /api/glba/batch      │ │
  └─────────────────┘   │                │   └──────────────┬──────────────┘ │
                        │                │                  │                │
  ┌─────────────────┐   │                │                  ▼                │
  │  Financial Aid  │───┘                │   ┌─────────────────────────────┐ │
  │    System       │                    │   │      DataAccess Layer       │ │
  └─────────────────┘                    │   └──────────────┬──────────────┘ │
                                         │                  │                │
                                         │                  ▼                │
                                         │   ┌─────────────────────────────┐ │
                                         │   │    ApiRequestLogs Table     │ │
                                         │   │   ┌─────┬─────┬─────────┐   │ │
                                         │   │   │ WHO │WHAT │ RESULT  │   │ │
                                         │   │   └─────┴─────┴─────────┘   │ │
                                         │   └─────────────────────────────┘ │
                                         └───────────────────────────────────┘
```

---

## What We Want to Validate

**[Architect]:** We've designed this feature internally. Now we need to validate it with people who represent actual users. Three key questions:

1. **Are we logging the right things?** — What do compliance officers actually need?
2. **Is our UI design useful?** — Can admins find what they need quickly?
3. **What are we missing?** — What obvious things did we overlook?

---

## Proposed Focus Group Participants

**[Quality]:** I've identified five stakeholder types we should invite:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     FOCUS GROUP PARTICIPANT MATRIX                          │
└─────────────────────────────────────────────────────────────────────────────┘

  ROLE                    PERSPECTIVE              WHAT THEY BRING
  ════                    ═══════════              ═══════════════

  ┌─────────────────┐     ┌──────────────────┐     ┌────────────────────────┐
  │  COMPLIANCE     │────►│ Legal/Regulatory │────►│ What MUST be logged    │
  │  OFFICER        │     │ Requirements     │     │ for GLBA audits        │
  └─────────────────┘     └──────────────────┘     └────────────────────────┘

  ┌─────────────────┐     ┌──────────────────┐     ┌────────────────────────┐
  │  SYSTEM         │────►│ Technical Ops    │────►│ How logs are used for  │
  │  ADMINISTRATOR  │     │ Debugging        │     │ troubleshooting        │
  └─────────────────┘     └──────────────────┘     └────────────────────────┘

  ┌─────────────────┐     ┌──────────────────┐     ┌────────────────────────┐
  │  SECURITY       │────►│ Threat Detection │────►│ What attackers look    │
  │  ANALYST        │     │ Forensics        │     │ like in logs           │
  └─────────────────┘     └──────────────────┘     └────────────────────────┘

  ┌─────────────────┐     ┌──────────────────┐     ┌────────────────────────┐
  │  API            │────►│ Integration Pain │────►│ What developers need   │
  │  DEVELOPER      │     │ Points           │     │ when things break      │
  └─────────────────┘     └──────────────────┘     └────────────────────────┘

  ┌─────────────────┐     ┌──────────────────┐     ┌────────────────────────┐
  │  DBA /          │────►│ Data Management  │────►│ Storage, retention,    │
  │  DATA ENGINEER  │     │ Performance      │     │ query performance      │
  └─────────────────┘     └──────────────────┘     └────────────────────────┘
```

---

## Discussion Topics & Points

**[Architect]:** Here's the structured discussion plan:

### Topic 1: What Should Be Logged? (25 min)

| Point | Key Questions | Who Leads |
|-------|---------------|-----------|
| 1.1 | What fields are legally required for GLBA compliance? | Compliance Officer |
| 1.2 | What helps you trace a failed request? | API Developer |
| 1.3 | What indicates a security threat? | Security Analyst |
| 1.4 | What's overkill / noise? | All |

### Topic 2: Body Logging Trade-offs (15 min)

| Point | Key Questions | Who Leads |
|-------|---------------|-----------|
| 2.1 | Do you ever need to see actual request/response bodies? | All |
| 2.2 | What's the PII risk in logged bodies? | Compliance + Security |
| 2.3 | Should bodies be opt-in, always-on, or never? | All |

### Topic 3: Retention & Storage (15 min)

| Point | Key Questions | Who Leads |
|-------|---------------|-----------|
| 3.1 | How long do you need logs for audits? | Compliance Officer |
| 3.2 | How long do you need logs for debugging? | API Developer |
| 3.3 | What's the storage/performance impact concern? | DBA |

### Topic 4: User Interface Needs (20 min)

| Point | Key Questions | Who Leads |
|-------|---------------|-----------|
| 4.1 | What's your first action when something breaks? | Sys Admin |
| 4.2 | What filters matter most? | All |
| 4.3 | Do you need export/download capability? | Security + Compliance |
| 4.4 | Real-time vs historical queries? | All |

### Topic 5: What Are We Missing? (15 min)

| Point | Key Questions | Who Leads |
|-------|---------------|-----------|
| 5.1 | What logging features do you wish you had? | All |
| 5.2 | What mistakes have you seen in other systems? | All |
| 5.3 | Any regulatory changes coming we should prepare for? | Compliance |

---

## Pre-Meeting Research Tasks

**[Backend]:** Before the focus group, we need to research:

| Research Topic | Owner | Due |
|----------------|-------|-----|
| GLBA audit requirements for access logging | [Quality] | Before meeting |
| PCI-DSS 10.x logging requirements (as comparison) | [Quality] | Before meeting |
| Common API logging patterns in enterprise tools | [Backend] | Before meeting |
| Typical log retention requirements by industry | [Architect] | Before meeting |

---

## Focus Group Logistics

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         FOCUS GROUP AGENDA                                  │
└─────────────────────────────────────────────────────────────────────────────┘

  TIME          ACTIVITY                              LEAD
  ════          ════════                              ════

  00:00-00:05   Welcome & Ground Rules                [Architect]
                │
                ▼
  00:05-00:15   System Overview & Current Design      [Backend]
                │
                ▼
  00:15-00:40   Topic 1: What Should Be Logged?       [Quality]
                │
                ▼
  00:40-00:55   Topic 2: Body Logging Trade-offs      [Architect]
                │
                ▼
  00:55-01:10   Topic 3: Retention & Storage          [Backend]
                │
                ▼
  01:10-01:30   Topic 4: User Interface Needs         [Frontend]
                │
                ▼
  01:30-01:45   Topic 5: What Are We Missing?         [Quality]
                │
                ▼
  01:45-01:55   Wrap-up & Next Steps                  [Architect]
                │
                ▼
  01:55-02:00   Thank You & Close                     [Architect]

  TOTAL: 2 Hours
```

---

## Expected Outputs

After the focus group, we will produce:

1. **Transcript** (Doc 115) — Full discussion record
2. **Analysis** (Doc 116) — Team review of suggestions
3. **Updated CTO Proposal** (Doc 117) — Final design incorporating feedback

---

## Pre-Read Materials for Participants

We'll send participants:

1. One-page system overview
2. Current entity design (simplified)
3. UI mockup wireframe
4. 5 key questions we want answered

---

## Participant Invites

| Role | Contact | Confirmed |
|------|---------|-----------|
| Compliance Officer | University GLBA Coordinator | ☐ Pending |
| System Administrator | IT Operations Lead | ☐ Pending |
| Security Analyst | InfoSec Team | ☐ Pending |
| API Developer | Integration Team Lead | ☐ Pending |
| DBA / Data Engineer | Database Admin | ☐ Pending |

---

**[Architect]:** Once we have confirmations, we'll proceed with the focus group. The goal is validation, not design-by-committee. We're looking for blind spots and real-world requirements we may have missed.

---

*Created: 2025-01-27*  
*Next: Focus Group Session (Doc 115)*

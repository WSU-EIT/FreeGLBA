# FreeGLBA - Generic Data Access Audit & Compliance Tracking System

## Overview

FreeGLBA is a **generic compliance tracking system** designed to monitor and audit access to protected data. While originally designed for **GLBA (Gramm-Leach-Bliley Act)** compliance in educational institutions, the system is flexible enough to track data access for **any regulatory framework** or **any industry**.

### Supported Use Cases

| Industry | Regulation | What to Track |
|----------|------------|---------------|
| Education | GLBA, FERPA | Student financial aid, academic records |
| Healthcare | HIPAA | Patient health records |
| Finance | SOX, PCI-DSS | Financial transactions, cardholder data |
| Government | FISMA | Citizen PII |
| General | GDPR, CCPA | Personal data access |
| Any | Internal Policy | Any sensitive data access |

### Core Concept

The system answers these fundamental audit questions:
- **WHO** accessed the data (UserId, UserName, UserDepartment)
- **WHOSE** data was accessed (SubjectId, SubjectType)
- **WHAT** type of access occurred (AccessType, DataCategory)
- **WHEN** the access happened (AccessedAt)
- **WHY** the access was needed (Purpose)
- **WHERE** the access came from (SourceSystem, IpAddress)

---

## Data Model

### Entity Relationship Diagram

```
┌─────────────────┐       ┌─────────────────────┐       ┌─────────────────┐
│  SourceSystem   │──────<│    AccessEvent      │>──────│   DataSubject   │
│                 │  1:N  │                     │  N:1  │                 │
│ Any external    │       │ Who accessed what   │       │ Individual whose│
│ application     │       │ when and why        │       │ data was viewed │
└─────────────────┘       └─────────────────────┘       └─────────────────┘
                                   │
                                   │ Aggregated into
                                   ▼
                          ┌─────────────────────┐
                          │  ComplianceReport   │
                          │                     │
                          │ Periodic summary    │
                          │ for auditors        │
                          └─────────────────────┘
```

---

## Entities

### 1. SourceSystem

**Purpose:** Tracks external systems that send access events. This is completely generic - any system that accesses protected data can be registered.

| Field | Type | Max Length | Description |
|-------|------|------------|-------------|
| `SourceSystemId` | GUID | - | Primary key (auto-generated) |
| `Name` | string | 200 | System identifier (any name you choose) |
| `DisplayName` | string | 200 | Friendly display name |
| `ApiKey` | string | 500 | Auto-generated API key for authentication |
| `ContactEmail` | string | 200 | Contact email for system administrator |
| `IsActive` | boolean | - | Whether system can send events |
| `LastEventReceivedAt` | DateTime? | - | Timestamp of most recent event (auto-updated) |
| `EventCount` | long | - | Total events received from this source (auto-updated) |

**Example Source Systems:**
- Banner (Higher Ed ERP)
- PeopleSoft (HR/Finance)
- Epic (Healthcare EMR)
- Salesforce (CRM)
- Custom internal applications
- Database query tools
- Report generators
- Any application that touches sensitive data

---

### 2. AccessEvent

**Purpose:** The core audit log - records every instance of protected data access. All fields except IDs and timestamps are **free-text**, making this completely generic.

| Field | Type | Max Length | Description | Required |
|-------|------|------------|-------------|----------|
| `AccessEventId` | GUID | - | Primary key (auto-generated) | Auto |
| `SourceSystemId` | GUID | - | Foreign key to SourceSystem (optional) | No |
| `SourceEventId` | string | 200 | Deduplication key from source system | No |
| `AccessedAt` | DateTime | - | When the access occurred | **Yes** |
| `ReceivedAt` | DateTime | - | When system received the event (auto-set) | Auto |
| `UserId` | string | 200 | Identifier of who accessed data | **Yes** |
| `UserName` | string | 200 | Display name of the accessor | No |
| `UserEmail` | string | 200 | Email of the accessor | No |
| `UserDepartment` | string | 200 | Department/team of the accessor | No |
| `SubjectId` | string | 200 | ID of person whose data was accessed | **Yes** |
| `SubjectType` | string | 50 | Type of subject (free-text) | No |
| `DataCategory` | string | 100 | Category of data accessed (free-text) | No |
| `AccessType` | string | 50 | Type of access (free-text) | **Yes** |
| `Purpose` | string | 500 | Business justification | No |
| `IpAddress` | string | 50 | IP address of accessor | No |
| `AdditionalData` | string | unlimited | JSON for any custom fields | No |

**Why These Fields Are Generic:**

| Field | Examples by Industry |
|-------|---------------------|
| `UserId` | Employee ID, Login username, Badge number, Email |
| `SubjectId` | Student ID, Patient MRN, Customer ID, SSN, Account # |
| `SubjectType` | Student, Patient, Customer, Employee, Member, Vendor |
| `DataCategory` | Financial Aid, PHI, PII, Payment Data, Tax Records |
| `AccessType` | View, Export, Print, Query, Modify, Delete, API Call |
| `UserDepartment` | Financial Aid, Nursing, IT, HR, Accounting |

**The AdditionalData JSON field** allows any custom fields your organization needs:
```json
{
  "applicationModule": "Student Accounts",
  "screenName": "Account Summary",
  "recordCount": 1,
  "exportFormat": "PDF",
  "approvalId": "APR-2024-001"
}
```

---

### 3. DataSubject

**Purpose:** Aggregates statistics for each individual whose data has been accessed. The only identifier is a generic `ExternalId` that can be anything.

| Field | Type | Max Length | Description |
|-------|------|------------|-------------|
| `DataSubjectId` | GUID | - | Primary key (auto-generated) |
| `ExternalId` | string | 200 | Any identifier (Student ID, MRN, Customer #, etc.) |
| `SubjectType` | string | 50 | Type of subject (free-text) |
| `FirstAccessedAt` | DateTime | - | When their data was first accessed (auto-set) |
| `LastAccessedAt` | DateTime | - | Most recent access to their data (auto-updated) |
| `TotalAccessCount` | long | - | How many times their data was accessed (auto-updated) |
| `UniqueAccessorCount` | int | - | How many different people accessed (calculated) |

---

### 4. ComplianceReport

**Purpose:** Stores generated compliance reports. Report types are free-text, allowing any type of report.

| Field | Type | Max Length | Description |
|-------|------|------------|-------------|
| `ComplianceReportId` | GUID | - | Primary key (auto-generated) |
| `ReportType` | string | 50 | Any report type (free-text) |
| `GeneratedAt` | DateTime | - | When the report was created (auto-set) |
| `GeneratedBy` | string | 200 | Who requested/created the report |
| `PeriodStart` | DateTime | - | Start of reporting period |
| `PeriodEnd` | DateTime | - | End of reporting period |
| `TotalEvents` | long | - | Total access events in period (auto-calculated) |
| `UniqueUsers` | int | - | Unique accessors in period (auto-calculated) |
| `UniqueSubjects` | int | - | Unique data subjects in period (auto-calculated) |
| `ReportData` | string | unlimited | JSON report content |
| `FileUrl` | string | 500 | URL to downloadable report file |

---

## Flexibility by Design

### No Hardcoded Values

All categorical fields use **free-text with suggestions**:

| Field | Suggested Values | But You Can Use... |
|-------|------------------|-------------------|
| SubjectType | Student, Employee, Customer | Patient, Member, Vendor, Applicant, anything |
| DataCategory | Financial Aid, Personal Info | PHI, PCI Data, FERPA Records, anything |
| AccessType | View, Export, Print | API Call, Batch Query, Screen Capture, anything |
| ReportType | Annual, Quarterly, Audit | HIPAA Review, SOX Compliance, anything |

### Source System Agnostic

The system doesn't care where data comes from:
- ERP systems (Banner, PeopleSoft, Workday)
- EMR systems (Epic, Cerner)
- CRM systems (Salesforce, Dynamics)
- Custom applications
- Database tools (SQL Server Management Studio)
- Report writers (Crystal Reports, SSRS)
- Anything that can call an API or export a CSV

### AdditionalData JSON

The `AdditionalData` field on AccessEvent accepts any JSON, allowing you to capture system-specific information without schema changes:

```json
// Healthcare example
{
  "encounterType": "Outpatient",
  "facility": "Main Campus",
  "breakTheGlass": false
}

// Financial example
{
  "transactionId": "TXN-123456",
  "accountType": "Checking",
  "amountViewed": true
}

// Education example
{
  "term": "Fall 2024",
  "aidYear": "2024-2025",
  "formType": "FAFSA"
}
```

---

## CSV Import Requirements

When importing data from external systems via CSV, the following fields should be mapped:

### Minimum Required Fields

```csv
accessed_at,user_id,subject_id,access_type
2024-01-15 10:30:00,jsmith,12345,View
2024-01-15 10:31:00,jdoe,67890,Export
```

### Recommended Fields

```csv
accessed_at,user_id,user_name,user_email,user_department,subject_id,subject_type,data_category,access_type,purpose,ip_address,source_event_id,additional_data
2024-01-15 10:30:00,jsmith,John Smith,jsmith@org.com,Finance,12345,Customer,Payment Data,View,Account inquiry,192.168.1.100,EVT-001,"{""screen"":""AccountDetail""}"
```

### Field Mapping

| CSV Column | Maps To | Required | Notes |
|------------|---------|----------|-------|
| `accessed_at` | AccessedAt | **Yes** | ISO 8601 format preferred |
| `user_id` | UserId | **Yes** | Any user identifier |
| `subject_id` | SubjectId | **Yes** | Any subject identifier |
| `access_type` | AccessType | **Yes** | Any access type description |
| `user_name` | UserName | No | Display name |
| `user_email` | UserEmail | No | Email address |
| `user_department` | UserDepartment | No | Department/team name |
| `subject_type` | SubjectType | No | Any subject classification |
| `data_category` | DataCategory | No | Any data classification |
| `purpose` | Purpose | No | Business justification |
| `ip_address` | IpAddress | No | Source IP |
| `source_event_id` | SourceEventId | No | For deduplication |
| `additional_data` | AdditionalData | No | JSON for custom fields |

---

## API Integration

External systems can send events via REST API:

### Authentication
```
Authorization: Bearer {api-key}
```

### Single Event
```http
POST /api/glba/events
Content-Type: application/json

{
  "accessedAt": "2024-01-15T10:30:00Z",
  "userId": "jsmith",
  "userName": "John Smith",
  "subjectId": "12345",
  "subjectType": "Customer",
  "dataCategory": "Payment Data",
  "accessType": "View",
  "purpose": "Account inquiry",
  "additionalData": "{\"screen\":\"AccountDetail\"}"
}
```

### Batch Events (up to 1000)
```http
POST /api/glba/events/batch
Content-Type: application/json

[
  { ... event 1 ... },
  { ... event 2 ... }
]
```

---

## Compliance Use Cases

### Any Regulatory Audit
Generate a report showing:
- Total access events for the period
- Breakdown by data category
- Breakdown by access type
- List of all users who accessed protected data
- List of all subjects whose data was accessed

### Subject Access Request (GDPR, CCPA, etc.)
When an individual requests information about who accessed their data:
- Search by SubjectId
- Return all AccessEvents for that subject
- Include accessor information
- Include timestamps and purposes

### Security Investigation
When investigating potential unauthorized access:
- Filter by user, date range, IP address
- Review access patterns for anomalies
- Export detailed logs for forensic analysis

### Policy Compliance Verification
Demonstrate that:
- Only authorized personnel access protected data
- Access is logged from approved source systems
- Appropriate business justifications are documented

---

## Data Retention

Retention requirements vary by regulation:
- **GLBA:** 7 years minimum
- **HIPAA:** 6 years
- **SOX:** 7 years
- **GDPR:** As long as necessary for purpose
- **Your Policy:** Configure as needed

---

## Glossary

| Term | Definition |
|------|------------|
| **Data Subject** | The individual whose data is being accessed |
| **Accessor** | The person viewing/using the protected data |
| **Source System** | External application that generates access events |
| **Access Event** | A single instance of protected data being accessed |
| **Data Category** | Classification of the data being accessed |
| **Access Type** | How the data was accessed |

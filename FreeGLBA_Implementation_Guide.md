# FreeGLBA - Implementation Status & Integration Guide

## Your Goal (The Complete Workflow)

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              USER'S WORKFLOW                                     │
└─────────────────────────────────────────────────────────────────────────────────┘

                                YOUR OTHER WEB APP
                            (e.g., data export tool)
                                      │
                                      │ User requests CSV export
                                      │ User accepts GLBA terms
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│  1. User clicks "Export Data"                                                    │
│  2. App shows GLBA acknowledgment modal                                          │
│  3. User agrees to terms                                                         │
│  4. App collects access metadata:                                                │
│     - Who is requesting (user ID, name, email, department)                       │
│     - Whose data (subject ID - student/employee ID)                              │
│     - What type of access (Export)                                               │
│     - Why (business purpose from user input)                                     │
│  5. App POSTs to glba.em.wsu.edu/api/glba/events                                │
│  6. If response.status == "accepted" → Provide CSV to user                       │
│     If response.status != "accepted" → Show error, deny export                   │
└─────────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      │ HTTP POST with API Key
                                      │ Authorization: Bearer {api-key}
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                         FREEGLBA (glba.em.wsu.edu)                               │
│                                                                                  │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐       │
│  │   Source    │    │   Access    │    │    Data     │    │ Compliance  │       │
│  │   Systems   │───▶│   Events    │───▶│   Subjects  │───▶│   Reports   │       │
│  └─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘       │
│        │                  │                  │                  │                │
│        │                  │                  │                  │                │
│   API Keys          Audit Log          Aggregated         Compliance            │
│   Auth              Who/What/When      Stats              Evidence              │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      │ Response
                                      ▼
                    {"eventId": "...", "status": "accepted", ...}
```

---

## Implementation Status

### ✅ COMPLETE - Ready to Use

| Component | Status | Location |
|-----------|--------|----------|
| **SourceSystem Entity** | ✅ Done | Stores API keys, tracks systems |
| **AccessEvent Entity** | ✅ Done | Core audit log |
| **DataSubject Entity** | ✅ Done | Aggregated per-person stats |
| **ComplianceReport Entity** | ✅ Done | Generated reports |
| **API Endpoint: POST /api/glba/events** | ✅ Done | `GlbaController.cs` |
| **API Endpoint: POST /api/glba/events/batch** | ✅ Done | `GlbaController.cs` |
| **API Key Validation Logic** | ✅ Done | `DataAccess.ApiKey.cs` |
| **API Key Middleware** | ✅ Done | `ApiKeyMiddleware.cs` |
| **Event Processing Logic** | ✅ Done | `DataAccess.ExternalApi.cs` |
| **Deduplication by SourceEventId** | ✅ Done | Checks before insert |
| **Dashboard UI** | ✅ Done | `GlbaDashboard.razor` |
| **Source Systems Management UI** | ✅ Done | `SourceSystemsPage.razor` |
| **Access Events List UI** | ✅ Done | `AccessEventsPage.razor` |
| **Data Subjects List UI** | ✅ Done | `DataSubjectsPage.razor` |
| **Compliance Reports UI** | ✅ Done | `ComplianceReportsPage.razor` |
| **Auto-generate API Keys** | ✅ Done | On SourceSystem create |
| **Auto-set ReceivedAt timestamp** | ✅ Done | On event processing |
| **Auto-update SourceSystem stats** | ✅ Done | EventCount, LastEventReceivedAt |
| **Auto-update DataSubject stats** | ✅ Done | TotalAccessCount, LastAccessedAt |

### ⚠️ NEEDS SETUP - One-Time Configuration

| Item | What to Do |
|------|------------|
| **Register API Key Middleware** | Add `app.UseApiKeyAuth();` to Program.cs |
| **Deploy to glba.em.wsu.edu** | Standard deployment |
| **Create SourceSystem record** | Use the UI after deployment |
| **Copy API key to other app** | One-time after creating SourceSystem |

### ❌ NOT IN FREEGLBA - Your Other App's Responsibility

| Item | Where It Lives |
|------|---------------|
| GLBA acknowledgment modal/checkbox | Your data export app |
| Collecting user info (who's exporting) | Your data export app |
| Collecting subject info (whose data) | Your data export app |
| Collecting purpose/justification | Your data export app |
| HTTP POST to FreeGLBA API | Your data export app |
| Conditional CSV delivery | Your data export app |

---

## Setup Steps

### Step 1: Register the API Key Middleware

Add this line to `Program.cs` after `app.UseAuthentication()`:

```csharp
// Around line 230 in Program.cs
app.UseAuthentication();
app.UseAuthorization();

// ADD THIS LINE:
app.UseApiKeyAuth();  // Validates API keys for /api/glba/events endpoints

app.UseAntiforgery();
```

**File:** `FreeGLBA\Program.cs`

### Step 2: Add the Using Statement

Add at the top of Program.cs (if not already there):

```csharp
using FreeGLBA.Middleware;
```

### Step 3: Deploy FreeGLBA

Deploy to `glba.em.wsu.edu` using your standard deployment process.

### Step 4: Create a Source System

1. Log in to `glba.em.wsu.edu`
2. Go to **Source Systems** page
3. Click **New Source System**
4. Fill in:
   - **Name:** `DataExportTool` (or your app's name)
   - **Display Name:** `WSU Data Export Tool`
   - **Contact Email:** `your-email@wsu.edu`
   - **Active:** ✓ (checked)
5. Click **Save**
6. **IMPORTANT:** Copy the displayed API key immediately - it won't be shown again!

### Step 5: Configure Your Other App

In your other web app's `appsettings.json`:

```json
{
  "GlbaTracking": {
    "Enabled": true,
    "Endpoint": "https://glba.em.wsu.edu/api/glba/events",
    "ApiKey": "YOUR_API_KEY_HERE"
  }
}
```

---

## API Reference for Your Other App

### Endpoint

```
POST https://glba.em.wsu.edu/api/glba/events
```

### Headers

```
Authorization: Bearer YOUR_API_KEY_HERE
Content-Type: application/json
```

### Request Body

```json
{
  "accessedAt": "2024-01-15T10:30:00Z",
  "userId": "john.smith",
  "userName": "John Smith",
  "userEmail": "john.smith@wsu.edu",
  "userDepartment": "Financial Aid",
  "subjectId": "11234567",
  "subjectType": "Student",
  "dataCategory": "Financial Aid",
  "accessType": "Export",
  "purpose": "Student requested aid verification letter",
  "ipAddress": "192.168.1.100",
  "sourceEventId": "EXPORT-2024-001-12345"
}
```

### Required Fields

| Field | Description |
|-------|-------------|
| `accessedAt` | When the access occurred (ISO 8601) |
| `userId` | Who is accessing (employee ID, username, etc.) |
| `subjectId` | Whose data is being accessed (student ID, etc.) |
| `accessType` | Type of access (Export, View, Print, etc.) |

### Response - Success

```json
{
  "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "receivedAt": "2024-01-15T10:30:01Z",
  "status": "accepted",
  "message": null
}
```

### Response - Duplicate

```json
{
  "eventId": null,
  "receivedAt": "2024-01-15T10:30:01Z",
  "status": "duplicate",
  "message": "Event with this SourceEventId already exists"
}
```

### Response - Error

```json
{
  "eventId": null,
  "receivedAt": "2024-01-15T10:30:01Z",
  "status": "error",
  "message": "Missing required field: SubjectId"
}
```

---

## Sample Code for Your Other App

### C# / .NET

```csharp
public class GlbaTrackingService
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _apiKey;

    public GlbaTrackingService(IConfiguration config)
    {
        _httpClient = new HttpClient();
        _endpoint = config["GlbaTracking:Endpoint"];
        _apiKey = config["GlbaTracking:ApiKey"];
    }

    public async Task<bool> LogAccessAndExport(
        string userId,
        string userName,
        string userEmail,
        string userDepartment,
        string subjectId,
        string purpose)
    {
        var request = new
        {
            accessedAt = DateTime.UtcNow,
            userId = userId,
            userName = userName,
            userEmail = userEmail,
            userDepartment = userDepartment,
            subjectId = subjectId,
            subjectType = "Student",
            dataCategory = "Financial Aid",
            accessType = "Export",
            purpose = purpose,
            sourceEventId = $"EXPORT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, _endpoint);
        httpRequest.Headers.Add("Authorization", $"Bearer {_apiKey}");
        httpRequest.Content = JsonContent.Create(request);

        var response = await _httpClient.SendAsync(httpRequest);
        
        if (!response.IsSuccessStatusCode)
            return false;

        var result = await response.Content.ReadFromJsonAsync<GlbaResponse>();
        return result?.Status == "accepted";
    }
}

public class GlbaResponse
{
    public Guid? EventId { get; set; }
    public DateTime ReceivedAt { get; set; }
    public string Status { get; set; }
    public string? Message { get; set; }
}
```

### Usage in Your Export Controller

```csharp
[HttpPost("export")]
public async Task<IActionResult> ExportStudentData(ExportRequest request)
{
    // 1. Verify user accepted GLBA terms
    if (!request.AcceptedGlbaTerms)
        return BadRequest("You must accept GLBA terms to export data");

    // 2. Log to FreeGLBA - MUST succeed before export
    var logged = await _glbaService.LogAccessAndExport(
        userId: User.Identity.Name,
        userName: User.FindFirst("name")?.Value,
        userEmail: User.FindFirst("email")?.Value,
        userDepartment: User.FindFirst("department")?.Value,
        subjectId: request.StudentId,
        purpose: request.Purpose
    );

    if (!logged)
        return StatusCode(503, "Unable to log GLBA access. Export denied.");

    // 3. Only now provide the CSV
    var csvData = await _dataService.GenerateCsv(request.StudentId);
    return File(csvData, "text/csv", $"student_{request.StudentId}.csv");
}
```

---

## Architecture Summary

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              COMPLETE SYSTEM                                     │
└─────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────┐         ┌─────────────────────────────────────────────┐
│   YOUR DATA EXPORT APP  │         │              FREEGLBA                        │
│   (yourapp.wsu.edu)     │         │          (glba.em.wsu.edu)                   │
│                         │         │                                              │
│  ┌───────────────────┐  │  POST   │  ┌─────────────────┐                        │
│  │ Export Request    │  │ ──────▶ │  │ ApiKeyMiddleware│ Validates API key       │
│  │ + GLBA Checkbox   │  │         │  └────────┬────────┘                        │
│  │ + Purpose Field   │  │         │           │                                  │
│  └────────┬──────────┘  │         │           ▼                                  │
│           │             │         │  ┌─────────────────┐                        │
│           │ if accepted │         │  │ GlbaController  │ POST /api/glba/events  │
│           │             │         │  └────────┬────────┘                        │
│           ▼             │         │           │                                  │
│  ┌───────────────────┐  │         │           ▼                                  │
│  │ GlbaTrackingService│  │         │  ┌─────────────────┐                        │
│  │ .LogAccessAndExport│  │         │  │ ProcessGlbaEvent│ Validates, dedupes     │
│  └────────┬──────────┘  │         │  └────────┬────────┘                        │
│           │             │         │           │                                  │
│           │ if status   │ ◀────── │           ▼                                  │
│           │ == accepted │ Response│  ┌─────────────────┐                        │
│           │             │         │  │ Database        │                        │
│           ▼             │         │  │ - AccessEvents  │                        │
│  ┌───────────────────┐  │         │  │ - DataSubjects  │                        │
│  │ Serve CSV to User │  │         │  │ - SourceSystems │                        │
│  └───────────────────┘  │         │  └─────────────────┘                        │
│                         │         │           │                                  │
│                         │         │           ▼                                  │
│                         │         │  ┌─────────────────┐                        │
│                         │         │  │ Dashboard/UI    │ View logs, reports     │
│                         │         │  └─────────────────┘                        │
│                         │         │                                              │
└─────────────────────────┘         └─────────────────────────────────────────────┘

        YOUR RESPONSIBILITY                      FREEGLBA (DONE)
```

---

## What's Left To Do

### In FreeGLBA (5 minutes)

1. **Add middleware registration** to Program.cs:
   ```csharp
   using FreeGLBA.Middleware;
   // ...
   app.UseApiKeyAuth();
   ```

2. **Deploy** to glba.em.wsu.edu

3. **Create SourceSystem** record via UI and copy API key

### In Your Other App (Your development work)

1. Add `GlbaTracking` config to appsettings.json
2. Create `GlbaTrackingService` class (see sample above)
3. Add GLBA acknowledgment UI (checkbox + purpose field)
4. Call tracking service before providing CSV
5. Only serve CSV if tracking returns "accepted"

---

## Quick Checklist

- [ ] Add `app.UseApiKeyAuth();` to Program.cs
- [ ] Add `using FreeGLBA.Middleware;` to Program.cs
- [ ] Deploy FreeGLBA to glba.em.wsu.edu
- [ ] Create SourceSystem record for your export app
- [ ] Copy API key to your export app's config
- [ ] Implement GlbaTrackingService in your export app
- [ ] Add GLBA acknowledgment UI to your export app
- [ ] Test end-to-end flow

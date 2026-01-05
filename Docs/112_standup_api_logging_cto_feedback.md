# 112 — Standup: API Request Logging - CTO Feedback Response

> **Document ID:** 112  
> **Category:** Standup  
> **Purpose:** Quick standup to address CTO feedback on API logging design  
> **Attendees:** [Architect], [Backend], [Frontend], [Quality], [Sanity]  
> **Date:** 2025-01-27  
> **Duration:** 15 minutes  
> **Outcome:** Design refined per CTO requirements

---

## CTO Feedback Summary

The CTO provided the following direction:

1. **Attribute-based approach** — Use `[ApiRequestLogging]` on controllers, not middleware
2. **Mandatory, not optional** — This is critical compliance infrastructure
3. **Prevent infinite loops** — Don't log the logging endpoint itself
4. **Track both directions** — Capture request IN and response OUT
5. **Timing for debugging** — Measure execution duration with Stopwatch

---

## Standup Transcript

**[Architect]:** Alright, quick standup. CTO gave us clear direction. Let's address each point.

**[Backend]:** First thing—switching from middleware to action filter. The CTO wants `[ApiRequestLogging]` on the controller, like how `[Authorize]` or `[Route]` works. This is actually cleaner.

```csharp
[ApiController]
[Route("api/[controller]")]
[ApiRequestLogging]  // <-- CTO wants this
public class GlbaController : ControllerBase
```

**[Architect]:** Right. Microsoft's tutorial on performance measurement actually shows this exact pattern—an `ActionFilterAttribute` with Stopwatch for timing. Here's the key insight:

```csharp
public class ApiRequestLoggingAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // Start timer, capture request
        var stopwatch = Stopwatch.StartNew();
        context.HttpContext.Items["ApiLog_Stopwatch"] = stopwatch;
        context.HttpContext.Items["ApiLog_RequestedAt"] = DateTime.UtcNow;
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        // Stop timer, capture response, save log
        var stopwatch = context.HttpContext.Items["ApiLog_Stopwatch"] as Stopwatch;
        stopwatch?.Stop();
        var durationMs = stopwatch?.ElapsedMilliseconds ?? 0;
        
        // Create and save log entry
    }
}
```

**[Quality]:** The `HttpContext.Items` dictionary is perfect for passing the stopwatch between the two methods. That's the standard pattern.

---

**[Backend]:** Now for the infinite loop prevention. CTO said: "if the /api/log endpoint is called to create a log, then hitting the log shouldn't be creating a log."

**[Architect]:** Two approaches:

| Approach | How It Works | Pros | Cons |
|----------|--------------|------|------|
| **Exclude attribute** | `[SkipApiLogging]` on specific actions | Explicit, visible | Must remember to add |
| **Path-based exclusion** | Check path in filter, skip if matches | Automatic | Less visible |
| **Self-detection** | Check if request IS a log write | Foolproof | Slightly complex |

**[Backend]:** I vote for approach 1 + 3 combined. We add `[SkipApiLogging]` to the log endpoints explicitly, AND the filter checks for self-referential calls as a safety net.

```csharp
[ApiController]
[Route("api/[controller]")]
[ApiRequestLogging]
public class ApiLogsController : ControllerBase
{
    [HttpPost]
    [SkipApiLogging]  // Prevents infinite loop
    public async Task<IActionResult> CreateLog([FromBody] ApiRequestLog log)
    {
        // ...
    }
}
```

**[Quality]:** And the filter checks:

```csharp
public override void OnActionExecuting(ActionExecutingContext context)
{
    // Check for skip attribute
    var skipAttribute = context.ActionDescriptor.EndpointMetadata
        .OfType<SkipApiLoggingAttribute>()
        .FirstOrDefault();
    
    if (skipAttribute != null)
    {
        context.HttpContext.Items["ApiLog_Skip"] = true;
        return;
    }
    
    // Start logging...
}
```

**[Sanity]:** Simple and explicit. I like it.

---

**[Architect]:** CTO also said this is mandatory, not optional. That changes our configuration model. We're not asking "should we log?" — we're always logging. The only config is "how much detail?"

**[Backend]:** Updated approach:

```csharp
// OLD thinking (optional)
public bool Enabled { get; set; } = true;

// NEW thinking (mandatory, configurable detail)
public bool IncludeRequestBody { get; set; } = false;
public bool IncludeResponseBody { get; set; } = false;
public int BodyLogLimit { get; set; } = 4096;
```

**[Quality]:** The attribute itself could have options too:

```csharp
[ApiRequestLogging]  // Default: log metadata only
[ApiRequestLogging(IncludeBodies = true)]  // Log bodies too
```

**[Architect]:** Good. Let's keep the attribute simple and pull detailed config from `IOptions<ApiLoggingOptions>`.

---

**[Backend]:** On timing—the Stopwatch approach is what Microsoft recommends. We start in `OnActionExecuting`, stop in `OnActionExecuted`. The elapsed time goes straight into `DurationMs`.

**[Frontend]:** For the UI, we can show a colored indicator:
- 🟢 < 100ms — Fast
- 🟡 100-500ms — Normal  
- 🟠 500-1000ms — Slow
- 🔴 > 1000ms — Very Slow

**[Quality]:** Good for debugging. Admins can filter by slow requests to find performance issues.

---

**[Architect]:** One more thing—CTO wants to track "both directions." The current design captures:

| Direction | What We Capture |
|-----------|-----------------|
| **IN (Request)** | Method, Path, Headers, Body (optional), IP, UserAgent |
| **OUT (Response)** | StatusCode, Body (optional), ErrorMessage, ExceptionType |
| **TIMING** | RequestedAt, RespondedAt, DurationMs |

**[Backend]:** That's already in our entity. We're good.

---

## Revised Implementation Plan

**[Architect]:** Let me summarize the changes:

### From Middleware → To Action Filter

| Before | After |
|--------|-------|
| `ApiRequestLoggingMiddleware` | `ApiRequestLoggingAttribute` |
| Registered in `Program.cs` | Applied to controllers |
| Logs all requests matching path | Logs only attributed controllers |
| Optional via config | Mandatory (attribute = logged) |

### New Attributes

| Attribute | Purpose |
|-----------|---------|
| `[ApiRequestLogging]` | Apply to controller to enable logging |
| `[SkipApiLogging]` | Apply to specific actions to prevent logging |

### Stopwatch Pattern

```csharp
OnActionExecuting:
  ├── Check for [SkipApiLogging] → exit if present
  ├── Start Stopwatch
  ├── Capture Request (method, path, headers, body)
  └── Store in HttpContext.Items

OnActionExecuted:
  ├── Check skip flag → exit if true
  ├── Stop Stopwatch
  ├── Capture Response (status, body, errors)
  ├── Build ApiRequestLog entity
  └── Fire-and-forget save to database
```

---

## Files Update

| Original Plan | Updated Plan |
|---------------|--------------|
| `FreeGLBA.App.ApiRequestLoggingMiddleware.cs` | `FreeGLBA.App.ApiRequestLoggingAttribute.cs` |
| — | `FreeGLBA.App.SkipApiLoggingAttribute.cs` (new) |
| `FreeGLBA.App.ApiRequestLog.cs` | Same |
| `FreeGLBA.App.DataObjects.ApiRequestLog.cs` | Same |
| `FreeGLBA.App.DataAccess.ApiRequestLog.cs` | Same |
| `FreeGLBA.App.ApiRequestLogs.razor` | Same |

---

## Action Items

| Task | Owner | Notes |
|------|-------|-------|
| Create `ApiRequestLoggingAttribute` | [Backend] | With Stopwatch timing |
| Create `SkipApiLoggingAttribute` | [Backend] | Simple marker attribute |
| Apply to GlbaController | [Backend] | Test end-to-end |
| Update UI with timing indicators | [Frontend] | Color-coded duration |

---

**[Sanity]:** Final check—are we overcomplicating?

**[Architect]:** No. The CTO gave clear requirements:
- ✅ Attribute-based: `[ApiRequestLogging]`
- ✅ Mandatory: no "disable" option
- ✅ No infinite loops: `[SkipApiLogging]`
- ✅ Both directions: request + response
- ✅ Timing: Stopwatch

We're implementing exactly what was asked.

**[Backend]:** Ready to code. Just need CTO sign-off on the final brief.

---

*Standup ended: 2025-01-27*

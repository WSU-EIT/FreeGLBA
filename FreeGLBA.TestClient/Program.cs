using FreeGLBA.Client;
using Microsoft.Extensions.Configuration;

Console.WriteLine("FreeGLBA Test Client");
Console.WriteLine("====================\n");

// Load configuration from appsettings.json and user secrets
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddUserSecrets<Program>(optional: true)
    .Build();

var endpoint = configuration["FreeGLBA:Endpoint"] ?? "https://localhost:7271";
var apiKey = configuration["FreeGLBA:ApiKey"] ?? "";

if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("ERROR: API key not configured!");
    Console.WriteLine();
    Console.WriteLine("Please set the API key using user secrets:");
    Console.WriteLine("  dotnet user-secrets set \"FreeGLBA:ApiKey\" \"your-api-key-here\"");
    Console.ResetColor();
    return;
}

Console.WriteLine($"Endpoint: {endpoint}");
Console.WriteLine($"API Key:  {apiKey[..Math.Min(10, apiKey.Length)]}...\n");

using var client = new GlbaClient(endpoint, apiKey);

// Test 1: Log a single access event
Console.WriteLine("Test 1: Logging a single access event...");
try
{
    var response = await client.LogAccessAsync(new GlbaEventRequest
    {
        SourceEventId = Guid.NewGuid().ToString(),
        AccessedAt = DateTime.UtcNow,
        UserId = "pepkad",
        UserName = "Test User",
        UserEmail = "pepkad@wsu.edu",
        UserDepartment = "IT",
        SubjectId = "STU-TEST-001",
        SubjectType = "Student",
        DataCategory = "Financial",
        AccessType = "View",
        Purpose = "Testing FreeGLBA client",
        IpAddress = "127.0.0.1"
    });

    Console.WriteLine($"  Success: {response.IsSuccess}");
    Console.WriteLine($"  Event ID: {response.EventId}");
    Console.WriteLine($"  Message: {response.Message}\n");
}
catch (Exception ex)
{
    Console.WriteLine($"  Error: {ex.Message}\n");
}

// Test 2: Log an export event with multiple subjects
Console.WriteLine("Test 2: Logging a bulk export event...");
try
{
    var response = await client.LogAccessAsync(new GlbaEventRequest
    {
        SourceEventId = Guid.NewGuid().ToString(),
        AccessedAt = DateTime.UtcNow,
        UserId = "pepkad",
        UserName = "Test User",
        UserEmail = "pepkad@wsu.edu",
        SubjectId = "BULK",
        SubjectType = "Student",
        SubjectIds = ["STU-001", "STU-002", "STU-003", "STU-004", "STU-005"],
        DataCategory = "Financial",
        AccessType = "Export",
        Purpose = "Testing bulk export logging"
    });

    Console.WriteLine($"  Success: {response.IsSuccess}");
    Console.WriteLine($"  Event ID: {response.EventId}");
    Console.WriteLine($"  Message: {response.Message}\n");
}
catch (Exception ex)
{
    Console.WriteLine($"  Error: {ex.Message}\n");
}

// Test 3: Use TryLogAccessAsync (no exceptions)
Console.WriteLine("Test 3: Using TryLogAccessAsync (fire-and-forget)...");
var success = await client.TryLogAccessAsync(new GlbaEventRequest
{
    AccessedAt = DateTime.UtcNow,
    UserId = "pepkad",
    SubjectId = "STU-TEST-002",
    AccessType = "View",
    Purpose = "Quick test"
});
Console.WriteLine($"  Success: {success}\n");

Console.WriteLine("Tests complete! Check your FreeGLBA dashboard at:");
Console.WriteLine($"  {endpoint}/AccessEvents");

// Dummy class for user secrets assembly reference
public partial class Program { }

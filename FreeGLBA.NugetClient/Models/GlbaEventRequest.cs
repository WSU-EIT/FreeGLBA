using System.Text.Json.Serialization;

namespace FreeGLBA.Client;

/// <summary>
/// Represents a GLBA access event to be logged.
/// </summary>
public class GlbaEventRequest
{
    /// <summary>
    /// Optional identifier from the source system for deduplication.
    /// If provided, duplicate events with the same SourceEventId will be rejected.
    /// </summary>
    [JsonPropertyName("sourceEventId")]
    public string? SourceEventId { get; set; }

    /// <summary>
    /// The timestamp when the data access occurred.
    /// </summary>
    [JsonPropertyName("accessedAt")]
    public DateTime AccessedAt { get; set; }

    /// <summary>
    /// The identifier of the user who accessed the data.
    /// Required.
    /// </summary>
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the user who accessed the data.
    /// </summary>
    [JsonPropertyName("userName")]
    public string? UserName { get; set; }

    /// <summary>
    /// The email address of the user who accessed the data.
    /// </summary>
    [JsonPropertyName("userEmail")]
    public string? UserEmail { get; set; }

    /// <summary>
    /// The department of the user who accessed the data.
    /// </summary>
    [JsonPropertyName("userDepartment")]
    public string? UserDepartment { get; set; }

    /// <summary>
    /// The identifier of the data subject (e.g., student ID, customer ID).
    /// Required.
    /// </summary>
    [JsonPropertyName("subjectId")]
    public string SubjectId { get; set; } = string.Empty;

    /// <summary>
    /// The type of data subject (e.g., "Student", "Employee", "Customer").
    /// </summary>
    [JsonPropertyName("subjectType")]
    public string? SubjectType { get; set; }

    /// <summary>
    /// The category of data accessed (e.g., "Financial", "Academic", "Personal").
    /// </summary>
    [JsonPropertyName("dataCategory")]
    public string? DataCategory { get; set; }

    /// <summary>
    /// The type of access performed (e.g., "View", "Export", "Print", "Download").
    /// Required.
    /// </summary>
    [JsonPropertyName("accessType")]
    public string AccessType { get; set; } = string.Empty;

    /// <summary>
    /// The business purpose or justification for the data access.
    /// </summary>
    [JsonPropertyName("purpose")]
    public string? Purpose { get; set; }

    /// <summary>
    /// The IP address from which the access was made.
    /// </summary>
    [JsonPropertyName("ipAddress")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Additional JSON data to store with the event.
    /// </summary>
    [JsonPropertyName("additionalData")]
    public string? AdditionalData { get; set; }
}

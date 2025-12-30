using System.Text.Json.Serialization;

namespace FreeGLBA.Client;

/// <summary>
/// Response returned after processing a single GLBA event.
/// </summary>
public class GlbaEventResponse
{
    /// <summary>
    /// The unique identifier assigned to the event.
    /// Only populated when Status is "accepted".
    /// </summary>
    [JsonPropertyName("eventId")]
    public Guid? EventId { get; set; }

    /// <summary>
    /// The timestamp when the event was received by the server.
    /// </summary>
    [JsonPropertyName("receivedAt")]
    public DateTime ReceivedAt { get; set; }

    /// <summary>
    /// The status of the event processing.
    /// Values: "accepted", "duplicate", "error"
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Additional message providing details about the status.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Returns true if the event was accepted successfully.
    /// </summary>
    [JsonIgnore]
    public bool IsSuccess => Status == "accepted";

    /// <summary>
    /// Returns true if the event was a duplicate of an existing event.
    /// </summary>
    [JsonIgnore]
    public bool IsDuplicate => Status == "duplicate";
}

/// <summary>
/// Response returned after processing a batch of GLBA events.
/// </summary>
public class GlbaBatchResponse
{
    /// <summary>
    /// The number of events that were accepted.
    /// </summary>
    [JsonPropertyName("accepted")]
    public int Accepted { get; set; }

    /// <summary>
    /// The number of events that were rejected due to errors.
    /// </summary>
    [JsonPropertyName("rejected")]
    public int Rejected { get; set; }

    /// <summary>
    /// The number of events that were duplicates.
    /// </summary>
    [JsonPropertyName("duplicate")]
    public int Duplicate { get; set; }

    /// <summary>
    /// Details about any errors that occurred during processing.
    /// </summary>
    [JsonPropertyName("errors")]
    public List<GlbaBatchError> Errors { get; set; } = new();

    /// <summary>
    /// Returns true if all events were accepted successfully.
    /// </summary>
    [JsonIgnore]
    public bool IsSuccess => Rejected == 0 && Errors.Count == 0;

    /// <summary>
    /// The total number of events processed.
    /// </summary>
    [JsonIgnore]
    public int Total => Accepted + Rejected + Duplicate;
}

/// <summary>
/// Error details for a single event in a batch.
/// </summary>
public class GlbaBatchError
{
    /// <summary>
    /// The zero-based index of the event in the batch that caused the error.
    /// </summary>
    [JsonPropertyName("index")]
    public int Index { get; set; }

    /// <summary>
    /// The error message describing what went wrong.
    /// </summary>
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;
}

using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace FreeGLBA;

// ============================================================================
// API KEY AUTHENTICATION DATA ACCESS
// ============================================================================

/// <summary>API Key authentication interface extensions.</summary>
public partial interface IDataAccess
{
    /// <summary>Validates an API key and returns the associated source system.</summary>
    Task<DataObjects.SourceSystem?> ValidateApiKeyAsync(string apiKey);

    /// <summary>Generates a new API key for a source system.</summary>
    Task<string> GenerateApiKeyAsync(Guid sourcesystemId);

    /// <summary>Gets all source systems for the dashboard.</summary>
    Task<List<DataObjects.SourceSystem>> GetSourceSystemsAsync();
}

public partial class DataAccess
{
    #region API Key Authentication

    /// <summary>
    /// Validates an API key and returns the associated source system.
    /// Returns null if key is invalid or source is inactive.
    /// </summary>
    public async Task<DataObjects.SourceSystem?> ValidateApiKeyAsync(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            return null;

        // Hash the provided key
        var hash = HashApiKey(apiKey);

        // Look up by hash
        var record = await data.SourceSystems
            .FirstOrDefaultAsync(x => x.ApiKey == hash && x.IsActive);

        if (record == null)
            return null;

        // Return as DataObject with inline mapping
        return new DataObjects.SourceSystem
        {
            SourceSystemId = record.SourceSystemId,
            Name = record.Name,
            DisplayName = record.DisplayName,
            ApiKey = record.ApiKey,
            ContactEmail = record.ContactEmail,
            IsActive = record.IsActive,
            LastEventReceivedAt = record.LastEventReceivedAt,
            EventCount = record.EventCount,
        };
    }

    /// <summary>
    /// Generates a new API key for a source system.
    /// Returns the plaintext key (show to user ONCE, then store only the hash).
    /// </summary>
    public async Task<string> GenerateApiKeyAsync(Guid sourcesystemId)
    {
        var record = await data.SourceSystems
            .FirstOrDefaultAsync(x => x.SourceSystemId == sourcesystemId);

        if (record == null)
            throw new InvalidOperationException("SourceSystem not found");

        // Generate random 32-byte key and encode as Base64
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        var plaintextKey = Convert.ToBase64String(bytes);

        // Store only the hash
        record.ApiKey = HashApiKey(plaintextKey);
        await data.SaveChangesAsync();

        // Return plaintext key (caller shows to user ONCE)
        return plaintextKey;
    }

    /// <summary>
    /// Gets all source systems for the dashboard.
    /// </summary>
    public async Task<List<DataObjects.SourceSystem>> GetSourceSystemsAsync()
    {
        return await data.SourceSystems
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new DataObjects.SourceSystem
            {
                SourceSystemId = x.SourceSystemId,
                Name = x.Name,
                DisplayName = x.DisplayName,
                ApiKey = x.ApiKey,
                ContactEmail = x.ContactEmail,
                IsActive = x.IsActive,
                LastEventReceivedAt = x.LastEventReceivedAt,
                EventCount = x.EventCount,
            })
            .ToListAsync();
    }

    /// <summary>
    /// Hashes an API key using SHA256.
    /// </summary>
    private static string HashApiKey(string key)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(key);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    #endregion
}

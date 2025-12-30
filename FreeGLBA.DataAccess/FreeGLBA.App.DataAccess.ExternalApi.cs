using Microsoft.EntityFrameworkCore;

namespace FreeGLBA;

// ============================================================================
// GLBA EXTERNAL API DATA ACCESS
// ============================================================================

/// <summary>Glba External API interface extensions.</summary>
public partial interface IDataAccess
{
    /// <summary>Process a single event from external source.</summary>
    Task<DataObjects.GlbaEventResponse> ProcessGlbaEventAsync(DataObjects.GlbaEventRequest request, Guid sourceSystemId);

    /// <summary>Process a batch of events from external source.</summary>
    Task<DataObjects.GlbaBatchResponse> ProcessGlbaBatchAsync(List<DataObjects.GlbaEventRequest> requests, Guid sourceSystemId);

    /// <summary>Get dashboard statistics.</summary>
    Task<DataObjects.GlbaStats> GetGlbaStatsAsync();

    /// <summary>Get recent events for dashboard feed.</summary>
    Task<List<DataObjects.AccessEvent>> GetRecentAccessEventsAsync(int limit = 50);
}

public partial class DataAccess
{
    #region Glba External API

    /// <summary>Process a single event from external source.</summary>
    public async Task<DataObjects.GlbaEventResponse> ProcessGlbaEventAsync(
        DataObjects.GlbaEventRequest request, Guid sourceSystemId)
    {
        var response = new DataObjects.GlbaEventResponse
        {
            ReceivedAt = DateTime.UtcNow
        };

        // Validation
        if (string.IsNullOrWhiteSpace(request.SubjectId))
        {
            response.Status = "error";
            response.Message = "Missing required field: SubjectId";
            return response;
        }

        // Deduplication check
        if (!string.IsNullOrEmpty(request.SourceEventId))
        {
            var exists = await data.AccessEvents.AnyAsync(x =>
                x.SourceSystemId == sourceSystemId &&
                x.SourceEventId == request.SourceEventId);

            if (exists)
            {
                response.Status = "duplicate";
                response.Message = "Event with this SourceEventId already exists";
                return response;
            }
        }

        // Create event record
        var evt = new EFModels.EFModels.AccessEventItem
        {
            AccessEventId = Guid.NewGuid(),
            SourceSystemId = sourceSystemId,
            ReceivedAt = DateTime.UtcNow,
            SourceEventId = request.SourceEventId,
            AccessedAt = request.AccessedAt,
            UserId = request.UserId,
            UserName = request.UserName,
            UserEmail = request.UserEmail,
            UserDepartment = request.UserDepartment,
            SubjectId = request.SubjectId,
            SubjectType = request.SubjectType,
            DataCategory = request.DataCategory,
            AccessType = request.AccessType,
            Purpose = request.Purpose,
            IpAddress = request.IpAddress,
            AdditionalData = request.AdditionalData,
        };

        data.AccessEvents.Add(evt);
        await data.SaveChangesAsync();

        // Update LastEventReceivedAt on source system (works with all providers including InMemory)
        var sourceSystem = await data.SourceSystems.FindAsync(sourceSystemId);
        if (sourceSystem != null)
        {
            sourceSystem.LastEventReceivedAt = DateTime.UtcNow;
            await data.SaveChangesAsync();
        }

        // Update DataSubject stats
        await UpdateDataSubjectStatsAsync(request.SubjectId);

        response.EventId = evt.AccessEventId;
        response.Status = "accepted";
        return response;
    }

    /// <summary>Process a batch of events from external source.</summary>
    public async Task<DataObjects.GlbaBatchResponse> ProcessGlbaBatchAsync(
        List<DataObjects.GlbaEventRequest> requests, Guid sourceSystemId)
    {
        var response = new DataObjects.GlbaBatchResponse();

        for (int i = 0; i < requests.Count; i++)
        {
            try
            {
                var result = await ProcessGlbaEventAsync(requests[i], sourceSystemId);
                switch (result.Status)
                {
                    case "accepted": response.Accepted++; break;
                    case "duplicate": response.Duplicate++; break;
                    default:
                        response.Rejected++;
                        response.Errors.Add(new DataObjects.GlbaBatchError { Index = i, Error = result.Message ?? "Unknown error" });
                        break;
                }
            }
            catch (Exception ex)
            {
                response.Rejected++;
                response.Errors.Add(new DataObjects.GlbaBatchError { Index = i, Error = ex.Message });
            }
        }

        return response;
    }

    /// <summary>Get dashboard statistics.</summary>
    public async Task<DataObjects.GlbaStats> GetGlbaStatsAsync()
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var weekStart = now.Date.AddDays(-(int)now.DayOfWeek);
        var monthStart = new DateTime(now.Year, now.Month, 1);

        return new DataObjects.GlbaStats
        {
            Today = await data.AccessEvents.CountAsync(x => x.AccessedAt >= todayStart),
            ThisWeek = await data.AccessEvents.CountAsync(x => x.AccessedAt >= weekStart),
            ThisMonth = await data.AccessEvents.CountAsync(x => x.AccessedAt >= monthStart),
        };
    }

    /// <summary>Get recent events for dashboard feed.</summary>
    public async Task<List<DataObjects.AccessEvent>> GetRecentAccessEventsAsync(int limit = 50)
    {
        return await data.AccessEvents
            .OrderByDescending(x => x.AccessedAt)
            .Take(limit)
            .Select(x => new DataObjects.AccessEvent
            {
                AccessEventId = x.AccessEventId,
                SourceSystemId = x.SourceSystemId,
                SourceEventId = x.SourceEventId,
                AccessedAt = x.AccessedAt,
                ReceivedAt = x.ReceivedAt,
                UserId = x.UserId,
                UserName = x.UserName,
                UserEmail = x.UserEmail,
                UserDepartment = x.UserDepartment,
                SubjectId = x.SubjectId,
                SubjectType = x.SubjectType,
                DataCategory = x.DataCategory,
                AccessType = x.AccessType,
                Purpose = x.Purpose,
                IpAddress = x.IpAddress,
                AdditionalData = x.AdditionalData,
            })
            .ToListAsync();
    }

    /// <summary>Update or create DataSubject stats on event.</summary>
    private async Task UpdateDataSubjectStatsAsync(string subjectId)
    {
        var subject = await data.DataSubjects
            .FirstOrDefaultAsync(x => x.ExternalId == subjectId);

        if (subject == null)
        {
            subject = new EFModels.EFModels.DataSubjectItem
            {
                DataSubjectId = Guid.NewGuid(),
                ExternalId = subjectId,
                FirstAccessedAt = DateTime.UtcNow,
                TotalAccessCount = 0
            };
            data.DataSubjects.Add(subject);
        }

        subject.LastAccessedAt = DateTime.UtcNow;
        subject.TotalAccessCount++;
    }

    #endregion
}

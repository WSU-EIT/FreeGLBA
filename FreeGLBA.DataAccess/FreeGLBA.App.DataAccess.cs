using Microsoft.EntityFrameworkCore;

namespace FreeGLBA;

// ============================================================================
// FREEGLBA PROJECT DATA ACCESS
// ============================================================================

// SourceSystem Data Access Methods
public partial class DataAccess
{
    #region SourceSystem

    public async Task<DataObjects.SourceSystemFilterResult> GetSourceSystemsAsync(DataObjects.SourceSystemFilter filter)
    {
        var query = data.SourceSystems
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            query = query.Where(x => x.Name.Contains(filter.Search) || x.DisplayName.Contains(filter.Search) || x.ApiKey.Contains(filter.Search));
        }

        var total = await query.CountAsync();

        query = filter.SortColumn switch
        {
            "Name" => filter.SortDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            "DisplayName" => filter.SortDescending ? query.OrderByDescending(x => x.DisplayName) : query.OrderBy(x => x.DisplayName),
            "ApiKey" => filter.SortDescending ? query.OrderByDescending(x => x.ApiKey) : query.OrderBy(x => x.ApiKey),
            "ContactEmail" => filter.SortDescending ? query.OrderByDescending(x => x.ContactEmail) : query.OrderBy(x => x.ContactEmail),
            "IsActive" => filter.SortDescending ? query.OrderByDescending(x => x.IsActive) : query.OrderBy(x => x.IsActive),
            _ => query.OrderByDescending(x => x.SourceSystemId)
        };

        var items = await query.Skip(filter.Skip).Take(filter.PageSize).ToListAsync();

        return new DataObjects.SourceSystemFilterResult
        {
            Records = items.Select(x => new DataObjects.SourceSystem
            {
                SourceSystemId = x.SourceSystemId,
                Name = x.Name,
                DisplayName = x.DisplayName,
                ApiKey = x.ApiKey,
                ContactEmail = x.ContactEmail,
                IsActive = x.IsActive,
                LastEventReceivedAt = x.LastEventReceivedAt,
                EventCount = x.EventCount,
            }).ToList(),
            TotalRecords = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<DataObjects.SourceSystem?> GetSourceSystemAsync(Guid id)
    {
        var item = await data.SourceSystems
            .FirstOrDefaultAsync(x => x.SourceSystemId == id);
        if (item == null) return null;

        return new DataObjects.SourceSystem
        {
            SourceSystemId = item.SourceSystemId,
            Name = item.Name,
            DisplayName = item.DisplayName,
            ApiKey = item.ApiKey,
            ContactEmail = item.ContactEmail,
            IsActive = item.IsActive,
            LastEventReceivedAt = item.LastEventReceivedAt,
            EventCount = item.EventCount,
        };
    }

    public async Task<DataObjects.SourceSystem?> SaveSourceSystemAsync(DataObjects.SourceSystem dto)
    {
        EFModels.EFModels.SourceSystemItem item;
        var isNew = dto.SourceSystemId == default;

        if (isNew)
        {
            item = new EFModels.EFModels.SourceSystemItem();
                item.SourceSystemId = Guid.NewGuid();
            data.SourceSystems.Add(item);
        }
        else
        {
            item = await data.SourceSystems.FindAsync(dto.SourceSystemId);
            if (item == null) return null;
        }

        item.Name = dto.Name;
        item.DisplayName = dto.DisplayName;
        item.ApiKey = dto.ApiKey;
        item.ContactEmail = dto.ContactEmail;
        item.IsActive = dto.IsActive;
        item.LastEventReceivedAt = dto.LastEventReceivedAt;
        item.EventCount = dto.EventCount;

        await data.SaveChangesAsync();
        dto.SourceSystemId = item.SourceSystemId;
        return dto;
    }

    public async Task<bool> DeleteSourceSystemAsync(Guid id)
    {
        var item = await data.SourceSystems.FindAsync(id);
        if (item == null) return false;
        data.SourceSystems.Remove(item);
        await data.SaveChangesAsync();
        return true;
    }

    public async Task<List<DataObjects.SourceSystemLookup>> GetSourceSystemLookupsAsync()
    {
        return await data.SourceSystems
            .Select(x => new DataObjects.SourceSystemLookup
            {
                SourceSystemId = x.SourceSystemId,
                DisplayName = x.Name
            })
            .ToListAsync();
    }

    #endregion
}


// AccessEvent Data Access Methods
public partial class DataAccess
{
    #region AccessEvent

    public async Task<DataObjects.AccessEventFilterResult> GetAccessEventsAsync(DataObjects.AccessEventFilter filter)
    {
        var query = data.AccessEvents
            .Include(x => x.SourceSystem)
            .AsQueryable();

        if (filter.SourceSystemIdFilter != default)
            query = query.Where(x => x.SourceSystemId == filter.SourceSystemIdFilter);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            query = query.Where(x => x.SourceEventId.Contains(filter.Search) || x.UserId.Contains(filter.Search) || x.UserName.Contains(filter.Search));
        }

        var total = await query.CountAsync();

        query = filter.SortColumn switch
        {
            "SourceSystemId" => filter.SortDescending ? query.OrderByDescending(x => x.SourceSystemId) : query.OrderBy(x => x.SourceSystemId),
            "SourceEventId" => filter.SortDescending ? query.OrderByDescending(x => x.SourceEventId) : query.OrderBy(x => x.SourceEventId),
            "AccessedAt" => filter.SortDescending ? query.OrderByDescending(x => x.AccessedAt) : query.OrderBy(x => x.AccessedAt),
            "ReceivedAt" => filter.SortDescending ? query.OrderByDescending(x => x.ReceivedAt) : query.OrderBy(x => x.ReceivedAt),
            "UserId" => filter.SortDescending ? query.OrderByDescending(x => x.UserId) : query.OrderBy(x => x.UserId),
            _ => query.OrderByDescending(x => x.AccessEventId)
        };

        var items = await query.Skip(filter.Skip).Take(filter.PageSize).ToListAsync();

        return new DataObjects.AccessEventFilterResult
        {
            Records = items.Select(x => new DataObjects.AccessEvent
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
                SourceSystemName = x.SourceSystem?.Name ?? string.Empty,
            }).ToList(),
            TotalRecords = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<DataObjects.AccessEvent?> GetAccessEventAsync(Guid id)
    {
        var item = await data.AccessEvents
            .Include(x => x.SourceSystem)
            .FirstOrDefaultAsync(x => x.AccessEventId == id);
        if (item == null) return null;

        return new DataObjects.AccessEvent
        {
            AccessEventId = item.AccessEventId,
            SourceSystemId = item.SourceSystemId,
            SourceEventId = item.SourceEventId,
            AccessedAt = item.AccessedAt,
            ReceivedAt = item.ReceivedAt,
            UserId = item.UserId,
            UserName = item.UserName,
            UserEmail = item.UserEmail,
            UserDepartment = item.UserDepartment,
            SubjectId = item.SubjectId,
            SubjectType = item.SubjectType,
            DataCategory = item.DataCategory,
            AccessType = item.AccessType,
            Purpose = item.Purpose,
            IpAddress = item.IpAddress,
            AdditionalData = item.AdditionalData,
            SourceSystemName = item.SourceSystem?.Name ?? string.Empty,
        };
    }

    public async Task<DataObjects.AccessEvent?> SaveAccessEventAsync(DataObjects.AccessEvent dto)
    {
        EFModels.EFModels.AccessEventItem item;
        var isNew = dto.AccessEventId == default;

        if (isNew)
        {
            item = new EFModels.EFModels.AccessEventItem();
                item.AccessEventId = Guid.NewGuid();
            data.AccessEvents.Add(item);
        }
        else
        {
            item = await data.AccessEvents.FindAsync(dto.AccessEventId);
            if (item == null) return null;
        }

        item.SourceSystemId = dto.SourceSystemId;
        item.SourceEventId = dto.SourceEventId;
        item.AccessedAt = dto.AccessedAt;
        item.ReceivedAt = dto.ReceivedAt;
        item.UserId = dto.UserId;
        item.UserName = dto.UserName;
        item.UserEmail = dto.UserEmail;
        item.UserDepartment = dto.UserDepartment;
        item.SubjectId = dto.SubjectId;
        item.SubjectType = dto.SubjectType;
        item.DataCategory = dto.DataCategory;
        item.AccessType = dto.AccessType;
        item.Purpose = dto.Purpose;
        item.IpAddress = dto.IpAddress;
        item.AdditionalData = dto.AdditionalData;

        await data.SaveChangesAsync();
        dto.AccessEventId = item.AccessEventId;
        return dto;
    }

    public async Task<bool> DeleteAccessEventAsync(Guid id)
    {
        var item = await data.AccessEvents.FindAsync(id);
        if (item == null) return false;
        data.AccessEvents.Remove(item);
        await data.SaveChangesAsync();
        return true;
    }

    public async Task<List<DataObjects.AccessEventLookup>> GetAccessEventLookupsAsync()
    {
        return await data.AccessEvents
            .Select(x => new DataObjects.AccessEventLookup
            {
                AccessEventId = x.AccessEventId,
                DisplayName = x.UserName
            })
            .ToListAsync();
    }

    #endregion
}


// DataSubject Data Access Methods
public partial class DataAccess
{
    #region DataSubject

    public async Task<DataObjects.DataSubjectFilterResult> GetDataSubjectsAsync(DataObjects.DataSubjectFilter filter)
    {
        var query = data.DataSubjects
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            query = query.Where(x => x.ExternalId.Contains(filter.Search) || x.SubjectType.Contains(filter.Search));
        }

        var total = await query.CountAsync();

        query = filter.SortColumn switch
        {
            "ExternalId" => filter.SortDescending ? query.OrderByDescending(x => x.ExternalId) : query.OrderBy(x => x.ExternalId),
            "SubjectType" => filter.SortDescending ? query.OrderByDescending(x => x.SubjectType) : query.OrderBy(x => x.SubjectType),
            "FirstAccessedAt" => filter.SortDescending ? query.OrderByDescending(x => x.FirstAccessedAt) : query.OrderBy(x => x.FirstAccessedAt),
            "LastAccessedAt" => filter.SortDescending ? query.OrderByDescending(x => x.LastAccessedAt) : query.OrderBy(x => x.LastAccessedAt),
            "TotalAccessCount" => filter.SortDescending ? query.OrderByDescending(x => x.TotalAccessCount) : query.OrderBy(x => x.TotalAccessCount),
            _ => query.OrderByDescending(x => x.DataSubjectId)
        };

        var items = await query.Skip(filter.Skip).Take(filter.PageSize).ToListAsync();

        return new DataObjects.DataSubjectFilterResult
        {
            Records = items.Select(x => new DataObjects.DataSubject
            {
                DataSubjectId = x.DataSubjectId,
                ExternalId = x.ExternalId,
                SubjectType = x.SubjectType,
                FirstAccessedAt = x.FirstAccessedAt,
                LastAccessedAt = x.LastAccessedAt,
                TotalAccessCount = x.TotalAccessCount,
                UniqueAccessorCount = x.UniqueAccessorCount,
            }).ToList(),
            TotalRecords = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<DataObjects.DataSubject?> GetDataSubjectAsync(Guid id)
    {
        var item = await data.DataSubjects
            .FirstOrDefaultAsync(x => x.DataSubjectId == id);
        if (item == null) return null;

        return new DataObjects.DataSubject
        {
            DataSubjectId = item.DataSubjectId,
            ExternalId = item.ExternalId,
            SubjectType = item.SubjectType,
            FirstAccessedAt = item.FirstAccessedAt,
            LastAccessedAt = item.LastAccessedAt,
            TotalAccessCount = item.TotalAccessCount,
            UniqueAccessorCount = item.UniqueAccessorCount,
        };
    }

    public async Task<DataObjects.DataSubject?> SaveDataSubjectAsync(DataObjects.DataSubject dto)
    {
        EFModels.EFModels.DataSubjectItem item;
        var isNew = dto.DataSubjectId == default;

        if (isNew)
        {
            item = new EFModels.EFModels.DataSubjectItem();
                item.DataSubjectId = Guid.NewGuid();
            data.DataSubjects.Add(item);
        }
        else
        {
            item = await data.DataSubjects.FindAsync(dto.DataSubjectId);
            if (item == null) return null;
        }

        item.ExternalId = dto.ExternalId;
        item.SubjectType = dto.SubjectType;
        item.FirstAccessedAt = dto.FirstAccessedAt;
        item.LastAccessedAt = dto.LastAccessedAt;
        item.TotalAccessCount = dto.TotalAccessCount;
        item.UniqueAccessorCount = dto.UniqueAccessorCount;

        await data.SaveChangesAsync();
        dto.DataSubjectId = item.DataSubjectId;
        return dto;
    }

    public async Task<bool> DeleteDataSubjectAsync(Guid id)
    {
        var item = await data.DataSubjects.FindAsync(id);
        if (item == null) return false;
        data.DataSubjects.Remove(item);
        await data.SaveChangesAsync();
        return true;
    }

    public async Task<List<DataObjects.DataSubjectLookup>> GetDataSubjectLookupsAsync()
    {
        return await data.DataSubjects
            .Select(x => new DataObjects.DataSubjectLookup
            {
                DataSubjectId = x.DataSubjectId,
                DisplayName = x.ExternalId
            })
            .ToListAsync();
    }

    #endregion
}


// ComplianceReport Data Access Methods
public partial class DataAccess
{
    #region ComplianceReport

    public async Task<DataObjects.ComplianceReportFilterResult> GetComplianceReportsAsync(DataObjects.ComplianceReportFilter filter)
    {
        var query = data.ComplianceReports
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            query = query.Where(x => x.ReportType.Contains(filter.Search) || x.GeneratedBy.Contains(filter.Search) || x.ReportData.Contains(filter.Search));
        }

        var total = await query.CountAsync();

        query = filter.SortColumn switch
        {
            "ReportType" => filter.SortDescending ? query.OrderByDescending(x => x.ReportType) : query.OrderBy(x => x.ReportType),
            "GeneratedAt" => filter.SortDescending ? query.OrderByDescending(x => x.GeneratedAt) : query.OrderBy(x => x.GeneratedAt),
            "GeneratedBy" => filter.SortDescending ? query.OrderByDescending(x => x.GeneratedBy) : query.OrderBy(x => x.GeneratedBy),
            "PeriodStart" => filter.SortDescending ? query.OrderByDescending(x => x.PeriodStart) : query.OrderBy(x => x.PeriodStart),
            "PeriodEnd" => filter.SortDescending ? query.OrderByDescending(x => x.PeriodEnd) : query.OrderBy(x => x.PeriodEnd),
            _ => query.OrderByDescending(x => x.ComplianceReportId)
        };

        var items = await query.Skip(filter.Skip).Take(filter.PageSize).ToListAsync();

        return new DataObjects.ComplianceReportFilterResult
        {
            Records = items.Select(x => new DataObjects.ComplianceReport
            {
                ComplianceReportId = x.ComplianceReportId,
                ReportType = x.ReportType,
                GeneratedAt = x.GeneratedAt,
                GeneratedBy = x.GeneratedBy,
                PeriodStart = x.PeriodStart,
                PeriodEnd = x.PeriodEnd,
                TotalEvents = x.TotalEvents,
                UniqueUsers = x.UniqueUsers,
                UniqueSubjects = x.UniqueSubjects,
                ReportData = x.ReportData,
                FileUrl = x.FileUrl,
            }).ToList(),
            TotalRecords = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<DataObjects.ComplianceReport?> GetComplianceReportAsync(Guid id)
    {
        var item = await data.ComplianceReports
            .FirstOrDefaultAsync(x => x.ComplianceReportId == id);
        if (item == null) return null;

        return new DataObjects.ComplianceReport
        {
            ComplianceReportId = item.ComplianceReportId,
            ReportType = item.ReportType,
            GeneratedAt = item.GeneratedAt,
            GeneratedBy = item.GeneratedBy,
            PeriodStart = item.PeriodStart,
            PeriodEnd = item.PeriodEnd,
            TotalEvents = item.TotalEvents,
            UniqueUsers = item.UniqueUsers,
            UniqueSubjects = item.UniqueSubjects,
            ReportData = item.ReportData,
            FileUrl = item.FileUrl,
        };
    }

    public async Task<DataObjects.ComplianceReport?> SaveComplianceReportAsync(DataObjects.ComplianceReport dto)
    {
        EFModels.EFModels.ComplianceReportItem item;
        var isNew = dto.ComplianceReportId == default;

        if (isNew)
        {
            item = new EFModels.EFModels.ComplianceReportItem();
                item.ComplianceReportId = Guid.NewGuid();
            data.ComplianceReports.Add(item);
        }
        else
        {
            item = await data.ComplianceReports.FindAsync(dto.ComplianceReportId);
            if (item == null) return null;
        }

        item.ReportType = dto.ReportType;
        item.GeneratedAt = dto.GeneratedAt;
        item.GeneratedBy = dto.GeneratedBy;
        item.PeriodStart = dto.PeriodStart;
        item.PeriodEnd = dto.PeriodEnd;
        item.TotalEvents = dto.TotalEvents;
        item.UniqueUsers = dto.UniqueUsers;
        item.UniqueSubjects = dto.UniqueSubjects;
        item.ReportData = dto.ReportData;
        item.FileUrl = dto.FileUrl;

        await data.SaveChangesAsync();
        dto.ComplianceReportId = item.ComplianceReportId;
        return dto;
    }

    public async Task<bool> DeleteComplianceReportAsync(Guid id)
    {
        var item = await data.ComplianceReports.FindAsync(id);
        if (item == null) return false;
        data.ComplianceReports.Remove(item);
        await data.SaveChangesAsync();
        return true;
    }

    public async Task<List<DataObjects.ComplianceReportLookup>> GetComplianceReportLookupsAsync()
    {
        return await data.ComplianceReports
            .Select(x => new DataObjects.ComplianceReportLookup
            {
                ComplianceReportId = x.ComplianceReportId,
                DisplayName = x.ReportType
            })
            .ToListAsync();
    }

    #endregion
}



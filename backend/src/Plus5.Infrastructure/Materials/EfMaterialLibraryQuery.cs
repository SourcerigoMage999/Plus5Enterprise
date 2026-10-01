using Microsoft.EntityFrameworkCore;
using Plus5.Application.Materials;
using Plus5.Domain.Materials;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Materials;

public sealed class EfMaterialLibraryQuery(Plus5DbContext dbContext) : IMaterialLibraryQuery
{
    public async Task<MaterialLibraryPage> GetPageAsync(
        Guid teacherAccountId,
        MaterialLibraryCriteria criteria,
        CancellationToken cancellationToken)
    {
        Validate(teacherAccountId, criteria);

        var materials = BuildAccessibleQuery(teacherAccountId, criteria.Ownership);
        var search = criteria.Search?.Trim();
        var subject = criteria.Subject?.Trim();
        var materialTypeCode = criteria.MaterialTypeCode?.Trim();
        var normalizedTag = criteria.Tag?.Trim().ToUpperInvariant();

        if (!string.IsNullOrWhiteSpace(search))
        {
            materials = materials.Where(item =>
                item.Title.Contains(search)
                || (item.Description != null && item.Description.Contains(search))
                || (item.Subject != null && item.Subject.Contains(search))
                || dbContext.MaterialVersionTags.Any(tag =>
                    tag.MaterialVersionId == item.VersionId && tag.Name.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(subject))
        {
            materials = materials.Where(item => item.Subject == subject);
        }

        if (criteria.ProgramId.HasValue)
        {
            materials = materials.Where(item => item.ProgramId == criteria.ProgramId);
        }

        if (criteria.SchoolGradeId.HasValue)
        {
            materials = materials.Where(item => item.SchoolGradeId == criteria.SchoolGradeId);
        }

        if (!string.IsNullOrWhiteSpace(materialTypeCode))
        {
            materials = materials.Where(item => item.MaterialTypeCode == materialTypeCode);
        }

        if (!string.IsNullOrWhiteSpace(normalizedTag))
        {
            materials = materials.Where(item => dbContext.MaterialVersionTags.Any(tag =>
                tag.MaterialVersionId == item.VersionId
                && tag.NormalizedName == normalizedTag));
        }

        var totalCount = await materials.LongCountAsync(cancellationToken);
        var ordered = criteria.Sort switch
        {
            MaterialLibrarySort.Oldest => materials
                .OrderBy(item => item.AddedAtUtc)
                .ThenBy(item => item.Title)
                .ThenBy(item => item.Id),
            MaterialLibrarySort.Title => materials
                .OrderBy(item => item.Title)
                .ThenByDescending(item => item.AddedAtUtc)
                .ThenBy(item => item.Id),
            _ => materials
                .OrderByDescending(item => item.AddedAtUtc)
                .ThenBy(item => item.Title)
                .ThenBy(item => item.Id),
        };

        var skip = ((long)criteria.Page - 1) * criteria.PageSize;
        if (skip > int.MaxValue)
        {
            return new MaterialLibraryPage([], criteria.Page, criteria.PageSize, totalCount);
        }

        var pageItems = await ordered
            .Skip((int)skip)
            .Take(criteria.PageSize)
            .ToListAsync(cancellationToken);
        var versionIds = pageItems.Select(item => item.VersionId).ToList();
        var tagRows = await dbContext.MaterialVersionTags
            .AsNoTracking()
            .Where(tag => versionIds.Contains(tag.MaterialVersionId))
            .OrderBy(tag => tag.Name)
            .Select(tag => new { tag.MaterialVersionId, tag.Name })
            .ToListAsync(cancellationToken);
        var tags = tagRows
            .GroupBy(tag => tag.MaterialVersionId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<string>)group
                .Select(tag => tag.Name)
                .ToList());

        return new MaterialLibraryPage(
            pageItems.Select(item => Map(item, tags.GetValueOrDefault(item.VersionId, []))).ToList(),
            criteria.Page,
            criteria.PageSize,
            totalCount);
    }

    public async Task<MaterialLibraryOverview> GetOverviewAsync(
        Guid teacherAccountId,
        MaterialLibraryOwnership ownership,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(teacherAccountId, Guid.Empty);
        if (!Enum.IsDefined(ownership))
        {
            throw new ArgumentOutOfRangeException(nameof(ownership));
        }

        var materials = BuildAccessibleQuery(teacherAccountId, ownership);
        var subjects = await materials
            .Where(item => item.Subject != null)
            .Select(item => item.Subject!)
            .Distinct()
            .OrderBy(value => value)
            .ToListAsync(cancellationToken);
        var programs = await materials
            .Where(item => item.ProgramId.HasValue && item.ProgramName != null)
            .Select(item => new { Id = item.ProgramId!.Value, Name = item.ProgramName! })
            .Distinct()
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
        var schoolGrades = await materials
            .Where(item => item.SchoolGradeId.HasValue && item.SchoolGradeName != null)
            .Select(item => new
            {
                Id = item.SchoolGradeId!.Value,
                Name = item.SchoolGradeName!,
                item.SchoolGradeCode,
            })
            .Distinct()
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
        var materialTypes = await materials
            .Select(item => item.MaterialTypeCode)
            .Distinct()
            .OrderBy(value => value)
            .ToListAsync(cancellationToken);
        var typeCountRows = await materials
            .GroupBy(item => item.MaterialTypeCode)
            .Select(group => new { Code = group.Key, Count = group.LongCount() })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Code)
            .ToListAsync(cancellationToken);
        var typeCounts = typeCountRows
            .Select(item => new MaterialLibraryTypeCount(item.Code, item.Count))
            .ToList();
        var recent = await materials
            .OrderByDescending(item => item.AddedAtUtc)
            .ThenBy(item => item.Title)
            .Take(4)
            .Select(item => new MaterialLibraryRecentItem(item.Id, item.Title, item.AddedAtUtc))
            .ToListAsync(cancellationToken);
        var tags = await (
            from material in materials
            join tag in dbContext.MaterialVersionTags.AsNoTracking()
                on material.VersionId equals tag.MaterialVersionId
            orderby tag.Name
            select tag.Name)
            .Distinct()
            .ToListAsync(cancellationToken);

        return new MaterialLibraryOverview(
            subjects,
            programs.Select(item => new MaterialLibraryFilterOption(item.Id, item.Name, null)).ToList(),
            schoolGrades.Select(item => new MaterialLibraryFilterOption(
                item.Id,
                item.Name,
                item.SchoolGradeCode)).ToList(),
            materialTypes,
            tags,
            typeCounts,
            recent);
    }

    private IQueryable<MaterialCandidate> BuildAccessibleQuery(
        Guid teacherAccountId,
        MaterialLibraryOwnership ownership)
    {
        var current =
            from material in dbContext.Materials.AsNoTracking()
            join version in dbContext.MaterialVersions.AsNoTracking()
                on material.CurrentVersionId equals version.Id
            join file in dbContext.MaterialFiles.AsNoTracking()
                on version.Id equals file.MaterialVersionId
            join programCandidate in dbContext.Programs.AsNoTracking()
                on version.ProgramId equals (Guid?)programCandidate.Id into programs
            from program in programs.DefaultIfEmpty()
            join gradeCandidate in dbContext.SchoolGrades.AsNoTracking()
                on version.SchoolGradeId equals (Guid?)gradeCandidate.Id into grades
            from grade in grades.DefaultIfEmpty()
            join levelCandidate in dbContext.ProficiencyLevels.AsNoTracking()
                on version.ProficiencyLevelId equals (Guid?)levelCandidate.Id into levels
            from level in levels.DefaultIfEmpty()
            where material.Status == MaterialStatus.Active
                && material.ArchivedAtUtc == null
                && version.Status == MaterialVersionStatus.Active
                && file.Status == MaterialFileStatus.Clean
            select new { material, version, file, program, grade, level };

        if (ownership == MaterialLibraryOwnership.Mine)
        {
            return current
                .Where(item => item.material.OwnerTeacherId == teacherAccountId)
                .Select(item => new MaterialCandidate
                {
                    Id = item.material.Id,
                    VersionId = item.version.Id,
                    Title = item.version.Title,
                    Description = item.version.Description,
                    MaterialTypeCode = item.version.MaterialTypeCode,
                    Subject = item.version.Subject,
                    ProgramId = item.version.ProgramId,
                    ProgramName = item.program == null ? null : item.program.Name,
                    SchoolGradeId = item.version.SchoolGradeId,
                    SchoolGradeCode = item.grade == null ? null : item.grade.Code,
                    SchoolGradeName = item.grade == null ? null : item.grade.Name,
                    ProficiencyLevelId = item.version.ProficiencyLevelId,
                    ProficiencyLevelCode = item.level == null ? null : item.level.Code,
                    ProficiencyLevelName = item.level == null ? null : item.level.Name,
                    FileFormat = (MaterialLibraryFileFormat)item.file.Format,
                    AddedAtUtc = item.version.ActivatedAtUtc ?? item.version.CreatedAtUtc,
                    IsOwner = true,
                    ShareAccess = null,
                });
        }

        return
            from item in current
            join share in dbContext.MaterialShares.AsNoTracking()
                on item.material.Id equals share.MaterialId
            where share.SharedWithTeacherId == teacherAccountId
                && item.material.OwnerTeacherId != teacherAccountId
                && item.material.Visibility == MaterialVisibility.Shared
            select new MaterialCandidate
            {
                Id = item.material.Id,
                VersionId = item.version.Id,
                Title = item.version.Title,
                Description = item.version.Description,
                MaterialTypeCode = item.version.MaterialTypeCode,
                Subject = item.version.Subject,
                ProgramId = item.version.ProgramId,
                ProgramName = item.program == null ? null : item.program.Name,
                SchoolGradeId = item.version.SchoolGradeId,
                SchoolGradeCode = item.grade == null ? null : item.grade.Code,
                SchoolGradeName = item.grade == null ? null : item.grade.Name,
                ProficiencyLevelId = item.version.ProficiencyLevelId,
                ProficiencyLevelCode = item.level == null ? null : item.level.Code,
                ProficiencyLevelName = item.level == null ? null : item.level.Name,
                FileFormat = (MaterialLibraryFileFormat)item.file.Format,
                AddedAtUtc = item.version.ActivatedAtUtc ?? item.version.CreatedAtUtc,
                IsOwner = false,
                ShareAccess = (MaterialLibraryShareAccess?)share.Permission,
            };
    }

    private static MaterialLibraryItem Map(
        MaterialCandidate item,
        IReadOnlyList<string> tags) => new(
            item.Id,
            item.VersionId,
            item.Title,
            item.Description,
            item.MaterialTypeCode,
            item.Subject,
            item.ProgramId,
            item.ProgramName,
            item.SchoolGradeId,
            item.SchoolGradeCode,
            item.SchoolGradeName,
            item.ProficiencyLevelId,
            item.ProficiencyLevelCode,
            item.ProficiencyLevelName,
            item.FileFormat,
            item.AddedAtUtc,
            item.IsOwner,
            item.ShareAccess,
            tags);

    private static void Validate(Guid teacherAccountId, MaterialLibraryCriteria criteria)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(teacherAccountId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(criteria);
        ArgumentOutOfRangeException.ThrowIfLessThan(criteria.Page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(criteria.PageSize, 1);
        if (!Enum.IsDefined(criteria.Ownership))
        {
            throw new ArgumentOutOfRangeException(nameof(criteria));
        }

        if (!Enum.IsDefined(criteria.Sort))
        {
            throw new ArgumentOutOfRangeException(nameof(criteria));
        }
    }

    private sealed class MaterialCandidate
    {
        public Guid Id { get; init; }
        public Guid VersionId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string MaterialTypeCode { get; init; } = string.Empty;
        public string? Subject { get; init; }
        public Guid? ProgramId { get; init; }
        public string? ProgramName { get; init; }
        public Guid? SchoolGradeId { get; init; }
        public string? SchoolGradeCode { get; init; }
        public string? SchoolGradeName { get; init; }
        public Guid? ProficiencyLevelId { get; init; }
        public string? ProficiencyLevelCode { get; init; }
        public string? ProficiencyLevelName { get; init; }
        public MaterialLibraryFileFormat FileFormat { get; init; }
        public DateTimeOffset AddedAtUtc { get; init; }
        public bool IsOwner { get; init; }
        public MaterialLibraryShareAccess? ShareAccess { get; init; }
    }
}

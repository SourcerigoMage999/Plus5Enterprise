using Microsoft.EntityFrameworkCore;
using Plus5.Application.Groups;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Groups;

public sealed class EfGroupCreationQuery(Plus5DbContext db) : IGroupCreationQuery
{
    public async Task<GroupPage<GroupLocation>> GetLocationsAsync(Guid owner, int page, string? search, CancellationToken cancellationToken)
    {
        if (page < 1 || search?.Length > 100) throw new ArgumentOutOfRangeException(nameof(page));
        var locations = db.Locations.AsNoTracking().Where(l => l.TeacherAccountId == owner && l.ArchivedAtUtc == null);
        if (!string.IsNullOrWhiteSpace(search)) locations = locations.Where(l => l.Name.Contains(search.Trim()));
        var total = await locations.LongCountAsync(cancellationToken);
        var skip = ((long)page - 1) * 25;
        var items = skip > int.MaxValue ? [] : await locations.OrderBy(l => l.Name).ThenBy(l => l.Id).Skip((int)skip).Take(25)
            .Select(l => new GroupLocation(l.Id, l.Name)).ToListAsync(cancellationToken);
        return new(items, page, 25, total);
    }

    public async Task<GroupPage<GroupCreationCandidate>?> GetCandidatesAsync(
        Guid owner, GroupCreationCriteria criteria, CancellationToken cancellationToken)
    {
        if (criteria.Page < 1 || criteria.PageSize is < 1 or > 100 || criteria.Search?.Length > 100
            || criteria.ProgramId == Guid.Empty || criteria.SchoolGradeId == Guid.Empty)
            throw new ArgumentOutOfRangeException(nameof(criteria));

        if (!await db.Programs.AsNoTracking().AnyAsync(program => program.Id == criteria.ProgramId
                && program.TeacherAccountId == owner, cancellationToken)
            || !await db.SchoolGrades.AsNoTracking().AnyAsync(grade => grade.Id == criteria.SchoolGradeId, cancellationToken))
            return null;

        var students = db.Students.AsNoTracking().Where(student => student.TeacherAccountId == owner
            && student.ArchivedAtUtc == null
            && !db.GroupMemberships.Any(member => member.StudentId == student.Id && member.LeftAtUtc == null));
        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var search = criteria.Search.Trim();
            students = students.Where(student => (student.FirstName + " " + student.LastName).Contains(search));
        }

        var total = await students.LongCountAsync(cancellationToken);
        var skip = ((long)criteria.Page - 1) * criteria.PageSize;
        var items = skip > int.MaxValue ? [] : await students
            .OrderByDescending(student => student.SchoolGradeId == criteria.SchoolGradeId)
            .ThenByDescending(student => student.ProgramId == criteria.ProgramId)
            .ThenBy(student => student.LastName).ThenBy(student => student.FirstName).ThenBy(student => student.Id)
            .Skip((int)skip).Take(criteria.PageSize)
            .Select(student => new GroupCreationCandidate(student.Id, student.FirstName, student.LastName,
                db.SchoolGrades.Where(grade => grade.Id == student.SchoolGradeId).Select(grade => grade.Name).Single(),
                db.Programs.Where(program => program.TeacherAccountId == owner && program.Id == student.ProgramId)
                    .Select(program => program.Name).FirstOrDefault(),
                student.SchoolGradeId == criteria.SchoolGradeId && student.ProgramId == criteria.ProgramId, student.RowVersion))
            .ToListAsync(cancellationToken);
        return new(items, criteria.Page, criteria.PageSize, total);
    }
}

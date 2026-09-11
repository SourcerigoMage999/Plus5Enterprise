using Microsoft.EntityFrameworkCore;
using Plus5.Application.Groups;
using Plus5.Domain.Groups;
using Plus5.Domain.Students;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Groups;
using Plus5.Infrastructure.Persistence;
using TeachingProgram = Plus5.Domain.Teaching.Program;

namespace Plus5.Api.Tests.Groups;

public sealed class GroupCreationQueryTests
{
    [Fact]
    public async Task CandidatesAreOwnerScopedRankedPagedAndDoNotChangeMembership()
    {
        await using var db = new Plus5DbContext(new DbContextOptionsBuilder<Plus5DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var now = DateTimeOffset.UtcNow;
        var owner = Guid.NewGuid();
        var grade = new SchoolGrade(Guid.NewGuid(), "7", "Sedmi razred", 7);
        var otherGrade = new SchoolGrade(Guid.NewGuid(), "8", "Osmi razred", 8);
        var program = new TeachingProgram(Guid.NewGuid(), owner, "Matematika", now);
        var foreignProgram = new TeachingProgram(Guid.NewGuid(), Guid.NewGuid(), "Foreign", now);
        Student Student(string name, Guid? teacher = null, Guid? schoolGrade = null) =>
            new(Guid.NewGuid(), teacher ?? owner, schoolGrade ?? grade.Id, name, "Test", StudentStatus.Active, now,
                teacher.HasValue ? foreignProgram.Id : program.Id, DeliveryMode.Individual);
        var recommended = Student("Zora");
        var other = Student("Ana", schoolGrade: otherGrade.Id);
        var member = Student("Member");
        var archived = Student("Archived");
        archived.Archive(now);
        db.AddRange(grade, otherGrade, program, foreignProgram, recommended, other, member, archived,
            Student("Foreign", foreignProgram.TeacherAccountId),
            new GroupMembership(Guid.NewGuid(), owner, Guid.NewGuid(), member.Id, now));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var query = new EfGroupCreationQuery(db);
        var criteria = new GroupCreationCriteria(1, 1, program.Id, grade.Id);
        var page = await query.GetCandidatesAsync(owner, criteria, CancellationToken.None);
        Assert.NotNull(page);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(recommended.Id, Assert.Single(page.Items).Id);
        Assert.True(page.Items[0].Recommended);
        Assert.Equal(other.Id, Assert.Single((await query.GetCandidatesAsync(owner, criteria with { Page = 2 }, CancellationToken.None))!.Items).Id);
        Assert.Empty((await query.GetCandidatesAsync(owner, criteria with { Search = "%" }, CancellationToken.None))!.Items);
        Assert.Empty((await query.GetCandidatesAsync(owner, criteria with { Page = int.MaxValue, PageSize = 100 }, CancellationToken.None))!.Items);
        Assert.Null(await query.GetCandidatesAsync(owner, criteria with { ProgramId = foreignProgram.Id }, CancellationToken.None));
        Assert.Null(await query.GetCandidatesAsync(Guid.NewGuid(), criteria, CancellationToken.None));
        Assert.Null(await query.GetCandidatesAsync(owner, criteria with { SchoolGradeId = Guid.NewGuid() }, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => query.GetCandidatesAsync(owner, criteria with { PageSize = 101 }, CancellationToken.None));
        Assert.Empty(db.ChangeTracker.Entries());
        Assert.Equal(1, await db.GroupMemberships.CountAsync());
    }
}

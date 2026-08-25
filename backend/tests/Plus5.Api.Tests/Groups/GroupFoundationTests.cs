using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Plus5.Domain.Groups;
using Plus5.Domain.Students;
using Plus5.Infrastructure.Persistence;
using TeachingGroup = Plus5.Domain.Groups.Group;

namespace Plus5.Api.Tests.Groups;

public sealed class GroupFoundationTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 8, 25, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void GroupPreservesOwnershipAndNormalizesProfile()
    {
        var teacherAccountId = Guid.NewGuid();
        var programId = Guid.NewGuid();
        var schoolGradeId = Guid.NewGuid();

        var group = new TeachingGroup(
            Guid.NewGuid(),
            teacherAccountId,
            programId,
            schoolGradeId,
            "  Grammar 8A  ",
            6,
            GroupStatus.Active,
            CreatedAtUtc,
            "  Fokus na gramatici.  ");

        Assert.Equal(teacherAccountId, group.TeacherAccountId);
        Assert.Equal(programId, group.ProgramId);
        Assert.Equal(schoolGradeId, group.SchoolGradeId);
        Assert.Equal("Grammar 8A", group.Name);
        Assert.Equal("GRAMMAR 8A", group.NormalizedName);
        Assert.Equal("Fokus na gramatici.", group.Description);
        Assert.Equal(6, group.Capacity);
    }

    [Fact]
    public void GroupRejectsInvalidCapacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateGroup(capacity: 0));
    }

    [Fact]
    public void CapacityCannotBeLowerThanActiveMemberCount()
    {
        var group = CreateGroup(capacity: 6);

        Assert.Throws<InvalidOperationException>(() =>
            group.ChangeCapacity(4, 5, CreatedAtUtc.AddMinutes(1)));

        group.ChangeCapacity(5, 5, CreatedAtUtc.AddMinutes(1));

        Assert.Equal(5, group.Capacity);
    }

    [Fact]
    public void GroupCannotBeArchivedWithActiveMemberships()
    {
        var group = CreateGroup();

        Assert.Throws<InvalidOperationException>(() =>
            group.Archive(1, CreatedAtUtc.AddMinutes(1)));

        group.Archive(0, CreatedAtUtc.AddMinutes(1));

        Assert.Equal(GroupStatus.Inactive, group.Status);
        Assert.Equal(CreatedAtUtc.AddMinutes(1), group.ArchivedAtUtc);
    }

    [Fact]
    public void MembershipPreservesHistoryAndCannotEndTwice()
    {
        var membership = new GroupMembership(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            CreatedAtUtc);
        var leftAtUtc = CreatedAtUtc.AddDays(30);

        membership.End(leftAtUtc);

        Assert.False(membership.IsActive);
        Assert.Equal(leftAtUtc, membership.LeftAtUtc);
        Assert.Throws<InvalidOperationException>(() => membership.End(leftAtUtc.AddDays(1)));
    }

    [Fact]
    public void MembershipCannotEndBeforeItStarts()
    {
        var membership = new GroupMembership(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            CreatedAtUtc);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            membership.End(CreatedAtUtc.AddTicks(-1)));
    }

    [Fact]
    public void StudentGroupTransitionKeepsProgramWhenMovingToIndividual()
    {
        var initialProgramId = Guid.NewGuid();
        var groupProgramId = Guid.NewGuid();
        var student = new Student(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Ana",
            "Kovač",
            StudentStatus.Active,
            CreatedAtUtc,
            initialProgramId,
            DeliveryMode.Individual);

        student.AssignToGroupProgram(groupProgramId, CreatedAtUtc.AddMinutes(1));

        Assert.Equal(groupProgramId, student.ProgramId);
        Assert.Equal(DeliveryMode.Group, student.DeliveryMode);

        student.MoveToIndividual(CreatedAtUtc.AddMinutes(2));

        Assert.Equal(groupProgramId, student.ProgramId);
        Assert.Equal(DeliveryMode.Individual, student.DeliveryMode);
    }

    [Fact]
    public void EfModelProtectsOwnershipMembershipHistoryAndConcurrency()
    {
        using var dbContext = CreateDbContext();
        var group = dbContext.Model.FindEntityType(typeof(TeachingGroup))!;
        var membership = dbContext.Model.FindEntityType(typeof(GroupMembership))!;

        Assert.All(group.GetForeignKeys(), foreignKey =>
            Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
        Assert.Contains(group.GetIndexes(), index => index.IsUnique
            && index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(TeachingGroup.TeacherAccountId),
                nameof(TeachingGroup.NormalizedName),
            ]));

        var rowVersion = group.FindProperty(nameof(TeachingGroup.RowVersion))!;
        Assert.True(rowVersion.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, rowVersion.ValueGenerated);

        Assert.Equal(2, membership.GetForeignKeys().Count());
        Assert.All(membership.GetForeignKeys(), foreignKey =>
        {
            Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
            Assert.Equal(
                nameof(GroupMembership.TeacherAccountId),
                foreignKey.Properties[0].Name);
        });

        var activeMembership = Assert.Single(
            membership.GetIndexes(),
            index => index.IsUnique
                && index.Properties.Count == 1
                && index.Properties[0].Name == nameof(GroupMembership.StudentId));
        Assert.Equal("[LeftAtUtc] IS NULL", activeMembership.GetFilter());
    }

    private static TeachingGroup CreateGroup(int capacity = 6) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Grammar 8A",
            capacity,
            GroupStatus.Active,
            CreatedAtUtc);

    private static Plus5DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Plus5DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new Plus5DbContext(options);
    }
}

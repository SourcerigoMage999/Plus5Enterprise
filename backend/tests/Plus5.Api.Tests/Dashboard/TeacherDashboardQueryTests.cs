using Microsoft.EntityFrameworkCore;
using Plus5.Application.Dashboard;
using Plus5.Domain.Groups;
using Plus5.Domain.Identity;
using Plus5.Domain.Scheduling;
using Plus5.Domain.Students;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Dashboard;
using Plus5.Infrastructure.Persistence;
using TeachingProgram = Plus5.Domain.Teaching.Program;

namespace Plus5.Api.Tests.Dashboard;

public sealed class TeacherDashboardQueryTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 6, 15, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task DashboardIsOwnerScopedAndReturnsTodayNextSessionAndHonestStudentCounts()
    {
        var teacher = new UserAccount(
            Guid.NewGuid(), "teacher@plus5.local", "TEACHER@PLUS5.LOCAL", Now.AddDays(-30));
        var foreignTeacher = new UserAccount(
            Guid.NewGuid(), "foreign@plus5.local", "FOREIGN@PLUS5.LOCAL", Now.AddDays(-30));
        var grade = new SchoolGrade(Guid.NewGuid(), "GRADE-8", "8. razred", 8);
        var program = new TeachingProgram(
            Guid.NewGuid(), teacher.Id, "General English", Now.AddDays(-30));
        var foreignProgram = new TeachingProgram(
            Guid.NewGuid(), foreignTeacher.Id, "Foreign", Now.AddDays(-30));
        var location = new Location(
            Guid.NewGuid(), teacher.Id, "Učionica 1", Now.AddDays(-30));
        var group = new Group(
            Guid.NewGuid(), teacher.Id, program.Id, grade.Id, "Grammar 8A", 5,
            GroupStatus.Active, Now.AddDays(-30));
        var foreignGroup = new Group(
            Guid.NewGuid(), foreignTeacher.Id, foreignProgram.Id, grade.Id, "Foreign group", 5,
            GroupStatus.Active, Now.AddDays(-30));
        var groupStudent = new Student(
            Guid.NewGuid(), teacher.Id, grade.Id, "Ana", "Anić", StudentStatus.Active,
            Now.AddDays(-30), program.Id, DeliveryMode.Group);
        var individualStudent = new Student(
            Guid.NewGuid(), teacher.Id, grade.Id, "Borna", "Borić", StudentStatus.OnHold,
            Now.AddDays(-30), program.Id, DeliveryMode.Individual);
        var inactiveStudent = new Student(
            Guid.NewGuid(), teacher.Id, grade.Id, "Cvita", "Celić", StudentStatus.Inactive,
            Now.AddDays(-30));
        var archivedStudent = new Student(
            Guid.NewGuid(), teacher.Id, grade.Id, "Dora", "Dorić", StudentStatus.Active,
            Now.AddDays(-30));
        archivedStudent.Archive(Now.AddDays(-1));
        var foreignStudent = new Student(
            Guid.NewGuid(), foreignTeacher.Id, grade.Id, "Ema", "Erić", StudentStatus.Active,
            Now.AddDays(-30), foreignProgram.Id, DeliveryMode.Group);
        var membership = new GroupMembership(
            Guid.NewGuid(), teacher.Id, group.Id, groupStudent.Id, Now.AddDays(-20));

        var held = Session(
            teacher.Id, DeliveryMode.Group, group.Id, 7, "Morning revision", location.Id);
        held.Start(Now.AddDays(-1));
        held.Complete(Now.AddHours(-1));
        var next = Session(
            teacher.Id, DeliveryMode.Individual, individualStudent.Id, 12, "Vocabulary");
        var cancelled = Session(
            teacher.Id, DeliveryMode.Group, group.Id, 13, "Cancelled");
        cancelled.Cancel(Now.AddMinutes(-1));
        var foreign = Session(
            foreignTeacher.Id, DeliveryMode.Group, foreignGroup.Id, 11, "Foreign");

        await using var dbContext = CreateDbContext();
        dbContext.AddRange(
            teacher,
            foreignTeacher,
            grade,
            program,
            foreignProgram,
            location,
            group,
            foreignGroup,
            groupStudent,
            individualStudent,
            inactiveStudent,
            archivedStudent,
            foreignStudent,
            membership,
            held,
            next,
            cancelled,
            foreign);
        await dbContext.SaveChangesAsync();

        var query = new EfTeacherDashboardQuery(dbContext, new FixedTimeProvider());
        var dashboard = await query.GetAsync(teacher.Id, CancellationToken.None);

        Assert.Equal("Europe/Zagreb", dashboard.TimeZoneId);
        Assert.Equal(new DateOnly(2026, 6, 15), dashboard.Today);
        Assert.Equal(Now, dashboard.GeneratedAtUtc);
        Assert.Equal([held.Id, next.Id], dashboard.TodaySessions.Select(session => session.Id));
        Assert.Equal(next.Id, dashboard.NextSession!.Id);
        Assert.Equal("Borna Borić", dashboard.NextSession.ContextName);
        Assert.Equal("8. razred", dashboard.NextSession.SchoolGradeName);
        Assert.Equal("General English", dashboard.NextSession.ProgramName);
        Assert.Equal("Vocabulary", dashboard.NextSession.Title);
        Assert.Equal(1, dashboard.NextSession.ParticipantCount);
        Assert.Equal("Učionica 1", dashboard.TodaySessions[0].LocationName);
        Assert.Equal(1, dashboard.TodaySessions[0].ParticipantCount);
        Assert.Equal(new TeacherDashboardStudentOverview(3, 1, 1, 1), dashboard.Students);
    }

    [Fact]
    public async Task EmptyDashboardUsesLocalDateAndZeroCounts()
    {
        await using var dbContext = CreateDbContext();
        var query = new EfTeacherDashboardQuery(dbContext, new FixedTimeProvider());

        var dashboard = await query.GetAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Empty(dashboard.TodaySessions);
        Assert.Null(dashboard.NextSession);
        Assert.Equal(new TeacherDashboardStudentOverview(0, 0, 0, 0), dashboard.Students);
        Assert.Equal(new DateOnly(2026, 6, 15), dashboard.Today);
    }

    private static Session Session(
        Guid teacherId,
        DeliveryMode deliveryMode,
        Guid contextId,
        int startsAtUtcHour,
        string title,
        Guid? locationId = null) => new(
            Guid.NewGuid(),
            teacherId,
            deliveryMode,
            contextId,
            new DateTimeOffset(2026, 6, 15, startsAtUtcHour, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 15, startsAtUtcHour, 45, 0, TimeSpan.Zero),
            "Europe/Zagreb",
            Now.AddDays(-2),
            title: title,
            locationId: locationId);

    private static Plus5DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Plus5DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new Plus5DbContext(options);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }
}

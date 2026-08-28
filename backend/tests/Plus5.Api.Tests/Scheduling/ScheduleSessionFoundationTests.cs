using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Plus5.Domain.Scheduling;
using Plus5.Domain.Students;
using Plus5.Infrastructure.Persistence;
using TeachingSession = Plus5.Domain.Scheduling.Session;

namespace Plus5.Api.Tests.Scheduling;

public sealed class ScheduleSessionFoundationTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 8, 28, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RegularGroupSeriesPreservesWallClockRuleAndContext()
    {
        var groupId = Guid.NewGuid();
        var series = CreateSeries(RecurringSessionSeriesKind.RegularGroupSchedule, groupId);

        Assert.Equal(groupId, series.GroupId);
        Assert.Null(series.StudentId);
        Assert.Equal(DayOfWeek.Tuesday, series.DayOfWeek);
        Assert.Equal(new TimeOnly(16, 0), series.LocalStartTime);
        Assert.Equal("Europe/Zagreb", series.TimeZoneId);
    }

    [Fact]
    public void IndividualSeriesUsesStudentContext()
    {
        var studentId = Guid.NewGuid();
        var series = CreateSeries(RecurringSessionSeriesKind.IndividualRecurrence, studentId);

        Assert.Equal(studentId, series.StudentId);
        Assert.Null(series.GroupId);
    }

    [Fact]
    public void SeriesRejectsInvalidRangesAndUnsafeLocationCombination()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RecurringSessionSeries(
            Guid.NewGuid(), Guid.NewGuid(), RecurringSessionSeriesKind.RegularGroupSchedule,
            Guid.NewGuid(), DayOfWeek.Tuesday, new DateOnly(2026, 9, 1), new DateOnly(2026, 8, 31),
            new TimeOnly(16, 0), new TimeOnly(17, 0), "Europe/Zagreb", CreatedAtUtc));

        Assert.Throws<ArgumentException>(() => new RecurringSessionSeries(
            Guid.NewGuid(), Guid.NewGuid(), RecurringSessionSeriesKind.RegularGroupSchedule,
            Guid.NewGuid(), DayOfWeek.Tuesday, new DateOnly(2026, 9, 1), new DateOnly(2027, 6, 30),
            new TimeOnly(16, 0), new TimeOnly(17, 0), "Europe/Zagreb", CreatedAtUtc,
            Guid.NewGuid(), "https://meet.example.test/group"));
    }

    [Fact]
    public void SeriesRejectsSelfReferentialHistory()
    {
        var seriesId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => new RecurringSessionSeries(
            seriesId, Guid.NewGuid(), RecurringSessionSeriesKind.RegularGroupSchedule,
            Guid.NewGuid(), DayOfWeek.Tuesday, new DateOnly(2026, 9, 1), new DateOnly(2027, 6, 30),
            new TimeOnly(16, 0), new TimeOnly(17, 0), "Europe/Zagreb", CreatedAtUtc,
            previousSeriesId: seriesId));
    }

    [Fact]
    public void LocationIsTeacherOwnedAndArchivedWithoutLosingHistory()
    {
        var teacherId = Guid.NewGuid();
        var location = new Location(Guid.NewGuid(), teacherId, "  Ucionica 1  ", CreatedAtUtc);

        location.Archive(CreatedAtUtc.AddDays(1));

        Assert.Equal(teacherId, location.TeacherAccountId);
        Assert.Equal("Ucionica 1", location.Name);
        Assert.Equal("UCIONICA 1", location.NormalizedName);
        Assert.Equal(CreatedAtUtc.AddDays(1), location.ArchivedAtUtc);
        Assert.Throws<InvalidOperationException>(() => location.Archive(CreatedAtUtc.AddDays(2)));
    }

    [Fact]
    public void FutureSeriesChangeSupersedesInsteadOfRewritingHistory()
    {
        var series = CreateSeries(RecurringSessionSeriesKind.RegularGroupSchedule, Guid.NewGuid());
        var finalDate = new DateOnly(2026, 10, 31);

        series.Supersede(finalDate, CreatedAtUtc.AddDays(1));

        Assert.Equal(finalDate, series.EndsOn);
        Assert.Equal(CreatedAtUtc.AddDays(1), series.SupersededAtUtc);
        Assert.Throws<InvalidOperationException>(() =>
            series.Supersede(finalDate, CreatedAtUtc.AddDays(2)));
    }

    [Fact]
    public void GroupSessionUsesExactlyOneContextAndMayBeOneOff()
    {
        var groupId = Guid.NewGuid();
        var session = CreateSession(DeliveryMode.Group, groupId);

        Assert.Equal(groupId, session.GroupId);
        Assert.Null(session.StudentId);
        Assert.Null(session.RecurringSessionSeriesId);
        Assert.Equal(SessionStatus.Scheduled, session.Status);
    }

    [Fact]
    public void ReschedulingOneOccurrenceMarksSeriesException()
    {
        var seriesId = Guid.NewGuid();
        var session = CreateSession(
            DeliveryMode.Individual,
            Guid.NewGuid(),
            seriesId,
            new DateOnly(2026, 9, 1));

        session.Reschedule(
            CreatedAtUtc.AddHours(3),
            CreatedAtUtc.AddHours(4),
            CreatedAtUtc.AddMinutes(1));

        Assert.True(session.IsSeriesException);
        Assert.Equal(CreatedAtUtc.AddHours(3), session.StartsAtUtc);
    }

    [Fact]
    public void SessionLifecycleIsExplicitAndTerminal()
    {
        var session = CreateSession(DeliveryMode.Individual, Guid.NewGuid());

        session.Start(CreatedAtUtc.AddMinutes(1));
        session.Complete(CreatedAtUtc.AddMinutes(2));

        Assert.Equal(SessionStatus.Held, session.Status);
        Assert.Throws<InvalidOperationException>(() => session.Cancel(CreatedAtUtc.AddMinutes(3)));
    }

    [Fact]
    public void CancellationPreservesSessionHistory()
    {
        var session = CreateSession(DeliveryMode.Individual, Guid.NewGuid());
        var cancelledAtUtc = CreatedAtUtc.AddMinutes(1);

        session.Cancel(cancelledAtUtc);

        Assert.Equal(SessionStatus.Cancelled, session.Status);
        Assert.Equal(cancelledAtUtc, session.CancelledAtUtc);
    }

    [Fact]
    public void EfModelProtectsOwnershipOccurrenceIdentityAndConcurrency()
    {
        using var dbContext = CreateDbContext();
        var series = dbContext.Model.FindEntityType(typeof(RecurringSessionSeries))!;
        var session = dbContext.Model.FindEntityType(typeof(TeachingSession))!;

        Assert.All(series.GetForeignKeys(), foreignKey =>
            Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
        Assert.True(series.FindProperty(nameof(RecurringSessionSeries.RowVersion))!.IsConcurrencyToken);
        Assert.True(session.FindProperty(nameof(TeachingSession.RowVersion))!.IsConcurrencyToken);

        var occurrenceIndex = Assert.Single(
            session.GetIndexes(),
            index => index.IsUnique
                && index.Properties.Select(property => property.Name).SequenceEqual([
                    nameof(TeachingSession.RecurringSessionSeriesId),
                    nameof(TeachingSession.SeriesOccurrenceDate),
                ]));
        Assert.Equal("[RecurringSessionSeriesId] IS NOT NULL", occurrenceIndex.GetFilter());
        Assert.All(session.GetForeignKeys(), foreignKey =>
            Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
    }

    private static RecurringSessionSeries CreateSeries(
        RecurringSessionSeriesKind kind,
        Guid contextId) => new(
            Guid.NewGuid(), Guid.NewGuid(), kind, contextId, DayOfWeek.Tuesday,
            new DateOnly(2026, 9, 1), new DateOnly(2027, 6, 30),
            new TimeOnly(16, 0), new TimeOnly(17, 30), "Europe/Zagreb", CreatedAtUtc);

    private static TeachingSession CreateSession(
        DeliveryMode deliveryMode,
        Guid contextId,
        Guid? seriesId = null,
        DateOnly? occurrenceDate = null) => new(
            Guid.NewGuid(), Guid.NewGuid(), deliveryMode, contextId,
            CreatedAtUtc.AddHours(1), CreatedAtUtc.AddHours(2), "Europe/Zagreb", CreatedAtUtc,
            recurringSessionSeriesId: seriesId,
            seriesOccurrenceDate: occurrenceDate);

    private static Plus5DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Plus5DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new Plus5DbContext(options);
    }
}

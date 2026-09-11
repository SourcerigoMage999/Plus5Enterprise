using Plus5.Application.Groups;
using Plus5.Domain.Scheduling;

namespace Plus5.Api.Tests.Groups;

public sealed class GroupScheduleGeneratorTests
{
    [Fact]
    public void OpenEndedSeriesGeneratesTwelveWeeksAndRetainsNullEnd()
    {
        var now = new DateTimeOffset(2026, 9, 7, 0, 0, 0, TimeSpan.Zero);
        var slots = new[] { new GroupScheduleSlot(1, new(16, 0), new(17, 0)) };
        var result = GroupScheduleGenerator.Generate(slots, new(2026, 9, 7), null, now)!;
        Assert.Equal(12, result.Count);
        Assert.Equal(new DateOnly(2026, 11, 23), result[^1].Date);
        Assert.Equal(14, result[0].Start.Hour);
        Assert.Equal(15, result[^1].Start.Hour);
        var series = new RecurringSessionSeries(Guid.NewGuid(), Guid.NewGuid(), RecurringSessionSeriesKind.RegularGroupSchedule,
            Guid.NewGuid(), DayOfWeek.Monday, new(2026, 9, 7), null, new(16, 0), new(17, 0), "Europe/Zagreb", now);
        Assert.Null(series.EndsOn);
        series.Supersede(new(2026, 10, 1), now);
        Assert.Equal(new DateOnly(2026, 10, 1), series.EndsOn);
        Assert.Single(GroupScheduleGenerator.Generate(slots, new(2026, 9, 7), new(2026, 9, 7), now)!);
        Assert.Equal(12, GroupScheduleGenerator.Generate(slots, new(2028, 1, 3), null, now)!.Count);
    }

    [Theory]
    [InlineData(2026, 3, 29)]
    [InlineData(2026, 10, 25)]
    public void DstInvalidOrAmbiguousTimeIsRejected(int year, int month, int day)
    {
        var date = new DateOnly(year, month, day);
        var now = new DateTimeOffset(date.AddDays(-1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        Assert.Null(GroupScheduleGenerator.Generate([new(0, new(2, 30), new(3, 30))], date, date, now));
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Plus5.Domain.Groups;
using Plus5.Domain.Identity;
using Plus5.Domain.Scheduling;
using Plus5.Domain.Students;

namespace Plus5.Infrastructure.Persistence;

internal sealed class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("Locations");
        builder.HasKey(location => location.Id);
        builder.Property(location => location.Name).HasMaxLength(Location.NameMaxLength).IsRequired();
        builder.Property(location => location.NormalizedName).HasMaxLength(Location.NameMaxLength).IsRequired();
        builder.Property(location => location.CreatedAtUtc).HasPrecision(7).IsRequired();
        builder.Property(location => location.ArchivedAtUtc).HasPrecision(7);

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(location => location.TeacherAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasAlternateKey(location => new { location.TeacherAccountId, location.Id })
            .HasName("AK_Locations_Teacher_Id");
        builder.HasIndex(location => new { location.TeacherAccountId, location.NormalizedName })
            .IsUnique()
            .HasDatabaseName("UX_Locations_Teacher_NormalizedName");
        builder.HasIndex(location => new { location.TeacherAccountId, location.ArchivedAtUtc })
            .HasDatabaseName("IX_Locations_Teacher_Archived");
    }
}

internal sealed class RecurringSessionSeriesConfiguration
    : IEntityTypeConfiguration<RecurringSessionSeries>
{
    public void Configure(EntityTypeBuilder<RecurringSessionSeries> builder)
    {
        builder.ToTable("RecurringSessionSeries", table =>
        {
            table.HasCheckConstraint(
                "CK_RecurringSessionSeries_Context",
                "([Kind] = 1 AND [GroupId] IS NOT NULL AND [StudentId] IS NULL) OR "
                + "([Kind] = 2 AND [GroupId] IS NULL AND [StudentId] IS NOT NULL)");
            table.HasCheckConstraint("CK_RecurringSessionSeries_Kind", "[Kind] IN (1, 2)");
            table.HasCheckConstraint("CK_RecurringSessionSeries_DayOfWeek", "[DayOfWeek] BETWEEN 0 AND 6");
            table.HasCheckConstraint("CK_RecurringSessionSeries_DateRange", "[EndsOn] >= [StartsOn]");
            table.HasCheckConstraint("CK_RecurringSessionSeries_TimeRange", "[LocalEndTime] > [LocalStartTime]");
            table.HasCheckConstraint(
                "CK_RecurringSessionSeries_Location",
                "[LocationId] IS NULL OR [OnlineMeetingUrl] IS NULL");
            table.HasCheckConstraint(
                "CK_RecurringSessionSeries_Previous",
                "[PreviousSeriesId] IS NULL OR [PreviousSeriesId] <> [Id]");
        });

        builder.HasKey(series => series.Id);
        builder.Property(series => series.Kind).HasConversion<int>().IsRequired();
        builder.Property(series => series.DayOfWeek).HasConversion<int>().IsRequired();
        builder.Property(series => series.StartsOn).HasColumnType("date").IsRequired();
        builder.Property(series => series.EndsOn).HasColumnType("date").IsRequired();
        builder.Property(series => series.LocalStartTime).HasColumnType("time(0)").IsRequired();
        builder.Property(series => series.LocalEndTime).HasColumnType("time(0)").IsRequired();
        builder.Property(series => series.TimeZoneId)
            .HasMaxLength(RecurringSessionSeries.TimeZoneIdMaxLength)
            .IsRequired();
        builder.Property(series => series.OnlineMeetingUrl)
            .HasMaxLength(RecurringSessionSeries.OnlineMeetingUrlMaxLength);
        builder.Property(series => series.CreatedAtUtc).HasPrecision(7).IsRequired();
        builder.Property(series => series.SupersededAtUtc).HasPrecision(7);
        builder.Property(series => series.RowVersion).IsRowVersion();

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(series => series.TeacherAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Group>()
            .WithMany()
            .HasForeignKey(series => new { series.TeacherAccountId, series.GroupId })
            .HasPrincipalKey(group => new { group.TeacherAccountId, group.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(series => new { series.TeacherAccountId, series.StudentId })
            .HasPrincipalKey(student => new { student.TeacherAccountId, student.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(series => new { series.TeacherAccountId, series.LocationId })
            .HasPrincipalKey(location => new { location.TeacherAccountId, location.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RecurringSessionSeries>()
            .WithMany()
            .HasForeignKey(series => new { series.TeacherAccountId, series.PreviousSeriesId })
            .HasPrincipalKey(series => new { series.TeacherAccountId, series.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasAlternateKey(series => new { series.TeacherAccountId, series.Id })
            .HasName("AK_RecurringSessionSeries_Teacher_Id");
        builder.HasIndex(series => new
        {
            series.TeacherAccountId,
            series.GroupId,
            series.SupersededAtUtc,
            series.DayOfWeek,
        }).HasDatabaseName("IX_RecurringSessionSeries_Group_Active_Day");
        builder.HasIndex(series => new
        {
            series.TeacherAccountId,
            series.StudentId,
            series.SupersededAtUtc,
            series.DayOfWeek,
        }).HasDatabaseName("IX_RecurringSessionSeries_Student_Active_Day");
        builder.HasIndex(series => series.PreviousSeriesId)
            .IsUnique()
            .HasFilter("[PreviousSeriesId] IS NOT NULL")
            .HasDatabaseName("UX_RecurringSessionSeries_PreviousSeriesId");
    }
}

internal sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("Sessions", table =>
        {
            table.HasCheckConstraint(
                "CK_Sessions_Context",
                "([DeliveryMode] = 1 AND [StudentId] IS NOT NULL AND [GroupId] IS NULL) OR "
                + "([DeliveryMode] = 2 AND [StudentId] IS NULL AND [GroupId] IS NOT NULL)");
            table.HasCheckConstraint("CK_Sessions_DeliveryMode", "[DeliveryMode] IN (1, 2)");
            table.HasCheckConstraint("CK_Sessions_Status", "[Status] IN (1, 2, 3, 4)");
            table.HasCheckConstraint("CK_Sessions_TimeRange", "[EndsAtUtc] > [StartsAtUtc]");
            table.HasCheckConstraint(
                "CK_Sessions_SeriesOccurrence",
                "([RecurringSessionSeriesId] IS NULL AND [SeriesOccurrenceDate] IS NULL) OR "
                + "([RecurringSessionSeriesId] IS NOT NULL AND [SeriesOccurrenceDate] IS NOT NULL)");
            table.HasCheckConstraint(
                "CK_Sessions_Cancellation",
                "([Status] = 4 AND [CancelledAtUtc] IS NOT NULL) OR ([Status] <> 4 AND [CancelledAtUtc] IS NULL)");
            table.HasCheckConstraint("CK_Sessions_Location", "[LocationId] IS NULL OR [OnlineMeetingUrl] IS NULL");
        });

        builder.HasKey(session => session.Id);
        builder.Property(session => session.DeliveryMode).HasConversion<int>().IsRequired();
        builder.Property(session => session.Title).HasMaxLength(Session.TitleMaxLength);
        builder.Property(session => session.Notes).HasMaxLength(Session.NotesMaxLength);
        builder.Property(session => session.StartsAtUtc).HasPrecision(7).IsRequired();
        builder.Property(session => session.EndsAtUtc).HasPrecision(7).IsRequired();
        builder.Property(session => session.TimeZoneId).HasMaxLength(Session.TimeZoneIdMaxLength).IsRequired();
        builder.Property(session => session.OnlineMeetingUrl).HasMaxLength(Session.OnlineMeetingUrlMaxLength);
        builder.Property(session => session.Status).HasConversion<int>().IsRequired();
        builder.Property(session => session.CreatedAtUtc).HasPrecision(7).IsRequired();
        builder.Property(session => session.UpdatedAtUtc).HasPrecision(7).IsRequired();
        builder.Property(session => session.CancelledAtUtc).HasPrecision(7);
        builder.Property(session => session.RowVersion).IsRowVersion();

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(session => session.TeacherAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Group>()
            .WithMany()
            .HasForeignKey(session => new { session.TeacherAccountId, session.GroupId })
            .HasPrincipalKey(group => new { group.TeacherAccountId, group.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(session => new { session.TeacherAccountId, session.StudentId })
            .HasPrincipalKey(student => new { student.TeacherAccountId, student.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(session => new { session.TeacherAccountId, session.LocationId })
            .HasPrincipalKey(location => new { location.TeacherAccountId, location.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RecurringSessionSeries>()
            .WithMany()
            .HasForeignKey(session => new { session.TeacherAccountId, session.RecurringSessionSeriesId })
            .HasPrincipalKey(series => new { series.TeacherAccountId, series.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(session => new { session.TeacherAccountId, session.StartsAtUtc, session.EndsAtUtc, session.Status })
            .HasDatabaseName("IX_Sessions_Teacher_Time_Status");
        builder.HasIndex(session => new
        {
            session.TeacherAccountId,
            session.LocationId,
            session.StartsAtUtc,
            session.EndsAtUtc,
            session.Status,
        })
            .HasFilter("[LocationId] IS NOT NULL")
            .HasDatabaseName("IX_Sessions_Location_Time_Status");
        builder.HasIndex(session => new { session.TeacherAccountId, session.GroupId, session.StartsAtUtc })
            .HasFilter("[GroupId] IS NOT NULL")
            .HasDatabaseName("IX_Sessions_Group_Start");
        builder.HasIndex(session => new { session.TeacherAccountId, session.StudentId, session.StartsAtUtc })
            .HasFilter("[StudentId] IS NOT NULL")
            .HasDatabaseName("IX_Sessions_Student_Start");
        builder.HasIndex(session => new { session.RecurringSessionSeriesId, session.SeriesOccurrenceDate })
            .IsUnique()
            .HasFilter("[RecurringSessionSeriesId] IS NOT NULL")
            .HasDatabaseName("UX_Sessions_Series_Occurrence");
    }
}

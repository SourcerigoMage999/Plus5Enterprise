using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Plus5.Domain.Groups;
using Plus5.Domain.Identity;
using Plus5.Domain.LessonPlanning;
using Plus5.Domain.LessonSessions;
using Plus5.Domain.Materials;
using Plus5.Domain.Scheduling;
using Plus5.Domain.Students;

namespace Plus5.Infrastructure.Persistence;

internal sealed class LessonSessionConfiguration : IEntityTypeConfiguration<LessonSession>
{
    public void Configure(EntityTypeBuilder<LessonSession> builder)
    {
        builder.ToTable("LessonSessions", table =>
        {
            table.HasCheckConstraint("CK_LessonSessions_Participant", "([StudentId] IS NOT NULL AND [GroupId] IS NULL) OR ([StudentId] IS NULL AND [GroupId] IS NOT NULL)");
            table.HasCheckConstraint("CK_LessonSessions_Status", "[Status] IN (1, 2, 3)");
            table.HasCheckConstraint("CK_LessonSessions_Revision", "[Revision] > 0");
            table.HasCheckConstraint("CK_LessonSessions_CalendarSnapshot", "([CalendarSessionId] IS NULL AND [ScheduledStartsAtUtc] IS NULL AND [ScheduledEndsAtUtc] IS NULL AND [ScheduledTimeZoneId] IS NULL) OR ([CalendarSessionId] IS NOT NULL AND [ScheduledStartsAtUtc] IS NOT NULL AND [ScheduledEndsAtUtc] > [ScheduledStartsAtUtc] AND [ScheduledTimeZoneId] IS NOT NULL)");
            table.HasCheckConstraint("CK_LessonSessions_Lifecycle", "([Status] = 1 AND [CompletedAtUtc] IS NULL AND [VoidedAtUtc] IS NULL AND [VoidReason] IS NULL) OR ([Status] = 2 AND [CompletedAtUtc] IS NOT NULL AND [VoidedAtUtc] IS NULL AND [VoidReason] IS NULL) OR ([Status] = 3 AND [CompletedAtUtc] IS NULL AND [VoidedAtUtc] IS NOT NULL AND [VoidReason] IS NOT NULL)");
            table.HasTrigger("TR_LessonSessions_ProtectLifecycle");
        });
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.TeacherAccountId, item.Id }).HasName("AK_LessonSessions_Teacher_Id");
        builder.Property(item => item.Status).HasConversion<int>().IsRequired();
        builder.Property(item => item.ScheduledStartsAtUtc).HasPrecision(7);
        builder.Property(item => item.ScheduledEndsAtUtc).HasPrecision(7);
        builder.Property(item => item.ScheduledTimeZoneId).HasMaxLength(LessonSession.TimeZoneIdMaxLength);
        builder.Property(item => item.StartedAtUtc).HasPrecision(7).IsRequired();
        builder.Property(item => item.CompletedAtUtc).HasPrecision(7);
        builder.Property(item => item.VoidedAtUtc).HasPrecision(7);
        builder.Property(item => item.VoidReason).HasMaxLength(LessonSession.VoidReasonMaxLength);
        builder.Property(item => item.LastCommittedAtUtc).HasPrecision(7).IsRequired();
        builder.Property(item => item.RowVersion).IsRowVersion();
        builder.HasOne<UserAccount>().WithMany().HasForeignKey(item => item.TeacherAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LessonPlan>().WithMany().HasForeignKey(item => new { item.TeacherAccountId, item.LessonPlanId })
            .HasPrincipalKey(item => new { item.TeacherAccountId, item.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LessonPlanVersion>().WithMany().HasForeignKey(item => new { item.LessonPlanId, item.LessonPlanVersionId })
            .HasPrincipalKey(item => new { item.LessonPlanId, item.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Session>().WithMany().HasForeignKey(item => new { item.TeacherAccountId, item.CalendarSessionId })
            .HasPrincipalKey(item => new { item.TeacherAccountId, item.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Student>().WithMany().HasForeignKey(item => new { item.TeacherAccountId, item.StudentId })
            .HasPrincipalKey(item => new { item.TeacherAccountId, item.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Group>().WithMany().HasForeignKey(item => new { item.TeacherAccountId, item.GroupId })
            .HasPrincipalKey(item => new { item.TeacherAccountId, item.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => item.CalendarSessionId).IsUnique()
            .HasFilter("[CalendarSessionId] IS NOT NULL AND [Status] <> 3")
            .HasDatabaseName("UX_LessonSessions_Calendar_NonVoided");
        builder.HasIndex(item => new { item.TeacherAccountId, item.LessonPlanId, item.StudentId }).IsUnique()
            .HasFilter("[CalendarSessionId] IS NULL AND [Status] = 1 AND [StudentId] IS NOT NULL")
            .HasDatabaseName("UX_LessonSessions_Unscheduled_Student_Active");
        builder.HasIndex(item => new { item.TeacherAccountId, item.LessonPlanId, item.GroupId }).IsUnique()
            .HasFilter("[CalendarSessionId] IS NULL AND [Status] = 1 AND [GroupId] IS NOT NULL")
            .HasDatabaseName("UX_LessonSessions_Unscheduled_Group_Active");
        builder.HasIndex(item => new { item.TeacherAccountId, item.Status, item.LastCommittedAtUtc })
            .HasDatabaseName("IX_LessonSessions_Teacher_Status_Committed");
    }
}

internal sealed class LessonSessionActivityConfiguration : IEntityTypeConfiguration<LessonSessionActivity>
{
    public void Configure(EntityTypeBuilder<LessonSessionActivity> builder)
    {
        builder.ToTable("LessonSessionActivities", table =>
        {
            table.HasCheckConstraint("CK_LessonSessionActivities_Origin", "[Origin] IN (1, 2)");
            table.HasCheckConstraint("CK_LessonSessionActivities_Source", "([Origin] = 1 AND [SourceLessonActivityId] IS NOT NULL) OR ([Origin] = 2 AND [SourceLessonActivityId] IS NULL)");
            table.HasCheckConstraint("CK_LessonSessionActivities_Status", "[Status] BETWEEN 1 AND 6");
            table.HasCheckConstraint("CK_LessonSessionActivities_SortOrder", "[SortOrder] >= 0");
            table.HasCheckConstraint("CK_LessonSessionActivities_Duration", "[DurationMinutes] BETWEEN 1 AND 480");
            table.HasCheckConstraint("CK_LessonSessionActivities_Purpose", "[Purpose] BETWEEN 1 AND 6");
            table.HasCheckConstraint("CK_LessonSessionActivities_WorkMode", "[WorkMode] BETWEEN 1 AND 3");
            table.HasTrigger("TR_LessonSessionActivities_ProtectTerminal");
        });
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.LessonSessionId, item.Id });
        builder.Property(item => item.Origin).HasConversion<int>().IsRequired();
        builder.Property(item => item.Status).HasConversion<int>().IsRequired();
        builder.Property(item => item.Name).HasMaxLength(LessonActivity.NameMaxLength).IsRequired();
        builder.Property(item => item.Purpose).HasConversion<int>().IsRequired();
        builder.Property(item => item.WorkMode).HasConversion<int>().IsRequired();
        builder.Property(item => item.Description).HasMaxLength(LessonActivity.DescriptionMaxLength);
        builder.Property(item => item.PurposeDescription).HasMaxLength(LessonActivity.PurposeDescriptionMaxLength);
        builder.Property(item => item.TeacherInstructions).HasMaxLength(LessonActivity.InstructionsMaxLength);
        builder.Property(item => item.StudentInstructions).HasMaxLength(LessonActivity.InstructionsMaxLength);
        builder.Property(item => item.Notes).HasMaxLength(LessonActivity.NotesMaxLength);
        builder.Property(item => item.StartedAtUtc).HasPrecision(7);
        builder.Property(item => item.CompletedAtUtc).HasPrecision(7);
        builder.HasOne<LessonSession>().WithMany().HasForeignKey(item => item.LessonSessionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LessonActivity>().WithMany().HasForeignKey(item => item.SourceLessonActivityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<KnowledgeBlockVersion>().WithMany().HasForeignKey(item => item.KnowledgeBlockVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.LessonSessionId, item.SortOrder }).IsUnique()
            .HasDatabaseName("UX_LessonSessionActivities_Session_Order");
        builder.HasIndex(item => new { item.LessonSessionId, item.SourceLessonActivityId }).IsUnique()
            .HasFilter("[SourceLessonActivityId] IS NOT NULL")
            .HasDatabaseName("UX_LessonSessionActivities_Session_Source");
    }
}

internal sealed class LessonSessionActivityMaterialVersionConfiguration
    : IEntityTypeConfiguration<LessonSessionActivityMaterialVersion>
{
    public void Configure(EntityTypeBuilder<LessonSessionActivityMaterialVersion> builder)
    {
        builder.ToTable("LessonSessionActivityMaterialVersions", table =>
            table.HasTrigger("TR_LessonSessionActivityMaterialVersions_Immutable"));
        builder.HasKey(item => new { item.LessonSessionActivityId, item.MaterialVersionId });
        builder.HasOne<LessonSessionActivity>().WithMany().HasForeignKey(item => item.LessonSessionActivityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MaterialVersion>().WithMany().HasForeignKey(item => item.MaterialVersionId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class LessonSessionActivityAssessableTaskVersionConfiguration
    : IEntityTypeConfiguration<LessonSessionActivityAssessableTaskVersion>
{
    public void Configure(EntityTypeBuilder<LessonSessionActivityAssessableTaskVersion> builder)
    {
        builder.ToTable("LessonSessionActivityAssessableTaskVersions", table =>
            table.HasTrigger("TR_LessonSessionActivityAssessableTaskVersions_Immutable"));
        builder.HasKey(item => new { item.LessonSessionActivityId, item.AssessableTaskVersionId });
        builder.HasOne<LessonSessionActivityMaterialVersion>().WithMany()
            .HasForeignKey(item => new { item.LessonSessionActivityId, item.MaterialVersionId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AssessableTaskVersion>().WithMany()
            .HasForeignKey(item => new { item.MaterialVersionId, item.AssessableTaskVersionId })
            .HasPrincipalKey(item => new { item.MaterialVersionId, item.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class LessonSessionOperationConfiguration : IEntityTypeConfiguration<LessonSessionOperation>
{
    public void Configure(EntityTypeBuilder<LessonSessionOperation> builder)
    {
        builder.ToTable("LessonSessionOperations", table =>
        {
            table.HasCheckConstraint("CK_LessonSessionOperations_Type", "[OperationType] IN (1, 2)");
            table.HasCheckConstraint("CK_LessonSessionOperations_Revision", "[ExpectedRevision] >= 0 AND [AppliedRevision] > 0 AND [AppliedRevision] >= [ExpectedRevision]");
            table.HasTrigger("TR_LessonSessionOperations_Immutable");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.OperationType).HasConversion<int>().IsRequired();
        builder.Property(item => item.RequestHash).HasMaxLength(LessonSessionOperation.RequestHashMaxLength).IsFixedLength().IsRequired();
        builder.Property(item => item.CommittedAtUtc).HasPrecision(7).IsRequired();
        builder.HasOne<LessonSession>().WithMany().HasForeignKey(item => new { item.TeacherAccountId, item.LessonSessionId })
            .HasPrincipalKey(item => new { item.TeacherAccountId, item.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.TeacherAccountId, item.OperationId }).IsUnique()
            .HasDatabaseName("UX_LessonSessionOperations_Teacher_Operation");
        builder.HasIndex(item => new { item.LessonSessionId, item.OperationId }).IsUnique()
            .HasDatabaseName("UX_LessonSessionOperations_Session_Operation");
    }
}

internal sealed class LessonSessionAuditEventConfiguration : IEntityTypeConfiguration<LessonSessionAuditEvent>
{
    public void Configure(EntityTypeBuilder<LessonSessionAuditEvent> builder)
    {
        builder.ToTable("LessonSessionAuditEvents", table => table.HasTrigger("TR_LessonSessionAuditEvents_AppendOnly"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Action).HasMaxLength(LessonSessionAuditEvent.ActionMaxLength).IsRequired();
        builder.Property(item => item.OldStatus).HasConversion<int?>();
        builder.Property(item => item.NewStatus).HasConversion<int>().IsRequired();
        builder.Property(item => item.OccurredAtUtc).HasPrecision(7).IsRequired();
        builder.HasOne<LessonSession>().WithMany().HasForeignKey(item => new { item.TeacherAccountId, item.LessonSessionId })
            .HasPrincipalKey(item => new { item.TeacherAccountId, item.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.LessonSessionId, item.OccurredAtUtc, item.Id });
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Plus5.Domain.Groups;
using Plus5.Domain.Identity;
using Plus5.Domain.LessonPlanning;
using Plus5.Domain.Scheduling;
using Plus5.Domain.Students;
using Plus5.Domain.Teaching;

namespace Plus5.Infrastructure.Persistence;

internal sealed class LessonPlanConfiguration : IEntityTypeConfiguration<LessonPlan>
{
    public void Configure(EntityTypeBuilder<LessonPlan> builder)
    {
        builder.ToTable("LessonPlans", table =>
        {
            table.HasCheckConstraint("CK_LessonPlans_Participant", "([StudentId] IS NOT NULL AND [GroupId] IS NULL) OR ([StudentId] IS NULL AND [GroupId] IS NOT NULL)");
            table.HasCheckConstraint("CK_LessonPlans_LifecycleStatus", "[LifecycleStatus] IN (1, 2)");
            table.HasCheckConstraint("CK_LessonPlans_Archive", "([LifecycleStatus] = 1 AND [ArchivedAtUtc] IS NULL) OR ([LifecycleStatus] = 2 AND [ArchivedAtUtc] IS NOT NULL)");
            table.HasTrigger("TR_LessonPlans_ValidateContext");
        });
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.TeacherAccountId, item.Id }).HasName("AK_LessonPlans_Teacher_Id");
        builder.Property(item => item.LifecycleStatus).HasConversion<int>().IsRequired();
        builder.Property(item => item.CreatedAtUtc).HasPrecision(7).IsRequired();
        builder.Property(item => item.ArchivedAtUtc).HasPrecision(7);
        builder.Property(item => item.RowVersion).IsRowVersion();
        builder.HasOne<UserAccount>().WithMany().HasForeignKey(item => item.TeacherAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Student>().WithMany().HasForeignKey(item => new { item.TeacherAccountId, item.StudentId })
            .HasPrincipalKey(item => new { item.TeacherAccountId, item.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Group>().WithMany().HasForeignKey(item => new { item.TeacherAccountId, item.GroupId })
            .HasPrincipalKey(item => new { item.TeacherAccountId, item.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Session>().WithMany().HasForeignKey(item => new { item.TeacherAccountId, item.SessionId })
            .HasPrincipalKey(item => new { item.TeacherAccountId, item.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LessonPlanVersion>().WithMany()
            .HasForeignKey(item => new { item.Id, item.CurrentReadyVersionId })
            .HasPrincipalKey(item => new { item.LessonPlanId, item.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.TeacherAccountId, item.LifecycleStatus, item.CreatedAtUtc })
            .HasDatabaseName("IX_LessonPlans_Teacher_Status_Created");
    }
}

internal sealed class LessonPlanVersionConfiguration : IEntityTypeConfiguration<LessonPlanVersion>
{
    public void Configure(EntityTypeBuilder<LessonPlanVersion> builder)
    {
        builder.ToTable("LessonPlanVersions", table =>
        {
            table.HasCheckConstraint("CK_LessonPlanVersions_VersionNumber", "[VersionNumber] > 0");
            table.HasCheckConstraint("CK_LessonPlanVersions_Status", "[Status] IN (1, 2, 3)");
            table.HasTrigger("TR_LessonPlanVersions_ProtectSnapshots");
        });
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.LessonPlanId, item.Id }).HasName("AK_LessonPlanVersions_Plan_Id");
        builder.Property(item => item.Goal).HasMaxLength(LessonPlanVersion.GoalMaxLength).IsRequired();
        builder.Property(item => item.Status).HasConversion<int>().IsRequired();
        builder.Property(item => item.CreatedAtUtc).HasPrecision(7).IsRequired();
        builder.Property(item => item.RowVersion).IsRowVersion();
        builder.HasOne<LessonPlan>().WithMany().HasForeignKey(item => item.LessonPlanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.LessonPlanId, item.VersionNumber }).IsUnique()
            .HasDatabaseName("UX_LessonPlanVersions_Plan_Version");
        builder.HasIndex(item => new { item.LessonPlanId, item.Status }).IsUnique()
            .HasFilter("[Status] IN (1, 2)")
            .HasDatabaseName("UX_LessonPlanVersions_Plan_WorkingStatus");
    }
}

internal sealed class LessonPlanVersionKnowledgeComponentConfiguration
    : IEntityTypeConfiguration<LessonPlanVersionKnowledgeComponent>
{
    public void Configure(EntityTypeBuilder<LessonPlanVersionKnowledgeComponent> builder)
    {
        builder.ToTable("LessonPlanVersionKnowledgeComponents", table =>
            table.HasTrigger("TR_LessonPlanVersionKnowledgeComponents_Validate"));
        builder.HasKey(item => new { item.LessonPlanVersionId, item.KnowledgeComponentId });
        builder.HasOne<LessonPlanVersion>().WithMany().HasForeignKey(item => item.LessonPlanVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<KnowledgeComponent>().WithMany().HasForeignKey(item => item.KnowledgeComponentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => item.KnowledgeComponentId)
            .HasDatabaseName("IX_LessonPlanVersionKnowledgeComponents_ComponentId");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Plus5.Domain.Groups;
using Plus5.Domain.Identity;
using Plus5.Domain.LessonPlanning;
using Plus5.Domain.Materials;
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
            table.HasCheckConstraint("CK_LessonPlanVersions_TargetDuration", "[TargetDurationMinutes] BETWEEN 5 AND 480");
            table.HasTrigger("TR_LessonPlanVersions_ProtectSnapshots");
        });
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.LessonPlanId, item.Id }).HasName("AK_LessonPlanVersions_Plan_Id");
        builder.Property(item => item.Goal).HasMaxLength(LessonPlanVersion.GoalMaxLength).IsRequired();
        builder.Property(item => item.Status).HasConversion<int>().IsRequired();
        builder.Property(item => item.CreatedAtUtc).HasPrecision(7).IsRequired();
        builder.Property(item => item.UpdatedAtUtc).HasPrecision(7).IsRequired();
        builder.Property(item => item.RowVersion).IsRowVersion();
        builder.HasOne<LessonPlan>().WithMany().HasForeignKey(item => item.LessonPlanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.LessonPlanId, item.VersionNumber }).IsUnique()
            .HasDatabaseName("UX_LessonPlanVersions_Plan_Version");
        builder.HasIndex(item => new { item.LessonPlanId, item.Status }).IsUnique()
            .HasFilter("[Status] IN (1, 2)")
            .HasDatabaseName("UX_LessonPlanVersions_Plan_WorkingStatus");
    }
}

internal sealed class KnowledgeBlockConfiguration : IEntityTypeConfiguration<KnowledgeBlock>
{
    public void Configure(EntityTypeBuilder<KnowledgeBlock> builder)
    {
        builder.ToTable("KnowledgeBlocks", table => table.HasCheckConstraint("CK_KnowledgeBlocks_Status", "[Status] IN (1, 2)"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Code).HasMaxLength(KnowledgeBlock.CodeMaxLength).IsRequired();
        builder.Property(item => item.Status).HasConversion<int>().IsRequired();
        builder.Property(item => item.CreatedAtUtc).HasPrecision(7).IsRequired();
        builder.HasIndex(item => item.Code).IsUnique();
        builder.HasOne<KnowledgeBlockVersion>().WithMany()
            .HasForeignKey(item => new { item.Id, item.CurrentPublishedVersionId })
            .HasPrincipalKey(item => new { item.KnowledgeBlockId, item.Id })
            .OnDelete(DeleteBehavior.Restrict);

        var created = new DateTimeOffset(2026, 10, 31, 0, 0, 0, TimeSpan.Zero);
        builder.HasData(KnowledgeBlockSeed.All.Select(item => new
        {
            Id = item.RootId,
            Code = item.Code,
            Status = KnowledgeBlockStatus.Active,
            CurrentPublishedVersionId = (Guid?)null,
            CreatedAtUtc = created,
        }));
    }
}

internal sealed class KnowledgeBlockVersionConfiguration : IEntityTypeConfiguration<KnowledgeBlockVersion>
{
    public void Configure(EntityTypeBuilder<KnowledgeBlockVersion> builder)
    {
        builder.ToTable("KnowledgeBlockVersions", table =>
        {
            table.HasCheckConstraint("CK_KnowledgeBlockVersions_Status", "[Status] IN (1, 2, 3)");
            table.HasCheckConstraint("CK_KnowledgeBlockVersions_Duration", "([Status] = 1 AND [MinDurationMinutes] IS NULL AND [OptimalDurationMinutes] IS NULL AND [MaxDurationMinutes] IS NULL) OR ([Status] IN (2, 3) AND [MinDurationMinutes] BETWEEN 1 AND 480 AND [OptimalDurationMinutes] BETWEEN [MinDurationMinutes] AND 480 AND [MaxDurationMinutes] BETWEEN [OptimalDurationMinutes] AND 480)");
        });
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.KnowledgeBlockId, item.Id });
        builder.Property(item => item.DisplayName).HasMaxLength(KnowledgeBlockVersion.DisplayNameMaxLength).IsRequired();
        builder.Property(item => item.Status).HasConversion<int>().IsRequired();
        builder.Property(item => item.ParticipantApplicability).HasConversion<int>().IsRequired();
        builder.Property(item => item.HomeworkMode).HasConversion<int>().IsRequired();
        builder.Property(item => item.AssessmentCapability).HasConversion<int>().IsRequired();
        builder.Property(item => item.CreatedAtUtc).HasPrecision(7).IsRequired();
        builder.Property(item => item.PublishedAtUtc).HasPrecision(7);
        builder.HasOne<KnowledgeBlock>().WithMany().HasForeignKey(item => item.KnowledgeBlockId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.KnowledgeBlockId, item.VersionNumber }).IsUnique();
        builder.HasIndex(item => item.KnowledgeBlockId).IsUnique().HasFilter("[Status] = 2");

        var created = new DateTimeOffset(2026, 10, 31, 0, 0, 0, TimeSpan.Zero);
        builder.HasData(KnowledgeBlockSeed.All.Select((item, index) => new
        {
            Id = item.VersionId,
            KnowledgeBlockId = item.RootId,
            VersionNumber = 1,
            Status = index < 5 ? KnowledgeBlockVersionStatus.Published : KnowledgeBlockVersionStatus.Draft,
            DisplayName = item.Name,
            MinDurationMinutes = index < 5 ? item.Min : null,
            OptimalDurationMinutes = index < 5 ? item.Optimal : null,
            MaxDurationMinutes = index < 5 ? item.Max : null,
            ParticipantApplicability = KnowledgeBlockParticipantApplicability.Both,
            HomeworkMode = index == 2 ? KnowledgeBlockHomeworkMode.RequiresHomeworkContext : index == 4 ? KnowledgeBlockHomeworkMode.Optional : KnowledgeBlockHomeworkMode.NotRelevant,
            AssessmentCapability = index is 2 or 4 ? KnowledgeBlockAssessmentCapability.MayContainAssessment : KnowledgeBlockAssessmentCapability.NotAssessment,
            CreatedAtUtc = created,
            PublishedAtUtc = index < 5 ? created : (DateTimeOffset?)null,
        }));
    }
}

internal sealed class LessonActivityConfiguration : IEntityTypeConfiguration<LessonActivity>
{
    public void Configure(EntityTypeBuilder<LessonActivity> builder)
    {
        builder.ToTable("LessonActivities", table =>
        {
            table.HasCheckConstraint("CK_LessonActivities_SortOrder", "[SortOrder] >= 0");
            table.HasCheckConstraint("CK_LessonActivities_Purpose", "[Purpose] BETWEEN 1 AND 6");
            table.HasCheckConstraint("CK_LessonActivities_Duration", "[DurationMinutes] BETWEEN 1 AND 480");
            table.HasCheckConstraint("CK_LessonActivities_WorkMode", "[WorkMode] BETWEEN 1 AND 3");
            table.HasTrigger("TR_LessonActivities_ValidateDraftAndKnowledgeBlock");
        });
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.LessonPlanVersionId, item.Id });
        builder.Property(item => item.Name).HasMaxLength(LessonActivity.NameMaxLength).IsRequired();
        builder.Property(item => item.Purpose).HasConversion<int>().IsRequired();
        builder.Property(item => item.WorkMode).HasConversion<int>().IsRequired();
        builder.Property(item => item.Description).HasMaxLength(LessonActivity.DescriptionMaxLength);
        builder.Property(item => item.PurposeDescription).HasMaxLength(LessonActivity.PurposeDescriptionMaxLength);
        builder.Property(item => item.TeacherInstructions).HasMaxLength(LessonActivity.InstructionsMaxLength);
        builder.Property(item => item.StudentInstructions).HasMaxLength(LessonActivity.InstructionsMaxLength);
        builder.Property(item => item.Notes).HasMaxLength(LessonActivity.NotesMaxLength);
        builder.HasOne<LessonPlanVersion>().WithMany().HasForeignKey(item => item.LessonPlanVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<KnowledgeBlockVersion>().WithMany().HasForeignKey(item => item.KnowledgeBlockVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.LessonPlanVersionId, item.SortOrder }).IsUnique();
    }
}

internal sealed class LessonActivityAiSuggestionConfiguration
    : IEntityTypeConfiguration<LessonActivityAiSuggestion>
{
    public void Configure(EntityTypeBuilder<LessonActivityAiSuggestion> builder)
    {
        builder.ToTable("LessonActivityAiSuggestions", table =>
        {
            table.HasCheckConstraint("CK_LessonActivityAiSuggestions_Status", "[Status] BETWEEN 1 AND 6");
            table.HasCheckConstraint("CK_LessonActivityAiSuggestions_Expiry", "[ExpiresAtUtc] > [GeneratedAtUtc]");
            table.HasCheckConstraint("CK_LessonActivityAiSuggestions_ContentLifecycle",
                "([Status] = 1 AND [ContentJson] IS NOT NULL AND ISJSON([ContentJson]) = 1 AND [ResolvedAtUtc] IS NULL) OR ([Status] BETWEEN 2 AND 6 AND [ContentJson] IS NULL AND [ResolvedAtUtc] IS NOT NULL)");
            table.HasTrigger("TR_LessonActivityAiSuggestions_Validate");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.BasedOnRowVersion).HasColumnType("binary(8)").IsRequired();
        builder.Property(item => item.Status).HasConversion<int>().IsRequired();
        builder.Property(item => item.Provider).HasMaxLength(LessonActivityAiSuggestion.ProviderMaxLength).IsRequired();
        builder.Property(item => item.Model).HasMaxLength(LessonActivityAiSuggestion.ModelMaxLength).IsRequired();
        builder.Property(item => item.PromptTemplateVersion).HasMaxLength(LessonActivityAiSuggestion.TemplateVersionMaxLength).IsRequired();
        builder.Property(item => item.ContentJson).HasColumnType("nvarchar(max)");
        builder.Property(item => item.GeneratedAtUtc).HasPrecision(7).IsRequired();
        builder.Property(item => item.ExpiresAtUtc).HasPrecision(7).IsRequired();
        builder.Property(item => item.ResolvedAtUtc).HasPrecision(7);
        builder.HasOne<UserAccount>().WithMany().HasForeignKey(item => item.TeacherAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LessonPlan>().WithMany()
            .HasForeignKey(item => new { item.TeacherAccountId, item.LessonPlanId })
            .HasPrincipalKey(item => new { item.TeacherAccountId, item.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LessonPlanVersion>().WithMany()
            .HasForeignKey(item => new { item.LessonPlanId, item.LessonPlanVersionId })
            .HasPrincipalKey(item => new { item.LessonPlanId, item.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LessonActivityAiSuggestion>().WithMany()
            .HasForeignKey(item => item.ReplacesSuggestionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.TeacherAccountId, item.Status, item.GeneratedAtUtc });
        builder.HasIndex(item => new { item.LessonActivityId, item.Status });
    }
}

internal sealed class LessonActivityKnowledgeComponentConfiguration : IEntityTypeConfiguration<LessonActivityKnowledgeComponent>
{
    public void Configure(EntityTypeBuilder<LessonActivityKnowledgeComponent> builder)
    {
        builder.ToTable("LessonActivityKnowledgeComponents", table => table.HasTrigger("TR_LessonActivityKnowledgeComponents_Validate"));
        builder.HasKey(item => new { item.LessonActivityId, item.KnowledgeComponentId });
        builder.HasOne<LessonActivity>().WithMany().HasForeignKey(item => item.LessonActivityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<KnowledgeComponent>().WithMany().HasForeignKey(item => item.KnowledgeComponentId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class LessonActivityMaterialVersionConfiguration
    : IEntityTypeConfiguration<LessonActivityMaterialVersion>
{
    public void Configure(EntityTypeBuilder<LessonActivityMaterialVersion> builder)
    {
        builder.ToTable("LessonActivityMaterialVersions", table =>
            table.HasTrigger("TR_LessonActivityMaterialVersions_Validate"));
        builder.HasKey(item => new { item.LessonActivityId, item.MaterialVersionId });
        builder.HasOne<LessonActivity>().WithMany()
            .HasForeignKey(item => item.LessonActivityId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MaterialVersion>().WithMany()
            .HasForeignKey(item => item.MaterialVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => item.MaterialVersionId)
            .HasDatabaseName("IX_LessonActivityMaterialVersions_MaterialVersionId");
    }
}

internal static class KnowledgeBlockSeed
{
    internal sealed record Item(Guid RootId, Guid VersionId, string Code, string Name, int? Min = null, int? Optimal = null, int? Max = null);
    internal static readonly Item[] All =
    [
        New(1, "Small Talk", 3, 5, 10), New(2, "Aktivacija predznanja", 5, 10, 15),
        New(3, "Pregled domaće zadaće", 5, 10, 20), New(4, "Objašnjenje novog gradiva", 8, 15, 25),
        New(5, "Interaktivni zadaci", 10, 20, 30), New(6, "Mini provjera razumijevanja"),
        New(7, "Završna refleksija"), New(8, "Speaking"), New(9, "Listening"), New(10, "Reading"),
        New(11, "Writing"), New(12, "Vocabulary Practice"), New(13, "Grammar Practice"), New(14, "Revision"),
        New(15, "Domaća zadaća"), New(16, "Procjena znanja"), New(17, "Rad na pogreškama"),
        New(18, "Motivacija učenika"), New(19, "Projektni zadatak"), New(20, "Suradnički zadatak"),
        New(21, "Edukativna igra"), New(22, "Simulacija stvarne situacije"), New(23, "Izazov"),
        New(24, "Priča"), New(25, "Misija"),
    ];

    private static Item New(int number, string name, int? min = null, int? optimal = null, int? max = null) =>
        new(Guid.Parse($"95000000-0000-0000-0000-{number:000000000000}"),
            Guid.Parse($"95100000-0000-0000-0000-{number:000000000000}"), $"KB-{number:000}", name, min, optimal, max);
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

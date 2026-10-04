using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Plus5.Domain.Identity;
using Plus5.Domain.Evidence;
using Plus5.Domain.Materials;
using Plus5.Domain.Teaching;

namespace Plus5.Infrastructure.Persistence;

internal sealed class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.ToTable("Materials", table =>
        {
            table.HasCheckConstraint("CK_Materials_Status", "[Status] IN (1, 2)");
            table.HasCheckConstraint("CK_Materials_Visibility", "[Visibility] IN (1, 2)");
            table.HasCheckConstraint(
                "CK_Materials_ArchivedStatus",
                "([Status] = 1 AND [ArchivedAtUtc] IS NULL) OR ([Status] = 2 AND [ArchivedAtUtc] IS NOT NULL)");
            table.HasTrigger("TR_Materials_ValidateCurrentVersion");
        });

        builder.HasKey(material => material.Id);
        builder.HasAlternateKey(material => new
        {
            material.Id,
            material.OwnerTeacherId,
        })
            .HasName("AK_Materials_Id_OwnerTeacherId");
        builder.Property(material => material.Status).HasConversion<int>().IsRequired();
        builder.Property(material => material.Visibility).HasConversion<int>().IsRequired();
        builder.Property(material => material.CreatedAtUtc).HasPrecision(7).IsRequired();
        builder.Property(material => material.UpdatedAtUtc).HasPrecision(7).IsRequired();
        builder.Property(material => material.ArchivedAtUtc).HasPrecision(7);
        builder.Property(material => material.RowVersion).IsRowVersion();

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(material => material.OwnerTeacherId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MaterialVersion>()
            .WithMany()
            .HasForeignKey(material => new
            {
                material.Id,
                material.CurrentVersionId,
            })
            .HasPrincipalKey(version => new
            {
                version.MaterialId,
                version.Id,
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(material => new
        {
            material.OwnerTeacherId,
            material.ArchivedAtUtc,
            material.Status,
        })
            .HasDatabaseName("IX_Materials_Owner_Archived_Status");
        builder.HasIndex(material => material.CurrentVersionId)
            .HasDatabaseName("IX_Materials_CurrentVersionId");
    }
}

internal sealed class MaterialVersionConfiguration : IEntityTypeConfiguration<MaterialVersion>
{
    public void Configure(EntityTypeBuilder<MaterialVersion> builder)
    {
        builder.ToTable("MaterialVersions", table =>
        {
            table.HasCheckConstraint("CK_MaterialVersions_VersionNumber", "[VersionNumber] > 0");
            table.HasCheckConstraint("CK_MaterialVersions_Status", "[Status] IN (1, 2, 3)");
            table.HasCheckConstraint(
                "CK_MaterialVersions_LifecycleTimes",
                "([Status] = 1 AND [ActivatedAtUtc] IS NULL AND [SupersededAtUtc] IS NULL) OR "
                + "([Status] = 2 AND [ActivatedAtUtc] IS NOT NULL AND [SupersededAtUtc] IS NULL) OR "
                + "([Status] = 3 AND [ActivatedAtUtc] IS NOT NULL AND [SupersededAtUtc] IS NOT NULL AND [SupersededAtUtc] >= [ActivatedAtUtc])");
            table.HasTrigger("TR_MaterialVersions_ProtectLifecycle");
        });

        builder.HasKey(version => version.Id);
        builder.HasAlternateKey(version => new
        {
            version.MaterialId,
            version.Id,
        })
            .HasName("AK_MaterialVersions_Material_Id");
        builder.Property(version => version.Title)
            .HasMaxLength(MaterialVersion.TitleMaxLength)
            .IsRequired();
        builder.Property(version => version.Description)
            .HasMaxLength(MaterialVersion.DescriptionMaxLength);
        builder.Property(version => version.MaterialTypeCode)
            .HasMaxLength(MaterialVersion.MaterialTypeCodeMaxLength)
            .IsRequired();
        builder.Property(version => version.Subject)
            .HasMaxLength(MaterialVersion.SubjectMaxLength);
        builder.Property(version => version.LanguageCode)
            .HasMaxLength(MaterialVersion.LanguageCodeMaxLength);
        builder.Property(version => version.LearningGoal)
            .HasMaxLength(MaterialVersion.LearningGoalMaxLength);
        builder.Property(version => version.Status).HasConversion<int>().IsRequired();
        builder.Property(version => version.CreatedAtUtc).HasPrecision(7).IsRequired();
        builder.Property(version => version.ActivatedAtUtc).HasPrecision(7);
        builder.Property(version => version.SupersededAtUtc).HasPrecision(7);

        builder.HasOne<Material>()
            .WithMany()
            .HasForeignKey(version => new
            {
                version.MaterialId,
                version.CreatedByTeacherId,
            })
            .HasPrincipalKey(material => new
            {
                material.Id,
                material.OwnerTeacherId,
            })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(version => version.CreatedByTeacherId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Plus5.Domain.Teaching.Program>()
            .WithMany()
            .HasForeignKey(version => new
            {
                version.CreatedByTeacherId,
                version.ProgramId,
            })
            .HasPrincipalKey(program => new
            {
                program.TeacherAccountId,
                program.Id,
            })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchoolGrade>()
            .WithMany()
            .HasForeignKey(version => version.SchoolGradeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProficiencyLevel>()
            .WithMany()
            .HasForeignKey(version => version.ProficiencyLevelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(version => new
        {
            version.MaterialId,
            version.VersionNumber,
        })
            .IsUnique()
            .HasDatabaseName("UX_MaterialVersions_Material_VersionNumber");
        builder.HasIndex(version => version.MaterialId)
            .IsUnique()
            .HasFilter("[Status] = 2")
            .HasDatabaseName("UX_MaterialVersions_Material_Active");
        builder.HasIndex(version => version.CreatedByTeacherId)
            .HasDatabaseName("IX_MaterialVersions_CreatedByTeacherId");
        builder.HasIndex(version => new
        {
            version.CreatedByTeacherId,
            version.ProgramId,
        })
            .HasDatabaseName("IX_MaterialVersions_Teacher_ProgramId");
        builder.HasIndex(version => version.SchoolGradeId)
            .HasDatabaseName("IX_MaterialVersions_SchoolGradeId");
        builder.HasIndex(version => version.ProficiencyLevelId)
            .HasDatabaseName("IX_MaterialVersions_ProficiencyLevelId");
    }
}

internal sealed class MaterialVersionTagConfiguration
    : IEntityTypeConfiguration<MaterialVersionTag>
{
    public void Configure(EntityTypeBuilder<MaterialVersionTag> builder)
    {
        builder.ToTable("MaterialVersionTags", table =>
            table.HasTrigger("TR_MaterialVersionTags_ProtectSnapshot"));
        builder.HasKey(tag => new
        {
            tag.MaterialVersionId,
            tag.NormalizedName,
        });
        builder.Property(tag => tag.Name)
            .HasMaxLength(MaterialVersionTag.NameMaxLength)
            .IsRequired();
        builder.Property(tag => tag.NormalizedName)
            .HasMaxLength(MaterialVersionTag.NameMaxLength)
            .IsRequired();
        builder.HasOne<MaterialVersion>()
            .WithMany()
            .HasForeignKey(tag => tag.MaterialVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(tag => new
        {
            tag.NormalizedName,
            tag.MaterialVersionId,
        })
            .HasDatabaseName("IX_MaterialVersionTags_NormalizedName_VersionId");
    }
}

internal sealed class MaterialVersionCurriculumOutcomeConfiguration
    : IEntityTypeConfiguration<MaterialVersionCurriculumOutcome>
{
    public void Configure(EntityTypeBuilder<MaterialVersionCurriculumOutcome> builder)
    {
        builder.ToTable("MaterialVersionCurriculumOutcomes", table =>
            table.HasTrigger("TR_MaterialVersionCurriculumOutcomes_ProtectSnapshot"));
        builder.HasKey(mapping => new
        {
            mapping.MaterialVersionId,
            mapping.CurriculumOutcomeId,
        });
        builder.HasOne<MaterialVersion>()
            .WithMany()
            .HasForeignKey(mapping => mapping.MaterialVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CurriculumOutcome>()
            .WithMany()
            .HasForeignKey(mapping => mapping.CurriculumOutcomeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(mapping => mapping.CurriculumOutcomeId)
            .HasDatabaseName("IX_MaterialVersionCurriculumOutcomes_OutcomeId");
    }
}

internal sealed class MaterialVersionKnowledgeComponentConfiguration
    : IEntityTypeConfiguration<MaterialVersionKnowledgeComponent>
{
    public void Configure(EntityTypeBuilder<MaterialVersionKnowledgeComponent> builder)
    {
        builder.ToTable("MaterialVersionKnowledgeComponents", table =>
            table.HasTrigger("TR_MaterialVersionKnowledgeComponents_ProtectSnapshot"));
        builder.HasKey(mapping => new
        {
            mapping.MaterialVersionId,
            mapping.KnowledgeComponentId,
        });
        builder.HasOne<MaterialVersion>()
            .WithMany()
            .HasForeignKey(mapping => mapping.MaterialVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<KnowledgeComponent>()
            .WithMany()
            .HasForeignKey(mapping => mapping.KnowledgeComponentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(mapping => mapping.KnowledgeComponentId)
            .HasDatabaseName("IX_MaterialVersionKnowledgeComponents_ComponentId");
    }
}

internal sealed class MaterialFileConfiguration : IEntityTypeConfiguration<MaterialFile>
{
    public void Configure(EntityTypeBuilder<MaterialFile> builder)
    {
        builder.ToTable("MaterialFiles", table =>
        {
            table.HasCheckConstraint("CK_MaterialFiles_Format", "[Format] IN (1, 2, 3, 4, 5)");
            table.HasCheckConstraint("CK_MaterialFiles_Status", "[Status] IN (1, 2, 3, 4, 5, 6, 7)");
            table.HasCheckConstraint("CK_MaterialFiles_DeclaredSize", "[DeclaredSizeBytes] > 0");
            table.HasCheckConstraint(
                "CK_MaterialFiles_FormatSize",
                "([Format] = 1 AND [DeclaredSizeBytes] <= 52428800) OR "
                + "([Format] = 2 AND [DeclaredSizeBytes] <= 26214400) OR "
                + "([Format] = 3 AND [DeclaredSizeBytes] <= 104857600) OR "
                + "([Format] = 4 AND [DeclaredSizeBytes] <= 262144000) OR "
                + "([Format] = 5 AND [DeclaredSizeBytes] <= 104857600)");
            table.HasCheckConstraint(
                "CK_MaterialFiles_ActualSize",
                "[ActualSizeBytes] IS NULL OR [ActualSizeBytes] = [DeclaredSizeBytes]");
            table.HasCheckConstraint(
                "CK_MaterialFiles_ScanAttempts",
                $"[ScanAttemptCount] >= 0 AND [ScanAttemptCount] <= {MaterialFilePolicy.MaximumScanAttempts}");
            table.HasCheckConstraint(
                "CK_MaterialFiles_UploadedChecksum",
                "([Status] = 1 AND [ActualSizeBytes] IS NULL AND [Sha256Checksum] IS NULL AND [UploadedAtUtc] IS NULL) OR "
                + "([Status] = 6 AND (([ActualSizeBytes] IS NULL AND [Sha256Checksum] IS NULL AND [UploadedAtUtc] IS NULL) OR "
                + "([ActualSizeBytes] IS NOT NULL AND [Sha256Checksum] IS NOT NULL AND [UploadedAtUtc] IS NOT NULL))) OR "
                + "([Status] NOT IN (1, 6) AND [ActualSizeBytes] IS NOT NULL AND [Sha256Checksum] IS NOT NULL AND [UploadedAtUtc] IS NOT NULL)");
            table.HasTrigger("TR_MaterialFiles_ProtectLifecycle");
        });

        builder.HasKey(file => file.Id);
        builder.Property(file => file.Format).HasConversion<int>().IsRequired();
        builder.Property(file => file.OriginalFileName)
            .HasMaxLength(MaterialFilePolicy.OriginalFileNameMaxLength)
            .IsRequired();
        builder.Property(file => file.DeclaredMediaType)
            .HasMaxLength(MaterialFilePolicy.MediaTypeMaxLength)
            .IsRequired();
        builder.Property(file => file.StorageProvider)
            .HasMaxLength(MaterialFilePolicy.StorageProviderMaxLength)
            .IsRequired();
        builder.Property(file => file.StorageContainer)
            .HasMaxLength(MaterialFilePolicy.StorageContainerMaxLength)
            .IsRequired();
        builder.Property(file => file.ObjectKey)
            .HasMaxLength(MaterialFilePolicy.ObjectKeyMaxLength)
            .IsRequired();
        builder.Property(file => file.Sha256Checksum)
            .HasMaxLength(MaterialFilePolicy.ChecksumMaxLength);
        builder.Property(file => file.Status).HasConversion<int>().IsRequired();
        builder.Property(file => file.LastScanResultCategory)
            .HasMaxLength(MaterialFilePolicy.ScanResultCategoryMaxLength);
        builder.Property(file => file.CreatedAtUtc).HasPrecision(7).IsRequired();
        builder.Property(file => file.UpdatedAtUtc).HasPrecision(7).IsRequired();
        builder.Property(file => file.UploadedAtUtc).HasPrecision(7);
        builder.Property(file => file.ScannedAtUtc).HasPrecision(7);

        builder.HasOne<MaterialVersion>()
            .WithOne()
            .HasForeignKey<MaterialFile>(file => file.MaterialVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(file => file.ObjectKey)
            .IsUnique()
            .HasDatabaseName("UX_MaterialFiles_ObjectKey");
        builder.HasIndex(file => new
        {
            file.Status,
            file.UpdatedAtUtc,
        })
            .HasDatabaseName("IX_MaterialFiles_Status_UpdatedAtUtc");
        builder.HasIndex(file => file.Sha256Checksum)
            .HasDatabaseName("IX_MaterialFiles_Sha256Checksum");
    }
}

internal sealed class MaterialShareConfiguration : IEntityTypeConfiguration<MaterialShare>
{
    public void Configure(EntityTypeBuilder<MaterialShare> builder)
    {
        builder.ToTable("MaterialShares", table =>
        {
            table.HasCheckConstraint("CK_MaterialShares_Permission", "[Permission] IN (1, 2)");
            table.HasTrigger("TR_MaterialShares_RejectOwnerSelfShare");
        });

        builder.HasKey(share => new
        {
            share.MaterialId,
            share.SharedWithTeacherId,
        });
        builder.Property(share => share.Permission).HasConversion<int>().IsRequired();
        builder.Property(share => share.CreatedAtUtc).HasPrecision(7).IsRequired();
        builder.Property(share => share.UpdatedAtUtc).HasPrecision(7).IsRequired();
        builder.HasOne<Material>()
            .WithMany()
            .HasForeignKey(share => share.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(share => share.SharedWithTeacherId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(share => new
        {
            share.SharedWithTeacherId,
            share.Permission,
            share.MaterialId,
        })
            .HasDatabaseName("IX_MaterialShares_Recipient_Permission_Material");
    }
}

internal sealed class AssessableTaskConfiguration : IEntityTypeConfiguration<AssessableTask>
{
    public void Configure(EntityTypeBuilder<AssessableTask> builder)
    {
        builder.ToTable("AssessableTasks");
        builder.HasKey(task => task.Id);
        builder.HasAlternateKey(task => new
        {
            task.MaterialId,
            task.Id,
        })
            .HasName("AK_AssessableTasks_Material_Id");
        builder.Property(task => task.CreatedAtUtc).HasPrecision(7).IsRequired();
        builder.HasOne<Material>()
            .WithMany()
            .HasForeignKey(task => task.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(task => task.MaterialId)
            .HasDatabaseName("IX_AssessableTasks_MaterialId");
    }
}

internal sealed class AssessableTaskVersionConfiguration
    : IEntityTypeConfiguration<AssessableTaskVersion>
{
    public void Configure(EntityTypeBuilder<AssessableTaskVersion> builder)
    {
        builder.ToTable("AssessableTaskVersions", table =>
        {
            table.HasCheckConstraint(
                "CK_AssessableTaskVersions_VersionNumber",
                "[VersionNumber] > 0");
            table.HasCheckConstraint(
                "CK_AssessableTaskVersions_SortOrder",
                "[SortOrder] >= 0");
            table.HasCheckConstraint(
                "CK_AssessableTaskVersions_Difficulty",
                $"[Difficulty] >= {EvidenceMetadata.MinimumDifficulty} AND [Difficulty] <= {EvidenceMetadata.MaximumDifficulty}");
            table.HasCheckConstraint(
                "CK_AssessableTaskVersions_EvidenceType",
                "[EvidenceType] IN (1, 2, 3, 4)");
            table.HasCheckConstraint(
                "CK_AssessableTaskVersions_MaxPoints",
                "[MaxPoints] > 0");
            table.HasCheckConstraint(
                "CK_AssessableTaskVersions_Evaluation",
                "[CorrectAnswer] IS NOT NULL OR [EvaluationCriterion] IS NOT NULL");
            table.HasTrigger("TR_AssessableTaskVersions_ProtectSnapshot");
        });

        builder.HasKey(version => version.Id);
        builder.HasAlternateKey(version => new
        {
            version.MaterialVersionId,
            version.Id,
        })
            .HasName("AK_AssessableTaskVersions_MaterialVersion_Id");
        builder.Property(version => version.Prompt)
            .HasMaxLength(AssessableTaskVersion.PromptMaxLength)
            .IsRequired();
        builder.Property(version => version.TaskTypeCode)
            .HasMaxLength(AssessableTaskVersion.TaskTypeCodeMaxLength)
            .IsRequired();
        builder.Property(version => version.EvidenceType).HasConversion<int>().IsRequired();
        builder.Property(version => version.CorrectAnswer)
            .HasMaxLength(AssessableTaskVersion.AnswerMaxLength);
        builder.Property(version => version.EvaluationCriterion)
            .HasMaxLength(AssessableTaskVersion.EvaluationCriterionMaxLength);
        builder.Property(version => version.MaxPoints)
            .HasPrecision(18, 4)
            .IsRequired();
        builder.Property(version => version.CreatedAtUtc).HasPrecision(7).IsRequired();

        builder.HasOne<AssessableTask>()
            .WithMany()
            .HasForeignKey(version => new
            {
                version.MaterialId,
                version.AssessableTaskId,
            })
            .HasPrincipalKey(task => new
            {
                task.MaterialId,
                task.Id,
            })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MaterialVersion>()
            .WithMany()
            .HasForeignKey(version => new
            {
                version.MaterialId,
                version.MaterialVersionId,
            })
            .HasPrincipalKey(version => new
            {
                version.MaterialId,
                version.Id,
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(version => new
        {
            version.AssessableTaskId,
            version.VersionNumber,
        })
            .IsUnique()
            .HasDatabaseName("UX_AssessableTaskVersions_Task_VersionNumber");
        builder.HasIndex(version => new
        {
            version.AssessableTaskId,
            version.MaterialVersionId,
        })
            .IsUnique()
            .HasDatabaseName("UX_AssessableTaskVersions_Task_MaterialVersion");
        builder.HasIndex(version => new
        {
            version.MaterialVersionId,
            version.SortOrder,
            version.Id,
        })
            .HasDatabaseName("IX_AssessableTaskVersions_MaterialVersion_Order");
    }
}

internal sealed class AssessableTaskVersionKnowledgeComponentConfiguration
    : IEntityTypeConfiguration<AssessableTaskVersionKnowledgeComponent>
{
    public void Configure(
        EntityTypeBuilder<AssessableTaskVersionKnowledgeComponent> builder)
    {
        builder.ToTable("AssessableTaskVersionKnowledgeComponents", table =>
            table.HasTrigger("TR_AssessableTaskVersionKnowledgeComponents_ProtectSnapshot"));
        builder.HasKey(mapping => new
        {
            mapping.AssessableTaskVersionId,
            mapping.KnowledgeComponentId,
        });
        builder.HasOne<AssessableTaskVersion>()
            .WithMany()
            .HasForeignKey(mapping => mapping.AssessableTaskVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<KnowledgeComponent>()
            .WithMany()
            .HasForeignKey(mapping => mapping.KnowledgeComponentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(mapping => mapping.KnowledgeComponentId)
            .HasDatabaseName("IX_AssessableTaskVersionKnowledgeComponents_ComponentId");
    }
}

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

internal sealed class PresentationDocumentConfiguration
    : IEntityTypeConfiguration<PresentationDocument>
{
    public void Configure(EntityTypeBuilder<PresentationDocument> builder)
    {
        builder.ToTable("PresentationDocuments", table =>
        {
            table.HasCheckConstraint(
                "CK_PresentationDocuments_SchemaVersion",
                "[DocumentSchemaVersion] > 0");
            table.HasCheckConstraint(
                "CK_PresentationDocuments_Canvas",
                $"[CanvasWidth] = {PresentationDocument.CanvasWidthValue} AND [CanvasHeight] = {PresentationDocument.CanvasHeightValue}");
            table.HasTrigger("TR_PresentationDocuments_ProtectSnapshot");
        });

        builder.HasKey(document => document.MaterialVersionId);
        builder.Property(document => document.DocumentSchemaVersion).IsRequired();
        builder.Property(document => document.CanvasWidth).IsRequired();
        builder.Property(document => document.CanvasHeight).IsRequired();
        builder.Property(document => document.CreatedAtUtc).HasPrecision(7).IsRequired();
        builder.Property(document => document.UpdatedAtUtc).HasPrecision(7).IsRequired();
        builder.Property(document => document.RowVersion).IsRowVersion();

        builder.HasOne<MaterialVersion>()
            .WithOne()
            .HasForeignKey<PresentationDocument>(document => document.MaterialVersionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PresentationSlideConfiguration
    : IEntityTypeConfiguration<PresentationSlide>
{
    public void Configure(EntityTypeBuilder<PresentationSlide> builder)
    {
        builder.ToTable("PresentationSlides", table =>
        {
            table.HasCheckConstraint(
                "CK_PresentationSlides_SortOrder",
                "[SortOrder] >= 0");
            table.HasTrigger("TR_PresentationSlides_ProtectSnapshot");
        });

        builder.HasKey(slide => slide.Id);
        builder.HasAlternateKey(slide => new
        {
            slide.MaterialVersionId,
            slide.Id,
        })
            .HasName("AK_PresentationSlides_MaterialVersion_Id");
        builder.Property(slide => slide.SortOrder).IsRequired();
        builder.HasOne<PresentationDocument>()
            .WithMany()
            .HasForeignKey(slide => slide.MaterialVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(slide => new
        {
            slide.MaterialVersionId,
            slide.SortOrder,
        })
            .IsUnique()
            .HasDatabaseName("UX_PresentationSlides_Document_SortOrder");
    }
}

internal sealed class PresentationAssetConfiguration
    : IEntityTypeConfiguration<PresentationAsset>
{
    public void Configure(EntityTypeBuilder<PresentationAsset> builder)
    {
        builder.ToTable("PresentationAssets", table =>
        {
            table.HasCheckConstraint(
                "CK_PresentationAssets_Status",
                $"[Status] = {(int)PresentationAssetStatus.Clean}");
            table.HasCheckConstraint(
                "CK_PresentationAssets_MediaType",
                "[MediaType] IN ('image/png', 'image/jpeg', 'image/webp')");
            table.HasCheckConstraint(
                "CK_PresentationAssets_Size",
                $"[SizeBytes] > 0 AND [SizeBytes] <= {PresentationAsset.MaximumImageSizeBytes}");
            table.HasTrigger("TR_PresentationAssets_ProtectSnapshot");
        });

        builder.HasKey(asset => asset.Id);
        builder.HasAlternateKey(asset => new
        {
            asset.MaterialVersionId,
            asset.Id,
        })
            .HasName("AK_PresentationAssets_MaterialVersion_Id");
        builder.Property(asset => asset.OriginalFileName)
            .HasMaxLength(MaterialFilePolicy.OriginalFileNameMaxLength)
            .IsRequired();
        builder.Property(asset => asset.MediaType)
            .HasMaxLength(MaterialFilePolicy.MediaTypeMaxLength)
            .IsRequired();
        builder.Property(asset => asset.StorageProvider)
            .HasMaxLength(MaterialFilePolicy.StorageProviderMaxLength)
            .IsRequired();
        builder.Property(asset => asset.StorageContainer)
            .HasMaxLength(MaterialFilePolicy.StorageContainerMaxLength)
            .IsRequired();
        builder.Property(asset => asset.ObjectKey)
            .HasMaxLength(MaterialFilePolicy.ObjectKeyMaxLength)
            .IsRequired();
        builder.Property(asset => asset.Sha256Checksum)
            .HasMaxLength(MaterialFilePolicy.ChecksumMaxLength)
            .IsFixedLength()
            .IsRequired();
        builder.Property(asset => asset.Status).HasConversion<int>().IsRequired();
        builder.Property(asset => asset.CreatedAtUtc).HasPrecision(7).IsRequired();
        builder.HasOne<PresentationDocument>()
            .WithMany()
            .HasForeignKey(asset => asset.MaterialVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(asset => asset.ObjectKey)
            .IsUnique()
            .HasDatabaseName("UX_PresentationAssets_ObjectKey");
    }
}

internal sealed class PresentationElementConfiguration
    : IEntityTypeConfiguration<PresentationElement>
{
    public void Configure(EntityTypeBuilder<PresentationElement> builder)
    {
        builder.ToTable("PresentationElements", table =>
        {
            table.HasCheckConstraint(
                "CK_PresentationElements_Type",
                "[ElementTypeCode] IN ('TEXT', 'IMAGE', 'SHAPE', 'TABLE')");
            table.HasCheckConstraint(
                "CK_PresentationElements_Schema",
                "[ElementSchemaVersion] = 1");
            table.HasCheckConstraint(
                "CK_PresentationElements_ZIndex",
                "[ZIndex] >= 0");
            table.HasCheckConstraint(
                "CK_PresentationElements_Bounds",
                $"[X] >= 0 AND [Y] >= 0 AND [Width] > 0 AND [Height] > 0 AND [X] + [Width] <= {PresentationDocument.CanvasWidthValue} AND [Y] + [Height] <= {PresentationDocument.CanvasHeightValue}");
            table.HasCheckConstraint(
                "CK_PresentationElements_Payload",
                $"ISJSON([PayloadJson]) = 1 AND DATALENGTH([PayloadJson]) <= {PresentationElement.MaximumPayloadBytes}");
            table.HasCheckConstraint(
                "CK_PresentationElements_Asset",
                "([ElementTypeCode] = 'IMAGE' AND [PresentationAssetId] IS NOT NULL) OR ([ElementTypeCode] <> 'IMAGE' AND [PresentationAssetId] IS NULL)");
            table.HasTrigger("TR_PresentationElements_ProtectSnapshot");
        });

        builder.HasKey(element => element.Id);
        builder.HasAlternateKey(element => new
        {
            element.MaterialVersionId,
            element.Id,
        })
            .HasName("AK_PresentationElements_MaterialVersion_Id");
        builder.Property(element => element.ElementTypeCode).HasMaxLength(32).IsRequired();
        builder.Property(element => element.ElementSchemaVersion).IsRequired();
        builder.Property(element => element.X).HasPrecision(9, 3).IsRequired();
        builder.Property(element => element.Y).HasPrecision(9, 3).IsRequired();
        builder.Property(element => element.Width).HasPrecision(9, 3).IsRequired();
        builder.Property(element => element.Height).HasPrecision(9, 3).IsRequired();
        builder.Property(element => element.PayloadJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired();
        builder.HasOne<PresentationSlide>()
            .WithMany()
            .HasForeignKey(element => new
            {
                element.MaterialVersionId,
                element.PresentationSlideId,
            })
            .HasPrincipalKey(slide => new
            {
                slide.MaterialVersionId,
                slide.Id,
            })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PresentationAsset>()
            .WithMany()
            .HasForeignKey(element => new
            {
                element.MaterialVersionId,
                element.PresentationAssetId,
            })
            .HasPrincipalKey(asset => new
            {
                asset.MaterialVersionId,
                asset.Id,
            })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(element => new
        {
            element.PresentationSlideId,
            element.ZIndex,
        })
            .IsUnique()
            .HasDatabaseName("UX_PresentationElements_Slide_ZIndex");
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

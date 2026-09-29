using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Plus5.Domain.Identity;
using Plus5.Domain.Materials;

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

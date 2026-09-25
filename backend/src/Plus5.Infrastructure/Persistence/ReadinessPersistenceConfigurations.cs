using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Plus5.Domain.Readiness;
using Plus5.Domain.Students;
using Plus5.Domain.Teaching;

namespace Plus5.Infrastructure.Persistence;

internal sealed class MasteryEstimateConfiguration : IEntityTypeConfiguration<MasteryEstimate>
{
    public void Configure(EntityTypeBuilder<MasteryEstimate> builder)
    {
        ConfigureEstimate(builder, "MasteryEstimates");
        builder.HasKey(estimate => new { estimate.StudentId, estimate.KnowledgeComponentId });
        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(estimate => estimate.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<KnowledgeComponent>()
            .WithMany()
            .HasForeignKey(estimate => estimate.KnowledgeComponentId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureEstimate(
        EntityTypeBuilder<MasteryEstimate> builder,
        string tableName)
    {
        builder.ToTable(tableName, table => ConfigureChecks(table, tableName));
        ConfigureProperties(builder);
        builder.HasIndex(estimate => new { estimate.StudentId, estimate.CalculatedAtUtc });
    }

    internal static void ConfigureChecks<T>(TableBuilder<T> table, string tableName)
        where T : class
    {
        table.HasCheckConstraint(
            $"CK_{tableName}_Score",
            "[Score] IS NULL OR [Score] BETWEEN 0 AND 1");
        table.HasCheckConstraint(
            $"CK_{tableName}_Evidence",
            "[EvidenceCount] >= 0 AND [EffectiveEvidenceWeight] >= 0");
        table.HasCheckConstraint(
            $"CK_{tableName}_Confidence",
            "[Confidence] IN (N'NoData', N'VeryLow', N'Low', N'Medium', N'High')");
        table.HasCheckConstraint(
            $"CK_{tableName}_Readiness",
            "[Readiness] IN (N'InsufficientData', N'NeedsWork', N'Developing', N'Ready', N'Strong')");
        table.HasCheckConstraint(
            $"CK_{tableName}_NoDataShape",
            "([Score] IS NULL AND [Confidence] = N'NoData' AND [Readiness] = N'InsufficientData' " +
            "AND [EvidenceCount] = 0 AND [EffectiveEvidenceWeight] = 0) OR " +
            "([Score] IS NOT NULL AND [Confidence] <> N'NoData')");
        table.HasCheckConstraint(
            $"CK_{tableName}_Utc",
            "DATEPART(TZOFFSET, [CalculatedAtUtc]) = 0");
    }

    internal static void ConfigureProperties<T>(EntityTypeBuilder<T> builder)
        where T : class
    {
        builder.Property<decimal?>(nameof(MasteryEstimate.Score)).HasPrecision(9, 8);
        builder.Property<decimal>(nameof(MasteryEstimate.EffectiveEvidenceWeight))
            .HasPrecision(18, 8);
        builder.Property<ReadinessConfidence>(nameof(MasteryEstimate.Confidence))
            .HasConversion<string>()
            .HasMaxLength(8)
            .IsRequired();
        builder.Property<ReadinessStatus>(nameof(MasteryEstimate.Readiness))
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();
        builder.Property<DateTimeOffset>(nameof(MasteryEstimate.CalculatedAtUtc))
            .HasPrecision(7)
            .IsRequired();
        builder.Property<string>(nameof(MasteryEstimate.AlgorithmVersion))
            .HasMaxLength(MasteryEstimate.AlgorithmVersionMaxLength)
            .IsRequired();
    }
}

internal sealed class KnowledgeAreaReadinessEstimateConfiguration
    : IEntityTypeConfiguration<KnowledgeAreaReadinessEstimate>
{
    public void Configure(EntityTypeBuilder<KnowledgeAreaReadinessEstimate> builder)
    {
        const string tableName = "KnowledgeAreaReadinessEstimates";
        builder.ToTable(tableName, table =>
            MasteryEstimateConfiguration.ConfigureChecks(table, tableName));
        MasteryEstimateConfiguration.ConfigureProperties(builder);
        builder.HasKey(estimate => new { estimate.StudentId, estimate.KnowledgeAreaId });
        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(estimate => estimate.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<KnowledgeArea>()
            .WithMany()
            .HasForeignKey(estimate => estimate.KnowledgeAreaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(estimate => new { estimate.StudentId, estimate.CalculatedAtUtc });
    }
}

internal sealed class CurriculumOutcomeReadinessEstimateConfiguration
    : IEntityTypeConfiguration<CurriculumOutcomeReadinessEstimate>
{
    public void Configure(EntityTypeBuilder<CurriculumOutcomeReadinessEstimate> builder)
    {
        const string tableName = "CurriculumOutcomeReadinessEstimates";
        builder.ToTable(tableName, table =>
            MasteryEstimateConfiguration.ConfigureChecks(table, tableName));
        MasteryEstimateConfiguration.ConfigureProperties(builder);
        builder.HasKey(estimate => new { estimate.StudentId, estimate.CurriculumOutcomeId });
        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(estimate => estimate.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CurriculumOutcome>()
            .WithMany()
            .HasForeignKey(estimate => estimate.CurriculumOutcomeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(estimate => new { estimate.StudentId, estimate.CalculatedAtUtc });
    }
}

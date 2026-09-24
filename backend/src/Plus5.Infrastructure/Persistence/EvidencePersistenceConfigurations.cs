using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Plus5.Domain.Evidence;
using Plus5.Domain.Students;
using Plus5.Domain.Teaching;

namespace Plus5.Infrastructure.Persistence;

internal sealed class EvidenceEventConfiguration : IEntityTypeConfiguration<EvidenceEvent>
{
    public void Configure(EntityTypeBuilder<EvidenceEvent> builder)
    {
        builder.ToTable("EvidenceEvents", table =>
        {
            table.HasCheckConstraint(
                "CK_EvidenceEvents_Kind",
                "[Kind] IN (1, 2, 3)");
            table.HasCheckConstraint(
                "CK_EvidenceEvents_LifecycleShape",
                "([Kind] = 1 AND [SupersedesEvidenceEventId] IS NULL AND [ReasonCode] IS NULL) " +
                "OR ([Kind] IN (2, 3) AND [SupersedesEvidenceEventId] IS NOT NULL " +
                "AND [ReasonCode] IS NOT NULL)");
            table.HasCheckConstraint(
                "CK_EvidenceEvents_NotOwnPredecessor",
                "[SupersedesEvidenceEventId] IS NULL OR [SupersedesEvidenceEventId] <> [Id]");
            table.HasCheckConstraint(
                "CK_EvidenceEvents_SourceId",
                "[SourceId] <> '00000000-0000-0000-0000-000000000000'");
            table.HasCheckConstraint(
                "CK_EvidenceEvents_SourceKind",
                "LEN(LTRIM(RTRIM([SourceKind]))) > 0 " +
                "AND [SourceKind] COLLATE Latin1_General_100_BIN2 " +
                "NOT LIKE '%[^A-Z0-9_.-]%' COLLATE Latin1_General_100_BIN2");
            table.HasCheckConstraint(
                "CK_EvidenceEvents_ReasonCode",
                "[ReasonCode] IS NULL OR " +
                "(LEN(LTRIM(RTRIM([ReasonCode]))) > 0 " +
                "AND [ReasonCode] COLLATE Latin1_General_100_BIN2 " +
                "NOT LIKE '%[^A-Z0-9_.-]%' COLLATE Latin1_General_100_BIN2)");
            table.HasCheckConstraint(
                "CK_EvidenceEvents_Utc",
                "DATEPART(TZOFFSET, [OccurredAtUtc]) = 0 " +
                "AND DATEPART(TZOFFSET, [RecordedAtUtc]) = 0");
            table.HasTrigger("TR_EvidenceEvents_ValidateAppendOnly");
            table.HasTrigger("TR_EvidenceEvents_BlockDelete");
        });

        builder.HasKey(evidence => evidence.Id);
        builder.HasAlternateKey(evidence => new
        {
            evidence.StudentId,
            evidence.SourceKind,
            evidence.SourceId,
            evidence.Id,
        })
            .HasName("AK_EvidenceEvents_SourceChain_Id");
        builder.Property(evidence => evidence.Kind)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(evidence => evidence.SourceKind)
            .HasMaxLength(EvidenceEvent.SourceKindMaxLength)
            .IsRequired();
        builder.Property(evidence => evidence.OccurredAtUtc)
            .HasPrecision(7)
            .IsRequired();
        builder.Property(evidence => evidence.RecordedAtUtc)
            .HasPrecision(7)
            .IsRequired();
        builder.Property(evidence => evidence.ReasonCode)
            .HasMaxLength(EvidenceEvent.ReasonCodeMaxLength);
        builder.Ignore(evidence => evidence.KnowledgeComponents);

        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(evidence => evidence.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EvidenceEvent>()
            .WithMany()
            .HasForeignKey(evidence => new
            {
                evidence.StudentId,
                evidence.SourceKind,
                evidence.SourceId,
                evidence.SupersedesEvidenceEventId,
            })
            .HasPrincipalKey(evidence => new
            {
                evidence.StudentId,
                evidence.SourceKind,
                evidence.SourceId,
                evidence.Id,
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(evidence => new
        {
            evidence.StudentId,
            evidence.SourceKind,
            evidence.SourceId,
        })
            .IsUnique()
            .HasFilter("[SupersedesEvidenceEventId] IS NULL")
            .HasDatabaseName("UX_EvidenceEvents_Student_Source_Root");
        builder.HasIndex(evidence => evidence.SupersedesEvidenceEventId)
            .IsUnique()
            .HasFilter("[SupersedesEvidenceEventId] IS NOT NULL")
            .HasDatabaseName("UX_EvidenceEvents_Predecessor_Successor");
        builder.HasIndex(evidence => new
        {
            evidence.StudentId,
            evidence.OccurredAtUtc,
            evidence.Id,
        })
            .HasDatabaseName("IX_EvidenceEvents_Student_Occurred_Id");
    }
}

internal sealed class EvidenceEventKnowledgeComponentConfiguration
    : IEntityTypeConfiguration<EvidenceEventKnowledgeComponent>
{
    public void Configure(EntityTypeBuilder<EvidenceEventKnowledgeComponent> builder)
    {
        builder.ToTable("EvidenceEventKnowledgeComponents", table =>
        {
            table.HasTrigger("TR_EvidenceEventKnowledgeComponents_ValidateTargets");
        });
        builder.HasKey(mapping => new
        {
            mapping.EvidenceEventId,
            mapping.KnowledgeComponentId,
        });
        builder.HasOne<EvidenceEvent>()
            .WithMany()
            .HasForeignKey(mapping => mapping.EvidenceEventId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<KnowledgeComponent>()
            .WithMany()
            .HasForeignKey(mapping => mapping.KnowledgeComponentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(mapping => mapping.KnowledgeComponentId)
            .HasDatabaseName("IX_EvidenceEventKnowledgeComponents_ComponentId");
    }
}

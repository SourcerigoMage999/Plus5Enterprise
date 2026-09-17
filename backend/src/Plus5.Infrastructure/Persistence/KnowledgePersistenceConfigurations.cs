using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Plus5.Domain.Teaching;

namespace Plus5.Infrastructure.Persistence;

internal sealed class KnowledgeModelConfiguration : IEntityTypeConfiguration<KnowledgeModel>
{
    public void Configure(EntityTypeBuilder<KnowledgeModel> builder)
    {
        builder.ToTable("KnowledgeModels", table =>
        {
            table.HasCheckConstraint(
                "CK_KnowledgeModels_Status",
                "[Status] IN (1, 2, 3)");
            table.HasTrigger("TR_KnowledgeModels_ProtectLifecycle");
        });

        builder.HasKey(model => model.Id);
        builder.Property(model => model.Code)
            .HasMaxLength(KnowledgeModel.CodeMaxLength)
            .IsRequired();
        builder.Property(model => model.Version)
            .HasMaxLength(KnowledgeModel.VersionMaxLength)
            .IsRequired();
        builder.Property(model => model.Status).IsRequired();
        builder.HasIndex(model => new { model.Code, model.Version })
            .IsUnique()
            .HasDatabaseName("UX_KnowledgeModels_Code_Version");
    }
}

internal sealed class KnowledgeAreaConfiguration : IEntityTypeConfiguration<KnowledgeArea>
{
    public void Configure(EntityTypeBuilder<KnowledgeArea> builder)
    {
        builder.ToTable("KnowledgeAreas", table =>
        {
            table.HasCheckConstraint(
                "CK_KnowledgeAreas_SortOrder",
                "[SortOrder] >= 0");
            table.HasTrigger("TR_KnowledgeAreas_ProtectPublishedStructure");
        });

        builder.HasKey(area => area.Id);
        builder.HasAlternateKey(area => new { area.KnowledgeModelId, area.Id })
            .HasName("AK_KnowledgeAreas_KnowledgeModel_Id");
        builder.Property(area => area.Name)
            .HasMaxLength(KnowledgeArea.NameMaxLength)
            .IsRequired();
        builder.Property(area => area.Description)
            .HasMaxLength(KnowledgeArea.DescriptionMaxLength);
        builder.Property(area => area.SortOrder).IsRequired();
        builder.HasOne<KnowledgeModel>()
            .WithMany()
            .HasForeignKey(area => area.KnowledgeModelId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(area => new
        {
            area.KnowledgeModelId,
            area.SortOrder,
            area.Id,
        })
            .HasDatabaseName("IX_KnowledgeAreas_Model_Sort_Id");
    }
}

internal sealed class KnowledgeComponentConfiguration
    : IEntityTypeConfiguration<KnowledgeComponent>
{
    public void Configure(EntityTypeBuilder<KnowledgeComponent> builder)
    {
        builder.ToTable("KnowledgeComponents", table =>
        {
            table.HasCheckConstraint(
                "CK_KnowledgeComponents_SortOrder",
                "[SortOrder] >= 0");
            table.HasCheckConstraint(
                "CK_KnowledgeComponents_Status",
                "[Status] IN (1, 2)");
            table.HasCheckConstraint(
                "CK_KnowledgeComponents_NotOwnParent",
                "[ParentComponentId] IS NULL OR [ParentComponentId] <> [Id]");
            table.HasCheckConstraint(
                "CK_KnowledgeComponents_NotOwnPredecessor",
                "[SupersedesKnowledgeComponentId] IS NULL OR [SupersedesKnowledgeComponentId] <> [Id]");
            table.HasTrigger("TR_KnowledgeComponents_ValidateIntegrity");
        });

        builder.HasKey(component => component.Id);
        builder.HasAlternateKey(component => new
        {
            component.KnowledgeModelId,
            component.KnowledgeAreaId,
            component.Id,
        })
            .HasName("AK_KnowledgeComponents_Model_Area_Id");
        builder.Property(component => component.Name)
            .HasMaxLength(KnowledgeComponent.NameMaxLength)
            .IsRequired();
        builder.Property(component => component.Description)
            .HasMaxLength(KnowledgeComponent.DescriptionMaxLength);
        builder.Property(component => component.SortOrder).IsRequired();
        builder.Property(component => component.Status).IsRequired();

        builder.HasOne<KnowledgeModel>()
            .WithMany()
            .HasForeignKey(component => component.KnowledgeModelId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<KnowledgeArea>()
            .WithMany()
            .HasForeignKey(component => new
            {
                component.KnowledgeModelId,
                component.KnowledgeAreaId,
            })
            .HasPrincipalKey(area => new
            {
                area.KnowledgeModelId,
                area.Id,
            })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<KnowledgeComponent>()
            .WithMany()
            .HasForeignKey(component => new
            {
                component.KnowledgeModelId,
                component.KnowledgeAreaId,
                component.ParentComponentId,
            })
            .HasPrincipalKey(component => new
            {
                component.KnowledgeModelId,
                component.KnowledgeAreaId,
                component.Id,
            })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<KnowledgeComponent>()
            .WithMany()
            .HasForeignKey(component => component.SupersedesKnowledgeComponentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(component => new
        {
            component.KnowledgeModelId,
            component.KnowledgeAreaId,
            component.ParentComponentId,
            component.SortOrder,
            component.Id,
        })
            .HasDatabaseName("IX_KnowledgeComponents_Model_Area_Parent_Sort_Id");
        builder.HasIndex(component => component.SupersedesKnowledgeComponentId)
            .HasDatabaseName("IX_KnowledgeComponents_SupersedesComponentId");
    }
}

internal sealed class CurriculumOutcomeKnowledgeComponentConfiguration
    : IEntityTypeConfiguration<CurriculumOutcomeKnowledgeComponent>
{
    public void Configure(EntityTypeBuilder<CurriculumOutcomeKnowledgeComponent> builder)
    {
        builder.ToTable("CurriculumOutcomeKnowledgeComponents", table =>
        {
            table.HasTrigger(
                "TR_CurriculumOutcomeKnowledgeComponents_ProtectPublishedModel");
        });
        builder.HasKey(mapping => new
        {
            mapping.CurriculumOutcomeId,
            mapping.KnowledgeComponentId,
        });
        builder.HasOne<CurriculumOutcome>()
            .WithMany()
            .HasForeignKey(mapping => mapping.CurriculumOutcomeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<KnowledgeComponent>()
            .WithMany()
            .HasForeignKey(mapping => mapping.KnowledgeComponentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(mapping => mapping.KnowledgeComponentId)
            .HasDatabaseName("IX_CurriculumOutcomeKnowledgeComponents_ComponentId");
    }
}

using Plus5.Domain.Teaching;
using TeachingProgram = Plus5.Domain.Teaching.Program;

namespace Plus5.Domain.Materials;

public sealed class MaterialVersion
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public const int MaterialTypeCodeMaxLength = 64;
    public const int SubjectMaxLength = 160;
    public const int LanguageCodeMaxLength = 32;
    public const int LearningGoalMaxLength = 2000;

    private MaterialVersion()
    {
    }

    public MaterialVersion(
        Guid id,
        Material material,
        int versionNumber,
        string title,
        string materialTypeCode,
        DateTimeOffset createdAtUtc,
        string? description = null,
        string? subject = null,
        string? languageCode = null,
        TeachingProgram? program = null,
        SchoolGrade? schoolGrade = null,
        ProficiencyLevel? proficiencyLevel = null,
        string? learningGoal = null)
    {
        ArgumentNullException.ThrowIfNull(material);
        MaterialGuard.Identifier(id, nameof(id));
        MaterialGuard.Utc(createdAtUtc, nameof(createdAtUtc));
        if (versionNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(versionNumber),
                "Version number must be greater than zero.");
        }

        if (material.Status != MaterialStatus.Active)
        {
            throw new InvalidOperationException(
                "A draft version cannot be created for an archived material.");
        }

        EnsureOwnedProgram(material.OwnerTeacherId, program);

        Id = id;
        MaterialId = material.Id;
        VersionNumber = versionNumber;
        CreatedByTeacherId = material.OwnerTeacherId;
        Title = MaterialGuard.RequiredText(title, TitleMaxLength, nameof(title));
        Description = MaterialGuard.OptionalText(
            description,
            DescriptionMaxLength,
            nameof(description));
        MaterialTypeCode = MaterialGuard.Code(
            materialTypeCode,
            MaterialTypeCodeMaxLength,
            nameof(materialTypeCode));
        Subject = MaterialGuard.OptionalText(subject, SubjectMaxLength, nameof(subject));
        LanguageCode = MaterialGuard.OptionalText(
            languageCode,
            LanguageCodeMaxLength,
            nameof(languageCode));
        ProgramId = program?.Id;
        SchoolGradeId = schoolGrade?.Id;
        ProficiencyLevelId = proficiencyLevel?.Id;
        LearningGoal = MaterialGuard.OptionalText(
            learningGoal,
            LearningGoalMaxLength,
            nameof(learningGoal));
        Status = MaterialVersionStatus.Draft;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid MaterialId { get; private set; }

    public int VersionNumber { get; private set; }

    public Guid CreatedByTeacherId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string MaterialTypeCode { get; private set; } = string.Empty;

    public string? Subject { get; private set; }

    public string? LanguageCode { get; private set; }

    public Guid? ProgramId { get; private set; }

    public Guid? SchoolGradeId { get; private set; }

    public Guid? ProficiencyLevelId { get; private set; }

    public string? LearningGoal { get; private set; }

    public MaterialVersionStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? ActivatedAtUtc { get; private set; }

    public DateTimeOffset? SupersededAtUtc { get; private set; }

    public void UpdateDraft(
        string title,
        string materialTypeCode,
        string? description = null,
        string? subject = null,
        string? languageCode = null,
        TeachingProgram? program = null,
        SchoolGrade? schoolGrade = null,
        ProficiencyLevel? proficiencyLevel = null,
        string? learningGoal = null)
    {
        EnsureDraft();
        EnsureOwnedProgram(CreatedByTeacherId, program);
        Title = MaterialGuard.RequiredText(title, TitleMaxLength, nameof(title));
        Description = MaterialGuard.OptionalText(
            description,
            DescriptionMaxLength,
            nameof(description));
        MaterialTypeCode = MaterialGuard.Code(
            materialTypeCode,
            MaterialTypeCodeMaxLength,
            nameof(materialTypeCode));
        Subject = MaterialGuard.OptionalText(subject, SubjectMaxLength, nameof(subject));
        LanguageCode = MaterialGuard.OptionalText(
            languageCode,
            LanguageCodeMaxLength,
            nameof(languageCode));
        ProgramId = program?.Id;
        SchoolGradeId = schoolGrade?.Id;
        ProficiencyLevelId = proficiencyLevel?.Id;
        LearningGoal = MaterialGuard.OptionalText(
            learningGoal,
            LearningGoalMaxLength,
            nameof(learningGoal));
    }

    public void Activate(MaterialFile file, DateTimeOffset activatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(file);
        EnsureDraft();
        MaterialGuard.Utc(activatedAtUtc, nameof(activatedAtUtc));
        if (activatedAtUtc < CreatedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(activatedAtUtc),
                "Activation cannot precede version creation.");
        }

        if (file.MaterialVersionId != Id || file.Status != MaterialFileStatus.Clean)
        {
            throw new InvalidOperationException(
                "A version can be activated only with its own clean file.");
        }

        Status = MaterialVersionStatus.Active;
        ActivatedAtUtc = activatedAtUtc;
    }

    public void Supersede(DateTimeOffset supersededAtUtc)
    {
        MaterialGuard.Utc(supersededAtUtc, nameof(supersededAtUtc));
        if (Status != MaterialVersionStatus.Active)
        {
            throw new InvalidOperationException("Only an active version can be superseded.");
        }

        if (supersededAtUtc < ActivatedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(supersededAtUtc),
                "Supersession cannot precede activation.");
        }

        Status = MaterialVersionStatus.Superseded;
        SupersededAtUtc = supersededAtUtc;
    }

    public MaterialVersion CreateRestoredDraft(
        Guid newVersionId,
        Material material,
        int newVersionNumber,
        DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(material);
        if (material.Id != MaterialId)
        {
            throw new ArgumentException(
                "A restored version must belong to the same material.",
                nameof(material));
        }

        if (newVersionNumber <= VersionNumber)
        {
            throw new ArgumentOutOfRangeException(
                nameof(newVersionNumber),
                "A restore creates a new, later version number.");
        }

        var restored = new MaterialVersion(
            newVersionId,
            material,
            newVersionNumber,
            Title,
            MaterialTypeCode,
            createdAtUtc,
            Description,
            Subject,
            LanguageCode);
        restored.ProficiencyLevelId = ProficiencyLevelId;
        restored.ProgramId = ProgramId;
        restored.SchoolGradeId = SchoolGradeId;
        restored.LearningGoal = LearningGoal;
        return restored;
    }

    internal void EnsureDraft()
    {
        if (Status != MaterialVersionStatus.Draft)
        {
            throw new InvalidOperationException(
                "An active or superseded material version is immutable.");
        }
    }

    private static void EnsureOwnedProgram(Guid teacherId, TeachingProgram? program)
    {
        if (program is not null && program.TeacherAccountId != teacherId)
        {
            throw new ArgumentException(
                "Material metadata can reference only a program owned by the material owner.",
                nameof(program));
        }
    }
}

namespace Plus5.Domain.LessonPlanning;

public sealed class KnowledgeBlock
{
    public const int CodeMaxLength = 16;

    private KnowledgeBlock() { }

    public KnowledgeBlock(Guid id, string code, DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty) throw new ArgumentException("Identifier is required.", nameof(id));
        if (createdAtUtc.Offset != TimeSpan.Zero) throw new ArgumentException("Timestamp must be UTC.", nameof(createdAtUtc));
        var normalized = code?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > CodeMaxLength)
            throw new ArgumentException("Code is required.", nameof(code));
        Id = id;
        Code = normalized;
        Status = KnowledgeBlockStatus.Active;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public KnowledgeBlockStatus Status { get; private set; }
    public Guid? CurrentPublishedVersionId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public void SetCurrentPublishedVersion(KnowledgeBlockVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);
        if (Status != KnowledgeBlockStatus.Active || version.KnowledgeBlockId != Id
            || version.Status != KnowledgeBlockVersionStatus.Published)
            throw new InvalidOperationException("Only an active root can point to its own Published version.");
        CurrentPublishedVersionId = version.Id;
    }
}

public enum KnowledgeBlockStatus { Active = 1, Retired = 2 }

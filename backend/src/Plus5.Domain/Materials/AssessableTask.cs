namespace Plus5.Domain.Materials;

public sealed class AssessableTask
{
    private AssessableTask()
    {
    }

    public AssessableTask(Guid id, Material material, DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(material);
        MaterialGuard.Identifier(id, nameof(id));
        MaterialGuard.Utc(createdAtUtc, nameof(createdAtUtc));

        if (material.Status != MaterialStatus.Active)
        {
            throw new InvalidOperationException(
                "An assessable task cannot be created for an archived material.");
        }

        Id = id;
        MaterialId = material.Id;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid MaterialId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
}

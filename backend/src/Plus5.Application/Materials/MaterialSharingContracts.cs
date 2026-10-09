namespace Plus5.Application.Materials;

public enum MaterialSharingVisibility
{
    Private = 1,
    Shared = 2,
}

public enum MaterialSharingAccess
{
    View = 1,
    Use = 2,
}

public sealed record MaterialSharingGrant(
    Guid TeacherAccountId,
    string Email,
    MaterialSharingAccess Access);

public sealed record MaterialSharingWorkspace(
    Guid MaterialId,
    string Title,
    string RowVersion,
    MaterialSharingVisibility Visibility,
    IReadOnlyList<MaterialSharingGrant> Grants);

public sealed record MaterialSharingGrantCommand(
    string RecipientEmail,
    MaterialSharingAccess Access);

public sealed record MaterialSharingCommand(
    string ExpectedRowVersion,
    MaterialSharingVisibility Visibility,
    IReadOnlyCollection<MaterialSharingGrantCommand> Grants);

public enum MaterialSharingOutcome
{
    Success,
    InvalidInput,
    NotFound,
    InvalidRecipient,
    Conflict,
}

public sealed record MaterialSharingResult(MaterialSharingOutcome Outcome);

public interface IMaterialSharingQuery
{
    Task<MaterialSharingWorkspace?> GetAsync(
        Guid teacherAccountId,
        Guid materialId,
        CancellationToken cancellationToken);
}

public interface IMaterialSharingService
{
    Task<MaterialSharingResult> SaveAsync(
        Guid teacherAccountId,
        Guid materialId,
        MaterialSharingCommand command,
        CancellationToken cancellationToken);
}

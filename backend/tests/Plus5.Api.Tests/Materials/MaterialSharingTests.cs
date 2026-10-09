using Microsoft.EntityFrameworkCore;
using Plus5.Application.Materials;
using Plus5.Domain.Identity;
using Plus5.Domain.Materials;
using Plus5.Infrastructure.Materials;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Api.Tests.Materials;

public sealed class MaterialSharingTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task WorkspaceIsOwnerOnlyAndReturnsExactGrantEmailsAndPermissions()
    {
        await using var db = CreateDbContext();
        var owner = CreateActiveAccount("owner@plus5.local");
        var recipient = CreateActiveAccount("recipient@plus5.local");
        var material = AddActiveMaterial(db, owner.Id);
        material.SetVisibility(MaterialVisibility.Shared, CreatedAt.AddMinutes(5));
        db.AddRange(owner, recipient, new MaterialShare(
            material,
            recipient.Id,
            MaterialShareAccess.Use,
            CreatedAt.AddMinutes(5)));
        await db.SaveChangesAsync();

        var query = new EfMaterialSharingQuery(db);
        var workspace = await query.GetAsync(owner.Id, material.Id, CancellationToken.None);

        Assert.NotNull(workspace);
        Assert.Equal(MaterialSharingVisibility.Shared, workspace.Visibility);
        var grant = Assert.Single(workspace.Grants);
        Assert.Equal(recipient.Email, grant.Email);
        Assert.Equal(MaterialSharingAccess.Use, grant.Access);
        Assert.Null(await query.GetAsync(recipient.Id, material.Id, CancellationToken.None));
    }

    [Fact]
    public async Task ServiceRejectsGrantsOnPrivateMaterialBeforeWriting()
    {
        await using var db = CreateDbContext();
        var service = new EfMaterialSharingService(
            db,
            new FixedTimeProvider(CreatedAt.AddMinutes(10)));

        var result = await service.SaveAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new MaterialSharingCommand(
                "AQ==",
                MaterialSharingVisibility.Private,
                [new("recipient@plus5.local", MaterialSharingAccess.View)]),
            CancellationToken.None);

        Assert.Equal(MaterialSharingOutcome.InvalidInput, result.Outcome);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    private static UserAccount CreateActiveAccount(string email)
    {
        var account = new UserAccount(
            Guid.NewGuid(),
            email,
            email.ToUpperInvariant(),
            CreatedAt);
        account.ConfirmEmail(CreatedAt.AddSeconds(1));
        return account;
    }

    private static Material AddActiveMaterial(Plus5DbContext db, Guid ownerId)
    {
        var material = new Material(Guid.NewGuid(), ownerId, CreatedAt);
        var version = new MaterialVersion(
            Guid.NewGuid(),
            material,
            1,
            "Shared worksheet",
            "WORKSHEET",
            CreatedAt);
        var file = new MaterialFile(
            Guid.NewGuid(),
            material,
            version,
            MaterialFileFormat.Pdf,
            "worksheet.pdf",
            "application/pdf",
            100,
            "R2",
            "clean",
            CreatedAt);
        file.MarkUploaded(100, new string('A', 64), CreatedAt.AddMinutes(1));
        file.StartScanning(CreatedAt.AddMinutes(2));
        file.MarkClean(CreatedAt.AddMinutes(3));
        version.Activate(file, CreatedAt.AddMinutes(4));
        material.SetCurrentVersion(version, CreatedAt.AddMinutes(4));
        db.AddRange(material, version, file);
        return material;
    }

    private static Plus5DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Plus5DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new Plus5DbContext(options);
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Plus5.Application.Materials;
using Plus5.Domain.Identity;
using Plus5.Domain.Materials;
using Plus5.Infrastructure.Materials;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Api.Tests.Materials;

public sealed class MaterialSharingSqlTests
{
    private const int PrivateMaterialShareViolation = 51220;
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 10, 8, 13, 0, 0, TimeSpan.Zero);

    [LocalSqlFact]
    public async Task OwnerCanGrantChangeRevokeAndSqlRejectsPrivateMaterialWithShares()
    {
        var (options, databaseName) = CreateDatabase();
        try
        {
            await using var db = new Plus5DbContext(options);
            await db.Database.MigrateAsync();
            var owner = CreateActiveAccount("share-owner@plus5.local");
            var recipient = CreateActiveAccount("share-recipient@plus5.local");
            db.AddRange(owner, recipient);
            await db.SaveChangesAsync();
            var material = await AddActiveMaterialAsync(db, owner.Id);

            var query = new EfMaterialSharingQuery(db);
            var service = new EfMaterialSharingService(
                db,
                new FixedTimeProvider(CreatedAt.AddHours(1)));
            var initial = Assert.IsType<MaterialSharingWorkspace>(
                await query.GetAsync(owner.Id, material.Id, CancellationToken.None));

            var shared = await service.SaveAsync(
                owner.Id,
                material.Id,
                new MaterialSharingCommand(
                    initial.RowVersion,
                    MaterialSharingVisibility.Shared,
                    [new(recipient.Email, MaterialSharingAccess.View)]),
                CancellationToken.None);
            Assert.Equal(MaterialSharingOutcome.Success, shared.Outcome);
            db.ChangeTracker.Clear();

            var afterGrant = Assert.IsType<MaterialSharingWorkspace>(
                await query.GetAsync(owner.Id, material.Id, CancellationToken.None));
            Assert.Equal(MaterialSharingAccess.View, Assert.Single(afterGrant.Grants).Access);

            var sqlViolation = await Assert.ThrowsAsync<SqlException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync($$"""
                    UPDATE [Materials]
                    SET [Visibility] = 1
                    WHERE [Id] = {{material.Id}};
                    """));
            Assert.Equal(PrivateMaterialShareViolation, sqlViolation.Number);

            var changed = await service.SaveAsync(
                owner.Id,
                material.Id,
                new MaterialSharingCommand(
                    afterGrant.RowVersion,
                    MaterialSharingVisibility.Shared,
                    [new(recipient.Email, MaterialSharingAccess.Use)]),
                CancellationToken.None);
            Assert.Equal(MaterialSharingOutcome.Success, changed.Outcome);
            db.ChangeTracker.Clear();

            var afterChange = Assert.IsType<MaterialSharingWorkspace>(
                await query.GetAsync(owner.Id, material.Id, CancellationToken.None));
            Assert.Equal(MaterialSharingAccess.Use, Assert.Single(afterChange.Grants).Access);

            var stale = await service.SaveAsync(
                owner.Id,
                material.Id,
                new MaterialSharingCommand(
                    afterGrant.RowVersion,
                    MaterialSharingVisibility.Private,
                    []),
                CancellationToken.None);
            Assert.Equal(MaterialSharingOutcome.Conflict, stale.Outcome);

            var madePrivate = await service.SaveAsync(
                owner.Id,
                material.Id,
                new MaterialSharingCommand(
                    afterChange.RowVersion,
                    MaterialSharingVisibility.Private,
                    []),
                CancellationToken.None);
            Assert.Equal(MaterialSharingOutcome.Success, madePrivate.Outcome);
            db.ChangeTracker.Clear();

            var final = Assert.IsType<MaterialSharingWorkspace>(
                await query.GetAsync(owner.Id, material.Id, CancellationToken.None));
            Assert.Equal(MaterialSharingVisibility.Private, final.Visibility);
            Assert.Empty(final.Grants);
            Assert.Empty(await db.MaterialShares.AsNoTracking().ToListAsync());
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    [LocalSqlFact]
    public async Task UnknownDeactivatedAndSelfRecipientsReturnTheSameGenericOutcome()
    {
        var (options, databaseName) = CreateDatabase();
        try
        {
            await using var db = new Plus5DbContext(options);
            await db.Database.MigrateAsync();
            var owner = CreateActiveAccount("enumeration-owner@plus5.local");
            var deactivated = CreateActiveAccount("deactivated@plus5.local");
            deactivated.Deactivate(CreatedAt.AddMinutes(2));
            db.AddRange(owner, deactivated);
            await db.SaveChangesAsync();
            var material = await AddActiveMaterialAsync(db, owner.Id);
            var workspace = Assert.IsType<MaterialSharingWorkspace>(
                await new EfMaterialSharingQuery(db).GetAsync(
                    owner.Id,
                    material.Id,
                    CancellationToken.None));
            var service = new EfMaterialSharingService(
                db,
                new FixedTimeProvider(CreatedAt.AddHours(1)));

            var emails = new[]
            {
                "unknown@plus5.local",
                deactivated.Email,
                owner.Email,
            };

            foreach (var email in emails)
            {
                var result = await service.SaveAsync(
                    owner.Id,
                    material.Id,
                    new MaterialSharingCommand(
                        workspace.RowVersion,
                        MaterialSharingVisibility.Shared,
                        [new(email, MaterialSharingAccess.View)]),
                    CancellationToken.None);

                Assert.Equal(MaterialSharingOutcome.InvalidRecipient, result.Outcome);
            }
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    private static async Task<Material> AddActiveMaterialAsync(
        Plus5DbContext db,
        Guid ownerId)
    {
        var material = new Material(Guid.NewGuid(), ownerId, CreatedAt);
        var version = new MaterialVersion(
            Guid.NewGuid(),
            material,
            1,
            "Sharing SQL worksheet",
            "WORKSHEET",
            CreatedAt);
        var file = new MaterialFile(
            Guid.NewGuid(),
            material,
            version,
            MaterialFileFormat.Pdf,
            "sharing.pdf",
            "application/pdf",
            100,
            "R2",
            "quarantine",
            CreatedAt);
        db.AddRange(material, version, file);
        await db.SaveChangesAsync();
        file.MarkUploaded(100, new string('A', 64), CreatedAt.AddMinutes(1));
        await db.SaveChangesAsync();
        file.StartScanning(CreatedAt.AddMinutes(2));
        await db.SaveChangesAsync();
        file.MarkClean(CreatedAt.AddMinutes(3));
        await db.SaveChangesAsync();
        version.Activate(file, CreatedAt.AddMinutes(4));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return await db.Materials.SingleAsync(candidate => candidate.Id == material.Id);
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

    private static (DbContextOptions<Plus5DbContext> Options, string DatabaseName) CreateDatabase()
    {
        var connection = new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING"));
        Assert.True(connection.DataSource is
            "localhost,1433" or "127.0.0.1,1433" or "database,1433");
        var databaseName = "Plus5_Phase68Test_" + Guid.NewGuid().ToString("N");
        connection.InitialCatalog = databaseName;
        return (new DbContextOptionsBuilder<Plus5DbContext>()
            .UseSqlServer(connection.ConnectionString)
            .Options, databaseName);
    }

    private static async Task DeleteDatabase(
        DbContextOptions<Plus5DbContext> options,
        string databaseName)
    {
        await using var cleanup = new Plus5DbContext(options);
        Assert.Equal(databaseName, cleanup.Database.GetDbConnection().Database);
        await cleanup.Database.EnsureDeletedAsync();
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }

    private sealed class LocalSqlFactAttribute : FactAttribute
    {
        public LocalSqlFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(
                    Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING")))
            {
                Skip = "Local disposable SQL credentials required.";
            }
        }
    }
}

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Plus5.Application.Materials;
using Plus5.Domain.Identity;
using Plus5.Domain.Materials;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Materials;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Api.Tests.Materials;

public sealed class MaterialLibrarySqlTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 9, 30, 10, 0, 0, TimeSpan.Zero);

    [LocalSqlFact]
    public async Task SqlQueryTranslatesAndEnforcesOwnerAndShareScope()
    {
        var (options, databaseName) = CreateDatabase();

        try
        {
            await using var db = new Plus5DbContext(options);
            await db.Database.MigrateAsync();
            var owner = CreateAccount("phase63-owner@plus5.local");
            var recipient = CreateAccount("phase63-recipient@plus5.local");
            var grade = new SchoolGrade(Guid.NewGuid(), "GRADE-8", "8. razred", 8);
            var program = new Plus5.Domain.Teaching.Program(
                Guid.NewGuid(), owner.Id, "English 8", CreatedAt);
            var material = new Material(Guid.NewGuid(), owner.Id, CreatedAt);
            var version = new MaterialVersion(
                Guid.NewGuid(),
                material,
                1,
                "Present Perfect worksheet",
                "WORKSHEET",
                CreatedAt,
                subject: "Grammar",
                program: program,
                schoolGrade: grade);
            var file = new MaterialFile(
                Guid.NewGuid(),
                material,
                version,
                MaterialFileFormat.Pdf,
                "material.pdf",
                "application/pdf",
                100,
                "R2",
                "quarantine",
                CreatedAt);
            db.AddRange(owner, recipient, grade, program, material, version, file);
            db.MaterialVersionTags.Add(new MaterialVersionTag(version, "present perfect"));
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

            material = await db.Materials.SingleAsync(item => item.Id == material.Id);
            material.SetVisibility(MaterialVisibility.Shared, CreatedAt.AddMinutes(5));
            await db.SaveChangesAsync();
            db.MaterialShares.Add(new MaterialShare(
                material,
                recipient.Id,
                MaterialShareAccess.View,
                CreatedAt.AddMinutes(5)));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var query = new EfMaterialLibraryQuery(db);
            var mine = await query.GetPageAsync(
                owner.Id,
                Criteria(MaterialLibraryOwnership.Mine),
                CancellationToken.None);
            var shared = await query.GetPageAsync(
                recipient.Id,
                Criteria(MaterialLibraryOwnership.SharedWithMe),
                CancellationToken.None);
            var unrelated = await query.GetPageAsync(
                Guid.NewGuid(),
                Criteria(MaterialLibraryOwnership.SharedWithMe),
                CancellationToken.None);
            var overview = await query.GetOverviewAsync(
                recipient.Id,
                MaterialLibraryOwnership.SharedWithMe,
                CancellationToken.None);

            Assert.Equal(material.Id, Assert.Single(mine.Items).Id);
            var sharedItem = Assert.Single(shared.Items);
            Assert.Equal(material.Id, sharedItem.Id);
            Assert.False(sharedItem.IsOwner);
            Assert.Equal(MaterialLibraryShareAccess.View, sharedItem.ShareAccess);
            Assert.Empty(unrelated.Items);
            Assert.Equal(["Grammar"], overview.Subjects);
            Assert.Equal("WORKSHEET", Assert.Single(overview.MaterialTypes));
            Assert.Equal("present perfect", Assert.Single(overview.Tags));
        }
        finally
        {
            await DeleteDatabase(options, databaseName);
        }
    }

    private static MaterialLibraryCriteria Criteria(MaterialLibraryOwnership ownership) =>
        new(1, 24, ownership, MaterialLibrarySort.Newest, null, null, null, null, null, null);

    private static UserAccount CreateAccount(string email) =>
        new(Guid.NewGuid(), email, email.ToUpperInvariant(), CreatedAt);

    private static (DbContextOptions<Plus5DbContext> Options, string DatabaseName) CreateDatabase()
    {
        var connection = new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("PLUS5_TEST_SQL_CONNECTION_STRING"));
        Assert.True(connection.DataSource is
            "localhost,1433" or "127.0.0.1,1433" or "database,1433");
        var databaseName = "Plus5_Phase63Test_" + Guid.NewGuid().ToString("N");
        connection.InitialCatalog = databaseName;
        var options = new DbContextOptionsBuilder<Plus5DbContext>()
            .UseSqlServer(connection.ConnectionString)
            .Options;
        return (options, databaseName);
    }

    private static async Task DeleteDatabase(
        DbContextOptions<Plus5DbContext> options,
        string databaseName)
    {
        await using var cleanup = new Plus5DbContext(options);
        Assert.Equal(databaseName, cleanup.Database.GetDbConnection().Database);
        await cleanup.Database.EnsureDeletedAsync();
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

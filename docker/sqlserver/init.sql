:setvar DatabaseName "Plus5"

IF DB_ID(N'$(DatabaseName)') IS NULL
BEGIN
    DECLARE @createDatabaseStatement nvarchar(max) =
        N'CREATE DATABASE ' + QUOTENAME(N'$(DatabaseName)') + N';';

    EXEC(@createDatabaseStatement);
END;
GO

DECLARE @migrationPassword nvarchar(128) = N'$(MigrationPassword)';
DECLARE @applicationPassword nvarchar(128) = N'$(ApplicationPassword)';
DECLARE @statement nvarchar(max);

IF SUSER_ID(N'plus5_migrator') IS NULL
BEGIN
    SET @statement = N'CREATE LOGIN [plus5_migrator] WITH PASSWORD = N'''
        + REPLACE(@migrationPassword, N'''', N'''''')
        + N''', CHECK_POLICY = ON, CHECK_EXPIRATION = OFF;';
END
ELSE
BEGIN
    SET @statement = N'ALTER LOGIN [plus5_migrator] WITH PASSWORD = N'''
        + REPLACE(@migrationPassword, N'''', N'''''')
        + N''';';
END;

EXEC(@statement);

IF SUSER_ID(N'plus5_app') IS NULL
BEGIN
    SET @statement = N'CREATE LOGIN [plus5_app] WITH PASSWORD = N'''
        + REPLACE(@applicationPassword, N'''', N'''''')
        + N''', CHECK_POLICY = ON, CHECK_EXPIRATION = OFF;';
END
ELSE
BEGIN
    SET @statement = N'ALTER LOGIN [plus5_app] WITH PASSWORD = N'''
        + REPLACE(@applicationPassword, N'''', N'''''')
        + N''';';
END;

EXEC(@statement);
GO

USE [Plus5];
GO

IF USER_ID(N'plus5_migrator') IS NULL
BEGIN
    CREATE USER [plus5_migrator] FOR LOGIN [plus5_migrator];
END;

IF USER_ID(N'plus5_app') IS NULL
BEGIN
    CREATE USER [plus5_app] FOR LOGIN [plus5_app];
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.database_role_members AS drm
    INNER JOIN sys.database_principals AS role_principal
        ON role_principal.principal_id = drm.role_principal_id
    INNER JOIN sys.database_principals AS member_principal
        ON member_principal.principal_id = drm.member_principal_id
    WHERE role_principal.name = N'db_owner'
        AND member_principal.name = N'plus5_migrator'
)
BEGIN
    ALTER ROLE [db_owner] ADD MEMBER [plus5_migrator];
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.database_role_members AS drm
    INNER JOIN sys.database_principals AS role_principal
        ON role_principal.principal_id = drm.role_principal_id
    INNER JOIN sys.database_principals AS member_principal
        ON member_principal.principal_id = drm.member_principal_id
    WHERE role_principal.name = N'db_datareader'
        AND member_principal.name = N'plus5_app'
)
BEGIN
    ALTER ROLE [db_datareader] ADD MEMBER [plus5_app];
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.database_role_members AS drm
    INNER JOIN sys.database_principals AS role_principal
        ON role_principal.principal_id = drm.role_principal_id
    INNER JOIN sys.database_principals AS member_principal
        ON member_principal.principal_id = drm.member_principal_id
    WHERE role_principal.name = N'db_datawriter'
        AND member_principal.name = N'plus5_app'
)
BEGIN
    ALTER ROLE [db_datawriter] ADD MEMBER [plus5_app];
END;
GO

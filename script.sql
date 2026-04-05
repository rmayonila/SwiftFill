IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [roles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_roles] PRIMARY KEY ([Id])
);

CREATE TABLE [users] (
    [Id] nvarchar(450) NOT NULL,
    [FirstName] nvarchar(max) NULL,
    [LastName] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_users] PRIMARY KEY ([Id])
);

CREATE TABLE [role_claims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_role_claims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_role_claims_roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [roles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [user_claims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_user_claims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_user_claims_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [user_logins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_user_logins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_user_logins_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [user_roles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_user_roles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_user_roles_roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [roles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_user_roles_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [user_tokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_user_tokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_user_tokens_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [users] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_role_claims_RoleId] ON [role_claims] ([RoleId]);

CREATE UNIQUE INDEX [RoleNameIndex] ON [roles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;

CREATE INDEX [IX_user_claims_UserId] ON [user_claims] ([UserId]);

CREATE INDEX [IX_user_logins_UserId] ON [user_logins] ([UserId]);

CREATE INDEX [IX_user_roles_RoleId] ON [user_roles] ([RoleId]);

CREATE INDEX [EmailIndex] ON [users] ([NormalizedEmail]);

CREATE UNIQUE INDEX [UserNameIndex] ON [users] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260326065933_InitialCreate', N'9.0.0');

COMMIT;
GO


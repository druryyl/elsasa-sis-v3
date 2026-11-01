-- =============================================================================
-- 20261101_0001_Init_Schemas.sql
-- Stage 1 schema provisioning (ADR-002): each domain owns an isolated schema.
-- Idempotent: every schema is created only when it does not already exist.
-- =============================================================================

IF SCHEMA_ID(N'operations') IS NULL
BEGIN
    EXEC (N'CREATE SCHEMA [operations] AUTHORIZATION [dbo];');
END;
GO

IF SCHEMA_ID(N'students') IS NULL
BEGIN
    EXEC (N'CREATE SCHEMA [students] AUTHORIZATION [dbo];');
END;
GO

IF SCHEMA_ID(N'finance') IS NULL
BEGIN
    EXEC (N'CREATE SCHEMA [finance] AUTHORIZATION [dbo];');
END;
GO

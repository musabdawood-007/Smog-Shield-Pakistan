-- ============================================================
-- SmogShield Pakistan - Database Setup & Stored Procedures
-- Run this file once in SSMS (SQL Server Management Studio)
-- Works on both: (localdb)\MSSQLLocalDB  OR  .\SQLEXPRESS
-- (The app auto-detects the server, so this script is optional -
--  DatabaseHelper.Initialize() creates everything automatically.)
-- ============================================================

IF DB_ID('SmogShield_Pakistan') IS NULL
    CREATE DATABASE SmogShield_Pakistan;
GO

USE SmogShield_Pakistan;
GO

-- TABLES

IF OBJECT_ID('dbo.VisitorLogs') IS NULL
BEGIN
    CREATE TABLE dbo.VisitorLogs (
        Id         INT IDENTITY(1,1) PRIMARY KEY,
        Name       NVARCHAR(100) NOT NULL,
        Email      NVARCHAR(255) NOT NULL DEFAULT '',
        Page       NVARCHAR(50)  NOT NULL,
        Details    NVARCHAR(255) NULL,
        VisitCount INT           NOT NULL DEFAULT 1,
        VisitedAt  DATETIME      NOT NULL DEFAULT GETDATE()
    );
END
GO

IF COL_LENGTH('dbo.VisitorLogs', 'Email') IS NULL
    ALTER TABLE dbo.VisitorLogs ADD Email NVARCHAR(255) NOT NULL DEFAULT '';
GO

IF COL_LENGTH('dbo.VisitorLogs', 'VisitCount') IS NULL
    ALTER TABLE dbo.VisitorLogs ADD VisitCount INT NOT NULL DEFAULT 1;
GO

IF OBJECT_ID('dbo.AirReadings') IS NULL
BEGIN
    CREATE TABLE dbo.AirReadings (
        Id         INT IDENTITY(1,1) PRIMARY KEY,
        Code       NVARCHAR(20)  NOT NULL,
        City       NVARCHAR(100) NOT NULL,
        Station    NVARCHAR(150) NULL,
        Pm25       FLOAT         NULL,
        Pm10       FLOAT         NULL,
        Aqi        FLOAT         NULL,
        Source     NVARCHAR(50)  NOT NULL,
        ObservedAt DATETIME      NOT NULL
    );
END
GO

-- STORED PROCEDURES - VisitorLogs

CREATE OR ALTER PROCEDURE dbo.sp_InsertVisitor
    @Name    NVARCHAR(100),
    @Email   NVARCHAR(255),
    @Page    NVARCHAR(50),
    @Details NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    -- Same email = one record; increment visit count instead of a duplicate row
    IF LEN(@Email) > 0 AND EXISTS (SELECT 1 FROM dbo.VisitorLogs WHERE Email = @Email)
    BEGIN
        UPDATE dbo.VisitorLogs
        SET VisitCount = VisitCount + 1, VisitedAt = GETDATE(),
            Name = @Name, Page = @Page, Details = @Details
        WHERE Email = @Email;
    END
    ELSE
        INSERT INTO dbo.VisitorLogs (Name, Email, Page, Details, VisitCount, VisitedAt)
        VALUES (@Name, @Email, @Page, @Details, 1, GETDATE());
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetVisitors
    @Limit INT = 50
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@Limit) Id, Name, Page, Details, VisitedAt, Email, VisitCount
    FROM dbo.VisitorLogs
    ORDER BY VisitedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetVisitorCount
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ISNULL(SUM(VisitCount), 0) AS Total FROM dbo.VisitorLogs;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_DeleteVisitor
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.VisitorLogs WHERE Id = @Id;
END
GO

-- STORED PROCEDURES - AirReadings

CREATE OR ALTER PROCEDURE dbo.sp_InsertReading
    @Code       NVARCHAR(20),
    @City       NVARCHAR(100),
    @Station    NVARCHAR(150),
    @Pm25       FLOAT,
    @Pm10       FLOAT,
    @Aqi        FLOAT,
    @Source     NVARCHAR(50),
    @ObservedAt DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.AirReadings (Code, City, Station, Pm25, Pm10, Aqi, Source, ObservedAt)
    VALUES (@Code, @City, @Station, @Pm25, @Pm10, @Aqi, @Source, @ObservedAt);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetLatestReadings
    @Limit INT = 50
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@Limit) Id, Code, City, Station, Pm25, Pm10, Aqi, Source, ObservedAt
    FROM dbo.AirReadings
    ORDER BY ObservedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetCityAverages
AS
BEGIN
    SET NOCOUNT ON;
    SELECT City,
           AVG(Pm25) AS AvgPm25,
           AVG(Pm10) AS AvgPm10,
           COUNT(*)  AS StationCount
    FROM dbo.AirReadings
    GROUP BY City
    ORDER BY AVG(Pm25) DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ClearOldReadings
    @Days INT = 30
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.AirReadings
    WHERE ObservedAt < DATEADD(DAY, -@Days, GETDATE());
END
GO

PRINT 'SmogShield Pakistan database setup complete.';
GO

using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using SmogShield_Pakistan.Models;

namespace SmogShield_Pakistan.Services
{
    public static class DatabaseHelper
    {
        // --------------------------------------------------------------
        // Auto-detect SQL Server instance (works on every team PC).
        // Order: LocalDB (default VS install) -> SQL Express -> localhost.
        // The first server that opens is cached for the app's lifetime,
        // so no one has to edit YOURPCNAME anymore.
        // --------------------------------------------------------------
        private static string? _cachedServer;

        private static string Server
        {
            get
            {
                if (_cachedServer is not null) return _cachedServer;
                foreach (var candidate in new[] { @"(localdb)\MSSQLLocalDB", @".\SQLEXPRESS", @"localhost" })
                {
                    try
                    {
                        using var probe = new SqlConnection(
                            $@"Server={candidate};Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=3;");
                        probe.Open();
                        _cachedServer = candidate;
                        return candidate;
                    }
                    catch { /* try next candidate */ }
                }
                _cachedServer = @".\SQLEXPRESS"; // original default if nothing responds
                return _cachedServer;
            }
        }

        private static string ConnectionString =>
            $@"Server={Server};Database=SmogShield_Pakistan;Trusted_Connection=True;TrustServerCertificate=True;";

        private static string MasterConnectionString =>
            $@"Server={Server};Trusted_Connection=True;TrustServerCertificate=True;";

        public static void Initialize()
        {
            try
            {
                using var master = new SqlConnection(MasterConnectionString);
                master.Open();
                using var cmd = master.CreateCommand();
                cmd.CommandText = "IF DB_ID('SmogShield_Pakistan') IS NULL CREATE DATABASE SmogShield_Pakistan;";
                cmd.ExecuteNonQuery();
            }
            catch { return; }

            try
            {
                using var conn = new SqlConnection(ConnectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    IF OBJECT_ID('dbo.VisitorLogs') IS NULL
                        CREATE TABLE dbo.VisitorLogs (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            Name NVARCHAR(100) NOT NULL,
                            Email NVARCHAR(255) NOT NULL DEFAULT '',
                            Page NVARCHAR(50) NOT NULL,
                            Details NVARCHAR(255) NULL,
                            VisitCount INT NOT NULL DEFAULT 1,
                            VisitedAt DATETIME NOT NULL DEFAULT GETDATE());

                    IF COL_LENGTH('dbo.VisitorLogs', 'Email') IS NULL
                        ALTER TABLE dbo.VisitorLogs ADD Email NVARCHAR(255) NOT NULL DEFAULT '';

                    IF COL_LENGTH('dbo.VisitorLogs', 'VisitCount') IS NULL
                        ALTER TABLE dbo.VisitorLogs ADD VisitCount INT NOT NULL DEFAULT 1;

                    IF OBJECT_ID('dbo.AirReadings') IS NULL
                        CREATE TABLE dbo.AirReadings (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            Code NVARCHAR(20) NOT NULL,
                            City NVARCHAR(100) NOT NULL,
                            Station NVARCHAR(150) NULL,
                            Pm25 FLOAT NULL,
                            Pm10 FLOAT NULL,
                            Aqi FLOAT NULL,
                            Source NVARCHAR(50) NOT NULL,
                            ObservedAt DATETIME NOT NULL);";
                cmd.ExecuteNonQuery();

                EnsureProcedures(conn);
            }
            catch { }
        }

        private static void EnsureProcedures(SqlConnection conn)
        {
            string[] procs =
            {
                @"CREATE OR ALTER PROCEDURE dbo.sp_InsertVisitor
                    @Name NVARCHAR(100), @Email NVARCHAR(255), @Page NVARCHAR(50), @Details NVARCHAR(255)
                AS
                BEGIN
                    SET NOCOUNT ON;
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
                END",
                @"CREATE OR ALTER PROCEDURE dbo.sp_GetVisitors
                    @Limit INT = 50
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT TOP (@Limit) Id, Name, Page, Details, VisitedAt, Email, VisitCount
                    FROM dbo.VisitorLogs
                    ORDER BY VisitedAt DESC;
                END",
                @"CREATE OR ALTER PROCEDURE dbo.sp_GetVisitorCount
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT ISNULL(SUM(VisitCount), 0) FROM dbo.VisitorLogs;
                END",
                @"CREATE OR ALTER PROCEDURE dbo.sp_DeleteVisitor
                    @Id INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    DELETE FROM dbo.VisitorLogs WHERE Id = @Id;
                END",
                @"CREATE OR ALTER PROCEDURE dbo.sp_GetLatestReadings
                    @Limit INT = 50
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT TOP (@Limit) Id, Code, City, Station, Pm25, Pm10, Aqi, Source, ObservedAt
                    FROM dbo.AirReadings
                    ORDER BY ObservedAt DESC;
                END",
                @"CREATE OR ALTER PROCEDURE dbo.sp_GetCityAverages
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
                END"
            };

            foreach (var sql in procs)
            {
                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }
                catch { }
            }
        }

        private static SqlConnection Open() => new SqlConnection(ConnectionString);

        public static void InsertVisitor(string name, string email, string page, string details = "")
        {
            email = email?.Trim() ?? "";
            try
            {
                using var conn = Open();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "sp_InsertVisitor";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Page", page);
                cmd.Parameters.AddWithValue("@Details", details);
                cmd.ExecuteNonQuery();
            }
            catch
            {
                try
                {
                    using var conn = Open();
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        IF LEN(@e) > 0 AND EXISTS (SELECT 1 FROM dbo.VisitorLogs WHERE Email = @e)
                            UPDATE dbo.VisitorLogs
                            SET VisitCount = VisitCount + 1, VisitedAt = GETDATE(),
                                Name = @n, Page = @p, Details = @d
                            WHERE Email = @e
                        ELSE
                            INSERT INTO dbo.VisitorLogs (Name, Email, Page, Details, VisitCount, VisitedAt)
                            VALUES (@n, @e, @p, @d, 1, GETDATE())";
                    cmd.Parameters.AddWithValue("@n", name);
                    cmd.Parameters.AddWithValue("@e", email);
                    cmd.Parameters.AddWithValue("@p", page);
                    cmd.Parameters.AddWithValue("@d", details);
                    cmd.ExecuteNonQuery();
                }
                catch { }
            }
        }

        public static List<VisitorLog> GetVisitors(int limit = 50)
        {
            var result = new List<VisitorLog>();
            try
            {
                using var conn = Open();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "sp_GetVisitors";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Limit", limit);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                    result.Add(ReadVisitor(reader));
            }
            catch
            {
                try
                {
                    using var conn = Open();
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText =
                        "SELECT TOP (@lim) Id, Name, Page, Details, VisitedAt, Email, VisitCount FROM dbo.VisitorLogs ORDER BY VisitedAt DESC";
                    cmd.Parameters.AddWithValue("@lim", limit);
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                        result.Add(ReadVisitor(reader));
                }
                catch { }
            }
            return result;
        }

        private static VisitorLog ReadVisitor(Microsoft.Data.SqlClient.SqlDataReader reader) => new()
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Page = reader.GetString(2),
            Details = reader.IsDBNull(3) ? "" : reader.GetString(3),
            VisitedAt = reader.GetDateTime(4),
            Email = reader.FieldCount > 5 && !reader.IsDBNull(5) ? reader.GetString(5) : "",
            VisitCount = reader.FieldCount > 6 && !reader.IsDBNull(6) ? reader.GetInt32(6) : 1
        };

        public static int GetVisitorCount()
        {
            try
            {
                using var conn = Open();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "sp_GetVisitorCount";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch
            {
                try
                {
                    using var conn = Open();
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT COUNT(*) FROM dbo.VisitorLogs";
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
                catch { return 0; }
            }
        }

        public static void DeleteVisitor(int id)
        {
            try
            {
                using var conn = Open();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "sp_DeleteVisitor";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();
            }
            catch
            {
                try
                {
                    using var conn = Open();
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "DELETE FROM dbo.VisitorLogs WHERE Id = @id";
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
                catch { }
            }
        }

        public static void SaveReadings(IReadOnlyList<Reading> readings)
        {
            try
            {
                using var conn = Open();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    IF NOT EXISTS (SELECT 1 FROM dbo.AirReadings WHERE Code = @c AND ObservedAt = @obs)
                    INSERT INTO dbo.AirReadings (Code, City, Station, Pm25, Pm10, Aqi, Source, ObservedAt)
                    VALUES (@c, @ct, @s, @p25, @p10, @aq, @src, @obs)";
                var pCode = cmd.Parameters.Add("@c", System.Data.SqlDbType.NVarChar, 20);
                var pCity = cmd.Parameters.Add("@ct", System.Data.SqlDbType.NVarChar, 100);
                var pStation = cmd.Parameters.Add("@s", System.Data.SqlDbType.NVarChar, 150);
                var pPm25 = cmd.Parameters.Add("@p25", System.Data.SqlDbType.Float);
                var pPm10 = cmd.Parameters.Add("@p10", System.Data.SqlDbType.Float);
                var pAqi = cmd.Parameters.Add("@aq", System.Data.SqlDbType.Float);
                var pSrc = cmd.Parameters.Add("@src", System.Data.SqlDbType.NVarChar, 50);
                var pObs = cmd.Parameters.Add("@obs", System.Data.SqlDbType.DateTime);

                foreach (var r in readings)
                {
                    pCode.Value = r.Code;
                    pCity.Value = r.City;
                    pStation.Value = r.Station ?? "";
                    pPm25.Value = r.Pm25;
                    pPm10.Value = r.Pm10;
                    pAqi.Value = r.Aqi;
                    pSrc.Value = r.Source;
                    pObs.Value = r.ObservedAt;
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        public static List<Reading> GetLatestReadings(int limit = 50)
        {
            var result = new List<Reading>();
            try
            {
                using var conn = Open();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "sp_GetLatestReadings";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Limit", limit);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    result.Add(new Reading
                    {
                        Code = reader.GetString(1),
                        City = reader.GetString(2),
                        Station = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        Pm25 = reader.IsDBNull(4) ? 0 : reader.GetDouble(4),
                        Pm10 = reader.IsDBNull(5) ? 0 : reader.GetDouble(5),
                        Aqi = reader.IsDBNull(6) ? 0 : reader.GetDouble(6),
                        Source = reader.GetString(7),
                        ObservedAt = reader.GetDateTime(8)
                    });
                }
            }
            catch { }
            return result;
        }
    }
}

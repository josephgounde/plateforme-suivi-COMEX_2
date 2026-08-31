USE [master]
GO
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'healthMonitorLogin')
BEGIN
    CREATE LOGIN [healthMonitorLogin] WITH PASSWORD = N'HealthMon_2026!Pwd';
END
GO
GRANT ALTER ANY AVAILABILITY GROUP TO [healthMonitorLogin];
GO
GRANT VIEW SERVER STATE TO [healthMonitorLogin];
GO

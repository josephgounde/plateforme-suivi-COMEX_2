SELECT name, state_desc FROM sys.databases WHERE name = 'PDOE_DB';
GO
SELECT COUNT(*) AS row_count FROM PDOE_DB.dbo.HaTestRows;
GO

-- Run on the CURRENT PRIMARY (pdoe-b)
BACKUP DATABASE PDOE_DB
    TO DISK = '/var/opt/mssql/backup/PDOE_DB_seed_c.bak'
    WITH INIT;
GO

BACKUP LOG PDOE_DB
    TO DISK = '/var/opt/mssql/backup/PDOE_DB_seed_c.trn'
    WITH INIT;
GO

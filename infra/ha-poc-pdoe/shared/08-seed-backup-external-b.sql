BACKUP DATABASE PDOE_DB
    TO DISK = '/var/opt/mssql/backup/PDOE_DB_external_full.bak'
    WITH INIT;
GO

BACKUP LOG PDOE_DB
    TO DISK = '/var/opt/mssql/backup/PDOE_DB_external_log.trn'
    WITH INIT;
GO

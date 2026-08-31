-- Run on pdoe-c
RESTORE DATABASE PDOE_DB
    FROM DISK = '/var/opt/mssql/backup/PDOE_DB_seed_c.bak'
    WITH MOVE 'PDOE_DB' TO '/var/opt/mssql/data/PDOE_DB.mdf',
         MOVE 'PDOE_DB_log' TO '/var/opt/mssql/data/PDOE_DB_log.ldf',
         NORECOVERY;
GO

RESTORE LOG PDOE_DB
    FROM DISK = '/var/opt/mssql/backup/PDOE_DB_seed_c.trn'
    WITH NORECOVERY;
GO

ALTER AVAILABILITY GROUP [pdoe_ag] JOIN WITH (CLUSTER_TYPE = EXTERNAL);
GO

ALTER DATABASE PDOE_DB SET HADR AVAILABILITY GROUP = [pdoe_ag];
GO

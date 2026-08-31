RESTORE DATABASE PDOE_DB
    FROM DISK = '/var/opt/mssql/backup/PDOE_DB_external_full.bak'
    WITH MOVE 'PDOE_DB' TO '/var/opt/mssql/data/PDOE_DB.mdf',
         MOVE 'PDOE_DB_log' TO '/var/opt/mssql/data/PDOE_DB_log.ldf',
         NORECOVERY;
GO

RESTORE LOG PDOE_DB
    FROM DISK = '/var/opt/mssql/backup/PDOE_DB_external_log.trn'
    WITH NORECOVERY;
GO

ALTER AVAILABILITY GROUP [pdoe_ag] JOIN WITH (CLUSTER_TYPE = EXTERNAL);
GO

ALTER DATABASE PDOE_DB SET HADR AVAILABILITY GROUP = [pdoe_ag];
GO

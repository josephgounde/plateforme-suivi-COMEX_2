CREATE MASTER KEY ENCRYPTION BY PASSWORD = 'PdoeMasterKey_2026!';
GO

CREATE CERTIFICATE dbm_certificate_c
    WITH SUBJECT = 'dbm_certificate_c';
GO

BACKUP CERTIFICATE dbm_certificate_c
    TO FILE = '/var/opt/mssql/backup/dbm_certificate_c.cer'
    WITH PRIVATE KEY (
        FILE = '/var/opt/mssql/backup/dbm_certificate_c.pvk',
        ENCRYPTION BY PASSWORD = 'PdoeCertPk_2026!'
    );
GO

CREATE ENDPOINT [Hadr_endpoint]
    AS TCP (LISTENER_PORT = 5022)
    FOR DATABASE_MIRRORING (
        ROLE = ALL,
        AUTHENTICATION = CERTIFICATE dbm_certificate_c,
        ENCRYPTION = REQUIRED ALGORITHM AES
    );
GO

ALTER ENDPOINT [Hadr_endpoint] STATE = STARTED;
GO

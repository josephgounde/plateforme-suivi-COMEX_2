CREATE MASTER KEY ENCRYPTION BY PASSWORD = 'PdoeMasterKey_2026!';
GO

CREATE CERTIFICATE dbm_certificate_b
    WITH SUBJECT = 'dbm_certificate_b';
GO

BACKUP CERTIFICATE dbm_certificate_b
    TO FILE = '/var/opt/mssql/backup/dbm_certificate_b.cer'
    WITH PRIVATE KEY (
        FILE = '/var/opt/mssql/backup/dbm_certificate_b.pvk',
        ENCRYPTION BY PASSWORD = 'PdoeCertPk_2026!'
    );
GO

CREATE ENDPOINT [Hadr_endpoint]
    AS TCP (LISTENER_PORT = 5022)
    FOR DATABASE_MIRRORING (
        ROLE = ALL,
        AUTHENTICATION = CERTIFICATE dbm_certificate_b,
        ENCRYPTION = REQUIRED ALGORITHM AES
    );
GO

ALTER ENDPOINT [Hadr_endpoint] STATE = STARTED;
GO

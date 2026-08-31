-- Run on pdoe-db-b: trust pdoe-db-a's certificate
CREATE LOGIN [dbm_login_a] WITH PASSWORD = 'PdoeDbmLogin_2026!';
GO

CREATE USER [dbm_user_a] FOR LOGIN [dbm_login_a];
GO

CREATE CERTIFICATE dbm_certificate_a
    AUTHORIZATION [dbm_user_a]
    FROM FILE = '/var/opt/mssql/backup/dbm_certificate_a.cer'
    WITH PRIVATE KEY (
        FILE = '/var/opt/mssql/backup/dbm_certificate_a.pvk',
        DECRYPTION BY PASSWORD = 'PdoeCertPk_2026!'
    );
GO

GRANT CONNECT ON ENDPOINT::[Hadr_endpoint] TO [dbm_login_a];
GO

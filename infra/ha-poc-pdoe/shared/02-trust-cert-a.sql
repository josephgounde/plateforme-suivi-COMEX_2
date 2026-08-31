-- Run on pdoe-db-a: trust pdoe-db-b's certificate
CREATE LOGIN [dbm_login_b] WITH PASSWORD = 'PdoeDbmLogin_2026!';
GO

CREATE USER [dbm_user_b] FOR LOGIN [dbm_login_b];
GO

CREATE CERTIFICATE dbm_certificate_b
    AUTHORIZATION [dbm_user_b]
    FROM FILE = '/var/opt/mssql/backup/dbm_certificate_b.cer'
    WITH PRIVATE KEY (
        FILE = '/var/opt/mssql/backup/dbm_certificate_b.pvk',
        DECRYPTION BY PASSWORD = 'PdoeCertPk_2026!'
    );
GO

GRANT CONNECT ON ENDPOINT::[Hadr_endpoint] TO [dbm_login_b];
GO

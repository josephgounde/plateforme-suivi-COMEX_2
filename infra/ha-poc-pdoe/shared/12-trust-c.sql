-- Run on BOTH pdoe-a and pdoe-b: trust pdoe-c's certificate
CREATE LOGIN [dbm_login_c] WITH PASSWORD = 'PdoeDbmLogin_2026!';
GO

CREATE USER [dbm_user_c] FOR LOGIN [dbm_login_c];
GO

CREATE CERTIFICATE dbm_certificate_c
    AUTHORIZATION [dbm_user_c]
    FROM FILE = '/var/opt/mssql/backup/dbm_certificate_c.cer'
    WITH PRIVATE KEY (
        FILE = '/var/opt/mssql/backup/dbm_certificate_c.pvk',
        DECRYPTION BY PASSWORD = 'PdoeCertPk_2026!'
    );
GO

GRANT CONNECT ON ENDPOINT::[Hadr_endpoint] TO [dbm_login_c];
GO

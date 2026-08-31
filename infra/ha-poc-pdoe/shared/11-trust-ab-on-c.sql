-- Run on pdoe-c: trust pdoe-a's certificate
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

-- Run on pdoe-c: trust pdoe-b's certificate
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

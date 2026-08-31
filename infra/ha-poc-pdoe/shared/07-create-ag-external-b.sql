CREATE AVAILABILITY GROUP [pdoe_ag]
    WITH (CLUSTER_TYPE = EXTERNAL)
    FOR DATABASE [PDOE_DB]
    REPLICA ON
        'pdoe-a' WITH (
            ENDPOINT_URL = 'TCP://pdoe-a:5022',
            AVAILABILITY_MODE = SYNCHRONOUS_COMMIT,
            FAILOVER_MODE = EXTERNAL,
            SEEDING_MODE = MANUAL
        ),
        'pdoe-b' WITH (
            ENDPOINT_URL = 'TCP://pdoe-b:5022',
            AVAILABILITY_MODE = SYNCHRONOUS_COMMIT,
            FAILOVER_MODE = EXTERNAL,
            SEEDING_MODE = MANUAL
        );
GO

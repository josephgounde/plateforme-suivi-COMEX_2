CREATE AVAILABILITY GROUP [pdoe_ag]
    WITH (CLUSTER_TYPE = NONE)
    FOR DATABASE [PDOE_DB]
    REPLICA ON
        'pdoe-a' WITH (
            ENDPOINT_URL = 'TCP://pdoe-a:5022',
            AVAILABILITY_MODE = SYNCHRONOUS_COMMIT,
            FAILOVER_MODE = MANUAL,
            SEEDING_MODE = MANUAL
        ),
        'pdoe-b' WITH (
            ENDPOINT_URL = 'TCP://pdoe-b:5022',
            AVAILABILITY_MODE = SYNCHRONOUS_COMMIT,
            FAILOVER_MODE = MANUAL,
            SEEDING_MODE = MANUAL
        );
GO
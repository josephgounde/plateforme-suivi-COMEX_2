SELECT
    r.replica_server_name,
    s.role_desc,
    s.connected_state_desc,
    s.synchronization_health_desc
FROM sys.dm_hadr_availability_replica_states AS s
JOIN sys.availability_replicas AS r ON s.replica_id = r.replica_id;
GO

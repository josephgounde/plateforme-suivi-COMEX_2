SELECT
    ars.replica_server_name,
    drs.synchronization_state_desc,
    drs.last_hardened_lsn,
    drs.last_commit_lsn,
    drs.log_send_queue_size,
    drs.redo_queue_size,
    drs.secondary_lag_seconds,
    CASE WHEN drs.synchronization_state_desc = 'SYNCHRONIZED'
         THEN DATEDIFF(SECOND, drs.last_hardened_time, SYSDATETIME())
    END AS synchronized_timespan_s,
    CASE WHEN drs.synchronization_state_desc <> 'SYNCHRONIZED'
         THEN DATEDIFF(SECOND, drs.last_hardened_time, SYSDATETIME())
    END AS desynchronized_timespan_s
FROM sys.dm_hadr_database_replica_states AS drs
JOIN sys.availability_replicas AS ars ON drs.replica_id = ars.replica_id
ORDER BY ars.replica_server_name;
GO

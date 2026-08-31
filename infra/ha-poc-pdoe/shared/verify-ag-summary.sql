-- Vue d'ensemble unique : fusionne l'etat de connectivite (niveau replica)
-- et l'etat de synchronisation (niveau base de donnees) dans une seule table.
-- A executer sur le noeud actuellement PRIMARY pour voir tous les replicas.
SELECT
    ars.replica_server_name,
    drs.synchronization_state_desc,
    CASE WHEN drs.synchronization_state_desc = 'SYNCHRONIZED'
         THEN DATEDIFF(SECOND, drs.last_hardened_time, SYSDATETIME())
    END AS synchronized_timespan_s,
    CASE WHEN drs.synchronization_state_desc <> 'SYNCHRONIZED'
         THEN DATEDIFF(SECOND, drs.last_hardened_time, SYSDATETIME())
    END AS desynchronized_timespan_s,
    ws.role_desc,
    ws.connected_state_desc,
    ws.synchronization_health_desc
FROM sys.availability_replicas AS ars
JOIN sys.dm_hadr_availability_replica_states AS ws ON ws.replica_id = ars.replica_id
LEFT JOIN sys.dm_hadr_database_replica_states AS drs ON drs.replica_id = ars.replica_id
ORDER BY ars.replica_server_name;
GO

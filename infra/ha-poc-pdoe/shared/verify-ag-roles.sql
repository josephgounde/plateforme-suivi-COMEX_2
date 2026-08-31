SELECT r.replica_server_name, s.role_desc
FROM sys.dm_hadr_availability_replica_states AS s
JOIN sys.availability_replicas AS r ON s.replica_id = r.replica_id;
GO

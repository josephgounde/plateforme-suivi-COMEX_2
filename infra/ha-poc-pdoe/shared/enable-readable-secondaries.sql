-- Autorise la connexion et la lecture directe sur les replicas secondaires.
-- A executer UNIQUEMENT sur le noeud actuellement PRIMARY
-- (verifier d'abord avec verify-ag-summary.sql ou verify-ag-roles.sql).
--
-- ALLOW_CONNECTIONS = ALL : n'importe quel client (SSMS, sqlcmd, l'appli) peut
--   se connecter directement au secondaire et lire, sans avoir besoin de
--   ApplicationIntent=ReadOnly dans la chaine de connexion.
-- (Alternative plus restrictive : ALLOW_CONNECTIONS = READ_ONLY, qui exige
--   ApplicationIntent=ReadOnly cote client -- pas necessaire ici.)
ALTER AVAILABILITY GROUP [pdoe_ag]
MODIFY REPLICA ON N'pdoe-a' WITH (SECONDARY_ROLE (ALLOW_CONNECTIONS = ALL));

ALTER AVAILABILITY GROUP [pdoe_ag]
MODIFY REPLICA ON N'pdoe-b' WITH (SECONDARY_ROLE (ALLOW_CONNECTIONS = ALL));

ALTER AVAILABILITY GROUP [pdoe_ag]
MODIFY REPLICA ON N'pdoe-c' WITH (SECONDARY_ROLE (ALLOW_CONNECTIONS = ALL));
GO

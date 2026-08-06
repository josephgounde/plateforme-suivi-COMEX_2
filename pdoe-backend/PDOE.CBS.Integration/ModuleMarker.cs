namespace PDOE.CBS.Integration;

/// Accès ABS2000 en lecture seule (taux, signature, solde). Les Handlers de ce module ne parlent qu'à
/// ICbsClient (PDOE.Infrastructure.Cbs) — bascule mock/réel pilotée par Cbs:BypassValidation dans Program.cs,
/// voir MockCbsClient/HttpCbsClient. Exception : ValiderSignatureVisuelle n'appelle pas ICbsClient — aucun
/// service ABS2000 ne permet de "valider visuellement" une signature, seulement de dire si elle existe. La
/// comparaison se fait par l'agent hors PDOE ; ce handler ne fait qu'enregistrer sa confirmation localement.
public static class ModuleMarker
{
}

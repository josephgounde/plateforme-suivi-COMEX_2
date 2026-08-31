// Résout une variable CSS (ex. '--pdoe-red') vers sa valeur hex/rgb réellement appliquée (thème courant compris).
// Nécessaire pour les bibliothèques tierces (ApexCharts, etc.) qui n'acceptent pas var(--x) directement dans leur
// config JS. Une chaîne qui ne commence pas par '--' est renvoyée telle quelle (compat rétro, couleur littérale).
export function resolveToken(nomOuValeur: string): string {
  if (!nomOuValeur.startsWith('--')) {
    return nomOuValeur;
  }
  const valeur = getComputedStyle(document.documentElement).getPropertyValue(nomOuValeur).trim();
  return valeur || nomOuValeur;
}

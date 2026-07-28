// Déclenche le téléchargement d'un Blob via une ancre <a> jetable, jamais insérée dans le DOM.
export function declencherTelechargement(blob: Blob, nomFichier: string): void {
  const url = URL.createObjectURL(blob);
  const lien = document.createElement('a');
  lien.href = url;
  lien.download = nomFichier;
  lien.click();
  URL.revokeObjectURL(url);
}

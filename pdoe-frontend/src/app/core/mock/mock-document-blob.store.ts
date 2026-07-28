// Garde en mémoire le File des documents uploadés (mock only) pour DocumentPreviewModalComponent.
// Les documents seedés au démarrage n'ont pas de fichier réel, donc pas d'entrée ici.

import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class MockDocumentBlobStore {
  private readonly fichiers = new Map<number, File>();

  enregistrer(documentId: number, fichier: File): void {
    this.fichiers.set(documentId, fichier);
  }

  obtenir(documentId: number): File | undefined {
    return this.fichiers.get(documentId);
  }
}

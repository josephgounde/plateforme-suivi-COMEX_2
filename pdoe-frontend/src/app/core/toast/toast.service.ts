// Toasts de confirmation, affichés globalement via ToastContainerComponent.
// Signal (pas un champ simple) car l'app est zoneless.

import { Injectable, signal } from '@angular/core';

export type ToastType = 'success' | 'error';

export interface Toast {
  id: number;
  type: ToastType;
  message: string;
}

const DUREE_AFFICHAGE_MS = 4000;

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly _toasts = signal<Toast[]>([]);
  readonly toasts = this._toasts.asReadonly();

  private compteur = 0;

  succes(message: string): void {
    this.pousser(message, 'success');
  }

  erreur(message: string): void {
    this.pousser(message, 'error');
  }

  fermer(id: number): void {
    this._toasts.update(list => list.filter(t => t.id !== id));
  }

  private pousser(message: string, type: ToastType): void {
    const id = ++this.compteur;
    this._toasts.update(list => [...list, { id, type, message }]);
    setTimeout(() => this.fermer(id), DUREE_AFFICHAGE_MS);
  }
}

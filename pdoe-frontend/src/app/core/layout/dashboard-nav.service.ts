// Pont entre AppShellComponent et le dashboard routé pour que la sidebar affiche ses items. Appeler clear() au ngOnDestroy.

import { Injectable, signal } from '@angular/core';

export interface DashboardNavItem {
  id: string;
  icone: string;
  libelle: string;
  count?: number;
}

@Injectable({ providedIn: 'root' })
export class DashboardNavService {
  readonly items = signal<DashboardNavItem[]>([]);
  readonly activeId = signal<string | null>(null);

  setItems(items: DashboardNavItem[]): void {
    this.items.set(items);
  }

  select(id: string): void {
    this.activeId.set(id);
  }

  // Ne remet PAS activeId à null : un dashboard démonté doit retrouver son onglet actif au retour, pas l'onglet par défaut.
  clear(): void {
    this.items.set([]);
  }
}

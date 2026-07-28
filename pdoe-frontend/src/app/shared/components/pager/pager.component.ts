// Pagination client : découpe une liste déjà en mémoire, sans requête serveur.

import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-pager',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './pager.component.html',
  styleUrl: './pager.component.scss'
})
export class PagerComponent {
  @Input({ required: true }) total = 0;
  @Input() pageSize = 10;
  @Input() page = 1;
  @Output() pageChange = new EventEmitter<number>();

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.total / this.pageSize));
  }

  get debutIndex(): number {
    return this.total === 0 ? 0 : (this.page - 1) * this.pageSize + 1;
  }

  get finIndex(): number {
    return Math.min(this.page * this.pageSize, this.total);
  }

  // Fenêtre glissante autour de la page courante, 1 et totalPages toujours visibles.
  get pagesAffichees(): (number | '…')[] {
    const total = this.totalPages;
    const courant = this.page;
    if (total <= 7) {
      return Array.from({ length: total }, (_, i) => i + 1);
    }
    const pages = new Set<number>([1, total, courant, courant - 1, courant + 1]);
    const triees = [...pages].filter(p => p >= 1 && p <= total).sort((a, b) => a - b);
    const resultat: (number | '…')[] = [];
    let precedent = 0;
    for (const p of triees) {
      if (precedent && p - precedent > 1) {
        resultat.push('…');
      }
      resultat.push(p);
      precedent = p;
    }
    return resultat;
  }

  allerA(page: number): void {
    if (page < 1 || page > this.totalPages || page === this.page) return;
    this.pageChange.emit(page);
  }
}

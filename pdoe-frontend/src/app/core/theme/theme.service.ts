// Bascule clair/sombre — attribut data-theme sur <html>, lu par styles.scss (:root[data-theme='dark']).
// Préférence explicite persistée dans localStorage ; à défaut, prefers-color-scheme sert uniquement
// de valeur initiale au tout premier chargement (jamais réévaluée ensuite tant qu'un choix existe).

import { Injectable, effect, signal } from '@angular/core';

export type Theme = 'light' | 'dark';

const STORAGE_KEY = 'pdoe_theme';

function themeInitiale(): Theme {
  const stocke = localStorage.getItem(STORAGE_KEY);
  if (stocke === 'light' || stocke === 'dark') {
    return stocke;
  }
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly theme = signal<Theme>(themeInitiale());

  constructor() {
    effect(() => {
      const theme = this.theme();
      document.documentElement.setAttribute('data-theme', theme);
      localStorage.setItem(STORAGE_KEY, theme);
    });
  }

  basculer(): void {
    this.theme.set(this.theme() === 'dark' ? 'light' : 'dark');
  }
}

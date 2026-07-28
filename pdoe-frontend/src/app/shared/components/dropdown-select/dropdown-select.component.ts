// Menu déroulant maison : remplace le <select> natif là où on a besoin de
// styler le survol des options, ce que le rendu OS de <option> ne permet pas.

import { Component, ElementRef, EventEmitter, HostListener, Input, Output, ViewChild, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

export interface DropdownOption {
  value: string;
  label: string;
}

@Component({
  selector: 'app-dropdown-select',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './dropdown-select.component.html',
  styleUrl: './dropdown-select.component.scss',
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => DropdownSelectComponent), multi: true }
  ]
})
export class DropdownSelectComponent implements ControlValueAccessor {
  @Input({ required: true }) options: DropdownOption[] = [];
  @Input() value = '';
  @Input() placeholder = 'Sélectionner…';
  @Input() disabled = false;
  // Reporté sur le bouton-déclencheur, pas l'hôte : <label for> ne cible qu'un élément focusable/labelable.
  @Input() id = '';
  // Opt-in : évite d'ajouter un champ de recherche inutile aux listes courtes (statuts, types…) déjà en place ailleurs.
  @Input() filtrable = false;
  @Output() valueChange = new EventEmitter<string>();

  @ViewChild('filtreInputRef') private filtreInputRef?: ElementRef<HTMLInputElement>;

  ouvert = false;
  filtreTexte = '';

  // Sans ControlValueAccessor, impossible de piloter ce composant via formControlName (formulaires réactifs).
  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  constructor(private elementRef: ElementRef<HTMLElement>) {}

  get libelleActuel(): string {
    // || plutôt que ?? : une valeur vide ('') doit retomber sur le placeholder, pas s'afficher comme un libellé vide.
    return this.options.find(o => o.value === this.value)?.label ?? (this.value || this.placeholder);
  }

  // Insensible à la casse et aux accents : "senegal" doit trouver "Sénégal".
  get optionsFiltrees(): DropdownOption[] {
    if (!this.filtrable || !this.filtreTexte.trim()) return this.options;
    const requete = this.normaliser(this.filtreTexte);
    return this.options.filter(o => this.normaliser(o.label).includes(requete));
  }

  private normaliser(texte: string): string {
    return texte.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();
  }

  // Ferme le menu au clic en dehors du composant — sans ça, il reste
  // ouvert tant qu'on ne choisit pas explicitement une option.
  @HostListener('document:click', ['$event'])
  surClicDocument(event: MouseEvent): void {
    if (this.ouvert && !this.elementRef.nativeElement.contains(event.target as Node)) {
      this.ouvert = false;
      this.onTouched();
    }
  }

  basculer(): void {
    if (this.disabled) return;
    this.ouvert = !this.ouvert;

    if (this.ouvert) {
      this.filtreTexte = '';
      if (this.filtrable) {
        // setTimeout : le *ngIf du champ ne s'est pas encore rendu au moment de cet appel.
        setTimeout(() => this.filtreInputRef?.nativeElement.focus());
      }
    }
  }

  choisir(option: DropdownOption): void {
    this.value = option.value;
    this.valueChange.emit(option.value);
    this.onChange(option.value);
    this.onTouched();
    this.ouvert = false;
    this.filtreTexte = '';
  }

  writeValue(value: string): void {
    this.value = value ?? '';
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }
}

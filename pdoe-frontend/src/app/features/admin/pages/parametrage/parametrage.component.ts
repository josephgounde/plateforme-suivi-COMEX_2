import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ParametrageApiService } from '../../../../core/api/parametrage-api.service';
import { ParametreMetier } from '../../../../core/models/dossier.model';

@Component({
  selector: 'app-parametrage',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './parametrage.component.html',
  styleUrl: './parametrage.component.scss'
})
export class ParametrageComponent implements OnInit {
  chargement = true;
  parametres: ParametreMetier[] = [];
  editCle: string | null = null;
  editValeur = '';
  enregistrement = false;
  succes = '';
  erreur = '';

  constructor(
    private api: ParametrageApiService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.api.list().subscribe({
      next: p => {
        this.parametres = p;
        this.chargement = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.chargement = false;
        this.cdr.detectChanges();
      }
    });
  }

  demarrerEdition(p: ParametreMetier): void {
    if (!p.modifiableUI) return;
    this.editCle = p.cle;
    this.editValeur = p.valeur;
    this.erreur = '';
    this.succes = '';
  }

  annuler(): void {
    this.editCle = null;
    this.editValeur = '';
  }

  enregistrer(p: ParametreMetier): void {
    if (!this.valide(p)) return;
    this.enregistrement = true;
    this.api.update(p.cle, this.editValeur).subscribe({
      next: maj => {
        const idx = this.parametres.findIndex(x => x.cle === p.cle);
        if (idx >= 0) this.parametres[idx] = maj;
        this.editCle = null;
        this.enregistrement = false;
        this.succes = `${p.cle} mis à jour.`;
        this.cdr.detectChanges();
        setTimeout(() => {
          this.succes = '';
          this.cdr.detectChanges();
        }, 3000);
      },
      error: () => {
        this.enregistrement = false;
        this.erreur = 'Échec de l\'enregistrement.';
        this.cdr.detectChanges();
      }
    });
  }

  valide(p: ParametreMetier): boolean {
    if (!this.editValeur.trim()) return false;
    const v = Number(this.editValeur);
    if (p.valeurMin && v < Number(p.valeurMin)) return false;
    if (p.valeurMax && v > Number(p.valeurMax)) return false;
    return true;
  }
}
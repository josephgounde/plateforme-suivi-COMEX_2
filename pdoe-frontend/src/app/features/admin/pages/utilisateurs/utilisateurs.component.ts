// Annuaire local des comptes PDOE (LDAP vérifie le compte, pas le profil applicatif). Pas de suppression,
// seulement désactivation : l'historique des dossiers traités doit rester résoluble.

import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { UtilisateurApiService } from '../../../../core/api/utilisateur-api.service';
import { Utilisateur, CreerUtilisateurRequest, ModifierUtilisateurRequest } from '../../../../core/models/dossier.model';
import { ProfilUtilisateur, PROFIL_LABELS } from '../../../../core/models/enums.model';
import { AuthService } from '../../../../core/auth/auth.service';
import { DropdownSelectComponent, DropdownOption } from '../../../../shared/components/dropdown-select/dropdown-select.component';

interface NouvelUtilisateur {
  loginAD: string;
  nom: string;
  prenom: string;
  email: string;
  profil: ProfilUtilisateur;
}

const NOUVEL_UTILISATEUR_VIDE: NouvelUtilisateur = {
  loginAD: '', nom: '', prenom: '', email: '', profil: ProfilUtilisateur.AGENT_ACCUEIL
};

interface IdentiteEdit {
  nom: string;
  prenom: string;
  email: string;
}

@Component({
  selector: 'app-utilisateurs',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, DropdownSelectComponent],
  templateUrl: './utilisateurs.component.html',
  styleUrl: './utilisateurs.component.scss'
})
export class UtilisateursComponent implements OnInit {
  chargement = true;
  utilisateurs: Utilisateur[] = [];
  succes = '';
  erreur = '';

  readonly profils = Object.values(ProfilUtilisateur);
  readonly profilLabels = PROFIL_LABELS;
  readonly profilOptions: DropdownOption[] = this.profils.map(p => ({ value: p, label: this.profilLabels[p] }));

  // Formulaire de création — affiché/masqué via afficherFormulaire.
  afficherFormulaire = false;
  nouvel: NouvelUtilisateur = { ...NOUVEL_UTILISATEUR_VIDE };
  creationEnCours = false;
  creationErreur = '';

  // Édition inline nom/prénom/email — une seule ligne éditable à la
  // fois (editIdentiteId), même gabarit que ParametrageComponent.
  editIdentiteId: number | null = null;
  editIdentite: IdentiteEdit = { nom: '', prenom: '', email: '' };

  private enregistrementsEnCours = new Set<number>();

  constructor(
    private api: UtilisateurApiService,
    private auth: AuthService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.charger();
  }

  private charger(): void {
    this.chargement = true;
    this.api.list().subscribe({
      next: liste => {
        this.utilisateurs = liste;
        this.chargement = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.chargement = false;
        this.cdr.detectChanges();
      }
    });
  }

  estSoiMeme(u: Utilisateur): boolean {
    return u.loginAD === this.auth.currentUser?.login;
  }

  enregistrementEnCours(id: number): boolean {
    return this.enregistrementsEnCours.has(id);
  }

  changerProfil(u: Utilisateur, profil: string): void {
    if (profil === u.profil) return;
    this.appliquer(u, { profil: profil as ProfilUtilisateur });
  }

  basculerActivation(u: Utilisateur): void {
    this.appliquer(u, { estActif: !u.estActif });
  }

  // ── Édition inline nom/prénom/email ──────────────────────────

  demarrerEditionIdentite(u: Utilisateur): void {
    this.editIdentiteId = u.utilisateurId;
    this.editIdentite = { nom: u.nom, prenom: u.prenom, email: u.email };
    this.erreur = '';
  }

  annulerEditionIdentite(): void {
    this.editIdentiteId = null;
  }

  identiteValide(): boolean {
    return !!(this.editIdentite.nom.trim() && this.editIdentite.prenom.trim() && this.editIdentite.email.trim());
  }

  enregistrerIdentite(u: Utilisateur): void {
    if (!this.identiteValide()) return;
    this.appliquer(u, { ...this.editIdentite });
  }

  private appliquer(u: Utilisateur, patch: ModifierUtilisateurRequest): void {
    // Garde de ré-entrance : [disabled] ne se reflète qu'au prochain cycle CD, un double-clic avant pourrait doubler la requête.
    if (this.enregistrementsEnCours.has(u.utilisateurId)) return;

    this.enregistrementsEnCours.add(u.utilisateurId);
    this.erreur = '';
    this.api.modifier(u.utilisateurId, patch).subscribe({
      next: maj => {
        const idx = this.utilisateurs.findIndex(x => x.utilisateurId === u.utilisateurId);
        if (idx >= 0) this.utilisateurs[idx] = maj;
        this.enregistrementsEnCours.delete(u.utilisateurId);
        if (this.editIdentiteId === u.utilisateurId) this.editIdentiteId = null;
        this.succes = `${maj.prenom} ${maj.nom} mis à jour.`;
        this.cdr.detectChanges();
        setTimeout(() => { this.succes = ''; this.cdr.detectChanges(); }, 3000);
      },
      error: () => {
        this.enregistrementsEnCours.delete(u.utilisateurId);
        this.erreur = 'Échec de la mise à jour — action non autorisée ou compte introuvable.';
        this.cdr.detectChanges();
      }
    });
  }

  ouvrirFormulaire(): void {
    this.afficherFormulaire = true;
    this.nouvel = { ...NOUVEL_UTILISATEUR_VIDE };
    this.creationErreur = '';
  }

  annulerCreation(): void {
    this.afficherFormulaire = false;
  }

  creationValide(): boolean {
    return !!(this.nouvel.loginAD.trim() && this.nouvel.nom.trim() && this.nouvel.prenom.trim() && this.nouvel.email.trim());
  }

  creer(): void {
    // Même garde de ré-entrance que appliquer() — évite une double création avant que [disabled] soit rendu.
    if (!this.creationValide() || this.creationEnCours) return;

    this.creationEnCours = true;
    this.creationErreur = '';
    const req: CreerUtilisateurRequest = { ...this.nouvel };

    this.api.creer(req).subscribe({
      next: cree => {
        this.utilisateurs = [...this.utilisateurs, cree];
        this.creationEnCours = false;
        this.afficherFormulaire = false;
        this.succes = `Compte ${cree.loginAD} créé.`;
        this.cdr.detectChanges();
        setTimeout(() => { this.succes = ''; this.cdr.detectChanges(); }, 3000);
      },
      error: err => {
        this.creationEnCours = false;
        this.creationErreur = err?.status === 409
          ? 'Ce login AD est déjà utilisé.'
          : 'Échec de la création — vérifiez les champs saisis.';
        this.cdr.detectChanges();
      }
    });
  }
}

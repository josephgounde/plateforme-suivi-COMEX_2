// Coquille post-authentification : barre latérale + topbar. Sidebar unique pour tous les profils (Admin/Super Admin ont un sous-menu).

import { Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router, RouterModule } from '@angular/router';
import { Subscription, filter } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { ProfilUtilisateur, PROFIL_LABELS } from '../models/enums.model';
import { NotificationsService } from '../notifications/notifications.service';
import { ThemeService } from '../theme/theme.service';
import { DashboardNavService } from './dashboard-nav.service';
import { ToastContainerComponent } from '../../shared/components/toast-container/toast-container.component';

interface NavItem {
  route: string;
  queryParams?: Record<string, string>;
  icone: string;
  libelle: string;
}

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterModule, ToastContainerComponent],
  templateUrl: './app-shell.component.html',
  styleUrl: './app-shell.component.scss'
})
export class AppShellComponent implements OnInit, OnDestroy {
  clocheOuverte = false;

  // Tiroir mobile — sans effet au-delà de 700px (cf. .scss), où la
  // barre latérale reste toujours dépliée.
  sidebarOuverte = false;

  // Scroll réel sur .shell-content, pas window — scrollPositionRestoration du router n'a donc aucun effet ici.
  @ViewChild('shellContent') private shellContent?: ElementRef<HTMLElement>;
  private navigationSub?: Subscription;

  constructor(
    public auth: AuthService,
    public notifs: NotificationsService,
    public theme: ThemeService,
    public dashboardNav: DashboardNavService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.notifs.charger();
    this.navigationSub = this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe(() => {
        if (this.shellContent) {
          this.shellContent.nativeElement.scrollTop = 0;
        }
      });
  }

  ngOnDestroy(): void {
    this.navigationSub?.unsubscribe();
  }

  get profilLabel(): string {
    const profil = this.auth.profil as ProfilUtilisateur;
    return PROFIL_LABELS[profil] ?? '';
  }

  // Année courante plutôt qu'une valeur figée dans le template — évite
  // d'avoir à revenir modifier ce fichier chaque 1er janvier.
  readonly anneeCourante = new Date().getFullYear();

  get initiales(): string {
    const nom = this.auth.currentUser?.nomComplet ?? '';
    return nom
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map(mot => mot[0])
      .join('')
      .toUpperCase();
  }

  // Une seule entrée pour les 5 profils métier (chacun n'a qu'un dashboard). "Journal d'audit" réservé au Super Admin.
  get navItems(): NavItem[] {
    const profil = this.auth.profil as ProfilUtilisateur;

    switch (profil) {
      case ProfilUtilisateur.AGENT_ACCUEIL:
        return [{ route: '/dashboard', icone: '📁', libelle: 'Mes dossiers' }];
      case ProfilUtilisateur.GESTIONNAIRE:
        return [{ route: '/dashboard', icone: '📥', libelle: "File d'attente" }];
      case ProfilUtilisateur.AGENT_COMEX:
        return [{ route: '/dashboard', icone: '🔍', libelle: 'Tableau de bord' }];
      case ProfilUtilisateur.TRESORERIE:
        return [{ route: '/dashboard', icone: '💱', libelle: 'Avis Trésorerie' }];
      case ProfilUtilisateur.DIRECTION:
        return [{ route: '/dashboard', icone: '📊', libelle: 'Supervision COMEX' }];
      case ProfilUtilisateur.ADMIN_DSIRI:
      case ProfilUtilisateur.SUPER_ADMIN: {
        const items: NavItem[] = [
          { route: '/dashboard', icone: '📊', libelle: "Vue d'ensemble" },
          { route: '/dashboard', queryParams: { vue: 'dossiers' }, icone: '📁', libelle: 'Tous les dossiers' },
          { route: '/admin/parametrage', icone: '⚙', libelle: 'Paramétrage système' },
          { route: '/admin/logs-notifications', icone: '📋', libelle: 'Logs de notifications' },
          { route: '/admin/notification-templates', icone: '✉', libelle: 'Modèles de notification' }
        ];
        if (profil === ProfilUtilisateur.SUPER_ADMIN) {
          items.push(
            { route: '/admin/journal-audit', icone: '🛡', libelle: "Journal d'audit" },
            { route: '/reporting', icone: '🗂', libelle: 'Journal des exports' }
          );
        }
        items.push(
          { route: '/admin/utilisateurs', icone: '👤', libelle: 'Gestion des utilisateurs' },
          { route: '/admin/workflow-etapes', icone: '🔀', libelle: 'Étapes du circuit' },
          { route: '/admin/checklist-config', icone: '☑', libelle: "Checklist d'apurement" },
          { route: '/admin/etapes-generiques', icone: '🧩', libelle: 'Dossiers — étapes personnalisées' }
        );
        return items;
      }
      default:
        return [];
    }
  }
}

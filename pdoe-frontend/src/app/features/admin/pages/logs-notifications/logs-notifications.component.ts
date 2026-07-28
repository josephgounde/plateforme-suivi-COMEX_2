// Journal d'envoi SMS/Email (dbo.Notifications) — à ne pas confondre avec
// le journal d'audit admin/sécurité (écran séparé).

import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { DossierApiService } from '../../../../core/api/dossier-api.service';
import { Notification } from '../../../../core/models/dossier.model';
import { StatutNotification } from '../../../../core/models/enums.model';
import { DropdownSelectComponent, DropdownOption } from '../../../../shared/components/dropdown-select/dropdown-select.component';

@Component({
  selector: 'app-logs-notifications',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, DropdownSelectComponent],
  templateUrl: './logs-notifications.component.html',
  styleUrl: './logs-notifications.component.scss'
})
export class LogsNotificationsComponent implements OnInit {
  chargement = true;
  erreur = '';
  notifications: Notification[] = [];
  filtreStatut: StatutNotification | 'TOUS' = 'TOUS';

  readonly StatutNotification = StatutNotification;

  readonly statutOptions: DropdownOption[] = [
    { value: 'TOUS', label: 'Tous les statuts' },
    { value: StatutNotification.ENVOYE, label: 'Envoyé' },
    { value: StatutNotification.EN_ATTENTE, label: 'En attente' },
    { value: StatutNotification.ECHEC, label: 'Échec' },
    { value: StatutNotification.ECHEC_DEFINITIF, label: 'Échec définitif' }
  ];

  constructor(
    private api: DossierApiService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.charger();
  }

  private charger(): void {
    this.chargement = true;
    this.api.getNotifications().subscribe({
      next: n => {
        this.notifications = n.slice().sort((a, b) => (b.dateEnvoi ?? '').localeCompare(a.dateEnvoi ?? ''));
        this.chargement = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.erreur = 'Échec du chargement des notifications.';
        this.chargement = false;
        this.cdr.detectChanges();
      }
    });
  }

  get notificationsFiltrees(): Notification[] {
    if (this.filtreStatut === 'TOUS') return this.notifications;
    return this.notifications.filter(n => n.statut === this.filtreStatut);
  }
}

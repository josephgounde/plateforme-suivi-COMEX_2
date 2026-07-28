// Personnalisation du libellé et du message de chaque type de notification,
// sans déploiement de code.

import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { NotificationTemplateApiService } from '../../../../core/api/notification-template-api.service';
import { NotificationTemplate } from '../../../../core/models/dossier.model';
import { CanalNotification } from '../../../../core/models/enums.model';
import { DropdownSelectComponent, DropdownOption } from '../../../../shared/components/dropdown-select/dropdown-select.component';

@Component({
  selector: 'app-notification-templates',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, DropdownSelectComponent],
  templateUrl: './notification-templates.component.html',
  styleUrl: './notification-templates.component.scss'
})
export class NotificationTemplatesComponent implements OnInit {
  chargement = true;
  templates: NotificationTemplate[] = [];
  editType: string | null = null;
  editLibelle = '';
  editMessage = '';
  editCanal: CanalNotification = CanalNotification.EMAIL;
  enregistrement = false;
  succes = '';
  erreur = '';

  readonly CanalNotification = CanalNotification;
  readonly canalOptions: DropdownOption[] = [
    { value: CanalNotification.EMAIL, label: 'Email' },
    { value: CanalNotification.SMS, label: 'SMS' },
    { value: CanalNotification.SMS_ET_EMAIL, label: 'SMS + Email' }
  ];

  constructor(
    private api: NotificationTemplateApiService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.api.list().subscribe({
      next: t => {
        this.templates = t;
        this.chargement = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.chargement = false;
        this.cdr.detectChanges();
      }
    });
  }

  demarrerEdition(t: NotificationTemplate): void {
    this.editType = t.typeEvenement;
    this.editLibelle = t.libelle;
    this.editMessage = t.message;
    this.editCanal = t.canalDefaut;
    this.erreur = '';
    this.succes = '';
  }

  annuler(): void {
    this.editType = null;
  }

  valide(): boolean {
    return this.editLibelle.trim().length > 0 && this.editMessage.trim().length > 0;
  }

  enregistrer(t: NotificationTemplate): void {
    if (!this.valide()) return;
    this.enregistrement = true;
    this.api
      .modifier(t.typeEvenement, {
        libelle: this.editLibelle.trim(),
        message: this.editMessage.trim(),
        canalDefaut: this.editCanal
      })
      .subscribe({
        next: maj => {
          const idx = this.templates.findIndex(x => x.typeEvenement === t.typeEvenement);
          if (idx >= 0) this.templates[idx] = maj;
          this.editType = null;
          this.enregistrement = false;
          this.succes = `${maj.typeEvenement} mis à jour.`;
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
}

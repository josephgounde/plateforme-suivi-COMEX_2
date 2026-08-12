// Aperçu d'un document joint, ouvert depuis DossierDetailComponent. Récupère le contenu réel via
// GET /dossiers/{dossierId}/documents/{documentId}/fichier (voir DossierApiService).

import { ChangeDetectorRef, Component, EventEmitter, HostListener, Input, OnChanges, OnDestroy, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { Subscription } from 'rxjs';
import { Document as PdoeDocument } from '../../../core/models/dossier.model';
import { TYPE_DOCUMENT_LABELS } from '../../../core/models/enums.model';
import { DossierApiService } from '../../../core/api/dossier-api.service';
import { declencherTelechargement } from '../../utils/telechargement.util';

type NatureApercu = 'image' | 'pdf' | 'inconnu';

@Component({
  selector: 'app-document-preview-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './document-preview-modal.component.html',
  styleUrl: './document-preview-modal.component.scss'
})
export class DocumentPreviewModalComponent implements OnChanges, OnDestroy {
  @Input({ required: true }) dossierId!: number;
  @Input({ required: true }) document!: PdoeDocument;
  @Output() fermer = new EventEmitter<void>();

  readonly typeDocumentLabels = TYPE_DOCUMENT_LABELS;

  chargement = false;
  erreur = '';
  fichier: Blob | null = null;
  nature: NatureApercu = 'inconnu';
  objectUrl: string | null = null;
  objectUrlSafe: SafeResourceUrl | null = null;

  private chargementEnCours?: Subscription;

  constructor(
    private dossierApi: DossierApiService,
    private sanitizer: DomSanitizer,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnChanges(): void {
    this.revoquerUrl();
    this.chargementEnCours?.unsubscribe();
    this.fichier = null;
    this.nature = 'inconnu';
    this.erreur = '';
    this.chargement = true;

    this.chargementEnCours = this.dossierApi
      .telechargerFichierDocument(this.dossierId, this.document.documentId)
      .subscribe({
        next: blob => {
          this.chargement = false;
          this.fichier = blob;
          this.objectUrl = URL.createObjectURL(blob);
          this.objectUrlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(this.objectUrl);
          this.nature = this.deduireNature(blob.type);
          // Zoneless (pas de zone.js) : la complétion de la requête HTTP en dehors d'un cycle
          // de détection déclenché par un binding ne rafraîchit pas la vue toute seule.
          this.cdr.detectChanges();
        },
        error: () => {
          this.chargement = false;
          this.erreur = "Impossible de charger ce document (fichier absent ou indisponible sur le serveur).";
          this.cdr.detectChanges();
        }
      });
  }

  ngOnDestroy(): void {
    this.revoquerUrl();
    this.chargementEnCours?.unsubscribe();
  }

  @HostListener('document:keydown.escape')
  surEchap(): void {
    this.fermer.emit();
  }

  telecharger(): void {
    if (this.fichier) {
      declencherTelechargement(this.fichier, this.document.nomFichier);
    }
  }

  formaterTaille(octets: number): string {
    if (octets < 1024) return `${octets} o`;
    if (octets < 1024 * 1024) return `${(octets / 1024).toFixed(0)} Ko`;
    return `${(octets / (1024 * 1024)).toFixed(1)} Mo`;
  }

  // Le Content-Type renvoyé par le serveur (déduit de l'extension côté DocumentsController) prime ;
  // repli sur l'extension du nom de fichier au cas où le blob nous revient sans type.
  private deduireNature(mimeType: string): NatureApercu {
    if (mimeType.startsWith('image/')) return 'image';
    if (mimeType === 'application/pdf') return 'pdf';

    const extension = this.document.nomFichier.split('.').pop()?.toLowerCase();
    if (extension === 'pdf') return 'pdf';
    if (extension && ['png', 'jpg', 'jpeg', 'gif', 'webp'].includes(extension)) return 'image';
    return 'inconnu';
  }

  private revoquerUrl(): void {
    if (this.objectUrl) {
      URL.revokeObjectURL(this.objectUrl);
      this.objectUrl = null;
      this.objectUrlSafe = null;
    }
  }
}

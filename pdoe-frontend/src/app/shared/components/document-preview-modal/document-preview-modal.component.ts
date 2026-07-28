// Aperçu d'un document joint, ouvert depuis DossierDetailComponent. Les
// documents seedés au démarrage n'ont pas de blob réel → état "indisponible".

import { Component, EventEmitter, HostListener, Input, OnChanges, OnDestroy, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { Document as PdoeDocument } from '../../../core/models/dossier.model';
import { TYPE_DOCUMENT_LABELS } from '../../../core/models/enums.model';
import { MockDocumentBlobStore } from '../../../core/mock/mock-document-blob.store';
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
  @Input({ required: true }) document!: PdoeDocument;
  @Output() fermer = new EventEmitter<void>();

  readonly typeDocumentLabels = TYPE_DOCUMENT_LABELS;

  fichier: File | null = null;
  nature: NatureApercu = 'inconnu';
  objectUrl: string | null = null;
  objectUrlSafe: SafeResourceUrl | null = null;

  constructor(
    private blobStore: MockDocumentBlobStore,
    private sanitizer: DomSanitizer
  ) {}

  ngOnChanges(): void {
    this.revoquerUrl();

    this.fichier = this.blobStore.obtenir(this.document.documentId) ?? null;
    if (!this.fichier) {
      this.nature = 'inconnu';
      return;
    }

    this.objectUrl = URL.createObjectURL(this.fichier);
    this.objectUrlSafe = this.sanitizer.bypassSecurityTrustResourceUrl(this.objectUrl);
    this.nature = this.fichier.type.startsWith('image/')
      ? 'image'
      : this.fichier.type === 'application/pdf'
        ? 'pdf'
        : 'inconnu';
  }

  ngOnDestroy(): void {
    this.revoquerUrl();
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

  private revoquerUrl(): void {
    if (this.objectUrl) {
      URL.revokeObjectURL(this.objectUrl);
      this.objectUrl = null;
      this.objectUrlSafe = null;
    }
  }
}

import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';

const routes: Routes = [
  { path: '', redirectTo: 'exports', pathMatch: 'full' },
  {
    path: 'exports',
    loadComponent: () =>
      import('./pages/journal-exports/journal-exports.component').then(m => m.JournalExportsComponent)
  }
];

@NgModule({
  imports: [CommonModule, RouterModule.forChild(routes)]
})
export class ReportingModule {}

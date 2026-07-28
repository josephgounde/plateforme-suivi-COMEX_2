import { NgModule } from '@angular/core';
import { provideServerRendering, withRoutes } from '@angular/ssr';
import { serverRoutes } from './app.routes.server';

@NgModule({
  providers: [provideServerRendering(withRoutes(serverRoutes))],
})
export class AppServerModule {}

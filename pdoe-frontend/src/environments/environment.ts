export const environment = {
  production: false,
  // Local uniquement — PDOE.Api tourne sur ce port (profil "http"), exposé sous /api via UsePathBase.
  apiUrl: 'http://localhost:5072/api',
  // mock.interceptor.ts route tout vers MockDataService quand true; voir HANDOFF_CONNEXION_FRONT_BACK.md pour l'état des surfaces prêtes.
  useMock: false,
  // Reste true indépendamment de useMock : PDOE.Gateway (AuthController) n'existe pas encore côté backend.
  useMockAuth: true,
  appVersion: '5.2.0',
  appName: 'PDOE — AfrilandFirstBank CI'
};
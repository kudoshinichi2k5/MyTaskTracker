import { Environment } from './environment.model';

export const environment: Environment = {
  production: false,
  taskApi: 'http://api.testing.local/api/v1',
  oauth: {
    issuer: 'http://localhost:8080/realms/TaskTrackerRealm',
    redirectUri: 'http://app.testing.local/tasks',
    clientId: 'angular-frontend-client',
    scope: 'openid profile email'
  }
};
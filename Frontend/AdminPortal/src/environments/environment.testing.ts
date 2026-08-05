import { Environment } from './environment.model';

export const environment: Environment = {
  production: false,
  taskApi: 'http://api.testing.local/api/v1',
  notificationApi: 'http://notifications.testing.local/api/v1',
  oauth: {
    issuer: 'http://localhost:8080/realms/TaskTrackerRealm',
    redirectUri: 'http://admin.testing.local/dashboard',
    clientId: 'angular-admin-client',
    scope: 'openid profile email'
  }
};
import { Environment } from './environment.model';

export const environment: Environment = {
  production: false,
  taskApi: 'http://api.testing.local/api/v1',
  notificationApi: 'http://notifications.testing.local/api/v1',
  oauth: {
    issuer: 'http://localhost:8080/realms/TaskTrackerRealm', // Riêng Keycloak vẫn giữ localhost:8080
    redirectUri: 'http://app.testing.local/app1/tasks',
    clientId: 'angular-frontend-client',
    scope: 'openid profile email'
  }
};
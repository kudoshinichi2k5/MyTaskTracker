import { Environment } from './environment.model';

export const environment: Environment = {
  production: false,
  // Same backend services as the Customer App - both frontends read/write
  // the same TaskService/NotificationService data through the same Keycloak
  // realm, just with a different client id and a different role requirement.
  taskApi: 'http://localhost:5002/api/v1',
  notificationApi: 'http://localhost:5003/api/v1',
  oauth: {
    issuer: 'http://localhost:8080/realms/TaskTrackerRealm',
    redirectUri: window.location.origin + '/dashboard',
    clientId: 'angular-admin-client',
    scope: 'openid profile email'
  }
};
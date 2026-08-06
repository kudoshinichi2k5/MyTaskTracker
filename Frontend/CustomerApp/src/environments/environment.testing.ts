import { Environment } from './environment.model';

export const environment: Environment = {
  production: false,
  taskApi: 'http://localhost:5002/api/v1',
  notificationApi: 'http://localhost:5003/api/v1',
  oauth: {
    issuer: 'http://localhost:8080/realms/TaskTrackerRealm',
    redirectUri: 'http://localhost/app1/tasks',
    clientId: 'angular-frontend-client',
    scope: 'openid profile email'
  }
};
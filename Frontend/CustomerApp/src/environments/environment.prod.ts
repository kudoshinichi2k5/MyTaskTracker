import { Environment } from './environment.model';

export const environment: Environment = {
  production: true,
  taskApi: 'https://api.prod.yourdomain.com/api/v1',
  notificationApi: 'https://notifications.prod.yourdomain.com/api/v1',
  oauth: {
    issuer: 'https://auth.prod.yourdomain.com/realms/TaskTrackerRealm',
    redirectUri: 'https://app.prod.yourdomain.com/app1/tasks',
    clientId: 'angular-frontend-client',
    scope: 'openid profile email'
  }
};
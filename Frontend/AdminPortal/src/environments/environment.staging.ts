import { Environment } from './environment.model';

export const environment: Environment = {
  production: false,
  taskApi: 'https://api.staging.yourdomain.com/api/v1',
  notificationApi: 'https://notifications.staging.yourdomain.com/api/v1',
  oauth: {
    issuer: 'https://auth.staging.yourdomain.com/realms/TaskTrackerRealm',
    redirectUri: 'https://app.staging.yourdomain.com/app2/tasks',
    clientId: 'angular-admin-client',
    scope: 'openid profile email'
  }
};
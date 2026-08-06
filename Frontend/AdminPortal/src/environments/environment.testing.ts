import { Environment } from './environment.model';

export const environment: Environment = {
  production: false,
  taskApi: 'http://api.testing.local/api/v1',
  notificationApi: 'http://notifications.testing.local/api/v1',
  oauth: {
    issuer: 'http://localhost:8080/realms/TaskTrackerRealm', // Keycloak Server giữ nguyên
    redirectUri: 'http://app.testing.local/app2/dashboard', // Chú ý: dùng app2 và đuôi /dashboard
    clientId: 'admin-portal-client', // Chú ý: Client ID của Admin
    scope: 'openid profile email'
  }
};
import { Environment } from './environment.model';

export const environment = {
  production: false,
  taskApi: 'http://localhost:5002/api/v1',
  // Cấu hình OAuth2 IdP
  oauth: {
    issuer: 'http://localhost:8080/realms/TaskTrackerRealm',
    redirectUri: window.location.origin + '/tasks', // Nơi IdP trả về sau khi đăng nhập xong
    clientId: 'angular-frontend-client',
    scope: 'openid profile email'
  }
};
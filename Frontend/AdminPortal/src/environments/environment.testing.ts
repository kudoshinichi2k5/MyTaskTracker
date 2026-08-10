import { Environment } from './environment.model';

export const environment: Environment = {
  production: false,
  authApi: 'http://api.testing.local/api/v1', // Trỏ về AuthService mới
  taskApi: 'http://api.testing.local/api/v1',
  notificationApi: 'http://notifications.testing.local/api/v1'
};

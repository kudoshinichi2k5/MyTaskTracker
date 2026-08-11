import { Environment } from './environment.model';

export const environment: Environment = {
  production: false,
  authApi: 'http://auth.testing.local/api/v1',
  taskApi: 'http://api.testing.local/api/v1',
  notificationApi: 'http://notifications.testing.local/api/v1'
};

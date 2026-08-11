import { Environment } from './environment.model';

export const environment: Environment = {
  production: false,
  // Both portals use the same TaskService/NotificationService backend and a
  // custom AuthService on port 5001 for sign-in and token issuance.
  authApi: 'http://localhost:5001/api/v1',
  taskApi: 'http://localhost:5002/api/v1',
  notificationApi: 'http://localhost:5003/api/v1'
};
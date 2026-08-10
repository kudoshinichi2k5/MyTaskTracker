import { Environment } from './environment.model';

export const environment: Environment = {
  production: false,
  // Same backend services as the Customer App - both frontends read/write
  // the same TaskService/NotificationService data.
  // Now using our custom AuthService on port 5001 instead of Keycloak.
  authApi: 'http://localhost:5001/api/v1',
  taskApi: 'http://localhost:5002/api/v1',
  notificationApi: 'http://localhost:5003/api/v1'
};
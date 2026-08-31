import { Environment } from './environment.model';

export const environment: Environment = {
  production: false,
  authApi: 'http://localhost:5001/api/v1',
  taskApi: 'http://localhost:5002/api/v1',
  notificationApi: 'http://localhost:5003/api/v1',
  projectApi: 'http://localhost:5004/api/v1',
  commentApi: 'http://localhost:5005/api/v1'
};

import { Environment } from './environment.model';

export const environment: Environment = {
  production: false,
  authApi: 'https://auth.staging.yourdomain.com/api/v1',
  taskApi: 'https://api.staging.yourdomain.com/api/v1',
  notificationApi: 'https://notifications.staging.yourdomain.com/api/v1',
  projectApi: 'https://projects.staging.yourdomain.com/api/v1',
  commentApi: 'https://comments.staging.yourdomain.com/api/v1'
};

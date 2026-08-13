import { Environment } from './environment.model';

export const environment: Environment = {
  production: true,
  authApi: 'https://auth.prod.yourdomain.com/api/v1',
  taskApi: 'https://api.prod.yourdomain.com/api/v1',
  notificationApi: 'https://notifications.prod.yourdomain.com/api/v1',
  projectApi: 'https://projects.prod.yourdomain.com/api/v1',
  commentApi: 'https://comments.prod.yourdomain.com/api/v1'
};

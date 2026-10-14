import { Environment } from './environment.model';

export const environment: Environment = {
  production: false,
  authApi: 'https://api.mytasktracker.work.gd/auth/api/v1',
  taskApi: 'https://api.mytasktracker.work.gd/tasks/api/v1',
  notificationApi: 'https://api.mytasktracker.work.gd/notifications/api/v1',
  projectApi: 'https://api.mytasktracker.work.gd/projects/api/v1',
  commentApi: 'https://api.mytasktracker.work.gd/comments/api/v1'
};

export interface Environment {
  production: boolean;
  authApi: string;
  taskApi: string;
  notificationApi?: string;
}
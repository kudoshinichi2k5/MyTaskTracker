export interface Environment {
  production: boolean;
  taskApi: string;
  notificationApi?: string;
  oauth: {
    issuer: string;
    redirectUri: string;
    clientId: string;
    scope: string;
  };
}
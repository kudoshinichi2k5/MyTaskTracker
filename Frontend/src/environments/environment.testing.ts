export const environment = {
  production: false,
  taskApi: 'http://api.testing.local/api/v1',
  oauth: {
    issuer: 'http://localhost:8080/realms/TaskTrackerRealm',
    redirectUri: 'http://app.testing.local/tasks', // QUAN TRỌNG: Trả về domain ảo của IIS
    clientId: 'angular-frontend-client',
    scope: 'openid profile email'
  }
};
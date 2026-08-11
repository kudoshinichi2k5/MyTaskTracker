import { Environment } from './environment.model';

export const environment = {
  production: false, // hoặc false tùy môi trường
  // Bỏ tên miền riêng, dùng đường dẫn tuyệt đối bắt nguồn từ domain hiện tại
  authApi: '/auth/api/v1',       // GỌI /token: /auth/token
  taskApi: '/tasks/api/v1',
  notificationApi: '/notifications/api/v1'
};
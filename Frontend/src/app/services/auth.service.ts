import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { tap } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private apiUrl = environment.authApi;

  login(username: string, password: string) {
    return this.http.post<{status: string, token: string}>(`${this.apiUrl}/login`, { username, password })
      .pipe(
        tap(res => localStorage.setItem('jwt_token', res.token)) // Lưu token sau khi thành công
      );
  }
}
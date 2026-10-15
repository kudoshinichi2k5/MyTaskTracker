import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting
} from '@angular/common/http/testing';

import { AuthService } from './auth.service';
import { environment } from '../../environments/environment';

describe('AuthService', () => {
  let service: AuthService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem('adminportal_session', JSON.stringify({
      accessToken: 'old-access-token',
      refreshToken: 'old-refresh-token',
      expiresAt: Date.now() + 60 * 60 * 1000,
      username: 'admin',
      roles: ['Admin']
    }));

    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(AuthService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
    localStorage.clear();
  });

  it('shares one refresh request between concurrent callers', () => {
    const results: string[] = [];
    service.refresh().subscribe((response) => results.push(response.accessToken));
    service.refresh().subscribe((response) => results.push(response.accessToken));

    const request = httpTestingController.expectOne(
      `${environment.authApi}/auth/refresh`
    );
    expect(request.request.body).toEqual({
      refreshToken: 'old-refresh-token'
    });

    request.flush({
      accessToken: 'new-access-token',
      refreshToken: 'new-refresh-token',
      expiresAt: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
      username: 'admin',
      roles: ['Admin']
    });

    expect(results).toEqual(['new-access-token', 'new-access-token']);
    expect(service.accessToken).toBe('new-access-token');
  });
});

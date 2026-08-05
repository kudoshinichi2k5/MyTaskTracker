import { Injectable, signal } from '@angular/core';

export type ToastType = 'error' | 'success';

export interface Toast {
  id: number;
  message: string;
  type: ToastType;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 1;
  private readonly _toasts = signal<Toast[]>([]);
  readonly toasts = this._toasts.asReadonly();

  private show(message: string, type: ToastType, durationMs: number) {
    const id = this.nextId++;
    this._toasts.update((list) => [...list, { id, message, type }]);
    setTimeout(() => this.dismiss(id), durationMs);
  }

  error(message: string, durationMs = 5000) {
    this.show(message, 'error', durationMs);
  }

  success(message: string, durationMs = 3000) {
    this.show(message, 'success', durationMs);
  }

  dismiss(id: number) {
    this._toasts.update((list) => list.filter((t) => t.id !== id));
  }
}
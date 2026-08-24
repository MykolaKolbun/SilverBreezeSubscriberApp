// Thin REST client for the ParkingSubscription backend.
import { API_BASE_URL } from './config';

export class ApiError extends Error {
  status: number;
  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

async function req<T>(
  path: string,
  opts: { method?: string; body?: unknown; token?: string | null } = {}
): Promise<T> {
  let res: Response;
  try {
    res = await fetch(API_BASE_URL + path, {
      method: opts.method ?? 'GET',
      headers: {
        'Content-Type': 'application/json',
        ...(opts.token ? { Authorization: `Bearer ${opts.token}` } : {}),
      },
      body: opts.body !== undefined ? JSON.stringify(opts.body) : undefined,
    });
  } catch {
    throw new ApiError(0, 'Не вдалося з’єднатися з сервером.');
  }

  if (res.status === 204) return undefined as T;

  const text = await res.text();
  let data: any = undefined;
  if (text) {
    try {
      data = JSON.parse(text);
    } catch {
      /* non-JSON body */
    }
  }

  if (!res.ok) {
    const msg =
      (data && (data.title || data.detail || data.message)) ||
      `Помилка запиту (${res.status})`;
    throw new ApiError(res.status, msg);
  }
  return data as T;
}

// ---- DTOs (mirror the backend) ----
export interface RegisterResult {
  userId: string;
  customerId: string;
  email: string;
  devConfirmationToken?: string | null;
}
export interface AuthResult {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  userId: string;
  customerId: string;
}
export interface ApiPlan {
  id: string;
  code: string;
  name: string;
  priceMinor: number;
  currency: string;
  durationDays: number;
}
export interface ApiCard {
  id: string;
  userId: string;
  subscriptionPlanId?: string | null;
  startDate: string; // YYYY-MM-DD
  endDate: string;
  status: string; // Active | Blocked | Suspended | Deleted
  qrPayload: string;
  isDeleted: boolean;
  updatedAt: string;
}
export interface PagedCards {
  items: ApiCard[];
  nextPagingToken?: string | null;
}
export interface InitiatePaymentResult {
  paymentId: string;
  providerPaymentId: string;
  clientSecret: string;
  amountMinor: number;
  currency: string;
}

// ---- Endpoints ----
export const api = {
  register: (b: { email: string; password: string; firstName?: string; surname?: string }) =>
    req<RegisterResult>('/auth/register', { method: 'POST', body: b }),

  confirmEmail: (b: { email: string; token: string }) =>
    req<void>('/auth/confirm-email', { method: 'POST', body: b }),

  login: (b: { email: string; password: string }) =>
    req<AuthResult>('/auth/login', { method: 'POST', body: b }),

  refresh: (refreshToken: string) =>
    req<AuthResult>('/auth/refresh', { method: 'POST', body: { refreshToken } }),

  getPlans: (token: string) => req<ApiPlan[]>('/plans', { token }),

  getParkingCards: (userId: string, token: string) =>
    req<PagedCards>(`/users/${userId}/parking-cards`, { token }),

  initiatePayment: (
    b: { userId: string; subscriptionPlanId: string; startDate?: string | null },
    token: string
  ) => req<InitiatePaymentResult>('/payments', { method: 'POST', body: b, token }),

  // Dev/stub: simulate the payment provider's "succeeded" webhook so the card
  // activates. Replace with a real provider flow later. (Endpoint is anonymous.)
  completePaymentDev: (providerPaymentId: string) =>
    req<unknown>('/payments/webhook', {
      method: 'POST',
      body: { providerPaymentId, status: 'succeeded' },
    }),
};

// QR image URL (fetched with an Authorization header by <Image>).
export const qrUrl = (cardId: string) => `${API_BASE_URL}/parking-cards/${cardId}/qr`;

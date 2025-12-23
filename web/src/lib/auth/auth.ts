import { raw } from "@/lib/api/client";
import type { AccessTokenResponse } from "@/lib/api/types";
import { clearAccessToken, setAccessToken } from "@/lib/auth/tokenStore";

export async function login(email: string, password: string) {
  const res = await raw.post<AccessTokenResponse>("/auth/login", { email, password }, { withCredentials: true });
  setAccessToken(res.data.accessToken);
}

export async function register(email: string, password: string) {
  await raw.post("/auth/register", { email, password }, { withCredentials: true });
}

export async function refresh() {
  const res = await raw.post<AccessTokenResponse>("/auth/refresh", undefined, { withCredentials: true });
  setAccessToken(res.data.accessToken);
  return res.data.accessToken;
}

export async function logout() {
  try {
    await raw.post("/auth/logout", undefined, { withCredentials: true });
  } finally {
    clearAccessToken();
  }
}


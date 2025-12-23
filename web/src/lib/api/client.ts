import axios, { AxiosError, AxiosInstance, AxiosRequestConfig } from "axios";
import { clearAccessToken, getAccessToken, setAccessToken } from "@/lib/auth/tokenStore";
import type { AccessTokenResponse, ProblemDetails } from "@/lib/api/types";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5000";

type RetriableRequestConfig = AxiosRequestConfig & {
  _retry?: boolean;
  _skipAuthRefresh?: boolean;
};

let refreshPromise: Promise<string> | null = null;

async function refreshAccessToken(raw: AxiosInstance): Promise<string> {
  if (!refreshPromise) {
    refreshPromise = raw
      .post<AccessTokenResponse>("/auth/refresh", undefined, { withCredentials: true })
      .then((r) => r.data.accessToken)
      .finally(() => {
        refreshPromise = null;
      });
  }
  return refreshPromise;
}

export function createApiClient() {
  const raw = axios.create({
    baseURL: API_BASE_URL,
    withCredentials: true
  });

  const api = axios.create({
    baseURL: API_BASE_URL,
    withCredentials: true
  });

  api.interceptors.request.use((config) => {
    const token = getAccessToken();
    if (token) {
      config.headers = config.headers ?? {};
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  });

  api.interceptors.response.use(
    (resp) => resp,
    async (error: AxiosError<ProblemDetails>) => {
      const status = error.response?.status;
      const config = (error.config ?? {}) as RetriableRequestConfig;

      if (status !== 401 || config._retry || config._skipAuthRefresh) throw error;

      const url = config.url ?? "";
      if (url.startsWith("/auth/login") || url.startsWith("/auth/refresh") || url.startsWith("/auth/register")) throw error;

      config._retry = true;
      try {
        const token = await refreshAccessToken(raw);
        setAccessToken(token);
        config.headers = config.headers ?? {};
        config.headers.Authorization = `Bearer ${token}`;
        return api.request(config);
      } catch {
        clearAccessToken();
        if (typeof window !== "undefined") window.location.href = "/login";
        throw error;
      }
    }
  );

  return { api, raw };
}

export const { api, raw } = createApiClient();


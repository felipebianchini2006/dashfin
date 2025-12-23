"use client";

import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { getAccessToken, subscribeAccessToken } from "@/lib/auth/tokenStore";
import * as authApi from "@/lib/auth/auth";

type AuthState = {
  accessToken: string | null;
  isReady: boolean;
  ensureAccessToken: () => Promise<string | null>;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
};

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [accessToken, setToken] = useState<string | null>(getAccessToken());
  const [isReady, setReady] = useState(false);

  useEffect(() => subscribeAccessToken(setToken), []);

  const ensureAccessToken = useCallback(async () => {
    const existing = getAccessToken();
    if (existing) {
      setReady(true);
      return existing;
    }
    try {
      const token = await authApi.refresh();
      setReady(true);
      return token;
    } catch {
      setReady(true);
      return null;
    }
  }, []);

  const login = useCallback(async (email: string, password: string) => authApi.login(email, password), []);
  const register = useCallback(async (email: string, password: string) => authApi.register(email, password), []);
  const logout = useCallback(async () => authApi.logout(), []);

  const value = useMemo<AuthState>(
    () => ({ accessToken, isReady, ensureAccessToken, login, register, logout }),
    [accessToken, ensureAccessToken, isReady, login, logout, register]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}


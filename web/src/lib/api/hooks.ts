"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api, raw } from "@/lib/api/client";
import { useRef } from "react";
import { AlertEventStatus } from "@/lib/api/types";
import type {
  AccountDto,
  AlertEventDto,
  BudgetDto,
  CategoryDto,
  DashboardBalancesDto,
  DashboardCategoriesDto,
  DashboardSummaryDto,
  DashboardTimeseriesDto,
  ImportDto,
  ImportRowAuditDto,
  ListImportsResponse,
  ListTransactionsResponse
} from "@/lib/api/types";

export type MeDto = {
  email: string;
  timezone: string;
  currency: string;
  displayPreferences: { theme: string; compactMode: boolean };
};

export function useMe() {
  return useQuery({
    queryKey: ["me"],
    queryFn: async () => (await api.get<MeDto>("/me")).data
  });
}

export function useAccounts() {
  return useQuery({
    queryKey: ["accounts"],
    queryFn: async () => (await api.get<AccountDto[]>("/accounts")).data
  });
}

export function useCreateAccount() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: { name: string; type: number; initialBalance?: number | null }) =>
      (await api.post<AccountDto>("/accounts", body)).data,
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ["accounts"] });
      await qc.invalidateQueries({ queryKey: ["dashboard"] });
    }
  });
}

export function useCategories() {
  return useQuery({
    queryKey: ["categories"],
    queryFn: async () => (await api.get<CategoryDto[]>("/categories")).data
  });
}

export function useCreateCategory() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: { name: string; parent_id?: string | null }) =>
      (await api.post<CategoryDto>("/categories", body)).data,
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ["categories"] });
    }
  });
}

export function useBudgets(month: string) {
  return useQuery({
    queryKey: ["budgets", month],
    queryFn: async () => (await api.get<BudgetDto[]>("/budgets", { params: { month } })).data
  });
}

export function useUpsertBudget() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: { category_id: string; month: string; amount: number }) =>
      (await api.post<BudgetDto>("/budgets", body)).data,
    onSuccess: async (_data, vars) => {
      await qc.invalidateQueries({ queryKey: ["budgets", vars.month] });
      await qc.invalidateQueries({ queryKey: ["dashboardSummary", vars.month] });
    }
  });
}

export function useAlerts(status?: AlertEventStatus) {
  return useQuery({
    queryKey: ["alerts", status ?? "all"],
    queryFn: async () =>
      (await api.get<AlertEventDto[]>("/alerts", { params: status ? { status: AlertEventStatus[status] } : {} })).data
  });
}

export function useUpdateAlertStatus() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (vars: { id: string; status: AlertEventStatus }) =>
      await api.patch(`/alerts/${vars.id}`, { status: vars.status }),
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ["alerts"] });
    }
  });
}

export function useTransactions(params: {
  from?: string;
  to?: string;
  q?: string;
  page?: number;
  page_size?: number;
  account_id?: string;
  category_id?: string;
  type?: 1 | 2;
}) {
  return useQuery({
    queryKey: ["transactions", params],
    queryFn: async () => (await api.get<ListTransactionsResponse>("/transactions", { params })).data
  });
}

export function useDashboardSummary(month: string) {
  return useQuery({
    queryKey: ["dashboardSummary", month],
    queryFn: async () => (await api.get<DashboardSummaryDto>("/dashboard/summary", { params: { month } })).data
  });
}

export function useDashboardCategories(month: string) {
  return useQuery({
    queryKey: ["dashboardCategories", month],
    queryFn: async () => (await api.get<DashboardCategoriesDto>("/dashboard/categories", { params: { month } })).data
  });
}

export function useDashboardTimeseries(month: string) {
  return useQuery({
    queryKey: ["dashboardTimeseries", month],
    queryFn: async () => (await api.get<DashboardTimeseriesDto>("/dashboard/timeseries", { params: { month } })).data
  });
}

export function useDashboardBalances() {
  return useQuery({
    queryKey: ["dashboardBalances"],
    queryFn: async () => (await api.get<DashboardBalancesDto>("/dashboard/balances")).data
  });
}

export function useUploadImport() {
  return useMutation({
    mutationFn: async (vars: { accountId: string; file: File }) => {
      const fd = new FormData();
      fd.append("account_id", vars.accountId);
      fd.append("pdf", vars.file);
      const res = await api.post<{ importId: string }>("/imports", fd, {
        headers: { "Content-Type": "multipart/form-data" }
      });
      return res.data;
    }
  });
}

export function useImports(params?: { status?: string; page?: number; page_size?: number }) {
  return useQuery({
    queryKey: ["imports", params ?? {}],
    queryFn: async () => (await api.get<ListImportsResponse>("/imports", { params })).data
  });
}

export function useImportsWithPolling(params?: { status?: string; page?: number; page_size?: number }) {
  const attempt = useRef(0);
  const lastUpdatedAt = useRef(0);

  return useQuery({
    queryKey: ["imports", params ?? {}],
    queryFn: async () => (await api.get<ListImportsResponse>("/imports", { params })).data,
    refetchInterval: (q) => {
      const data = q.state.data;
      const updatedAt = q.state.dataUpdatedAt;
      const anyProcessing =
        data?.items?.some((i) => i.status === 0 || i.status === 1) ?? false; // Uploaded/Processing

      if (!anyProcessing) {
        attempt.current = 0;
        lastUpdatedAt.current = updatedAt;
        return false;
      }

      if (updatedAt !== lastUpdatedAt.current) {
        lastUpdatedAt.current = updatedAt;
        attempt.current = Math.min(6, attempt.current + 1);
      }

      return Math.min(30000, 1000 * 2 ** Math.max(0, attempt.current - 1));
    }
  });
}

export function useImport(importId: string) {
  return useQuery({
    queryKey: ["import", importId],
    queryFn: async () => (await api.get<ImportDto>(`/imports/${importId}`)).data
  });
}

export function useImportWithPolling(importId: string) {
  const attempt = useRef(0);
  const lastUpdatedAt = useRef(0);

  return useQuery({
    queryKey: ["import", importId],
    queryFn: async () => (await api.get<ImportDto>(`/imports/${importId}`)).data,
    refetchInterval: (q) => {
      const data = q.state.data;
      const updatedAt = q.state.dataUpdatedAt;
      const processing = data?.status === 0 || data?.status === 1; // Uploaded/Processing

      if (!processing) {
        attempt.current = 0;
        lastUpdatedAt.current = updatedAt;
        return false;
      }

      if (updatedAt !== lastUpdatedAt.current) {
        lastUpdatedAt.current = updatedAt;
        attempt.current = Math.min(7, attempt.current + 1);
      }

      return Math.min(30000, 1000 * 2 ** Math.max(0, attempt.current - 1));
    }
  });
}

export function useImportRowsError(importId: string, opts?: { enabled?: boolean }) {
  return useQuery({
    queryKey: ["importRows", importId, "ERROR"],
    queryFn: async () =>
      (await api.get<ImportRowAuditDto[]>(`/imports/${importId}/rows`, { params: { status: "ERROR" } })).data,
    enabled: opts?.enabled ?? true
  });
}

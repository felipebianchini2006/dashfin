"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api, raw } from "@/lib/api/client";
import type {
  AccountDto,
  AlertEventDto,
  AlertEventStatus,
  BudgetDto,
  CategoryDto,
  DashboardBalancesDto,
  DashboardCategoriesDto,
  DashboardSummaryDto,
  DashboardTimeseriesDto,
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

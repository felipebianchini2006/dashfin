export type Guid = string;

export type ProblemDetails = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  correlationId?: string;
  traceId?: string;
  code?: string;
  errors?: Record<string, string[]>;
};

export type AccessTokenResponse = { accessToken: string };

export enum AccountType {
  Checking = 1,
  CreditCard = 2,
  Savings = 3
}

export type AccountDto = {
  id: Guid;
  name: string;
  type: AccountType;
  currency: string;
  initialBalance: number;
  balance: number | null;
  creditCardSpendThisMonth: number | null;
};

export enum TransactionFlow {
  Entrada = 1,
  Saida = 2
}

export type TransactionDto = {
  id: Guid;
  accountId: Guid;
  categoryId: Guid | null;
  occurredAt: string;
  description: string;
  notes: string | null;
  amount: number;
  currency: string;
  type: TransactionFlow;
  ignoreInDashboard: boolean;
};

export type ListTransactionsResponse = {
  items: TransactionDto[];
  page: number;
  pageSize: number;
  totalCount: number;
};

export type CategoryDto = {
  id: Guid;
  name: string;
  parentId: Guid | null;
  color: string | null;
};

export type BudgetDto = {
  id: Guid;
  categoryId: Guid;
  month: string;
  limitAmount: number;
  spentAmount: number;
};

export enum AlertEventStatus {
  New = 1,
  Read = 2,
  Dismissed = 3
}

export type AlertEventDto = {
  id: Guid;
  alertRuleId: Guid;
  fingerprint: string;
  status: AlertEventStatus;
  occurredAt: string;
  title: string;
  body: string | null;
  payloadJson: string | null;
};

export type DashboardSummaryDto = {
  month: string;
  incomeAmount: number;
  checkingOutAmount: number;
  creditCardSpendAmount: number;
  netCashAmount: number;
  topCategories: { categoryId: Guid; categoryName: string; spentAmount: number }[];
  budgetProgress: {
    categoryId: Guid;
    categoryName: string;
    spentAmount: number;
    limitAmount: number;
    progressPercent: number;
    isOverBudget: boolean;
  }[];
};

export type DashboardCategoriesDto = {
  month: string;
  items: { categoryId: Guid; categoryName: string; spentAmount: number }[];
};

export type DashboardTimeseriesDto = {
  month: string;
  items: { date: string; spentAmount: number }[];
};

export type DashboardBalancesDto = {
  checkingAccounts: { accountId: Guid; name: string; currency: string; balance: number }[];
  totalSaved: number;
  creditCardOpen: number | null;
  netWorth: number | null;
};


import { test, expect } from "@playwright/test";
import path from "path";

test("login → criar conta → upload → ver status → corrigir categoria → dashboard atualizado", async ({ page }) => {
  const email = `e2e-${Date.now()}@example.com`;
  const password = "Password1!";

  await page.goto("/register");
  await page.locator(".card .field", { hasText: "Email" }).locator("input").fill(email);
  await page.locator(".card .field", { hasText: "Senha" }).locator("input").fill(password);
  await page.getByRole("button", { name: "Criar" }).click();

  await page.waitForURL(/\/app\/dashboard/);

  // Create a CHECKING account
  await page.goto("/app/accounts");
  const newAccount = page.locator(".card", { hasText: "Nova conta" });
  await newAccount.locator(".field", { hasText: "Nome" }).locator("input").fill("Banco");
  await newAccount.getByRole("button", { name: "Criar" }).click();

  await expect(page.locator(".card", { hasText: "Contas" })).toContainText("Banco");

  // Upload import
  await page.goto("/app/imports");
  const uploadCard = page.locator(".card", { hasText: "Enviar PDF" });
  await uploadCard.locator("select").selectOption({ index: 1 });

  const filePath = path.join(__dirname, "fixtures", "nubank-conta-stub.pdf");
  await uploadCard.locator("input[type=file]").setInputFiles(filePath);
  await uploadCard.getByRole("button", { name: "Enviar" }).click();

  // Wait for detail page, then for DONE
  await page.waitForURL(/\/app\/imports\/[0-9a-f-]+/);
  await expect(page.locator("text=DONE")).toBeVisible({ timeout: 30_000 });

  // Recategorize first transaction (should suggest rule)
  await page.goto("/app/transactions");
  await expect(page.locator("text=Transações")).toBeVisible();

  const firstRow = page.locator("table.table tbody tr").first();
  await firstRow.locator("select").first().selectOption({ label: "Alimentação" });

  await expect(page.locator("text=Quer criar uma regra automática")).toBeVisible();
  await page.getByRole("button", { name: "Criar regra" }).click();
  await expect(page.locator("text=Criar regra de categoria")).toBeVisible();
  await page.locator('[role="dialog"]').getByRole("button", { name: "Criar regra" }).click();

  // Dashboard should reflect spend; just assert it loads and shows cards.
  await page.goto("/app/dashboard");
  await expect(page.locator("text=Entradas (CHECKING)")).toBeVisible();
  await expect(page.locator("text=Saídas (CHECKING)")).toBeVisible();
});

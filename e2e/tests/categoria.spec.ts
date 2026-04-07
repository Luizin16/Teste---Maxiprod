import { test, expect } from '@playwright/test';
import { CategoriasPage } from '../pages';

/**
 * E2E — Categorias CRUD
 */
test.describe('Categorias — CRUD', () => {
  let categoriasPage: CategoriasPage;

  test.beforeEach(async ({ page }) => {
    categoriasPage = new CategoriasPage(page);
    await categoriasPage.goto();
  });

  test('deve exibir a lista de categorias ao acessar /categorias', async ({ page }) => {
    await expect(page).toHaveURL('/categorias');
    await expect(page.locator('table, [role="list"]')).toBeVisible();
  });

  test('deve criar categoria do tipo Despesa com sucesso', async ({ page }) => {
    const descricao = `Despesa E2E ${Date.now()}`;
    await categoriasPage.criarCategoria(descricao, 'Despesa');
    await categoriasPage.waitForToast('sucesso');
    await expect(categoriasPage.categoriaNaTabela(descricao)).toBeVisible();
  });

  test('deve criar categoria do tipo Receita com sucesso', async ({ page }) => {
    const descricao = `Receita E2E ${Date.now()}`;
    await categoriasPage.criarCategoria(descricao, 'Receita');
    await categoriasPage.waitForToast('sucesso');
    await expect(categoriasPage.categoriaNaTabela(descricao)).toBeVisible();
  });

  test('deve criar categoria do tipo Ambas com sucesso', async ({ page }) => {
    const descricao = `Ambas E2E ${Date.now()}`;
    await categoriasPage.criarCategoria(descricao, 'Ambas');
    await categoriasPage.waitForToast('sucesso');
    await expect(categoriasPage.categoriaNaTabela(descricao)).toBeVisible();
  });

  test('deve exibir erro ao criar categoria com descrição vazia', async ({ page }) => {
    await page.getByRole('button', { name: /nova categoria|adicionar/i }).click();
    await page.getByRole('button', { name: /salvar/i }).click();
    await expect(page.getByText(/descrição.*obrigatória|required/i)).toBeVisible();
  });

  test('[BUG-005] não deve ser possível editar categoria — endpoint PUT ausente', async ({ page }) => {
    /**
     * 🐛 BUG-005: CategoriasController não possui endpoint PUT.
     * O frontend pode ou não oferecer botão de edição,
     * mas qualquer tentativa de PUT retornará 405.
     */
    const descricao = `Para Editar ${Date.now()}`;
    await categoriasPage.criarCategoria(descricao, 'Despesa');
    await categoriasPage.waitForToast().catch(() => {});

    const row = page.locator('tr').filter({ hasText: descricao });
    const btnEditar = row.getByRole('button', { name: /editar|edit/i });

    if (await btnEditar.isVisible({ timeout: 2000 }).catch(() => false)) {
      await btnEditar.click();
      // Tentar salvar a edição
      await page.getByRole('button', { name: /salvar/i }).click();
      // Como PUT não existe, deve mostrar erro
      const toastErro = page.getByText(/erro|falhou|400|405/i);
      await expect(toastErro).toBeVisible({ timeout: 5000 });
      console.warn('[BUG-005] Confirmado: edição de categoria falha — endpoint PUT não existe');
    } else {
      console.info('[BUG-005] Frontend não expõe botão de edição de categoria (UI já oculta o recurso ausente)');
    }
  });
});

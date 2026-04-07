import { type Page, type Locator, expect } from '@playwright/test';

// ─────────────────────────────────────────────────────────────
// Base Page
// ─────────────────────────────────────────────────────────────

export class BasePage {
  constructor(protected readonly page: Page) {}

  async waitForToast(text?: string) {
    const toast = this.page.locator('[aria-live], .go2072408551, div[role="status"]').first();
    await toast.waitFor({ state: 'visible', timeout: 8000 });
    if (text) {
      await expect(toast).toContainText(text);
    }
  }

  async navigateTo(path: string) {
    await this.page.goto(path);
    await this.page.waitForLoadState('networkidle');
  }
}

// ─────────────────────────────────────────────────────────────
// Pessoas Page
// ─────────────────────────────────────────────────────────────

export class PessoasPage extends BasePage {
  readonly url = '/pessoas';
  readonly btnNovaPessoa: Locator;
  readonly inputNome: Locator;
  readonly inputDataNascimento: Locator;
  readonly btnSalvar: Locator;
  readonly btnCancelar: Locator;

  constructor(page: Page) {
    super(page);
    this.btnNovaPessoa = page.getByRole('button', { name: /nova pessoa|adicionar/i });
    this.inputNome = page.getByPlaceholder(/nome/i).or(page.locator('input[name="nome"]'));
    this.inputDataNascimento = page.locator('input[name="dataNascimento"]').or(page.locator('input[type="date"]'));
    this.btnSalvar = page.getByRole('button', { name: /salvar/i });
    this.btnCancelar = page.getByRole('button', { name: /cancelar/i });
  }

  async goto() {
    await this.navigateTo(this.url);
  }

  async abrirFormulario() {
    await this.btnNovaPessoa.click();
    await this.inputNome.waitFor({ state: 'visible' });
  }

  async preencherFormulario(nome: string, dataNascimento: string) {
    await this.inputNome.fill(nome);
    await this.inputDataNascimento.fill(dataNascimento);
  }

  async criarPessoa(nome: string, dataNascimento: string) {
    await this.abrirFormulario();
    await this.preencherFormulario(nome, dataNascimento);
    await this.btnSalvar.click();
  }

  async deletarPessoa(nome: string) {
    const row = this.page.locator('tr, [data-testid="pessoa-row"]').filter({ hasText: nome });
    const btnDelete = row.getByRole('button', { name: /excluir|deletar|remover/i });
    await btnDelete.click();
    // confirmar dialog se existir
    const btnConfirm = this.page.getByRole('button', { name: /confirmar|sim|ok/i });
    if (await btnConfirm.isVisible({ timeout: 2000 }).catch(() => false)) {
      await btnConfirm.click();
    }
  }

  pessoaNaTabela(nome: string): Locator {
    return this.page.locator('table, [role="table"]').getByText(nome);
  }
}

// ─────────────────────────────────────────────────────────────
// Transacoes Page
// ─────────────────────────────────────────────────────────────

export class TransacoesPage extends BasePage {
  readonly url = '/transacoes';
  readonly btnNovaTransacao: Locator;
  readonly inputDescricao: Locator;
  readonly inputValor: Locator;
  readonly inputData: Locator;
  readonly btnSalvar: Locator;

  constructor(page: Page) {
    super(page);
    this.btnNovaTransacao = page.getByRole('button', { name: /nova transação|adicionar/i });
    this.inputDescricao = page.locator('input[name="descricao"]').or(page.getByPlaceholder(/descrição/i));
    this.inputValor = page.locator('input[name="valor"]').or(page.locator('input[type="number"]'));
    this.inputData = page.locator('input[name="data"]').or(page.locator('input[type="date"]'));
    this.btnSalvar = page.getByRole('button', { name: /salvar/i });
  }

  async goto() {
    await this.navigateTo(this.url);
  }

  async abrirFormulario() {
    await this.btnNovaTransacao.click();
    await this.inputDescricao.waitFor({ state: 'visible' });
  }

  async selecionarTipo(tipo: 'Despesa' | 'Receita') {
    await this.page.locator('select[name="tipo"]')
      .or(this.page.getByRole('combobox').filter({ hasText: /tipo/i }))
      .selectOption({ label: tipo });
  }

  async selecionarPessoa(nome: string) {
    const pessoaSearch = this.page.getByPlaceholder(/pesquisar pessoas/i)
      .or(this.page.locator('[aria-label="Lista de pessoas"]'));
    await pessoaSearch.fill(nome);
    await this.page.getByText(nome, { exact: false }).first().click();
  }

  async selecionarCategoria(descricao: string) {
    const catSearch = this.page.getByPlaceholder(/pesquisar categorias/i)
      .or(this.page.locator('[aria-label="Lista de categorias"]'));
    await catSearch.fill(descricao);
    await this.page.getByText(descricao, { exact: false }).first().click();
  }

  async criarTransacao(opts: {
    descricao: string;
    valor: string;
    tipo: 'Despesa' | 'Receita';
    pessoa: string;
    categoria: string;
    data?: string;
  }) {
    await this.abrirFormulario();
    await this.inputDescricao.fill(opts.descricao);
    await this.inputValor.fill(opts.valor);
    if (opts.data) await this.inputData.fill(opts.data);
    await this.selecionarTipo(opts.tipo);
    await this.selecionarPessoa(opts.pessoa);
    await this.selecionarCategoria(opts.categoria);
    await this.btnSalvar.click();
  }

  campoReceitaDesabilitado(): Locator {
    return this.page.locator('option[value="Receita"][disabled], select[name="tipo"] option:disabled');
  }

  avisoDeMenor(): Locator {
    return this.page.getByText(/menores só podem registrar despesas/i);
  }
}

// ─────────────────────────────────────────────────────────────
// Categorias Page
// ─────────────────────────────────────────────────────────────

export class CategoriasPage extends BasePage {
  readonly url = '/categorias';

  constructor(page: Page) {
    super(page);
  }

  async goto() {
    await this.navigateTo(this.url);
  }

  async criarCategoria(descricao: string, finalidade: 'Despesa' | 'Receita' | 'Ambas') {
    await this.page.getByRole('button', { name: /nova categoria|adicionar/i }).click();
    await this.page.locator('input[name="descricao"]').fill(descricao);
    await this.page.locator('select[name="finalidade"]')
      .or(this.page.getByRole('combobox'))
      .selectOption({ label: finalidade });
    await this.page.getByRole('button', { name: /salvar/i }).click();
  }

  categoriaNaTabela(descricao: string): Locator {
    return this.page.locator('table').getByText(descricao);
  }
}

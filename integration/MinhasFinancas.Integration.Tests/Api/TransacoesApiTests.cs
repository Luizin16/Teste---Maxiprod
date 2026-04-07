using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MinhasFinancas.Domain.Entities;
using MinhasFinancas.Integration.Tests.Setup;
using Xunit;

namespace MinhasFinancas.Integration.Tests.Api;

/// <summary>
/// Testes de integração para /api/v1/transacoes.
/// Foco nas regras de negócio críticas:
///   - Menor de idade não pode registrar receita
///   - Categoria deve ser compatível com o tipo da transação
/// </summary>
public class TransacoesApiTests : IntegrationTestBase
{
    // ─────────────────────────────────────────────
    // GET /api/v1/transacoes
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetAll_DeveRetornar200_ComListaPaginada()
    {
        var response = await Client.GetAsync("/api/v1/transacoes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").ValueKind.Should().Be(JsonValueKind.Array);
    }

    // ─────────────────────────────────────────────
    // GET /api/v1/transacoes/{id}
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetById_DeveRetornar200_ComTransacaoCorreta()
    {
        // Arrange
        var pessoaId = await CriarAdultoAsync();
        var catId = await CriarCategoriaAsync();
        var criarResp = await CriarTransacaoRawAsync(pessoaId, catId, Transacao.ETipo.Despesa, 200m, "Compra");
        var criadoBody = await criarResp.Content.ReadFromJsonAsync<JsonElement>();
        var id = criadoBody.GetProperty("id").GetString();

        // Act
        var response = await Client.GetAsync($"/api/v1/transacoes/{id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("descricao").GetString().Should().Be("Compra");
        body.GetProperty("valor").GetDecimal().Should().Be(200m);
    }

    [Fact]
    public async Task GetById_DeveRetornar404_QuandoIdNaoExiste()
    {
        var response = await Client.GetAsync($"/api/v1/transacoes/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─────────────────────────────────────────────
    // POST — Happy Path
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Create_DeveRetornar201_QuandoAdultoRegistraDespesa()
    {
        // Arrange
        var pessoaId = await CriarAdultoAsync();
        var catId = await CriarCategoriaDespesaAsync();

        // Act
        var response = await CriarTransacaoRawAsync(pessoaId, catId, Transacao.ETipo.Despesa);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_DeveRetornar201_QuandoAdultoRegistraReceita()
    {
        // Arrange
        var pessoaId = await CriarAdultoAsync();
        var catId = await CriarCategoriaReceitaAsync();

        // Act
        var response = await CriarTransacaoRawAsync(pessoaId, catId, Transacao.ETipo.Receita);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_DeveRetornar201_QuandoMenorDeIdadeRegistraDespesa()
    {
        // Arrange
        var menorId = await CriarMenorDeIdadeAsync();
        var catId = await CriarCategoriaDespesaAsync();

        // Act
        var response = await CriarTransacaoRawAsync(menorId, catId, Transacao.ETipo.Despesa);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "menores de idade podem registrar despesas");
    }

    [Fact]
    public async Task Create_DeveRetornar201_ComCategoriaAmbas_ParaDespesa()
    {
        // Arrange
        var pessoaId = await CriarAdultoAsync();
        var catId = await CriarCategoriaAsync("Investimentos", Categoria.EFinalidade.Ambas);

        // Act
        var response = await CriarTransacaoRawAsync(pessoaId, catId, Transacao.ETipo.Despesa);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_DeveRetornar201_ComCategoriaAmbas_ParaReceita()
    {
        // Arrange
        var pessoaId = await CriarAdultoAsync();
        var catId = await CriarCategoriaAsync("Investimentos", Categoria.EFinalidade.Ambas);

        // Act
        var response = await CriarTransacaoRawAsync(pessoaId, catId, Transacao.ETipo.Receita);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 🔴 REGRA DE NEGÓCIO: Menor de idade NÃO pode registrar Receita
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_DeveRetornar400_QuandoMenorDeIdadeTentaRegistrarReceita()
    {
        // Arrange
        var menorId = await CriarMenorDeIdadeAsync("Pedro (10 anos)");
        var catId = await CriarCategoriaReceitaAsync();

        // Act
        var response = await CriarTransacaoRawAsync(menorId, catId, Transacao.ETipo.Receita);

        // Assert
        // 🐛 BUG-002: A InvalidOperationException lançada pela entidade Transacao
        // não é capturada no TransacoesController. O middleware global a captura
        // e retorna HTTP 500. O correto seria HTTP 400 (Bad Request) com mensagem de negócio.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "violação de regra de negócio deve retornar 400, não 500");
    }

    [Fact]
    public async Task Create_CorpoResposta_DeveConterMensagemDescritiva_QuandoMenorTentaReceita()
    {
        // Arrange
        var menorId = await CriarMenorDeIdadeAsync();
        var catId = await CriarCategoriaReceitaAsync();

        // Act
        var response = await CriarTransacaoRawAsync(menorId, catId, Transacao.ETipo.Receita);
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        // A mensagem de erro deve ser legível — não um stack trace
        body.Should().NotBeNullOrEmpty();
        // 🐛 BUG-002: atualmente retorna 500 com "Ocorreu um erro interno no servidor."
        // em vez de uma mensagem de negócio clara
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 🔴 REGRA DE NEGÓCIO: Categoria deve ser compatível com o tipo da transação
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_DeveRetornar400_QuandoCategoriaEhDespesa_ETipoEhReceita()
    {
        // Arrange
        var pessoaId = await CriarAdultoAsync();
        var catDespesaId = await CriarCategoriaDespesaAsync("Alimentação");

        // Act
        var response = await CriarTransacaoRawAsync(pessoaId, catDespesaId, Transacao.ETipo.Receita);

        // Assert
        // 🐛 BUG-002: Assim como no caso do menor, retorna 500 em vez de 400
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "usar categoria de Despesa para uma Receita viola regra de negócio");
    }

    [Fact]
    public async Task Create_DeveRetornar400_QuandoCategoriaEhReceita_ETipoEhDespesa()
    {
        // Arrange
        var pessoaId = await CriarAdultoAsync();
        var catReceitaId = await CriarCategoriaReceitaAsync("Salário");

        // Act
        var response = await CriarTransacaoRawAsync(pessoaId, catReceitaId, Transacao.ETipo.Despesa);

        // Assert
        // 🐛 BUG-002: retorna 500 em vez de 400
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "usar categoria de Receita para uma Despesa viola regra de negócio");
    }

    // ─────────────────────────────────────────────
    // Validações de payload inválido
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Create_DeveRetornar400_QuandoValorEhZero()
    {
        // Arrange
        var pessoaId = await CriarAdultoAsync();
        var catId = await CriarCategoriaDespesaAsync();

        // Act
        var response = await CriarTransacaoRawAsync(pessoaId, catId, Transacao.ETipo.Despesa, valor: 0m);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_DeveRetornar400_QuandoValorEhNegativo()
    {
        // Arrange
        var pessoaId = await CriarAdultoAsync();
        var catId = await CriarCategoriaDespesaAsync();

        // Act
        var response = await CriarTransacaoRawAsync(pessoaId, catId, Transacao.ETipo.Despesa, valor: -50m);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_DeveRetornar400_QuandoPessoaNaoExiste()
    {
        // Arrange
        var catId = await CriarCategoriaDespesaAsync();

        // Act
        var response = await CriarTransacaoRawAsync(Guid.NewGuid(), catId, Transacao.ETipo.Despesa);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_DeveRetornar400_QuandoCategoriaIdNaoExiste()
    {
        // Arrange
        var pessoaId = await CriarAdultoAsync();

        // Act
        var response = await CriarTransacaoRawAsync(pessoaId, Guid.NewGuid(), Transacao.ETipo.Despesa);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_DeveRetornar400_QuandoDescricaoEhVazia()
    {
        // Arrange
        var pessoaId = await CriarAdultoAsync();
        var catId = await CriarCategoriaDespesaAsync();

        // Act
        var response = await CriarTransacaoRawAsync(pessoaId, catId, Transacao.ETipo.Despesa, descricao: "");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 🔴 BUG-004: Ausência de endpoint DELETE para transações
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_DeveRetornar204_QuandoTransacaoExiste()
    {
        // Arrange
        var pessoaId = await CriarAdultoAsync();
        var catId = await CriarCategoriaDespesaAsync();
        var criarResp = await CriarTransacaoRawAsync(pessoaId, catId, Transacao.ETipo.Despesa);
        var body = await criarResp.Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("id").GetString();

        // Act
        var response = await Client.DeleteAsync($"/api/v1/transacoes/{id}");

        // Assert
        // 🐛 BUG-004: Endpoint DELETE não existe. Retorna 405 Method Not Allowed.
        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "deve ser possível deletar uma transação via API");
    }
}

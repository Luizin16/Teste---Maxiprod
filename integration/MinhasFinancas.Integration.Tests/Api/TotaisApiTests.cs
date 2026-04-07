using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MinhasFinancas.Domain.Entities;
using MinhasFinancas.Integration.Tests.Setup;
using Xunit;

namespace MinhasFinancas.Integration.Tests.Api;

/// <summary>
/// Testes de integração para /api/v1/totais.
/// Valida os cálculos de receitas, despesas e saldo por pessoa e por categoria.
/// </summary>
public class TotaisApiTests : IntegrationTestBase
{
    // ─────────────────────────────────────────────
    // GET /api/v1/totais/pessoas
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetTotaisPorPessoa_DeveRetornar200_ComListaPaginada()
    {
        var response = await Client.GetAsync("/api/v1/totais/pessoas");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetTotaisPorPessoa_DeveCalcularReceitas_Corretamente()
    {
        // Arrange
        var pessoaId = await CriarAdultoAsync("Calculista Receita");
        var catReceitaId = await CriarCategoriaReceitaAsync("Salário");

        await CriarTransacaoRawAsync(pessoaId, catReceitaId, Transacao.ETipo.Receita, 1000m, "Salário 1");
        await CriarTransacaoRawAsync(pessoaId, catReceitaId, Transacao.ETipo.Receita, 500m, "Salário 2");

        // Act
        var response = await Client.GetAsync("/api/v1/totais/pessoas");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = body.GetProperty("items");

        // Assert
        var pessoa = EncontrarPessoa(items, pessoaId);
        pessoa.Should().NotBeNull("a pessoa deve aparecer nos totais");
        pessoa!.Value.GetProperty("totalReceitas").GetDecimal().Should().Be(1500m);
    }

    [Fact]
    public async Task GetTotaisPorPessoa_DeveCalcularDespesas_Corretamente()
    {
        // Arrange
        var pessoaId = await CriarAdultoAsync("Calculista Despesa");
        var catDespesaId = await CriarCategoriaDespesaAsync("Alimentação");

        await CriarTransacaoRawAsync(pessoaId, catDespesaId, Transacao.ETipo.Despesa, 200m, "Supermercado");
        await CriarTransacaoRawAsync(pessoaId, catDespesaId, Transacao.ETipo.Despesa, 150m, "Restaurante");

        // Act
        var response = await Client.GetAsync("/api/v1/totais/pessoas");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = body.GetProperty("items");

        // Assert
        var pessoa = EncontrarPessoa(items, pessoaId);
        pessoa.Should().NotBeNull();
        pessoa!.Value.GetProperty("totalDespesas").GetDecimal().Should().Be(350m);
    }

    [Fact]
    public async Task GetTotaisPorPessoa_DeveCalcularSaldo_Corretamente()
    {
        // Arrange
        var pessoaId = await CriarAdultoAsync("Calculista Saldo");
        var catReceitaId = await CriarCategoriaReceitaAsync();
        var catDespesaId = await CriarCategoriaDespesaAsync();

        await CriarTransacaoRawAsync(pessoaId, catReceitaId, Transacao.ETipo.Receita, 3000m, "Salário");
        await CriarTransacaoRawAsync(pessoaId, catDespesaId, Transacao.ETipo.Despesa, 1000m, "Aluguel");

        // Act
        var response = await Client.GetAsync("/api/v1/totais/pessoas");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var pessoa = EncontrarPessoa(body.GetProperty("items"), pessoaId);

        // Assert — saldo = receitas - despesas = 3000 - 1000 = 2000
        pessoa.Should().NotBeNull();
        pessoa!.Value.GetProperty("saldo").GetDecimal().Should().Be(2000m);
    }

    // ─────────────────────────────────────────────
    // GET /api/v1/totais/categorias
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetTotaisPorCategoria_DeveRetornar200_ComListaPaginada()
    {
        var response = await Client.GetAsync("/api/v1/totais/categorias");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetTotaisPorCategoria_DeveSomar_TransacoesDaMesmaCategoria()
    {
        // Arrange
        var pessoaId = await CriarAdultoAsync();
        var catId = await CriarCategoriaDespesaAsync("Transporte Especial");

        await CriarTransacaoRawAsync(pessoaId, catId, Transacao.ETipo.Despesa, 50m, "Uber");
        await CriarTransacaoRawAsync(pessoaId, catId, Transacao.ETipo.Despesa, 30m, "Ônibus");

        // Act
        var response = await Client.GetAsync("/api/v1/totais/categorias");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = body.GetProperty("items");

        // Assert
        var catItem = EncontrarCategoria(items, catId);
        catItem.Should().NotBeNull();
        catItem!.Value.GetProperty("totalDespesas").GetDecimal().Should().Be(80m);
    }

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────

    private static JsonElement? EncontrarPessoa(JsonElement items, Guid pessoaId)
    {
        for (int i = 0; i < items.GetArrayLength(); i++)
        {
            var item = items[i];
            if (item.GetProperty("pessoaId").GetString() == pessoaId.ToString())
                return item;
        }
        return null;
    }

    private static JsonElement? EncontrarCategoria(JsonElement items, Guid categoriaId)
    {
        for (int i = 0; i < items.GetArrayLength(); i++)
        {
            var item = items[i];
            if (item.GetProperty("categoriaId").GetString() == categoriaId.ToString())
                return item;
        }
        return null;
    }
}

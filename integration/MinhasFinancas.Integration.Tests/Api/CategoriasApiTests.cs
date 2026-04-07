using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MinhasFinancas.Domain.Entities;
using MinhasFinancas.Integration.Tests.Setup;
using Xunit;

namespace MinhasFinancas.Integration.Tests.Api;

/// <summary>
/// Testes de integração para /api/v1/categorias.
/// Valida CRUD e finalidades disponíveis.
/// </summary>
public class CategoriasApiTests : IntegrationTestBase
{
    // ─────────────────────────────────────────────
    // GET /api/v1/categorias
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetAll_DeveRetornar200_ComListaPaginada()
    {
        var response = await Client.GetAsync("/api/v1/categorias");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetAll_DeveFiltrarPorBusca_QuandoSearchFornecido()
    {
        // Arrange
        await CriarCategoriaAsync("Alimentação Especial");
        await CriarCategoriaAsync("Transporte Urbano");

        // Act
        var response = await Client.GetAsync("/api/v1/categorias?search=Alimentação");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = body.GetProperty("items");

        // Assert
        items.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
        var descricoes = Enumerable.Range(0, items.GetArrayLength())
            .Select(i => items[i].GetProperty("descricao").GetString())
            .ToList();
        descricoes.Should().Contain(d => d!.Contains("Alimentação"));
    }

    // ─────────────────────────────────────────────
    // GET /api/v1/categorias/{id}
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetById_DeveRetornar200_ComCategoriaCorreta()
    {
        // Arrange
        var id = await CriarCategoriaAsync("Salário", Categoria.EFinalidade.Receita);

        // Act
        var response = await Client.GetAsync($"/api/v1/categorias/{id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("descricao").GetString().Should().Be("Salário");
        body.GetProperty("id").GetString().Should().Be(id.ToString());
    }

    [Fact]
    public async Task GetById_DeveRetornar404_QuandoIdNaoExiste()
    {
        var response = await Client.GetAsync($"/api/v1/categorias/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─────────────────────────────────────────────
    // POST /api/v1/categorias
    // ─────────────────────────────────────────────

    [Theory]
    [InlineData("Alimentação", 0)]  // Despesa = 0
    [InlineData("Salário", 1)]      // Receita = 1
    [InlineData("Investimentos", 2)] // Ambas = 2
    public async Task Create_DeveRetornar201_ParaTodasAsFinalidades(string descricao, int finalidade)
    {
        // Arrange
        var payload = new { descricao, finalidade };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/categorias", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("descricao").GetString().Should().Be(descricao);
    }

    [Fact]
    public async Task Create_DeveRetornar400_QuandoDescricaoEhVazia()
    {
        // Arrange
        var payload = new { descricao = "", finalidade = 0 };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/categorias", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_DeveRetornar400_QuandoDescricaoUltrapassaLimiteDeCaracteres()
    {
        // Arrange — 201 chars > limite de 200
        var payload = new { descricao = new string('A', 201), finalidade = 0 };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/categorias", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 🔴 BUG-005: Ausência de endpoints PUT e DELETE para categorias
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_DeveRetornar204_QuandoDadosValidos()
    {
        // Arrange
        var id = await CriarCategoriaAsync("Categoria Original");
        var payload = new { descricao = "Categoria Atualizada", finalidade = 0 };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/v1/categorias/{id}", payload);

        // Assert
        // 🐛 BUG-005: endpoint PUT não existe. Retorna 405 Method Not Allowed.
        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "deve ser possível atualizar uma categoria");
    }

    [Fact]
    public async Task Delete_DeveRetornar204_QuandoCategoriaExiste()
    {
        // Arrange
        var id = await CriarCategoriaAsync("Para Deletar");

        // Act
        var response = await Client.DeleteAsync($"/api/v1/categorias/{id}");

        // Assert
        // 🐛 BUG-005: endpoint DELETE não existe. Retorna 405 Method Not Allowed.
        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "deve ser possível deletar uma categoria");
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MinhasFinancas.Integration.Tests.Setup;
using Xunit;

namespace MinhasFinancas.Integration.Tests.Api;

/// <summary>
/// Testes de integração para o endpoint /api/v1/pessoas.
/// Valida CRUD completo e regras de validação.
/// </summary>
public class PessoasApiTests : IntegrationTestBase
{
    // ─────────────────────────────────────────────
    // GET /api/v1/pessoas
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetAll_DeveRetornar200_ComListaPaginada()
    {
        // Act
        var response = await Client.GetAsync("/api/v1/pessoas");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").ValueKind.Should().Be(JsonValueKind.Array);
        body.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetAll_DeveRetornar200_QuandoNaoHaPessoas()
    {
        // Act
        var response = await Client.GetAsync("/api/v1/pessoas?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─────────────────────────────────────────────
    // GET /api/v1/pessoas/{id}
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetById_DeveRetornar200_ComPessoaCorreta()
    {
        // Arrange
        var (id, nome) = await CriarPessoaAsync("João Silva");

        // Act
        var response = await Client.GetAsync($"/api/v1/pessoas/{id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetString().Should().Be(id.ToString());
        body.GetProperty("nome").GetString().Should().Be("João Silva");
        body.GetProperty("idade").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetById_DeveRetornar404_QuandoIdNaoExiste()
    {
        // Act
        var response = await Client.GetAsync($"/api/v1/pessoas/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─────────────────────────────────────────────
    // POST /api/v1/pessoas
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Create_DeveRetornar201_ComPessoaCriada()
    {
        // Arrange
        var payload = new
        {
            nome = "Maria Santos",
            dataNascimento = DateTime.Today.AddYears(-25).ToString("yyyy-MM-dd")
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/pessoas", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("nome").GetString().Should().Be("Maria Santos");
        body.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Create_DeveRetornar400_QuandoNomeEhVazio()
    {
        // Arrange
        var payload = new
        {
            nome = "",
            dataNascimento = DateTime.Today.AddYears(-20).ToString("yyyy-MM-dd")
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/pessoas", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_DeveRetornar400_QuandoDataNascimentoEhFutura()
    {
        // Arrange
        var payload = new
        {
            nome = "Futuro",
            dataNascimento = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd")
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/pessoas", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_DevePersistir_IdadeCalculada_Corretamente()
    {
        // Arrange
        var nascimento = DateTime.Today.AddYears(-17);
        var payload = new
        {
            nome = "Menor de Idade",
            dataNascimento = nascimento.ToString("yyyy-MM-dd")
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/pessoas", payload);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        body.GetProperty("idade").GetInt32().Should().Be(17);
    }

    // ─────────────────────────────────────────────
    // PUT /api/v1/pessoas/{id}
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Update_DeveRetornar204_QuandoDadosValidos()
    {
        // Arrange
        var (id, _) = await CriarPessoaAsync("Nome Antigo");
        var payload = new
        {
            nome = "Nome Novo",
            dataNascimento = DateTime.Today.AddYears(-30).ToString("yyyy-MM-dd")
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/v1/pessoas/{id}", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Update_DeveAtualizarDados_Corretamente()
    {
        // Arrange
        var (id, _) = await CriarPessoaAsync("Nome Antigo");
        var payload = new
        {
            nome = "Nome Atualizado",
            dataNascimento = DateTime.Today.AddYears(-25).ToString("yyyy-MM-dd")
        };

        // Act
        await Client.PutAsJsonAsync($"/api/v1/pessoas/{id}", payload);
        var getResponse = await Client.GetAsync($"/api/v1/pessoas/{id}");
        var body = await getResponse.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        body.GetProperty("nome").GetString().Should().Be("Nome Atualizado");
    }

    [Fact]
    public async Task Update_DeveRetornar404_QuandoPessoaNaoExiste()
    {
        // Arrange
        var payload = new
        {
            nome = "Qualquer",
            dataNascimento = DateTime.Today.AddYears(-20).ToString("yyyy-MM-dd")
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/v1/pessoas/{Guid.NewGuid()}", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─────────────────────────────────────────────
    // DELETE /api/v1/pessoas/{id}
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Delete_DeveRetornar204_QuandoPessoaExiste()
    {
        // Arrange
        var (id, _) = await CriarPessoaAsync("Para Deletar");

        // Act
        var response = await Client.DeleteAsync($"/api/v1/pessoas/{id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_DeveTornarPessoaInacessivel_AposExclusao()
    {
        // Arrange
        var (id, _) = await CriarPessoaAsync("Para Deletar");

        // Act
        await Client.DeleteAsync($"/api/v1/pessoas/{id}");
        var getResponse = await Client.GetAsync($"/api/v1/pessoas/{id}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_DeveRetornar404_QuandoPessoaNaoExiste()
    {
        // Act
        var response = await Client.DeleteAsync($"/api/v1/pessoas/{Guid.NewGuid()}");

        // Assert
        // ⚠️ BUG-001 adjacente: comportamento esperado é 404,
        // mas o repositório silencia a ausência (não lança KeyNotFoundException no Delete por ID)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 🔴 BUG-001: Cascade delete — transações devem ser removidas com a pessoa
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_DeveRemoverTransacoesAssociadas_QuandoPessoaEhExcluida()
    {
        // Arrange
        var pessoaId = await CriarAdultoAsync("Pessoa com transações");
        var categoriaId = await CriarCategoriaAsync("Cat Ambas");

        // Criar 2 transações para essa pessoa
        var r1 = await CriarTransacaoRawAsync(pessoaId, categoriaId, Domain.Entities.Transacao.ETipo.Despesa, 50m, "T1");
        var r2 = await CriarTransacaoRawAsync(pessoaId, categoriaId, Domain.Entities.Transacao.ETipo.Despesa, 75m, "T2");
        r1.EnsureSuccessStatusCode();
        r2.EnsureSuccessStatusCode();

        var body1 = await r1.Content.ReadFromJsonAsync<JsonElement>();
        var body2 = await r2.Content.ReadFromJsonAsync<JsonElement>();
        var t1Id = body1.GetProperty("id").GetString();
        var t2Id = body2.GetProperty("id").GetString();

        // Act — deletar a pessoa
        var deleteResponse = await Client.DeleteAsync($"/api/v1/pessoas/{pessoaId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert — transações devem ter sido removidas em cascata
        // 🐛 BUG-001: Este teste FALHA porque cascade delete não está configurado no DbContext.
        // O relacionamento Transacao->Pessoa não possui .OnDelete(DeleteBehavior.Cascade),
        // portanto as transações ficam órfãs no banco.
        var t1Response = await Client.GetAsync($"/api/v1/transacoes/{t1Id}");
        var t2Response = await Client.GetAsync($"/api/v1/transacoes/{t2Id}");

        t1Response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "a transação deveria ter sido removida em cascata junto com a pessoa");
        t2Response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "a transação deveria ter sido removida em cascata junto com a pessoa");
    }
}

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MinhasFinancas.Domain.Entities;

namespace MinhasFinancas.Integration.Tests.Setup;

/// <summary>
/// Classe base para testes de integração.
/// Fornece factory, client e helpers compartilhados.
/// </summary>
public abstract class IntegrationTestBase : IDisposable
{
    protected readonly TestWebApplicationFactory Factory;
    protected readonly HttpClient Client;

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    protected IntegrationTestBase()
    {
        Factory = new TestWebApplicationFactory();
        Client = Factory.CreateApiClient();
    }

    // ── Helpers de Pessoa ──────────────────────────────

    protected async Task<(Guid Id, string Nome)> CriarPessoaAsync(
        string nome = "Pessoa Teste",
        DateTime? dataNascimento = null)
    {
        var payload = new
        {
            nome,
            dataNascimento = (dataNascimento ?? DateTime.Today.AddYears(-30)).ToString("yyyy-MM-dd")
        };

        var response = await Client.PostAsJsonAsync("/api/v1/pessoas", payload);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = Guid.Parse(body.GetProperty("id").GetString()!);

        return (id, nome);
    }

    protected async Task<Guid> CriarMenorDeIdadeAsync(string nome = "Menor Teste")
    {
        var (id, _) = await CriarPessoaAsync(nome, DateTime.Today.AddYears(-10));
        return id;
    }

    protected async Task<Guid> CriarAdultoAsync(string nome = "Adulto Teste")
    {
        var (id, _) = await CriarPessoaAsync(nome, DateTime.Today.AddYears(-30));
        return id;
    }

    // ── Helpers de Categoria ──────────────────────────────

    protected async Task<Guid> CriarCategoriaAsync(
        string descricao = "Categoria Teste",
        Categoria.EFinalidade finalidade = Categoria.EFinalidade.Ambas)
    {
        var payload = new { descricao, finalidade = (int)finalidade };
        var response = await Client.PostAsJsonAsync("/api/v1/categorias", payload);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return Guid.Parse(body.GetProperty("id").GetString()!);
    }

    protected async Task<Guid> CriarCategoriaReceitaAsync(string descricao = "Salário") =>
        await CriarCategoriaAsync(descricao, Categoria.EFinalidade.Receita);

    protected async Task<Guid> CriarCategoriaDespesaAsync(string descricao = "Alimentação") =>
        await CriarCategoriaAsync(descricao, Categoria.EFinalidade.Despesa);

    // ── Helpers de Transação ──────────────────────────────

    protected async Task<HttpResponseMessage> CriarTransacaoRawAsync(
        Guid pessoaId,
        Guid categoriaId,
        Transacao.ETipo tipo,
        decimal valor = 100m,
        string descricao = "Transação Teste")
    {
        var payload = new
        {
            descricao,
            valor,
            tipo = (int)tipo,
            categoriaId,
            pessoaId,
            data = DateTime.Today.ToString("yyyy-MM-dd")
        };

        return await Client.PostAsJsonAsync("/api/v1/transacoes", payload);
    }

    public void Dispose()
    {
        Client.Dispose();
        Factory.Dispose();
    }
}

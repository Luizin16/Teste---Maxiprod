using FluentAssertions;
using MinhasFinancas.Domain.Entities;
using Xunit;

namespace MinhasFinancas.Unit.Tests.Domain;

/// <summary>
/// Testes unitários para a entidade Categoria.
/// Foco: método PermiteTipo — regra de negócio de compatibilidade de finalidade.
/// </summary>
public class CategoriaTests
{
    // ─────────────────────────────────────────────
    // Categoria de Despesa
    // ─────────────────────────────────────────────

    [Fact]
    public void PermiteTipo_DeveRetornarTrue_QuandoCategoriaEhDespesa_ETipoEhDespesa()
    {
        // Arrange
        var categoria = new Categoria
        {
            Descricao = "Alimentação",
            Finalidade = Categoria.EFinalidade.Despesa
        };

        // Act & Assert
        categoria.PermiteTipo(Transacao.ETipo.Despesa).Should().BeTrue();
    }

    [Fact]
    public void PermiteTipo_DeveRetornarFalse_QuandoCategoriaEhDespesa_ETipoEhReceita()
    {
        // Arrange
        var categoria = new Categoria
        {
            Descricao = "Alimentação",
            Finalidade = Categoria.EFinalidade.Despesa
        };

        // Act & Assert
        categoria.PermiteTipo(Transacao.ETipo.Receita).Should().BeFalse(
            "categoria de Despesa não deve aceitar Receita");
    }

    // ─────────────────────────────────────────────
    // Categoria de Receita
    // ─────────────────────────────────────────────

    [Fact]
    public void PermiteTipo_DeveRetornarTrue_QuandoCategoriaEhReceita_ETipoEhReceita()
    {
        // Arrange
        var categoria = new Categoria
        {
            Descricao = "Salário",
            Finalidade = Categoria.EFinalidade.Receita
        };

        // Act & Assert
        categoria.PermiteTipo(Transacao.ETipo.Receita).Should().BeTrue();
    }

    [Fact]
    public void PermiteTipo_DeveRetornarFalse_QuandoCategoriaEhReceita_ETipoEhDespesa()
    {
        // Arrange
        var categoria = new Categoria
        {
            Descricao = "Salário",
            Finalidade = Categoria.EFinalidade.Receita
        };

        // Act & Assert
        categoria.PermiteTipo(Transacao.ETipo.Despesa).Should().BeFalse(
            "categoria de Receita não deve aceitar Despesa");
    }

    // ─────────────────────────────────────────────
    // Categoria Ambas
    // ─────────────────────────────────────────────

    [Fact]
    public void PermiteTipo_DeveRetornarTrue_QuandoCategoriaEhAmbas_ETipoEhDespesa()
    {
        // Arrange
        var categoria = new Categoria
        {
            Descricao = "Investimentos",
            Finalidade = Categoria.EFinalidade.Ambas
        };

        // Act & Assert
        categoria.PermiteTipo(Transacao.ETipo.Despesa).Should().BeTrue();
    }

    [Fact]
    public void PermiteTipo_DeveRetornarTrue_QuandoCategoriaEhAmbas_ETipoEhReceita()
    {
        // Arrange
        var categoria = new Categoria
        {
            Descricao = "Investimentos",
            Finalidade = Categoria.EFinalidade.Ambas
        };

        // Act & Assert
        categoria.PermiteTipo(Transacao.ETipo.Receita).Should().BeTrue();
    }
}

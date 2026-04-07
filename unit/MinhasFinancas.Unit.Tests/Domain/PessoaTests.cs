using FluentAssertions;
using MinhasFinancas.Domain.Entities;
using Xunit;

namespace MinhasFinancas.Unit.Tests.Domain;

/// <summary>
/// Testes unitários para a entidade Pessoa.
/// Valida cálculo de idade e a regra de maioridade.
/// </summary>
public class PessoaTests
{
    // ─────────────────────────────────────────────
    // Cálculo de idade
    // ─────────────────────────────────────────────

    [Fact]
    public void Idade_DeveSer_CalculadaCorretamente_ParaMaiorDeIdade()
    {
        // Arrange
        var nascimento = DateTime.Today.AddYears(-30);
        var pessoa = new Pessoa { Nome = "Adulto", DataNascimento = nascimento };

        // Act & Assert
        pessoa.Idade.Should().Be(30);
    }

    [Fact]
    public void Idade_DeveSer_CalculadaCorretamente_QuandoAniversarioAindaNaoOcorreuNoAno()
    {
        // Arrange – nasceu amanhã, mas no ano passado, logo ainda não fez aniversário
        var nascimento = DateTime.Today.AddYears(-18).AddDays(1);
        var pessoa = new Pessoa { Nome = "Quase adulto", DataNascimento = nascimento };

        // Act & Assert
        pessoa.Idade.Should().Be(17, "o aniversário de 18 anos ainda não ocorreu");
    }

    [Fact]
    public void Idade_DeveSer_18_QuandoAniversarioEHoje()
    {
        // Arrange
        var nascimento = DateTime.Today.AddYears(-18);
        var pessoa = new Pessoa { Nome = "Exatos 18", DataNascimento = nascimento };

        // Act & Assert
        pessoa.Idade.Should().Be(18);
    }

    [Fact]
    public void Idade_DeveSer_CalculadaCorretamente_ParaMenorDeIdade()
    {
        // Arrange
        var nascimento = DateTime.Today.AddYears(-10);
        var pessoa = new Pessoa { Nome = "Criança", DataNascimento = nascimento };

        // Act & Assert
        pessoa.Idade.Should().Be(10);
    }

    // ─────────────────────────────────────────────
    // EhMaiorDeIdade
    // ─────────────────────────────────────────────

    [Fact]
    public void EhMaiorDeIdade_DeveRetornarTrue_ParaPessoaCom18Anos()
    {
        // Arrange
        var pessoa = new Pessoa
        {
            Nome = "Adulto",
            DataNascimento = DateTime.Today.AddYears(-18)
        };

        // Act & Assert
        pessoa.EhMaiorDeIdade().Should().BeTrue();
    }

    [Fact]
    public void EhMaiorDeIdade_DeveRetornarTrue_ParaPessoaCom30Anos()
    {
        // Arrange
        var pessoa = new Pessoa
        {
            Nome = "Adulto Pleno",
            DataNascimento = DateTime.Today.AddYears(-30)
        };

        // Act & Assert
        pessoa.EhMaiorDeIdade().Should().BeTrue();
    }

    [Fact]
    public void EhMaiorDeIdade_DeveRetornarFalse_ParaPessoaCom17Anos()
    {
        // Arrange
        var pessoa = new Pessoa
        {
            Nome = "Menor",
            DataNascimento = DateTime.Today.AddYears(-17)
        };

        // Act & Assert
        pessoa.EhMaiorDeIdade().Should().BeFalse();
    }

    [Fact]
    public void EhMaiorDeIdade_DeveRetornarFalse_ParaCriancaDe5Anos()
    {
        // Arrange
        var pessoa = new Pessoa
        {
            Nome = "Criança",
            DataNascimento = DateTime.Today.AddYears(-5)
        };

        // Act & Assert
        pessoa.EhMaiorDeIdade().Should().BeFalse();
    }

    [Fact]
    public void EhMaiorDeIdade_DeveRetornarFalse_QuandoFazAniversarioAmanha()
    {
        // Arrange – exatamente 1 dia antes de completar 18 anos
        var pessoa = new Pessoa
        {
            Nome = "Quase Adulto",
            DataNascimento = DateTime.Today.AddYears(-18).AddDays(1)
        };

        // Act & Assert
        pessoa.EhMaiorDeIdade().Should().BeFalse(
            "ainda não completou 18 anos — o aniversário é amanhã");
    }
}

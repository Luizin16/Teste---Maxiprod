using FluentAssertions;
using MinhasFinancas.Domain.Entities;
using Xunit;

namespace MinhasFinancas.Unit.Tests.Domain;

/// <summary>
/// Testes unitários para a entidade Transacao.
/// Foco: regras de negócio aplicadas nos property setters:
///   - Menor de idade não pode ter Receita
///   - Categoria incompatível lança exceção
/// </summary>
public class TransacaoTests
{
    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────

    private static Pessoa CriarMaiorDeIdade(string nome = "Adulto") =>
        new() { Nome = nome, DataNascimento = DateTime.Today.AddYears(-30) };

    private static Pessoa CriarMenorDeIdade(string nome = "Menor") =>
        new() { Nome = nome, DataNascimento = DateTime.Today.AddYears(-10) };

    private static Categoria CriarCategoria(Categoria.EFinalidade finalidade, string descricao = "Cat") =>
        new() { Descricao = descricao, Finalidade = finalidade };

    private static Transacao CriarTransacaoBase(Transacao.ETipo tipo) =>
        new() { Descricao = "Teste", Valor = 100m, Tipo = tipo };

    // ─────────────────────────────────────────────
    // REGRA: Menor de idade não pode ter Receita
    // ─────────────────────────────────────────────

    [Fact]
    public void AtribuirPessoa_DeveLancarExcecao_QuandoMenorDeIdadeTentaRegistrarReceita()
    {
        // Arrange
        var transacao = CriarTransacaoBase(Transacao.ETipo.Receita);
        var menor = CriarMenorDeIdade();

        // Act
        var act = () => transacao.Pessoa = menor;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*menor*18*", "a mensagem deve indicar a restrição para menores");
    }

    [Fact]
    public void AtribuirPessoa_NaoDeveLancarExcecao_QuandoMenorDeIdadeRegistraDespesa()
    {
        // Arrange
        var transacao = CriarTransacaoBase(Transacao.ETipo.Despesa);
        var menor = CriarMenorDeIdade();

        // Act
        var act = () => transacao.Pessoa = menor;

        // Assert
        act.Should().NotThrow("menores de idade podem registrar despesas");
    }

    [Fact]
    public void AtribuirPessoa_NaoDeveLancarExcecao_QuandoAdultoRegistraReceita()
    {
        // Arrange
        var transacao = CriarTransacaoBase(Transacao.ETipo.Receita);
        var adulto = CriarMaiorDeIdade();

        // Act
        var act = () => transacao.Pessoa = adulto;

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AtribuirPessoa_NaoDeveLancarExcecao_QuandoAdultoRegistraDespesa()
    {
        // Arrange
        var transacao = CriarTransacaoBase(Transacao.ETipo.Despesa);
        var adulto = CriarMaiorDeIdade();

        // Act
        var act = () => transacao.Pessoa = adulto;

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AtribuirPessoa_ComExatamente18Anos_DevePermitirReceita()
    {
        // Arrange – borda: exatos 18 anos (aniversário hoje)
        var transacao = CriarTransacaoBase(Transacao.ETipo.Receita);
        var pessoa = new Pessoa
        {
            Nome = "Exatos 18",
            DataNascimento = DateTime.Today.AddYears(-18)
        };

        // Act
        var act = () => transacao.Pessoa = pessoa;

        // Assert
        act.Should().NotThrow("18 anos completos já é maior de idade");
    }

    [Fact]
    public void AtribuirPessoa_Com17Anos364Dias_NaoDevePermitirReceita()
    {
        // Arrange – 1 dia antes de completar 18
        var transacao = CriarTransacaoBase(Transacao.ETipo.Receita);
        var pessoa = new Pessoa
        {
            Nome = "Quase adulto",
            DataNascimento = DateTime.Today.AddYears(-18).AddDays(1)
        };

        // Act
        var act = () => transacao.Pessoa = pessoa;

        // Assert
        act.Should().Throw<InvalidOperationException>("ainda é menor de idade");
    }

    // ─────────────────────────────────────────────
    // REGRA: Categoria deve ser compatível com o tipo
    // ─────────────────────────────────────────────

    [Fact]
    public void AtribuirCategoria_DeveLancarExcecao_QuandoCategoriaEhDespesa_ETipoTransacaoEhReceita()
    {
        // Arrange
        var transacao = CriarTransacaoBase(Transacao.ETipo.Receita);
        var categoria = CriarCategoria(Categoria.EFinalidade.Despesa);

        // Act
        var act = () => transacao.Categoria = categoria;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*categoria de despesa*", "mensagem deve informar incompatibilidade");
    }

    [Fact]
    public void AtribuirCategoria_DeveLancarExcecao_QuandoCategoriaEhReceita_ETipoTransacaoEhDespesa()
    {
        // Arrange
        var transacao = CriarTransacaoBase(Transacao.ETipo.Despesa);
        var categoria = CriarCategoria(Categoria.EFinalidade.Receita);

        // Act
        var act = () => transacao.Categoria = categoria;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*categoria de receita*", "mensagem deve informar incompatibilidade");
    }

    [Fact]
    public void AtribuirCategoria_NaoDeveLancarExcecao_QuandoCategoriaEhDespesa_ETipoTransacaoEhDespesa()
    {
        // Arrange
        var transacao = CriarTransacaoBase(Transacao.ETipo.Despesa);
        var categoria = CriarCategoria(Categoria.EFinalidade.Despesa);

        // Act
        var act = () => transacao.Categoria = categoria;

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AtribuirCategoria_NaoDeveLancarExcecao_QuandoCategoriaEhReceita_ETipoTransacaoEhReceita()
    {
        // Arrange
        var transacao = CriarTransacaoBase(Transacao.ETipo.Receita);
        var categoria = CriarCategoria(Categoria.EFinalidade.Receita);

        // Act
        var act = () => transacao.Categoria = categoria;

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AtribuirCategoria_Ambas_DevePermitirTantoDespesaQuantoReceita()
    {
        // Arrange
        var categAmb = CriarCategoria(Categoria.EFinalidade.Ambas);

        var transacaoDespesa = CriarTransacaoBase(Transacao.ETipo.Despesa);
        var transacaoReceita = CriarTransacaoBase(Transacao.ETipo.Receita);

        // Act & Assert — ambos não devem lançar
        ((Action)(() => transacaoDespesa.Categoria = categAmb)).Should().NotThrow();
        ((Action)(() => transacaoReceita.Categoria = categAmb)).Should().NotThrow();
    }

    // ─────────────────────────────────────────────
    // Atribuição define CategoriaId / PessoaId
    // ─────────────────────────────────────────────

    [Fact]
    public void AtribuirCategoria_DeveDefinirCategoriaId_Corretamente()
    {
        // Arrange
        var transacao = CriarTransacaoBase(Transacao.ETipo.Despesa);
        var categoria = CriarCategoria(Categoria.EFinalidade.Despesa);

        // Act
        transacao.Categoria = categoria;

        // Assert
        transacao.CategoriaId.Should().Be(categoria.Id);
    }

    [Fact]
    public void AtribuirPessoa_DeveDefinirPessoaId_Corretamente()
    {
        // Arrange
        var transacao = CriarTransacaoBase(Transacao.ETipo.Despesa);
        var pessoa = CriarMaiorDeIdade();

        // Act
        transacao.Pessoa = pessoa;

        // Assert
        transacao.PessoaId.Should().Be(pessoa.Id);
    }

    // ─────────────────────────────────────────────
    // Valor
    // ─────────────────────────────────────────────

    [Theory]
    [InlineData(0.01)]
    [InlineData(1)]
    [InlineData(999999.99)]
    public void Transacao_DeveAceitar_ValoresPositivos(decimal valor)
    {
        // Arrange & Act
        var transacao = new Transacao { Descricao = "Teste", Valor = valor, Tipo = Transacao.ETipo.Despesa };

        // Assert
        transacao.Valor.Should().Be(valor);
    }
}

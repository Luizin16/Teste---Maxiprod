# 🐛 Bugs Encontrados — MinhasFinancas

Documento de rastreamento de falhas identificadas durante a análise e execução dos testes.
> ⚠️ Nenhuma alteração foi feita no código da aplicação. Todos os itens abaixo são apenas documentados.

---

## BUG-001 — Cascade Delete não configurado

| Campo | Detalhe |
|---|---|
| **Severidade** | 🔴 Crítico |
| **Tipo** | Regra de Negócio / Integridade de Dados |
| **Camada** | Infrastructure → `MinhasFinancasDbContext.cs` |
| **Regra violada** | "Exclusão em cascata de transações ao excluir pessoa" |

### Descrição

O requisito funcional especifica que ao excluir uma Pessoa, todas as suas Transações devem ser removidas automaticamente. Porém, o `OnModelCreating` do `MinhasFinancasDbContext` configura os relacionamentos **sem** definir o comportamento de exclusão:

```csharp
// MinhasFinancasDbContext.cs — como está (incorreto)
modelBuilder.Entity<Transacao>()
    .HasOne(t => t.Pessoa)
    .WithMany()
    .HasForeignKey(t => t.PessoaId);
// ← falta: .OnDelete(DeleteBehavior.Cascade)
```

O comportamento padrão do EF Core com SQLite para FK sem configuração explícita é `Restrict` ou `NoAction`, resultando em transações órfãs no banco após a exclusão da pessoa.

### Como Reproduzir

1. Criar uma Pessoa via `POST /api/v1/pessoas`
2. Criar uma Transação para essa pessoa via `POST /api/v1/transacoes`
3. Deletar a pessoa via `DELETE /api/v1/pessoas/{id}` → retorna `204`
4. Verificar via `GET /api/v1/transacoes/{idDaTransacao}` → retorna `200` (transação ainda existe — **deveria ser 404**)

### Validação no Banco

```sql
-- Após deletar a pessoa, verificar transações órfãs:
SELECT * FROM Transacoes WHERE PessoaId = '<id-da-pessoa-deletada>';
-- Resultado esperado: 0 rows
-- Resultado atual:    N rows (bug)
```

### Correção Sugerida

```csharp
modelBuilder.Entity<Transacao>()
    .HasOne(t => t.Pessoa)
    .WithMany(p => p.Transacoes)
    .HasForeignKey(t => t.PessoaId)
    .OnDelete(DeleteBehavior.Cascade); // ← adicionar
```

---

## BUG-002 — `InvalidOperationException` retorna HTTP 500 em vez de 400

| Campo | Detalhe |
|---|---|
| **Severidade** | 🔴 Crítico |
| **Tipo** | Tratamento de Erro / Regra de Negócio |
| **Camada** | API → `TransacoesController.cs` + `ExceptionMiddleware.cs` |
| **Regras violadas** | Menor de idade sem receita · Categoria incompatível |

### Descrição

As regras de negócio críticas são aplicadas nos *property setters* da entidade `Transacao`:

- `Pessoa = value` → lança `InvalidOperationException` se menor + receita
- `Categoria = value` → lança `InvalidOperationException` se tipo incompatível

O `TransacaoService.CreateAsync` não trata essas exceções. O `TransacoesController` captura apenas `ArgumentException`:

```csharp
// TransacoesController.cs — como está (incompleto)
catch (ArgumentException ex)
{
    return BadRequest(ex.Message);
}
// InvalidOperationException não é capturada → vai para o ExceptionMiddleware
```

O `ExceptionMiddleware` captura qualquer exceção e **sempre** retorna `500`:

```csharp
context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
```

### Impacto

| Cenário | Status Atual | Status Esperado |
|---|---|---|
| Menor tenta registrar receita | `500 Internal Server Error` | `400 Bad Request` |
| Categoria Despesa em transação Receita | `500 Internal Server Error` | `400 Bad Request` |
| Categoria Receita em transação Despesa | `500 Internal Server Error` | `400 Bad Request` |

### Como Reproduzir

```bash
# 1. Criar uma pessoa menor de idade
curl -X POST http://localhost:5000/api/v1/pessoas \
  -H "Content-Type: application/json" \
  -d '{"nome":"Criança","dataNascimento":"2016-01-01"}'

# 2. Criar categoria de receita
curl -X POST http://localhost:5000/api/v1/categorias \
  -H "Content-Type: application/json" \
  -d '{"descricao":"Salário","finalidade":1}'

# 3. Tentar criar transação de receita para o menor
curl -X POST http://localhost:5000/api/v1/transacoes \
  -H "Content-Type: application/json" \
  -d '{"descricao":"Test","valor":100,"tipo":1,"categoriaId":"<cat-id>","pessoaId":"<pessoa-id>","data":"2025-01-01"}'

# Resultado atual:   HTTP 500 — "Ocorreu um erro interno no servidor."
# Resultado esperado: HTTP 400 — "Menores de 18 anos não podem registrar receitas."
```

### Correção Sugerida

```csharp
// TransacoesController.cs
catch (InvalidOperationException ex)
{
    return BadRequest(new { message = ex.Message });
}
catch (ArgumentException ex)
{
    return BadRequest(new { message = ex.Message });
}
```

---

## BUG-003 — Filtro de categorias por tipo não é passado no formulário

| Campo | Detalhe |
|---|---|
| **Severidade** | 🟠 Alto |
| **Tipo** | Lógica de UI / UX |
| **Camada** | Frontend → `TransacaoForm.tsx` |

### Descrição

O componente `LazyCategoriaSelect` aceita a prop `selectedTipo` para filtrar categorias compatíveis com o tipo de transação selecionado. Porém, em `TransacaoForm.tsx`, a prop não é passada:

```tsx
// TransacaoForm.tsx — como está (incompleto)
<LazyCategoriaSelect
  value={selectedCategoriaObj}
  onChange={(c) => { ... }}
  error={errors.categoriaId}
  // ← selectedTipo não é passado!
/>
```

O parâmetro `selectedTipo` é declarado na interface do componente:
```tsx
// LazyCategoriaSelect.tsx
interface LazyCategoriaSelectProps {
  selectedTipo?: TipoTransacao; // ← existe, mas nunca é recebido
  ...
}
```

### Impacto

O usuário vê **todas** as categorias independente do tipo de transação selecionado. Isso permite:
- Selecionar categoria de *Receita* para uma transação de *Despesa*
- Selecionar categoria de *Despesa* para uma transação de *Receita*

Se o usuário fizer isso e tentar salvar, a API retorna `500` (BUG-002), gerando uma experiência confusa.

### Correção Sugerida

```tsx
// TransacaoForm.tsx
const { watch } = useForm<TransacaoFormData>({ ... });
const selectedTipo = watch('tipo');

<LazyCategoriaSelect
  value={selectedCategoriaObj}
  onChange={(c) => { ... }}
  selectedTipo={selectedTipo}   // ← adicionar
  error={errors.categoriaId}
/>
```

---

## BUG-004 — Endpoint DELETE ausente em `/api/v1/transacoes`

| Campo | Detalhe |
|---|---|
| **Severidade** | 🟡 Médio |
| **Tipo** | CRUD Incompleto |
| **Camada** | API → `TransacoesController.cs` |

### Descrição

O `TransacoesController` não implementa o verbo `DELETE`. Tentativas de deletar uma transação resultam em `405 Method Not Allowed`.

```bash
curl -X DELETE http://localhost:5000/api/v1/transacoes/<id>
# HTTP 405 Method Not Allowed
```

Adicionalmente, o `TransacaoService` (`ITransacaoService`) não possui método `DeleteAsync`.

---

## BUG-005 — Endpoints PUT e DELETE ausentes em `/api/v1/categorias`

| Campo | Detalhe |
|---|---|
| **Severidade** | 🟡 Médio |
| **Tipo** | CRUD Incompleto |
| **Camada** | API → `CategoriasController.cs` |

### Descrição

O `CategoriasController` implementa apenas `GET` (lista e por ID) e `POST`. Os verbos `PUT` e `DELETE` estão ausentes, tornando o CRUD de categorias incompleto.

```bash
curl -X PUT http://localhost:5000/api/v1/categorias/<id> -d '...'
# HTTP 405 Method Not Allowed

curl -X DELETE http://localhost:5000/api/v1/categorias/<id>
# HTTP 405 Method Not Allowed
```

---

## Resumo Executivo

| ID | Severidade | Camada | Regra de Negócio Violada | Status |
|---|---|---|---|---|
| BUG-001 | 🔴 Crítico | Infrastructure | Cascade delete | Aberto |
| BUG-002 | 🔴 Crítico | API | Menor sem receita · Categoria incompatível retornam 500 | Aberto |
| BUG-003 | 🟠 Alto | Frontend | Filtro de categoria por tipo não funciona | Aberto |
| BUG-004 | 🟡 Médio | API | CRUD de Transação incompleto (sem DELETE) | Aberto |
| BUG-005 | 🟡 Médio | API | CRUD de Categoria incompleto (sem PUT/DELETE) | Aberto |

# 🚀 PRÓXIMO PASSO: OBJETIVO 2 - VALIDAR EDGE CASES

**Status:** ⏳ Aguardando conclusão do Objetivo 1  
**Tempo Estimado:** 3 horas  
**Prioridade:** 🔴 Alta (Estabilidade)

---

## 📋 RESUMO DO OBJETIVO 2

O Objetivo 1 ativou os gráficos com sucesso, mas não há validação robusta quando algo dá errado. Objetivo 2 é garantir que a aplicação não quebra em cenários extremos.

### Cenários Críticos a Tratar

| # | Cenário | Impacto | Severidade |
|---|---------|---------|-----------|
| 1️⃣ | Edital sem assuntos | Divisão por zero em PercentualCompatibilidade | 🔴 CRÍTICO |
| 2️⃣ | DataProva nula/default | Cálculo de dias incorreto | 🔴 CRÍTICO |
| 3️⃣ | Edital deletado entre seleção | KeyNotFoundException não tratada | 🟡 ALTA |
| 4️⃣ | Ambos editais sem data de prova | MensagemEstrategicaTempo ambígua | 🟡 ALTA |
| 5️⃣ | ID de edital inválido | Erro não informativo ao usuário | 🟡 ALTA |

---

## 🎯 CHECKLIST DO QUE FAZER

### Etapa 1: Validações em Service (1.5 horas)

**Arquivo:** `Services/ComparadorEditaisService.cs`  
**Método:** `CarregarEditalCompleto(int id)`

```csharp
// ✅ TODO: Adicione estas validações
private async Task<Edital> CarregarEditalCompleto(int id)
{
    var edital = await _context.Editais
        .AsNoTracking()
        .Include(e => e.EditalAssuntos)
            .ThenInclude(ea => ea.Assunto)
                .ThenInclude(a => a.Disciplina)
        .FirstOrDefaultAsync(e => e.Id == id);

    // ✅ VALIDAÇÃO 1: Edital existe?
    if (edital == null)
        throw new KeyNotFoundException(
            $"❌ Edital com ID {id} não encontrado no banco de dados.");
    
    // ✅ VALIDAÇÃO 2: Edital possui assuntos?
    if (edital.EditalAssuntos == null || edital.EditalAssuntos.Count == 0)
        throw new InvalidOperationException(
            $"❌ Edital '{edital.Cargo}' não possui assuntos cadastrados. " +
            $"Adicione assuntos antes de comparar.");

    return edital;
}
```

**Por quê?**
- Evita NullReferenceException
- Fornece mensagens claras ao usuário
- Impede divisão por zero em cálculos

---

### Etapa 2: Validações em DTO (1 hora)

**Arquivo:** `Models/DTOs/ResultadoComparacao.cs`  
**Método:** `CalcularPrioridadeTemporal()`

```csharp
// ✅ TODO: Adicione validação de data
public void CalcularPrioridadeTemporal()
{
    var hoje = DateTime.Today;

    // ✅ VALIDAÇÃO: DataProva válida para Base?
    if (EditalBase.DataProva == default || EditalBase.DataProva < new DateTime(2020, 1, 1))
    {
        MensagemEstrategicaTempo = "⚠️ Data de prova não definida para o edital base.";
        return;
    }

    // ✅ VALIDAÇÃO: DataProva válida para Alvo?
    if (EditalAlvo.DataProva == default || EditalAlvo.DataProva < new DateTime(2020, 1, 1))
    {
        MensagemEstrategicaTempo = "⚠️ Data de prova não definida para o edital alvo.";
        return;
    }

    // ... resto do código de cálculo
}
```

**Por quê?**
- Evita cálculos com datas inválidas
- Previne comportamentos estranhos (dias negativos)

---

### Etapa 3: Try-Catch em ViewModel (0.5 horas)

**Arquivo:** `ViewModels/ComparadorEditaisViewModel.cs`  
**Método:** `CompararAsync()`

```csharp
// ✅ TODO: Envolva em try-catch
private async Task CompararAsync()
{
    if (EditalBaseSelecionado == null || EditalAlvoSelecionado == null) 
        return;

    if (EditalBaseSelecionado.Id == EditalAlvoSelecionado.Id)
    {
        _notificationService.ShowWarning(
            "Atenção", 
            "Selecione dois editais diferentes para comparar.");
        return;
    }

    IsBusy = true;
    try
    {
        var resultado = await _comparadorService.CompararAsync(
            EditalBaseSelecionado.Id,
            EditalAlvoSelecionado.Id
        );

        Resultado = resultado;
        TemResultado = true;
        AtualizarGraficos(resultado);
    }
    // ✅ NOVO: Captura exceções conhecidas
    catch (KeyNotFoundException ex)
    {
        _notificationService.ShowError(
            "Edital Não Encontrado", 
            ex.Message);
    }
    catch (InvalidOperationException ex)
    {
        _notificationService.ShowError(
            "Edital Incompleto", 
            ex.Message);
    }
    catch (Exception ex)
    {
        _notificationService.ShowError(
            "Erro ao Comparar", 
            $"Falha inesperada: {ex.Message}");
    }
    finally
    {
        IsBusy = false;
    }
}
```

**Por quê?**
- Captura erros e mostra mensagens amigáveis
- Evita que exceções quebre a aplicação
- Usuário sabe o que aconteceu

---

### Etapa 4: Testes Unitários (1 hora)

**Arquivo:** `Tests/ComparadorEditaisServiceTests.cs` (NOVO)

```csharp
// ✅ TODO: Criar testes para edge cases
[TestClass]
public class ComparadorEditaisServiceTests
{
    [TestMethod]
    [ExpectedException(typeof(KeyNotFoundException))]
    public async Task CompararAsync_EditalNaoExiste_ThrowsException()
    {
        // Arrange
        var mockContext = new Mock<StudyMinderContext>();
        var service = new ComparadorEditaisService(mockContext.Object);

        // Act
        await service.CompararAsync(999, 1);

        // Assert: Exception lançada
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task CompararAsync_EditalSemAssuntos_ThrowsException()
    {
        // Arrange: Edital sem assuntos
        
        // Act & Assert: InvalidOperationException esperada
    }

    // ... mais testes
}
```

**Por quê?**
- Validar que cada cenário extremo é tratado
- Automático e repetível
- Previne regressão

---

## 📊 IMPACTO ESPERADO

**Antes (Objetivo 1):**
```
Usuario clica Comparar
    ↓
❌ CRASH: NullReferenceException
❌ CRASH: Divisão por zero
❌ CRASH: InvalidOperation
```

**Depois (Objetivo 2):**
```
Usuario clica Comparar
    ↓
✅ Captura erro
✅ Mostra mensagem amigável
✅ Aplicação continua rodando
```

---

## 🔧 TAREFAS ESPECÍFICAS

### Task 2.1: Adicionar validações em Service
- [ ] Abrir `ComparadorEditaisService.cs`
- [ ] Localizar `CarregarEditalCompleto(int id)`
- [ ] Adicionar 2 validações (null check + assuntos check)
- [ ] Testar: Debug > selecionar edital inválido > crash esperado

### Task 2.2: Adicionar validações em DTO
- [ ] Abrir `ResultadoComparacao.cs`
- [ ] Localizar `CalcularPrioridadeTemporal()`
- [ ] Adicionar 3 validações (data válida)
- [ ] Testar: Debug > comparar com data nula > msg esperada

### Task 2.3: Adicionar try-catch em ViewModel
- [ ] Abrir `ComparadorEditaisViewModel.cs`
- [ ] Localizar `CompararAsync()`
- [ ] Envolver em try-catch-finally
- [ ] Capturar KeyNotFoundException, InvalidOperationException, Exception
- [ ] Mostrar mensagens via NotificationService

### Task 2.4: Criar testes unitários
- [ ] Criar novo arquivo `Tests/ComparadorEditaisServiceTests.cs`
- [ ] Adicionar 5 testes (um para cada cenário)
- [ ] Executar testes: Test > Run All Tests
- [ ] Esperado: Todos passarem ✅

---

## 📝 REFERÊNCIAS

**Arquivo de Código Original:**
- `SNIPPETS_IMPLEMENTACAO.txt` - Contém código pronto para copiar/colar

**Documentação:**
- `PLANO_ACAO_COMPARADOR.md` - Seção "OBJETIVO 2: VALIDAR EDGE CASES"

**Exemplo de Implementação:**
- Ver OBJETIVO 2 em PLANO_ACAO_COMPARADOR.md para código detalhado

---

## ⏰ TIMELINE SUGERIDA

```
Dia 1 (depois que Objetivo 1 passar no teste):
├─ Manhã: Task 2.1 + 2.2 (Validações) - 1.5h
├─ Tarde: Task 2.3 (Try-catch) - 0.5h
└─ Final: Task 2.4 (Testes) - 1h
```

---

## 🎯 DEFINIÇÃO DE PRONTO

Objetivo 2 estará pronto quando:

- [ ] ✅ Todas as 5 validações implementadas
- [ ] ✅ Sem NullReferenceException em nenhum cenário
- [ ] ✅ Mensagens de erro informativas ao usuário
- [ ] ✅ 5 testes unitários criados e passando
- [ ] ✅ Projeto compila sem erros
- [ ] ✅ Teste manual: todas 5 edge cases tratadas gracefully

---

## 🚨 SINAIS DE ALERTA

Se você ver:
- ❌ `NullReferenceException` → Falta validação null check
- ❌ `DivideByZeroException` → Falta validação de contagem
- ❌ Mensagem de erro confusa → Falta mensagem clara
- ❌ Teste falhando → Validação não está sendo feita

**Ação:** Volte ao código e revise a validação

---

## 💡 DICA

Comece com Task 2.1 + 2.2 (as validações mais fáceis), depois prossiga para 2.3 (try-catch), e finalize com 2.4 (testes).

A ordem importa porque:
1. Validações previnem erros
2. Try-catch captura os erros
3. Testes garantem que funciona

---

**Próximo Objetivo 2 quando terminar Objetivo 1! 🚀**

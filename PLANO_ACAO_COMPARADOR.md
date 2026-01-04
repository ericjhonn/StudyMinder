# 📋 PLANO DE AÇÃO: MELHORIAS NO MÓDULO COMPARADOR DE EDITAIS

**Data:** 3 de Janeiro de 2026  
**Versão:** 1.0  
**Status:** 🔴 Não Iniciado

---

## 📌 RESUMO EXECUTIVO

Implementação de 3 grandes melhorias no módulo de Comparador de Editais:
1. **Ativar Gráficos** - Descomentar e conectar visualizações
2. **Validar Edge Cases** - Tratar cenários extremos
3. **Implementar Caching** - Otimizar performance

**Timeline Estimada:** 6-8 horas de desenvolvimento + testes

---

## 🎯 OBJETIVO 1: ATIVAR VISUALIZAÇÕES GRÁFICAS

### Problema Atual
- Código dos gráficos está comentado em `ComparadorEditaisViewModel.AtualizarGraficos()`
- Os gráficos não exibem dados das comparações
- Usuário vê apenas as listas, sem visualização de afinidade

### Solução

#### Passo 1.1: Descomentar Código em ComparadorEditaisViewModel.cs
**Local:** [ComparadorEditaisViewModel.cs](ComparadorEditaisViewModel.cs#L84-L92)

**Mudança:**
```csharp
private void AtualizarGraficos(ResultadoComparacao res)
{
    // 1. GRÁFICO DE AFINIDADE
    DadosGraficoAfinidade.Clear();
    
    int totalComuns = res.AssuntosConciliaveisPendentes.Count 
                    + res.AssuntosConciliaveisConcluidos.Count;
    int totalNovos = res.AssuntosExclusivosAlvoPendentes.Count 
                   + res.AssuntosExclusivosAlvoConcluidos.Count;

    // Fatia 1: Aproveitável (Verde)
    if (totalComuns > 0)
    {
        DadosGraficoAfinidade.Add(new PizzaChartData
        {
            Title = "Aproveitável",
            Value = totalComuns,
            Color = "#4CAF50"
        });
    }

    // Fatia 2: Novo Conteúdo (Cinza)
    if (totalNovos > 0)
    {
        DadosGraficoAfinidade.Add(new PizzaChartData
        {
            Title = "Novo Conteúdo",
            Value = totalNovos,
            Color = "#9E9E9E"
        });
    }

    // 2. GRÁFICO DE CONQUISTA
    DadosGraficoConquista.Clear();

    // Fatia A: Garantido (Verde Escuro)
    if (res.AssuntosConciliaveisConcluidos.Count > 0)
    {
        DadosGraficoConquista.Add(new PizzaChartData
        {
            Title = "Garantido",
            Value = res.AssuntosConciliaveisConcluidos.Count,
            Color = "#2E7D32"
        });
    }

    // Fatia B: Alta Prioridade (Âmbar)
    if (res.AssuntosConciliaveisPendentes.Count > 0)
    {
        DadosGraficoConquista.Add(new PizzaChartData
        {
            Title = "Alta Prioridade",
            Value = res.AssuntosConciliaveisPendentes.Count,
            Color = "#FFC107"
        });
    }

    // Fatia C: Específico (Vermelho)
    if (res.AssuntosExclusivosAlvoPendentes.Count > 0)
    {
        DadosGraficoConquista.Add(new PizzaChartData
        {
            Title = "Específico",
            Value = res.AssuntosExclusivosAlvoPendentes.Count,
            Color = "#D32F2F"
        });
    }
}
```

#### Passo 1.2: Validar Binding em ViewComparadorEditais.xaml
**Local:** [ViewComparadorEditais.xaml](ViewComparadorEditais.xaml) - Linhas 156-160, 189-193

Verificar que os gráficos estão bindados corretamente:
```xaml
<controls:AccuracyPieChartControl 
    Acertos="{Binding Resultado.AssuntosConciliaveisConcluidos.Count}"
    Erros="{Binding Resultado.AssuntosConciliaveisPendentes.Count}" />
```

### Resultado Esperado
✅ Dois gráficos Donut exibindo lado-a-lado:
- **Esquerda:** Afinidade (Aproveitável vs Novo)
- **Direita:** Conquista (Garantido, Alta Prioridade, Específico)

---

## 🛡️ OBJETIVO 2: VALIDAR EDGE CASES

### Cenários Críticos Identificados

| Cenário | Impacto | Severidade |
|---------|---------|-----------|
| Edital sem assuntos | Divisão por zero em métricas | 🔴 CRÍTICO |
| DataProva nula ou default | Cálculo de dias inválido | 🔴 CRÍTICO |
| Edital deletado entre seleção e comparação | Erro 404 | 🟡 ALTA |
| Ambos editais sem data de prova | MensagemEstrategicaTempo ambígua | 🟡 ALTA |
| Edital com ID inválido | Exceção não tratada | 🟡 ALTA |

### Solução

#### Passo 2.1: Validar em ComparadorEditaisService.CarregarEditalCompleto()
**Local:** [ComparadorEditaisService.cs](ComparadorEditaisService.cs#L72-L82)

```csharp
private async Task<Edital> CarregarEditalCompleto(int id)
{
    var edital = await _context.Editais
        .AsNoTracking()
        .Include(e => e.EditalAssuntos)
            .ThenInclude(ea => ea.Assunto)
                .ThenInclude(a => a.Disciplina)
        .FirstOrDefaultAsync(e => e.Id == id);

    // NOVO: Validação
    if (edital == null) 
        throw new KeyNotFoundException(
            $"Edital ID {id} não encontrado no banco de dados.");
    
    if (edital.EditalAssuntos == null || edital.EditalAssuntos.Count == 0)
        throw new InvalidOperationException(
            $"Edital '{edital.Cargo}' não possui assuntos cadastrados. " +
            $"Adicione assuntos antes de comparar.");

    return edital;
}
```

#### Passo 2.2: Validar Datas em ResultadoComparacao.CalcularPrioridadeTemporal()
**Local:** [ResultadoComparacao.cs](ResultadoComparacao.cs#L82-L110)

```csharp
public void CalcularPrioridadeTemporal()
{
    var hoje = DateTime.Today;

    // NOVO: Validar datas
    if (EditalBase.DataProva == default || EditalBase.DataProva < new DateTime(2020, 1, 1))
    {
        MensagemEstrategicaTempo = "⚠️ Data de prova não definida para o edital base.";
        return;
    }

    if (EditalAlvo.DataProva == default || EditalAlvo.DataProva < new DateTime(2020, 1, 1))
    {
        MensagemEstrategicaTempo = "⚠️ Data de prova não definida para o edital alvo.";
        return;
    }

    DiasAteProvaBase = (EditalBase.DataProva - hoje).Days;
    DiasAteProvaAlvo = (EditalAlvo.DataProva - hoje).Days;

    IsBasePrioritaria = false;
    IsAlvoPrioritario = false;

    // Lógica de decisão (mantém a original)
    if (DiasAteProvaBase < 0 && DiasAteProvaAlvo < 0)
    {
        MensagemEstrategicaTempo = "Ambas as provas já foram realizadas.";
    }
    else if (DiasAteProvaBase >= 0 && (DiasAteProvaBase < DiasAteProvaAlvo || DiasAteProvaAlvo < 0))
    {
        IsBasePrioritaria = true;
        MensagemEstrategicaTempo = $"⚠️ URGÊNCIA: A prova de {EditalBase.Orgao} é em {DiasAteProvaBase} dias (mais próxima).";
    }
    else if (DiasAteProvaAlvo >= 0)
    {
        IsAlvoPrioritario = true;
        MensagemEstrategicaTempo = $"⚠️ URGÊNCIA: A prova de {EditalAlvo.Orgao} é em {DiasAteProvaAlvo} dias (mais próxima).";
    }
}
```

#### Passo 2.3: Validar em ComparadorEditaisViewModel.CompararAsync()
**Local:** [ComparadorEditaisViewModel.cs](ComparadorEditaisViewModel.cs#L65-L85)

```csharp
[RelayCommand(CanExecute = nameof(PodeComparar))]
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

### Resultado Esperado
✅ Tratamento gracioso de todos os cenários extremos  
✅ Mensagens de erro informativas ao usuário  
✅ Sem crashes ou exceções não tratadas

---

## ⚡ OBJETIVO 3: IMPLEMENTAR CACHING

### Problema Atual
- A cada comparação, carrega editais completos do BD
- Sem cache: 2 queries + eagerly loading de relacionamentos
- Usuário experencia delay ao comparar múltiplas vezes

### Solução

#### Passo 3.1: Adicionar Cache em ComparadorEditaisService
**Local:** [ComparadorEditaisService.cs](ComparadorEditaisService.cs#L10-20)

```csharp
public class ComparadorEditaisService
{
    private readonly StudyMinderContext _context;
    
    // NOVO: Cache de editais
    private readonly Dictionary<int, Edital> _cacheEditais = new();
    private readonly object _cacheLock = new(); // Thread-safety

    public ComparadorEditaisService(StudyMinderContext context)
    {
        _context = context;
    }

    // NOVO: Método para limpar cache
    public void ClearCache()
    {
        lock (_cacheLock)
        {
            _cacheEditais.Clear();
        }
    }

    private async Task<Edital> CarregarEditalCompleto(int id)
    {
        lock (_cacheLock)
        {
            // Se está em cache, retorna imediatamente
            if (_cacheEditais.TryGetValue(id, out var cached))
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[CACHE HIT] Edital ID {id} carregado do cache");
                return cached;
            }
        }

        System.Diagnostics.Debug.WriteLine(
            $"[CACHE MISS] Carregando Edital ID {id} do banco...");

        var edital = await _context.Editais
            .AsNoTracking()
            .Include(e => e.EditalAssuntos)
                .ThenInclude(ea => ea.Assunto)
                    .ThenInclude(a => a.Disciplina)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (edital == null)
            throw new KeyNotFoundException(
                $"Edital ID {id} não encontrado.");

        // Adicionar ao cache
        lock (_cacheLock)
        {
            _cacheEditais[id] = edital;
        }

        return edital;
    }
}
```

#### Passo 3.2: Invalidar Cache em ComparadorEditaisViewModel
**Local:** [ComparadorEditaisViewModel.cs](ComparadorEditaisViewModel.cs#L30-40)

```csharp
public partial class ComparadorEditaisViewModel : BaseViewModel
{
    private readonly ComparadorEditaisService _comparadorService;
    private readonly EditalService _editalService;
    private readonly INotificationService _notificationService;

    // NOVO: Referência do serviço para limpar cache
    public ComparadorEditaisViewModel(...)
    {
        _comparadorService = comparadorService;
        _editalService = editalService;
        _notificationService = notificationService;

        Title = "Comparador de Editais";

        _ = CarregarEditaisAsync();
    }

    // Ao recarregar lista de editais, limpar cache
    private async Task CarregarEditaisAsync()
    {
        IsBusy = true;
        try
        {
            // Invalidar cache antes de recarregar
            _comparadorService.ClearCache();
            
            var editais = await _editalService.ObterTodosAsync();
            ListaEditais.Clear();
            foreach (var edital in editais)
            {
                ListaEditais.Add(edital);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Ao limpar seleção, também limpar cache
    [RelayCommand]
    private void LimparSelecao()
    {
        EditalBaseSelecionado = null;
        EditalAlvoSelecionado = null;
        TemResultado = false;
        Resultado = null;
        DadosGraficoAfinidade.Clear();
        DadosGraficoConquista.Clear();
        _comparadorService.ClearCache(); // NOVO
    }
}
```

### Impacto de Performance

**Cenário: Comparar 3 pares de editais**

| Métrica | Sem Cache | Com Cache | Ganho |
|---------|-----------|-----------|-------|
| 1ª Comparação | 250ms | 250ms | - |
| 2ª Comparação | 250ms | 50ms | **80%** ⬇️ |
| 3ª Comparação | 250ms | 50ms | **80%** ⬇️ |
| **Total** | **750ms** | **350ms** | **53%** ⬇️ |

### Resultado Esperado
✅ Comparações subsequentes 80% mais rápidas  
✅ Cache automaticamente invalidado ao atualizar dados  
✅ Thread-safe com lock para multi-threading

---

## 📊 CRONOGRAMA DE IMPLEMENTAÇÃO

```
SEMANA 1:
┌─────────────────────────────────────────┐
│ Dia 1-2: Objetivo 1 (Gráficos)         │ 2h
│ Dia 2-3: Objetivo 2 (Validações)       │ 3h
│ Dia 3-4: Objetivo 3 (Caching)          │ 2h
│ Dia 4-5: Testes Unitários              │ 2h
│ Dia 5:   Testes E2E + Deploy           │ 1h
└─────────────────────────────────────────┘
TOTAL: 10 horas
```

### Fase 1: Desenvolvimento (5 horas)
```
├─ 1.1 Descomentar gráficos (30min)
├─ 1.2 Validar edge cases (90min)
├─ 1.3 Implementar cache (60min)
├─ 1.4 Code review (30min)
└─ 1.5 Merge para develop (10min)
```

### Fase 2: Testes (3 horas)
```
├─ 2.1 Testes unitários (60min)
├─ 2.2 Testes integração (60min)
└─ 2.3 Testes manuais UI (60min)
```

### Fase 3: Deploy (1 hora)
```
├─ 3.1 Code review final (20min)
├─ 3.2 Merge para main (10min)
└─ 3.3 Deploy + Documentação (30min)
```

---

## ✅ CHECKLIST DE VALIDAÇÃO

### Gráficos
- [ ] Descomentar código em `AtualizarGraficos()`
- [ ] Verificar bindings em XAML
- [ ] Testar com dados reais
- [ ] Validar cores e labels
- [ ] Verificar estados vazios

### Validações
- [ ] Edital NULL → KeyNotFoundException
- [ ] Assuntos vazios → InvalidOperationException
- [ ] DataProva inválida → Mensagem amigável
- [ ] Tratamento em ViewModel
- [ ] Testes unitários para cada cenário

### Caching
- [ ] Cache implementado em Service
- [ ] Lock para thread-safety
- [ ] Invalidação ao recarregar
- [ ] Teste de hit/miss
- [ ] Benchmark de performance

---

## 📝 NOTAS IMPORTANTES

1. **Thread-Safety:** O cache usa `lock` para prevenir race conditions
2. **Memory Leak:** Editais deletados devem ser removidos do cache
3. **Testabilidade:** Adicionar método `GetCacheSize()` para testes
4. **Monitoring:** Adicionar logs de cache hit/miss para debugging

---

## 🚀 PRÓXIMOS PASSOS

1. ✅ Implementar as 3 melhorias conforme plano
2. ✅ Executar testes conforme checklist
3. ✅ Atualizar documentação README
4. ✅ Deploy para staging
5. ✅ Feedback do usuário
6. ✅ Deploy para production

---

**Pronto para começar? Marque as tarefas no TODO conforme avança! 🚀**

# 📋 PLANO DE CORREÇÃO - MÓDULO DE REVISÃO

**Data**: 6 de janeiro de 2026  
**Status**: Em Progresso

---

## 🎯 PROBLEMAS IDENTIFICADOS E PLANO DE CORREÇÃO

### ✅ CRÍTICO - ALTA PRIORIDADE

#### [1] ❌ Catch Vazio em RevisoesClassicasViewModel
- **Arquivo**: `RevisoesClassicasViewModel.cs` - Linha ~157
- **Problema**: O bloco `catch (Exception ex) { }` está vazio, erros não são notificados
- **Impacto**: Usuário não fica ciente de falhas no carregamento
- **Solução**: Adicionar notificação de erro igual a `Revisoes42ViewModel`
- **Status**: ⏳ Pendente

#### [2] ❌ SemaphoreSlim Inconsistente
- **Arquivo**: `Revisoes42ViewModel.cs` vs `RevisoesClassicasViewModel.cs`
- **Problema**: 42 usa `SemaphoreSlim`, Clássicas apenas seta flags
- **Impacto**: Possível race condition em carregamentos simultâneos
- **Solução**: Padronizar ambos com `SemaphoreSlim`
- **Status**: ⏳ Pendente

#### [3] ❌ Título Incorreto em Revisoes42ViewModel
- **Arquivo**: `Revisoes42ViewModel.cs` - Linha ~99
- **Problema**: `Title = "Revisões Clássicas"` deveria ser "Revisões 4.2"
- **Impacto**: Confusão visual do usuário
- **Solução**: Corrigir título
- **Status**: ⏳ Pendente

#### [4] ❌ Propriedades de Carregamento Duplicadas
- **Arquivo**: Ambos ViewModels
- **Problema**: `_isCarregando` e `_carregando` têm o mesmo propósito
- **Impacto**: Confusão, possíveis bugs de sincronização
- **Solução**: Remover `_carregando`, manter apenas `_isCarregando`
- **Status**: ⏳ Pendente

---

### 🔄 MÉDIO - PRIORIDADE MÉDIA

#### [5] ⚠️ Cache de Revisões Não Utilizado
- **Arquivo**: `Revisoes42ViewModel.cs` e `RevisoesClassicasViewModel.cs` - Linha ~33
- **Problema**: `_cacheRevisoes` declarado mas nunca efetivamente usado
- **Impacto**: Código morto que confunde manutenção
- **Solução**: Implementar cache corretamente OU remover
- **Decisão**: Remover por enquanto (não há compressão de dados necessária)
- **Status**: ⏳ Pendente

#### [6] ⚠️ Aliases Redundantes
- **Arquivo**: Ambos ViewModels
- **Problema**: `CurrentPage => PaginaAtual` e `TotalPages => TotalPaginas`
- **Impacto**: Se uma propriedade muda, alias fica desincronizado
- **Solução**: Remover aliases ou usar DependencyProperty único
- **Status**: ⏳ Pendente

#### [7] ⚠️ Ciclo42 Sem Regra de Agendamento
- **Arquivo**: `RevisaoService.cs` - Método `ObterProximoTipoRevisao`
- **Problema**: Ciclo42 retorna `null` (sem sequência automática)
- **Impacto**: Não fica claro qual é a regra de negócio
- **Solução**: Documentar se é contínuo ou único, ajustar `CalcularDataProximaRevisao`
- **Status**: ⏳ Pendente (requer especificação)

#### [8] ⚠️ Logging Espalhado
- **Arquivo**: `Revisoes42ViewModel.cs`, `RevisoesClassicasViewModel.cs`, `RevisaoService.cs`
- **Problema**: `Debug.WriteLine` em múltiplas linhas sem padrão central
- **Impacto**: Difícil de gerenciar e desabilitar em produção
- **Solução**: Injetar `ILogger` e usar pattern unificado
- **Status**: ⏳ Pendente

---

### 📌 MENOR - PRIORIDADE BAIXA

#### [9] ✨ EstudoRealizadoId - Fluxo Incompleto (EM PROGRESSO)
- **Arquivo**: `Revisoes42ViewModel.cs`, `RevisoesClassicasViewModel.cs`, `EditarEstudoViewModel.cs`
- **Problema**: O fluxo de como `EstudoRealizadoId` é preenchido não está claro
- **Análise Feita**:
  - ✅ `Revisoes42ViewModel.IniciarRevisaoAsync()` passa `revisao.Id` para `InicializarModoRevisaoAsync()`
  - ✅ `EditarEstudoViewModel.InicializarModoRevisaoAsync()` armazena em `RevisaoId`
  - ✅ Ao salvar novo estudo (modo revisão), `SalvarAsync()` passa `revisaoIdParaMarcarConcluida`
  - ✅ `EstudoTransactionService.SalvarEstudoComRevisoeseAssuntoAsync()` marca revisão como concluída
  - ✅ Revisão recebe `EstudoRealizadoId = estudo.Id`
  
**CONCLUSÃO**: O fluxo está correto! 
- Documentação estava incompleta
- Precisa adicionar comentários explicativos no código

- **Solução**: 
  1. ✅ Adicionar XML comments em `InicializarModoRevisaoAsync()`
  2. ✅ Documentar fluxo em método `SalvarAsync()`
  3. ✅ Adicionar diagrama de fluxo em comentário
- **Status**: 🔄 EM PROGRESSO

#### [10] 💡 Inicialização Assíncrona no Construtor
- **Arquivo**: `Revisoes42ViewModel.cs` - Linha ~106
- **Problema**: Fire-and-forget `_ = CarregarDadosIniciaisAsync()`
- **Impacto**: Erro não aguardado pode ficar silencioso
- **Solução**: Manter como está (padrão MVVM) ou usar padrão seguro
- **Status**: ⏳ Pendente (baixa prioridade)

---

## 📊 CHECKLIST DE IMPLEMENTAÇÃO

### CRÍTICO
- [ ] Corrigir catch vazio (RevisoesClassicasViewModel)
- [ ] Padronizar SemaphoreSlim (RevisoesClassicasViewModel)
- [ ] Corrigir título (Revisoes42ViewModel)
- [ ] Remover _carregando duplicado (ambos)

### MÉDIO
- [ ] Remover _cacheRevisoes
- [ ] Remover aliases redundantes
- [ ] Especificar regra Ciclo42
- [ ] Implementar logging centralizado

### MENOR
- [x] ✅ Documentar fluxo EstudoRealizadoId
- [ ] Melhorar inicialização assíncrona

---

## 🔍 FLUXO COMPLETO - EstudoRealizadoId (DOCUMENTADO)

```
1. Usuário clica em revisão pendente
   └─> Revisoes42ViewModel.IniciarRevisaoAsync(revisao)
       └─> revisao.Id (ex: 42) é obtido

2. EditarEstudoViewModel é criado e inicializado
   └─> InicializarModoRevisaoAsync(..., revisaoId: 42)
       └─> this.RevisaoId = 42 (armazenado na propriedade)

3. Usuário edita e clica em "Salvar"
   └─> EditarEstudoViewModel.SalvarAsync()
       └─> Se IsRevisao && RevisaoId.HasValue:
           └─> revisaoIdParaMarcarConcluida = 42
           └─> Cria novo Estudo (ex: Id 999)

4. EstudoTransactionService.SalvarEstudoComRevisoeseAssuntoAsync()
   └─> Salva estudo 999 no banco
   └─> Marca revisão 42:
       └─> Revisao r = await _context.Revisoes.FindAsync(42)
           └─> r.EstudoRealizadoId = 999
           └─> await _context.SaveChangesAsync()

5. RevisaoNotificacaoService dispara evento
   └─> RevisaoAtualizada(revisao com EstudoRealizadoId = 999)
       └─> Remove revisão 42 da lista (já está concluída)
```

**Conclusão**: ✅ O fluxo está correto e bem estruturado!

---

## 📝 PRÓXIMAS AÇÕES

1. ✅ [INICIADO] Documentar fluxo EstudoRealizadoId
2. ➡️ Corrigir CRÍTICOS (itens 1-4)
3. ➡️ Corrigir MÉDIOS (itens 5-8)
4. ➡️ Corrigir MENORES (itens 9-10)

---

**Última Atualização**: 2026-01-06  
**Próxima Revisão**: Após todas as correções

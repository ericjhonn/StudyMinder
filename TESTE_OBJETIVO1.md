# 🧪 GUIA DE TESTE MANUAL - OBJETIVO 1: ATIVAR GRÁFICOS

**Responsável:** Desenvolvedor  
**Data Teste:** [Data do Teste]  
**Status:** [ ] Pendente | [ ] Em Progresso | [ ] Completo

---

## 📋 PRÉ-REQUISITOS

- [ ] Projeto compilado sem erros
- [ ] Banco de dados com pelo menos 2 editais contendo assuntos
- [ ] Aplicação pronta para executar
- [ ] Visual Studio debugger disponível

---

## 🧪 TESTE 1: Gráficos em Comparador

### Passo 1: Abrir Comparador
1. Executar aplicação (F5)
2. Navegar para aba "Comparador de Editais"
3. ✅ Esperado: View carrega sem erros, dois ComboBox vazios

### Passo 2: Selecionar Editais
1. Clicar no ComboBox "Edital Base (O que estudo)"
2. Selecionar um edital (ex: "OAB - 2024")
3. Clicar no ComboBox "Edital Alvo (O que desejo)"
4. Selecionar edital DIFERENTE (ex: "Magistratura - 2024")
5. ✅ Esperado: Ambos editais selecionados, botão "Comparar" habilitado

### Passo 3: Clicar Comparar
1. Clicar botão "Comparar"
2. Aguardar 1-2 segundos
3. ✅ Esperado:
   - Resultado carrega sem erros
   - Duas cards aparecem lado-a-lado:
     - "Afinidade de Conteúdo" (esquerda)
     - "Cenário Real" (direita)

### Passo 4: Validar Gráfico de Afinidade
1. Observar gráfico à esquerda
2. ✅ Esperado:
   - [ ] Donut é visível com cor(es)
   - [ ] Se tem assuntos comuns: fatia **Verde** rotulada "Aproveitável"
   - [ ] Se tem assuntos novos: fatia **Cinza** rotulada "Novo Conteúdo"
   - [ ] Percentual "87.3% Compatível" exibe embaixo
   - [ ] Hover no gráfico mostra tooltip com detalhes

### Passo 5: Validar Gráfico de Conquista
1. Observar gráfico à direita
2. ✅ Esperado:
   - [ ] Donut é visível com cores múltiplas
   - [ ] Se tem concluídos: fatia **Verde Escuro** "Garantido"
   - [ ] Se tem pendentes comuns: fatia **Âmbar/Amarelo** "Alta Prioridade"
   - [ ] Se tem exclusivos pendentes: fatia **Vermelho** "Específico"
   - [ ] Percentual "67.2% Concluído" exibe embaixo com ícone 🎯
   - [ ] Hover no gráfico mostra tooltip

---

## 🧪 TESTE 2: Compatibilidade com Home

### Passo 1: Abrir Home
1. Navegar para aba "Dashboard/Home"
2. ✅ Esperado: View carrega sem erros, gráfico de questões existe

### Passo 2: Validar Gráfico de Questões
1. Observar gráfico "Desempenho em Questões" ou similar
2. ✅ Esperado:
   - [ ] Donut com duas fatias:
     - **Verde** "Acertos: 76%"
     - **Vermelho** "Erros: 24%"
   - [ ] Números e percentuais exibem corretamente
   - [ ] Hover mostra tooltip "questões"
   - [ ] Nenhum erro no console de debug

### Passo 3: Validar Backward Compatibility
1. Se há seleção de filtros (períodos, disciplinas), alterar seleção
2. ✅ Esperado:
   - [ ] Gráfico atualiza corretamente com novos dados
   - [ ] Acertos/Erros mudam proporcionalmente
   - [ ] Sem travamentos ou exceções

---

## 🧪 TESTE 3: Edge Cases

### Caso 1: Editais sem Assuntos
1. Em Comparador, selecionar edital que não possui assuntos
2. Clicar "Comparar"
3. ✅ Esperado:
   - [ ] Gráficos mostram estado vazio (cinza neutro)
   - [ ] Mensagem de erro amigável ao usuário (ou aviso silencioso)
   - [ ] Sem crash da aplicação

### Caso 2: Ambos Gráficos Vazios
1. Se resultado tiver 0 assuntos em todas as categorias
2. ✅ Esperado:
   - [ ] Ambos donuts mostram "Sem dados"
   - [ ] Percentuais mostram 0%
   - [ ] UI permanece estável

### Caso 3: Trocar Seleção Durante Comparação
1. Enquanto resultado está carregando, clicar em outro edital
2. Clicar "Comparar" novamente
3. ✅ Esperado:
   - [ ] Gráficos atualizam com novos dados
   - [ ] Sem erros de binding
   - [ ] Sem dados misturados de comparação anterior

### Caso 4: Selecionar Mesmo Edital Duas Vezes
1. Em Comparador, selecionar o MESMO edital em Base e Alvo
2. Clicar "Comparar"
3. ✅ Esperado:
   - [ ] Aviso: "Selecione dois editais diferentes para comparar"
   - [ ] Gráficos NÃO são renderizados
   - [ ] Botão "Comparar" desabilitado

---

## 📊 TESTE 4: Performance Visual

### Teste de Renderização
1. Comparar 3 pares de editais consecutivamente
2. ✅ Esperado:
   - [ ] Cada gráfico renderiza em <1 segundo
   - [ ] Sem lag visual
   - [ ] Memória não cresce indefinidamente
   - [ ] Suave transição entre gráficos

### Teste de Responsividade
1. Redimensionar janela durante gráfico ativo
2. ✅ Esperado:
   - [ ] Gráfico se adapta ao novo tamanho
   - [ ] Sem distorção ou overlap
   - [ ] Labels permanecem legíveis

---

## 🐛 TESTE 5: Debug & Console

### Verificar Logs
1. Abrir Debug Output (View > Output)
2. Realizar comparação
3. ✅ Esperado - No console devem aparecer:
   ```
   [DEBUG] OnDadosChanged - Dados property changed
   [DEBUG] ConfigurarGraficoPizza - Modo Genérico: 2 itens
   ```

### Verificar Não-Errors
1. Abrir Error List (View > Error List)
2. ✅ Esperado:
   - [ ] 0 erros de compilação
   - [ ] 0 warnings não resolvidos
   - [ ] Nenhuma exceção não tratada

---

## 📝 FORMULÁRIO DE RESULTADO

### Resultado Geral
- [ ] ✅ PASSOU - Todos os testes passaram
- [ ] ⚠️ PASSOU COM AVISOS - Funciona mas tem issues menores
- [ ] ❌ FALHOU - Precisa de correção

### Testes Que Passaram
- [ ] Teste 1: Gráficos em Comparador
- [ ] Teste 2: Compatibilidade com Home
- [ ] Teste 3: Edge Cases
- [ ] Teste 4: Performance Visual
- [ ] Teste 5: Debug & Console

### Issues Encontradas
```
[Descrever qualquer problema encontrado]

[ ] Crítico (bloqueia funcionalidade)
[ ] Maior (degrada experiência)
[ ] Menor (cosmético/refinement)
```

### Observações
```
[Adicionar observações, screenshots, etc.]
```

### Assinado
**Testador:** _________________  
**Data:** _________________  
**Hora:** _________________  

---

## 🎯 SUCESSO!

Se todos os testes passaram, o **Objetivo 1 está completo**! 🎉

Próximo passo: **Objetivo 2 - Validar Edge Cases**

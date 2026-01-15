# 📚 StudyMinder - Sistema Inteligente de Gestão de Estudos

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0) ![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat&logo=dotnet&logoColor=white) ![WPF](https://img.shields.io/badge/UI-WPF-blue?style=flat&logo=windows) ![Status](https://img.shields.io/badge/Status-Em_Desenvolvimento-yellow) ![MVVM](https://img.shields.io/badge/Architecture-MVVM-green)

> **O seu assistente pessoal para gestão de estudos de alta performance.**  
> *Domine o seu edital com algoritmos de repetição espaçada e ciclos de estudo automatizados.*

**StudyMinder** é uma aplicação desktop nativa para Windows (x64), desenvolvida com as tecnologias mais recentes do ecossistema Microsoft (.NET 9). Seu objetivo é eliminar a gestão manual de planilhas, oferecendo um sistema inteligente que decide **o que** estudar e **quando** revisar, utilizando metodologias científicas comprovadas de aprendizado.

### ✨ Diferenciais Principais

🧠 **Motor de Revisões Cientificamente Comprovado**
- Curva de Ebbinghaus com intervalos automáticos (24h, 7d, 30d, 90d, 120d, 180d)
- Ciclo 4.2 focado em produtividade (4 dias teoria + 2 dias revisão + 1 descanso)
- Revisão Cíclica sem datas fixas, baseada em filas
- Análise automática de fraquezas (acertos/erros)

📊 **Dashboard Inteligente**
- Heatmap visual de consistência de estudos
- KPIs de desempenho em tempo real
- Gráficos estatísticos avançados (OxyPlot)

🎯 **Gestão Completa de Editais**
- Cadastro de concursos com cronograma detalhado
- Fases de processo seletivo
- Associação de assuntos por edital
- Comparador visual de múltiplos editais

⏱️ **Cronômetro Pomodoro Integrado**
- Registro automático de horas de estudo
- Sincronização com sessões
- Historial completo

🏗️ **Arquitetura Robusta**
- MVVM puro com 24 ViewModels especializados
- 27 serviços de domínio
- 38+ views/dialogs reutilizáveis
- SQLite com EF Core 9.0

---

## 🤝 Apoie uma Causa Real

O **StudyMinder** é um software **100% gratuito e livre (GPLv3)**. Não há cobranças, não há "versão premium", não há publicidade.

No entanto, este projeto tem uma **missão maior**. Se este software lhe ajudar nos estudos, considere fazer uma doação voluntária para o **Hospital Napoleão Laureano**, referência no tratamento oncológico na Paraíba.

> **Hospital Napoleão Laureano** — *O hospital da vida*
>
> **📍 Site Oficial:** [https://hlaureano.org.br/](https://hlaureano.org.br/)  
> **💚 Faça uma Doação:** [https://hlaureano.org.br/a-fundacao/faca-uma-doacao/](https://hlaureano.org.br/a-fundacao/faca-uma-doacao/)
>
> **Nota:** As doações devem ser feitas diretamente à instituição através do link oficial. Este projeto não intermedeia valores.

---

## Galeria

| Dashboard | Ciclo de Estudos |
|:---:|:---:|
| ![Dashboard](https://via.placeholder.com/600x400?text=Dashboard+com+Heatmap+e+Gráficos) <br> *Visão geral com Heatmap de consistência e KPIs de desempenho.* | ![Ciclo](https://via.placeholder.com/600x400?text=Modo+Ciclo+de+Estudos) <br> *Gerenciamento de tempo e ordem de matérias.* |

| Modo Foco | Gestão de Editais |
|:---:|:---:|
| ![Timer](https://via.placeholder.com/600x400?text=Cronômetro+e+Pomodoro) <br> *Cronómetro integrado para registo automático de horas líquidas.* | ![Editais](https://via.placeholder.com/600x400?text=Gestão+de+Editais) <br> *Cadastro detalhado de concursos e datas de prova.* |

---

---

## 🧠 Motor de Revisões Inteligente

O **RevisaoService** é o coração da aplicação, implementando 3 metodologias científicas de aprendizado:

### 1️⃣ **Método Clássico (Curva de Ebbinghaus)**

Ideal para **retenção de longo prazo**. Ao concluir uma sessão de estudo, o sistema agenda automaticamente revisões futuras baseadas na data de conclusão, seguindo a curva científica de Hermann Ebbinghaus:

- **24 Horas** — Fixação imediata da memória
- **7 Dias** — Consolidação de curto prazo
- **30 Dias** — Consolidação de longo prazo
- **90 Dias** — Reforço extremo
- **120 Dias** — Persistência
- **180 Dias** — Memória permanente

*Lógica:* `DataEstudo + Intervalo = DataProgramada`

### 2️⃣ **Método Ciclo 4.2**

Abordagem **semanal focada em produtividade** com balanceamento entre aprendizado e descanso:

- **Teoria (4 dias)** — O aluno avança em novos conteúdos
- **Revisão (2 dias)** — O sistema analisa os últimos 4 dias e gera lista focada **apenas nas fraquezas** (questões com erros)
- **Descanso (1 dia)** — Dia livre para descanso mental e prevenção de *burnout*

*Ciclo:* 7 dias com análise inteligente de gaps

### 3️⃣ **Revisão Cíclica**

Para quem prefere **rotação contínua** sem datas fixas:

- Baseada na **ordem do edital**
- Utiliza **filas de revisão** dinâmicas
- Permite flexibilidade total de agendamento

---

## 📊 Serviços de Domínio (27 Serviços)

### **Serviços de Negócio Críticos**

| Serviço | Responsabilidade | Linhas |
|---------|------------------|--------|
| **RevisaoService** | Motor inteligente de agendamento de revisões | 540+ |
| **EstudoService** | Gerenciamento de sessões de estudo | |
| **EstudoTransactionService** | Transações complexas (criar estudo + revisar) | |
| **CicloEstudoService** | Gestão de ciclos semanais 4.2 | |
| **RevisaoCicloAtivoService** | Gerencia o ciclo 4.2 em andamento | |
| **AssuntoService** | CRUD de tópicos/assuntos | |
| **DisciplinaService** | CRUD de disciplinas | |
| **EditalService** | CRUD de editais/concursos | |
| **EditalTransactionService** | Transações complexas de editais | |
| **EditalCronogramaService** | Cronogramas de prova e fases | |
| **ComparadorEditaisService** | Comparação visual de múltiplos editais | |
| **TipoEstudoService** | Tipos de estudo cadastrados | |

### **Serviços de Suporte e Infraestrutura**

| Serviço | Responsabilidade |
|---------|------------------|
| **AuditoriaService** | Rastreamento automático de mudanças |
| **DataService** | Acesso genérico aos dados |
| **ConfigurationService** | Carregamento/persistência de configurações |
| **BackupService / IBackupService** | Backup automático do banco de dados |
| **ThemeManager** | Gerenciamento de temas (Light/Dark) |
| **PomodoroTimerService** | Cronômetro Pomodoro integrado |
| **NavigationService** | Sistema de navegação entre views |

### **Serviços de Notificações Inteligentes**

| Serviço | Propósito |
|---------|-----------|
| **NotificationService** | Sistema central de notificações |
| **EstudoNotificacaoService** | Alertas de sessões de estudo |
| **RevisaoNotificacaoService** | Lembretes de revisões pendentes |
| **EditalCronogramaNotificacaoService** | Alertas de datas de prova |

---

## 🎨 Interface e Apresentação

### **Views/Telas Principais (38+ Views)**

| View | ViewModel | Funcionalidade |
|------|-----------|---|
| **ViewHome** | HomeViewModel | Dashboard com Heatmap, KPIs, gráficos |
| **ViewEstudos** | EstudosViewModel | Listagem de sessões de estudo |
| **ViewEstudoEditar** | EditarEstudoViewModel | Criar/editar uma sessão de estudo |
| **ViewRevisoesClassicas** | RevisoesClassicasViewModel | Revisões Ebbinghaus pendentes |
| **ViewRevisoes42** | Revisoes42ViewModel | Revisões Ciclo 4.2 |
| **ViewRevisoesCiclicas** | RevisoesCiclicasViewModel | Revisões Cíclicas |
| **ViewDisciplinas** | DisciplinasViewModel | Gestão de disciplinas |
| **ViewDisciplinaEditar** | EditarDisciplinaViewModel | Criar/editar disciplina |
| **ViewAssuntoEditar** | EditarAssuntoViewModel | Criar/editar assunto |
| **ViewEditais** | EditaisViewModel | Gestão de editais/concursos |
| **ViewEditalEditar** | EditarEditalViewModel | Criar/editar edital |
| **ViewEditalEditarAssuntos** | — | Associar assuntos a edital |
| **ViewEditalEditarCronograma** | — | Cronograma e fases |
| **ViewEditalEditarInformacoes** | — | Informações gerais do edital |
| **ViewCalendario** | CalendarioViewModel | Calendário visual com Heatmap |
| **ViewGraficos** | GraficosViewModel | Estatísticas e gráficos avançados |
| **ViewCicloEstudo** | CicloEstudoViewModel | Gerenciador do Ciclo 4.2 |
| **ViewComparadorEditais** | ComparadorEditaisViewModel | Comparação entre editais |
| **ViewConfiguracoes** | ConfiguracoesViewModel | Preferências da aplicação |
| **ViewSobre** | SobreViewModel | Sobre e informações |

### **Dialogs Especializados**

- `AdicionarAssuntosEmLoteDialog` — Importação em lote de assuntos
- `CustomMessageBoxWindow` — Caixas de mensagem customizadas
- `SplashScreen` — Tela de carregamento inicial
- `MoverAssuntoDialog` — Reorganizar assuntos entre disciplinas
- `MoverEventoDialog` — Reorganizar eventos do cronograma
- `RemoverAssuntoDialog` — Remoção com confirmação
- `DiaDetalhesPanel` — Detalhes completos de um dia no calendário
- `LoadingAndEmptyStatePanel` — Estados de carregamento e vazio

### **Componentes Reutilizáveis (Controls)**

| Control | Função |
|---------|--------|
| **PieChartControl.xaml** | Gráfico de pizza para distribuição por disciplina |
| **AccuracyPieChartControl.xaml** | Gráfico de pizza para acertos vs erros |
| **KPICard.xaml** | Cards de indicadores-chave de desempenho |

### **29 Conversores XAML**

Conversores especializados para binding entre modelos e UI:

- `BooleanToVisibilityConverter`, `InverseBooleanToVisibilityConverter`
- `BooleanToColorConverter`, `SimpleBoolToColorConverter`
- `BooleanToStatusConverter`, `BooleanToTextConverter`
- `HeatmapColorConverter` — Mapeamento de intensidade para cores
- `PeriodoToStringConverter`, `PeriodoMultiValueConverter`
- `AssuntoEstatisticasConverter`, `EditalEstatisticasConverter`
- `EditalStatusConverter`, `TipoEventoConverter`, `TipoEstudoColorConverter`
- `StringToBrushConverter`, `StringToBooleanConverter`, `StringToVisibilityConverter`
- `DoubleToPercentageConverter`, `HorasFormatConverter` (TimeSpan → "4h 30m")
- `NotNullToVisibilityConverter`, `CountToVisibilityConverter`
- `LoadingAndEmptyStateConverter`, `NullableIntConverter`
- `RevisaoConverters`, `MessageTypeConverters`
- [+ 9 conversores especializados]

### **3 Behaviors XAML**

| Behavior | Propósito |
|----------|-----------|
| **DurationValidationBehavior** | Valida durações de estudo |
| **EditableViewBehavior** | Comportamento para views em modo edição |
| **PlotViewTrackerBehavior** | Rastreamento de mouse em gráficos OxyPlot |

---

---

---

## 🏗️ Stack Tecnológico

O projeto foi construído com as melhores práticas modernas de desenvolvimento .NET:

| Camada | Tecnologias |
|--------|------------|
| **Framework Core** | .NET 9.0 (C# 13) |
| **Interface (UI)** | WPF (XAML) + Fluent Design System |
| **Arquitetura** | MVVM (Model-View-ViewModel) |
| **Banco de Dados** | SQLite com Entity Framework Core 9.0 |
| **State Management** | CommunityToolkit.MVVM (RelayCommand, ObservableObject) |
| **Componentes UI** | MahApps.Metro, OxyPlot (Gráficos), FluentWPF |
| **Serialização** | Newtonsoft.Json |
| **DI Container** | Microsoft.Extensions.DependencyInjection |
| **Behaviors** | Microsoft.Xaml.Behaviors.Wpf |

### 📦 Principais Dependências

```xml
CommunityToolkit.Mvvm (8.4.0)                  - MVVM moderna e eficiente
Microsoft.EntityFrameworkCore.Sqlite (9.0.0)  - ORM robusto
FluentWPF (0.10.2)                             - Design fluente Windows
OxyPlot.Wpf (2.1.2)                            - Gráficos e heatmaps
MahApps.Metro.IconPacks.Material (6.2.1)      - Ícones modernos
Microsoft.Xaml.Behaviors.Wpf (1.1.135)        - Behaviors declarativos
Newtonsoft.Json (13.0.4)                       - Serialização JSON
System.Drawing.Common (8.0.10)                 - Manipulação de imagens
```

## 🚀 Como Executar

### Pré-requisitos

- **Windows 10** (versão 1809 ou superior) ou **Windows 11**
- [**.NET Desktop Runtime 9.0**](https://dotnet.microsoft.com/download/dotnet/9.0) (para executar)
- [**SDK do .NET 9.0**](https://dotnet.microsoft.com/download/dotnet/9.0) (para compilar)
- **Visual Studio 2022** (recomendado) ou outro editor que suporte C# 13

### Instalação e Desenvolvimento

#### 1. Clone o repositório

```bash
git clone https://github.com/seu-usuario/StudyMinder.git
cd StudyMinder
```

#### 2. Execute via terminal

```bash
dotnet run --project StudyMinder
```

O banco de dados `StudyMinder.db` será criado automaticamente na primeira execução com seed de dados.

#### 3. Compile em Visual Studio

Abra `StudyMinder.sln` no Visual Studio 2022 e pressione `F5` para depuração ou `Ctrl+Shift+B` para compilar.

### 📦 Gerar Executável (Deploy)

Para criar uma versão **self-contained** (que não exige .NET instalado no PC de destino):

#### Método 1: Script Facilitador

```cmd
.\Publicar.bat
```

#### Método 2: Comando Manual

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

O executável será gerado em `bin/Release/net9.0-windows/win-x64/publish/`.

---

## 🏗️ Configuração de Compilação (Release)

Propriedades do projeto para Release otimizado:

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  <SelfContained>true</SelfContained>
  <PublishSingleFile>true</PublishSingleFile>
  <PublishReadyToRun>true</PublishReadyToRun>
  <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
</PropertyGroup>
```

Resultados:
- ✅ Executável único e portável
- ✅ Otimizado para execução rápida (ReadyToRun)
- ✅ Sem dependências externas
- ✅ Compatível com Windows x64

---

## 📊 Recursos Implementados

### ✅ Dashboard Inteligente
- Heatmap visual de consistência (dias estudados por mês)
- KPIs de desempenho em tempo real:
  - Total de horas estudadas
  - Taxa de acertos/erros
  - Disciplina mais estudada
  - Próximas revisões
- Gráficos OxyPlot interativos

### ✅ Agendamento Automático de Revisões
- 3 metodologias científicas (Ebbinghaus, Ciclo 4.2, Cíclico)
- Sincronização automática com sessões de estudo
- Notificações inteligentes por tipo de revisão
- Rastreamento de revisões realizadas vs pendentes

### ✅ Gestão Completa de Editais
- Cadastro de concursos com informações detalhadas
- Cronograma com fases de processo seletivo
- Datas de prova com alertas automáticos
- Associação de assuntos por edital
- Comparador visual de múltiplos editais

### ✅ Ciclo de Estudo 4.2
- Semana estruturada (4 dias teoria + 2 dias revisão + 1 dia descanso)
- Análise inteligente de erros dos últimos 4 dias
- Geração automática de lista de revisão focada
- Notificações de transição de fase

### ✅ Cronômetro Pomodoro Integrado
- Registro automático de horas de estudo
- Integração bidireccional com sessões
- Sincronização de duração
- Histórico de sessões

### ✅ Calendário Visual
- Heatmap de consistência (mapa de calor)
- Detalhes completos por dia:
  - Sessões estudadas
  - Revisões agendadas
  - Anotações
- Navegação entre meses

### ✅ Gestão de Disciplinas e Assuntos
- CRUD completo com validação
- Reordenação em lote
- Filtragem por edital
- Visualização por hierarquia

### ✅ Registros Detalhados de Estudo
- Duração com cronômetro integrado
- Acertos e erros por questão
- Páginas estudadas
- Material utilizado
- Professor/Fonte
- Tópicos cobertos
- Comentários pessoais

### ✅ Notificações Inteligentes
- Alertas de revisões pendentes
- Notificações de próximas provas
- Lembretes de consistência
- Feedback de conclusão

### ✅ Auditoria e Rastreamento
- Todas as entidades rastreadas (DataCriacao, DataModificacao)
- Histórico de mudanças
- Backup automático

### ✅ Interface Moderna
- Design Fluent Windows 10/11
- Efeitos acrílicos (FluentWPF)
- Temas Light/Dark dinâmicos
- Ícones Material Design
- Responsivo e otimizado

---

## 📋 Roadmap e Futuro

- [ ] **Sincronização em Nuvem** — Backup automático via Google Drive/OneDrive
- [ ] **App Mobile** — Versão companion em MAUI para revisar no telemóvel
- [ ] **Modo Foco Total** — Bloqueio de notificações do Windows durante o cronómetro
- [ ] **Exportação PDF** — Relatórios semanais de desempenho para impressão
- [ ] **Integrações** — Sincronização com Google Classroom, Zoom, etc.
- [ ] **Análise Preditiva** — IA para prever data ideal de prova baseada em progresso
- [ ] **Multiplataforma** — Versão para macOS e Linux

---

---

---

## 📁 Estrutura do Projeto

A solução segue rigorosamente o padrão **MVVM** para garantir manutenibilidade, testabilidade e escalabilidade:

```
StudyMinder/
├── StudyMinder.sln
├── StudyMinder/
│   ├── App.xaml / App.xaml.cs              # Inicialização e DI
│   ├── MainWindow.xaml / MainWindow.xaml.cs  # Shell principal
│   │
│   ├── Models/                             # 13 Entidades EF Core
│   │   ├── Disciplina.cs, Assunto.cs
│   │   ├── Estudo.cs, Revisao.cs
│   │   ├── Edital.cs, EditalAssunto.cs, EditalCronograma.cs
│   │   ├── CicloEstudo.cs, RevisaoCicloAtivo.cs
│   │   └── TipoEstudo.cs, FaseEdital.cs, etc.
│   │
│   ├── Data/                               # Acesso aos Dados
│   │   ├── StudyMinderContext.cs           # DbContext (14 DbSets)
│   │   └── DesignTimeDbContextFactory.cs   # Factory para migrations
│   │
│   ├── Services/                           # 27 Serviços de Domínio
│   │   ├── RevisaoService.cs               # Motor inteligente de revisões
│   │   ├── EstudoService.cs, EstudoTransactionService.cs
│   │   ├── CicloEstudoService.cs, RevisaoCicloAtivoService.cs
│   │   ├── EditalService.cs, EditalTransactionService.cs
│   │   ├── EditalCronogramaService.cs, ComparadorEditaisService.cs
│   │   ├── AssuntoService.cs, DisciplinaService.cs
│   │   ├── EstudoNotificacaoService.cs, RevisaoNotificacaoService.cs
│   │   ├── EditalCronogramaNotificacaoService.cs
│   │   ├── PomodoroTimerService.cs
│   │   ├── AuditoriaService.cs, BackupService.cs
│   │   ├── ConfigurationService.cs, ThemeManager.cs
│   │   └── NotificationService.cs
│   │
│   ├── ViewModels/                         # 24 ViewModels + Base
│   │   ├── BaseViewModel.cs                # Classe base
│   │   ├── HomeViewModel.cs                # Dashboard
│   │   ├── EstudosViewModel.cs, EditarEstudoViewModel.cs
│   │   ├── RevisoesClassicasViewModel.cs, Revisoes42ViewModel.cs, RevisoesCiclicasViewModel.cs
│   │   ├── DisciplinasViewModel.cs, EditarDisciplinaViewModel.cs, EditarAssuntoViewModel.cs
│   │   ├── EditaisViewModel.cs, EditarEditalViewModel.cs
│   │   ├── CalendarioViewModel.cs
│   │   ├── GraficosViewModel.cs
│   │   ├── CicloEstudoViewModel.cs
│   │   ├── ComparadorEditaisViewModel.cs
│   │   ├── ConfiguracoesViewModel.cs, SobreViewModel.cs
│   │   └── IEditableViewModel.cs, IRefreshable.cs
│   │
│   ├── Views/                              # 38+ Views/Dialogs XAML
│   │   ├── ViewHome.xaml                   # Dashboard
│   │   ├── ViewEstudos.xaml, ViewEstudoEditar.xaml
│   │   ├── ViewRevisoesClassicas.xaml
│   │   ├── ViewRevisoes42.xaml
│   │   ├── ViewRevisoesCiclicas.xaml
│   │   ├── ViewDisciplinas.xaml, ViewDisciplinaEditar.xaml
│   │   ├── ViewAssuntoEditar.xaml, ViewEditarAssunto.xaml
│   │   ├── ViewEditais.xaml, ViewEditalEditar.xaml
│   │   ├── ViewEditalEditarAssuntos.xaml
│   │   ├── ViewEditalEditarCronograma.xaml
│   │   ├── ViewEditalEditarInformacoes.xaml
│   │   ├── ViewCalendario.xaml
│   │   ├── ViewGraficos.xaml
│   │   ├── ViewCicloEstudo.xaml
│   │   ├── ViewComparadorEditais.xaml
│   │   ├── ViewConfiguracoes.xaml, ViewSobre.xaml
│   │   ├── Dialogs/
│   │   │   ├── AdicionarAssuntosEmLoteDialog.xaml
│   │   │   ├── AdicionarEstudoDialog.xaml
│   │   │   ├── CustomMessageBoxWindow.xaml
│   │   │   ├── MoverAssuntoDialog.xaml, MoverEventoDialog.xaml
│   │   │   ├── RemoverAssuntoDialog.xaml
│   │   │   └── SplashScreen.xaml
│   │   └── Panels/
│   │       ├── DiaDetalhesPanel.xaml
│   │       └── LoadingAndEmptyStatePanel.xaml
│   │
│   ├── Navigation/
│   │   └── NavigationService.cs            # Sistema de navegação
│   │
│   ├── Controls/                           # 3 Componentes Customizados
│   │   ├── PieChartControl.xaml
│   │   ├── AccuracyPieChartControl.xaml
│   │   └── KPICard.xaml
│   │
│   ├── Converters/                         # 29 Conversores XAML
│   │   ├── BooleanToVisibilityConverter.cs
│   │   ├── BooleanToColorConverter.cs
│   │   ├── HeatmapColorConverter.cs
│   │   ├── PeriodoToStringConverter.cs
│   │   ├── DoubleToPercentageConverter.cs
│   │   ├── HorasFormatConverter.cs
│   │   ├── StringToBrushConverter.cs
│   │   ├── TipoEstudoColorConverter.cs
│   │   └── [+ 21 mais conversores especializados]
│   │
│   ├── Behaviors/                          # 3 Behaviors Customizados
│   │   ├── DurationValidationBehavior.cs
│   │   ├── EditableViewBehavior.cs
│   │   └── PlotViewTrackerBehavior.cs
│   │
│   ├── Styles/                             # Estilos e Templates
│   │   └── *.xaml
│   │
│   ├── Themes/                             # Temas (Light/Dark)
│   │   └── *.xaml
│   │
│   ├── Config/                             # Configurações
│   │   └── userprefs.json
│   │
│   ├── Images/                             # Recursos visuais
│   ├── Fonts/                              # Tipografias
│   ├── Resources/                          # Assets
│   │
│   └── Utils/                              # Utilitários gerais
```

### 🗄️ Modelo de Dados (14 Entidades)

| Entidade | Propósito |
|----------|-----------|
| **Disciplina** | Disciplinas/Matérias de estudo |
| **Assunto** | Tópicos específicos dentro de uma disciplina |
| **Estudo** | Sessão de estudo com duração, acertos/erros, páginas |
| **Revisao** | Agendamento de revisões (Ebbinghaus, Ciclo 4.2, Cíclico) |
| **Edital** | Edital/Concurso com informações |
| **EditalAssunto** | Associação de assuntos a editais |
| **EditalCronograma** | Cronograma com fases e datas de prova |
| **CicloEstudo** | Ciclo de estudo semanal (4.2) |
| **RevisaoCicloAtivo** | Revisões do ciclo 4.2 ativo |
| **TipoEstudo** | Tipos de sessão (Aula, Exercício, Revisão) |
| **TiposProva** | Modalidades de prova (Objetiva, Dissertativa) |
| **Escolaridade** | Níveis de educação (Médio, Superior) |
| **StatusEdital** | Estados do concurso (Planejamento, Estudo, Realizado) |
| **FaseEdital** | Fases do processo seletivo |

**Suporte a Auditoria:** Todas as entidades implementam `IAuditable` com rastreamento automático de `DataCriacao` e `DataModificacao`.


## 🤝 Contribuindo

Contribuições são muito bem-vindas! Se deseja contribuir para o StudyMinder:

1. **Faça um Fork** do projeto
2. **Crie uma Branch** para sua feature (`git checkout -b feature/MinhaFeature`)
3. **Faça o Commit** com mensagem clara (`git commit -m 'Adiciona MinhaFeature'`)
4. **Faça o Push** para a branch (`git push origin feature/MinhaFeature`)
5. **Abra um Pull Request** descrevendo sua contribuição

### Diretrizes
- Mantenha o padrão MVVM
- Adicione testes quando possível
- Atualize a documentação
- Siga as convenções de nomenclatura C#

---

## 📄 Licença

Este projeto é **livre e de código aberto**.

Distribuído sob a licença **GNU General Public License v3.0 (GPLv3)**. Consulte o arquivo `LICENSE.txt` para mais detalhes.

---

## 👤 Autor

Desenvolvido com ❤️ por **Eric Jhon**.

---

## 📞 Contato e Suporte

- **Issues e Bugs:** Abra uma issue no repositório GitHub
- **Dúvidas:** Abra uma discussion no repositório
- **Sugestões:** Contribute or open a feature request

---

## 🙏 Agradecimentos

- Ao **Hospital Napoleão Laureano** pela inspiração de criar um software que serve a educação
- À comunidade .NET e open-source
- A todos que usam, testam e contribuem com feedback

---

## 📚 Referências

### Metodologias de Aprendizado
- **Curva de Ebbinghaus** — Spaced Repetition Theory
  - Hermann Ebbinghaus (1885) — "Memory: A Contribution to Experimental Psychology"
- **Ciclo 4.2** — Productivity Methodology
  - Baseado em research sobre ritmos de estudo e descanso
- **Pomodoro Technique** — Time Management
  - Francesco Cirillo

### Tecnologias
- [Microsoft .NET 9.0 Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [WPF Documentation](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/)
- [Entity Framework Core 9.0](https://learn.microsoft.com/en-us/ef/core/)
- [MVVM Toolkit](https://learn.microsoft.com/en-us/windows/communitytoolkit/mvvm/mvvm-introduction)

---

**Versão:** 3.0 (Em Desenvolvimento)  
**Última Atualização:** Janeiro 2026  
*Construído com ❤️, C# e muito café.* ☕📚

# S t u d y M i n d e r
# Sistema de Expedição Pokémon

Sistema distribuído de gerenciamento de expedições Pokémon usando processos concorrentes, memória compartilhada e sincronização com semáforos e mutex.

## 📋 Pré-requisitos

### Windows

1. **MinGW (GCC para Windows)**
   - Baixe em: http://www.mingw.org/
   - Instale em: `C:\MinGW`
   - Ou via MSYS2: https://www.msys2.org/

2. **.NET SDK 6.0 ou superior**
   - Baixe em: https://dotnet.microsoft.com/download
   - Você tem: .NET 8.0.303 ✅

3. **PowerShell**
   - Já incluído no Windows ✅

## 🚀 Como Compilar e Executar

### Opção 1: Script Automatizado (Recomendado)

```powershell
.\build-and-run.ps1
```

Este script irá:
1. Compilar todos os programas C (master_controller, trainer, arena)
2. Compilar o monitor gráfico C#
3. Perguntar como você quer executar

### Opção 2: Compilação Manual

#### Compilar programas C:

```powershell
gcc -o master_controller.exe master_controller.c shared_memory.c -lkernel32 -luser32
gcc -o trainer.exe trainer.c shared_memory.c -lkernel32 -luser32
gcc -o arena.exe arena.c shared_memory.c -lkernel32 -luser32
```

#### Compilar monitor C#:

```powershell
cd PokemonMonitorApp
dotnet build -c Release
cd ..
```

### Opção 3: Executar Diretamente

#### Apenas sistema C (console):
```powershell
.\master_controller.exe
```

#### Sistema C + Monitor Gráfico:
```powershell
# Terminal 1 - Abra o monitor a partir da pasta de build do projeto C#
Start-Process -FilePath "c:\Trabalho1-SO2\PokemonMonitorApp\bin\Release\net8.0-windows\PokemonMonitorApp.exe" -WorkingDirectory "c:\Trabalho1-SO2\PokemonMonitorApp\bin\Release\net8.0-windows"

# Terminal 2 - Rode o sistema C
.\master_controller.exe
```

## 🎮 Comandos Durante Execução

Quando o `master_controller.exe` estiver rodando, você pode digitar:

- `m` ou `monitor` - Exibir estatísticas do sistema
- `s` ou `shutdown` - Desligar o sistema ordenadamente
- `q` ou `quit` - Sair imediatamente

## 📁 Estrutura do Projeto

```
.
├── master_controller.c    # Controlador principal do sistema
├── trainer.c              # Processo treinador (produtor)
├── arena.c                # Processo arena (consumidor)
├── shared_memory.c        # Implementação da memória compartilhada
├── shared_memory.h        # Cabeçalho da memória compartilhada
├── PokemonMonitorApp/     # Projeto .NET do monitor (WinForms)
│   ├── Program.cs
│   └── PokemonMonitor.cs  # Código fonte do monitor gráfico
├── build-and-run.ps1      # Script de build e execução
└── README.md              # Este arquivo
```

## 🔧 Arquitetura do Sistema

### Componentes

1. **Master Controller** (`master_controller.exe`)
   - Inicia e gerencia 3 processos Arena
   - Inicia e gerencia 5 processos Trainer
   - Aceita comandos do usuário
   - Coordena o shutdown do sistema

2. **Trainer** (`trainer.exe`)
   - Gera requisições de Pokémon aleatórias
   - Envia para a fila compartilhada (produtor)
   - 5 instâncias rodando simultaneamente

3. **Arena** (`arena.exe`)
   - Processa batalhas Pokémon
   - Consome da fila compartilhada (consumidor)
   - 3 instâncias rodando simultaneamente

4. **Monitor Gráfico** (`PokemonMonitor.exe`)
   - Interface Windows Forms
   - Exibe estatísticas em tempo real
   - Visualiza a fila de Pokémon
   - Permite desligar o sistema

### Sincronização

- **Memória Compartilhada**: Comunicação entre processos
- **Mutex**: Proteção de seção crítica
- **Semáforos**: 
  - `SemEmpty`: Controla espaços vazios na fila (max 10)
  - `SemFull`: Controla itens disponíveis na fila

### Problema Produtor-Consumidor

- **Buffer Circular**: Tamanho 10
- **Produtores**: 5 Trainers
- **Consumidores**: 3 Arenas
- **Prioridade**: Pokémon feridos têm prioridade

## 🐛 Solução de Problemas

### Erro: "gcc não é reconhecido"

Adicione MinGW ao PATH:
```powershell
$env:Path = "C:\MinGW\bin;" + $env:Path
```

Ou adicione permanentemente nas variáveis de ambiente do Windows.

### Erro: "dotnet não é reconhecido"

Reinstale o .NET SDK e reinicie o terminal.

### Monitor não abre

Execute o EXE diretamente da pasta de build do projeto para garantir que os arquivos `.deps.json` e `.runtimeconfig.json` corretos sejam usados:
```powershell
Start-Process -FilePath "c:\Trabalho1-SO2\PokemonMonitorApp\bin\Release\net8.0-windows\PokemonMonitorApp.exe" -WorkingDirectory "c:\Trabalho1-SO2\PokemonMonitorApp\bin\Release\net8.0-windows"
```
Se o Windows avisar que falta ".NET Desktop Runtime", instale a versão 8.x:
https://aka.ms/dotnet/8/desktop/runtime

### Sistema trava ou não responde

1. Pressione `Ctrl+C` para parar
2. Verifique se há processos órfãos:
   ```powershell
   Get-Process | Where-Object {$_.Name -match "trainer|arena|master"}
   ```
3. Mate processos se necessário:
   ```powershell
   Stop-Process -Name trainer,arena,master_controller -Force
   ```

## 📊 Estatísticas Exibidas

- **Total de Batalhas**: Contador global
- **Pokémon Feridos**: Pokémon com prioridade atendidos
- **Arenas Ocupadas**: Quantas das 3 arenas estão ativas
- **Fila de Pokémon**: Lista de requisições pendentes

## 🎓 Conceitos de SO2 Implementados

- ✅ Processos concorrentes
- ✅ Memória compartilhada (IPC)
- ✅ Sincronização com Mutex
- ✅ Sincronização com Semáforos
- ✅ Problema Produtor-Consumidor
- ✅ Buffer circular
- ✅ Fila com prioridade
- ✅ Tratamento de sinais (Ctrl+C)
- ✅ Gerenciamento de processos filhos

## 👨‍💻 Desenvolvimento

### Adicionar mais treinadores/arenas

Edite `master_controller.c`:

```c
#define MAX_TRAINERS 5  // Altere este valor
#define MAX_ARENAS 3    // Altere este valor
```

Recompile o projeto.

### Alterar tamanho do buffer

Edite `shared_memory.h`:

```c
#define BUFFER_SIZE 10  // Altere este valor
```

Recompile o projeto.

## 📝 Licença

Projeto acadêmico para disciplina de Sistemas Operacionais 2.

# Sistema de Expedição Pokémon

Este projeto contém processos em C (master_controller, trainer, arena) e um monitor gráfico em C# (WinForms) que se comunicam via memória compartilhada.

## Requisitos

- PowerShell (já incluso no Windows)
- MinGW GCC instalado e no PATH (ex.: C:\MinGW\bin)
  - Download: http://www.mingw.org/ ou via MSYS2: https://www.msys2.org/
- .NET 8 Desktop Runtime (Microsoft.WindowsDesktop.App 8.x) para executar o monitor gráfico
  - Alternativa para compilar: .NET SDK 8 (inclui o runtime)
  - Download: https://dotnet.microsoft.com/download

## Como executar

Abra um PowerShell na pasta do projeto e rode o script automatizado:

```powershell
.\build-and-run.ps1
```

O script compila os programas C e o monitor C# e oferece as opções de execução:
- [1] Apenas o sistema C (console)
- [2] Sistema C + abrir o monitor gráfico

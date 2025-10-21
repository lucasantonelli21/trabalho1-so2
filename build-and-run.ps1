# Script de Build e Execucao - Sistema de Expedicao Pokemon
# Usando MinGW GCC

Write-Host "=== Sistema de Expedicao Pokemon ===" -ForegroundColor Cyan
Write-Host ""

# Adicionar MinGW ao PATH temporariamente
$env:Path = "C:\MinGW\bin;" + $env:Path

# Raiz do script (para resolver caminhos de forma confiavel)
if ($PSScriptRoot) {
    $ScriptRoot = $PSScriptRoot
} elseif ($MyInvocation -and $MyInvocation.MyCommand -and $MyInvocation.MyCommand.Path) {
    $ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
} else {
    $ScriptRoot = (Get-Location).Path
}

# Verificar se GCC esta disponivel
Write-Host "Verificando compilador..." -ForegroundColor Yellow
$gccVersion = & gcc --version 2>&1 | Select-Object -First 1
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRO: GCC nao encontrado! Verifique se MinGW esta instalado em C:\MinGW" -ForegroundColor Red
    Write-Host "Voce tambem pode tentar: C:\msys64\mingw64\bin" -ForegroundColor Yellow
    exit 1
}
Write-Host "GCC encontrado: $gccVersion" -ForegroundColor Green
Write-Host ""

# Limpar builds anteriores
Write-Host "Limpando builds anteriores..." -ForegroundColor Yellow
Remove-Item *.exe -ErrorAction SilentlyContinue
Remove-Item *.o -ErrorAction SilentlyContinue
Write-Host "Limpeza concluida" -ForegroundColor Green
Write-Host ""

# Compilar os programas C
Write-Host "Compilando programas C..." -ForegroundColor Yellow

Write-Host "  [1/3] Compilando master_controller.exe..." -ForegroundColor Cyan
& gcc -o master_controller.exe master_controller.c shared_memory.c -lkernel32 -luser32
if ($LASTEXITCODE -ne 0) {
    Write-Host "Erro ao compilar master_controller" -ForegroundColor Red
    exit 1
}
Write-Host "  master_controller.exe compilado" -ForegroundColor Green

Write-Host "  [2/3] Compilando trainer.exe..." -ForegroundColor Cyan
& gcc -o trainer.exe trainer.c shared_memory.c -lkernel32 -luser32
if ($LASTEXITCODE -ne 0) {
    Write-Host "Erro ao compilar trainer" -ForegroundColor Red
    exit 1
}
Write-Host "  trainer.exe compilado" -ForegroundColor Green

Write-Host "  [3/3] Compilando arena.exe..." -ForegroundColor Cyan
& gcc -o arena.exe arena.c shared_memory.c -lkernel32 -luser32
if ($LASTEXITCODE -ne 0) {
    Write-Host "Erro ao compilar arena" -ForegroundColor Red
    exit 1
}
Write-Host "  arena.exe compilado" -ForegroundColor Green
Write-Host ""

# Compilar o monitor C#
Write-Host "Compilando monitor C# (PokemonMonitor)..." -ForegroundColor Yellow

# Checar .NET e Desktop Runtime/SDK instalados (exibe aviso se faltar)
& dotnet --list-runtimes > $null 2>&1
$hasDotnet = ($LASTEXITCODE -eq 0)
& dotnet --list-sdks > $null 2>&1
$hasSdk = ($LASTEXITCODE -eq 0)
if (-not $hasDotnet) {
    Write-Host "  .NET nao encontrado - Monitor C# nao sera compilado" -ForegroundColor Yellow
} else {
    $runtimes = & dotnet --list-runtimes
    $hasWinDesktop = $false
    if ($runtimes) {
        $hasWinDesktop = $runtimes -match "Microsoft.WindowsDesktop.App 8\."
    }
    if (-not $hasWinDesktop) {
        Write-Host "  Aviso: Microsoft.WindowsDesktop.App (Desktop Runtime) 8.x nao detectado." -ForegroundColor Yellow
        Write-Host "  Se o executavel reclamar do runtime, instale: https://aka.ms/dotnet/8/desktop/runtime" -ForegroundColor DarkYellow
    }

    $pmProjectPath = Join-Path $ScriptRoot "PokemonMonitorApp\PokemonMonitorApp.csproj"
    if (-not (Test-Path -PathType Leaf $pmProjectPath)) {
        $pmFolder = Join-Path $ScriptRoot "PokemonMonitorApp"
        if (Test-Path $pmFolder) {
            $candidate = Get-ChildItem -Path $pmFolder -Filter *.csproj -File -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($candidate) { $pmProjectPath = $candidate.FullName }
        }
    }

    if (Test-Path -PathType Leaf $pmProjectPath) {
        Write-Host "  Projeto encontrado: $pmProjectPath" -ForegroundColor DarkGray
        if ($hasSdk) {
            Write-Host "  Compilando PokemonMonitor (Release)..." -ForegroundColor Cyan
            $buildOut = & dotnet build $pmProjectPath -c Release 2>&1
            if ($LASTEXITCODE -eq 0) {
                $pmProjectDir = Split-Path $pmProjectPath -Parent
                $pmBuildDir = Join-Path $pmProjectDir "bin\Release\net8.0-windows"
                $pmExeFromBuild = Join-Path $pmBuildDir "PokemonMonitorApp.exe"
                Write-Host "  PokemonMonitorApp (Release) compilado" -ForegroundColor Green
            } else {
                Write-Host "  Falha ao compilar monitor C#" -ForegroundColor Red
                Write-Host $buildOut -ForegroundColor DarkYellow
                Write-Host "  O sistema C funcionara normalmente" -ForegroundColor Yellow
            }
        } else {
            Write-Host "  .NET SDK nao detectado - pulando compilacao do monitor. Instale .NET SDK 8.x para compilar." -ForegroundColor Yellow
            Write-Host "  Link: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor DarkYellow
        }
    } else {
        Write-Host "  Projeto PokemonMonitorApp nao encontrado em: $pmProjectPath" -ForegroundColor Yellow
    }
}
Write-Host ""

# Verificar executaveis criados
Write-Host "Verificando executaveis..." -ForegroundColor Yellow
$executables = @("master_controller.exe", "trainer.exe", "arena.exe")
$allExist = $true
foreach ($exe in $executables) {
    if (Test-Path $exe) {
        Write-Host "  $exe criado com sucesso" -ForegroundColor Green
    } else {
        Write-Host "  $exe nao encontrado!" -ForegroundColor Red
        $allExist = $false
    }
}
Write-Host ""

if (-not $allExist) {
    Write-Host "ERRO: Alguns executaveis nao foram criados!" -ForegroundColor Red
    exit 1
}

# Perguntar se deseja executar
Write-Host "=== Build concluido com sucesso! ===" -ForegroundColor Green
Write-Host ""
Write-Host "Como deseja executar?" -ForegroundColor Cyan
Write-Host "  [1] Apenas o sistema C (master_controller)" -ForegroundColor White
Write-Host "  [2] Sistema C + Monitor Grafico (se disponivel)" -ForegroundColor White
Write-Host "  [3] Nao executar agora" -ForegroundColor White
Write-Host ""
$choice = Read-Host "Escolha uma opcao (1-3)"

switch ($choice) {
    "1" {
        Write-Host ""
        Write-Host "Iniciando sistema..." -ForegroundColor Green
        Write-Host "Pressione Ctrl+C para parar" -ForegroundColor Yellow
        Write-Host ""
        & .\master_controller.exe
    }
    "2" {
        Write-Host ""
        Write-Host "Iniciando sistema com monitor..." -ForegroundColor Green
        
        # Tenta iniciar o monitor direto da pasta de build (evita erros de runtime)
        $pmProjectPath = Join-Path $ScriptRoot "PokemonMonitorApp\PokemonMonitorApp.csproj"
        $pmProjectDir = Split-Path $pmProjectPath -Parent
        $pmBuildDir = Join-Path $pmProjectDir "bin\Release\net8.0-windows"
        $pmExeFromBuild = Join-Path $pmBuildDir "PokemonMonitorApp.exe"

        if (-not (Test-Path $pmExeFromBuild)) {
            # Se nao existir o exe, tentar compilar agora
            if (-not (Test-Path -PathType Leaf $pmProjectPath)) {
                $pmFolder = Join-Path $ScriptRoot "PokemonMonitorApp"
                if (Test-Path $pmFolder) {
                    $candidate = Get-ChildItem -Path $pmFolder -Filter *.csproj -File -ErrorAction SilentlyContinue | Select-Object -First 1
                    if ($candidate) { $pmProjectPath = $candidate.FullName; $pmProjectDir = Split-Path $pmProjectPath -Parent }
                }
            }
            if (Test-Path -PathType Leaf $pmProjectPath) {
                Write-Host "  Compilando monitor C# (on-demand)..." -ForegroundColor Cyan
                $buildOut2 = & dotnet build $pmProjectPath -c Release 2>&1
                if ($LASTEXITCODE -eq 0) {
                    $pmBuildDir = Join-Path $pmProjectDir "bin\Release\net8.0-windows"
                    $pmExeFromBuild = Join-Path $pmBuildDir "PokemonMonitorApp.exe"
                } else {
                    Write-Host "  Falha ao compilar monitor C#" -ForegroundColor Red
                    Write-Host $buildOut2 -ForegroundColor DarkYellow
                }
            } else {
                Write-Host "  Projeto PokemonMonitorApp nao encontrado para compilar" -ForegroundColor Yellow
            }
        }

        if (Test-Path $pmExeFromBuild) {
            # Executa a partir da pasta de build para usar .deps e runtimeconfig corretos
            Start-Process -FilePath $pmExeFromBuild -WorkingDirectory $pmBuildDir
            Start-Sleep -Seconds 2
        } elseif (Test-Path "PokemonMonitor.exe") {
            # Fallback para copia no root, se existir
            Start-Process -FilePath ".\PokemonMonitor.exe"
            Start-Sleep -Seconds 2
        } else {
            Write-Host "  Aviso: Executavel do monitor nao encontrado. Tente recompilar (opcao 2 requer build C#)." -ForegroundColor Yellow
        }
        
        Write-Host "Pressione Ctrl+C para parar" -ForegroundColor Yellow
        Write-Host ""
        & .\master_controller.exe
    }
    "3" {
        Write-Host ""
        Write-Host "Para executar depois, use:" -ForegroundColor Cyan
        Write-Host "  .\master_controller.exe" -ForegroundColor White
        Write-Host ""
    }
    default {
        Write-Host "Opcao invalida!" -ForegroundColor Red
    }
}

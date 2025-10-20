# Script de build para Windows
Write-Host "=== Building Pokemon Expedition System ===" -ForegroundColor Green

# Compilar com Visual Studio (ajuste o caminho conforme necessário)
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe

if (-not (Test-Path $msbuild)) {
    $msbuild = "msbuild.exe"
}

Write-Host "Compilando projetos..." -ForegroundColor Yellow

# Criar solution file temporária
$solutionContent = @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
Project("{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}") = "PokemonExpedition", "PokemonExpedition.vcxproj", "{12345678-1234-1234-1234-123456789012}"
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|x64 = Debug|x64
		Release|x64 = Release|x64
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{12345678-1234-1234-1234-123456789012}.Debug|x64.ActiveCfg = Debug|x64
		{12345678-1234-1234-1234-123456789012}.Debug|x64.Build.0 = Debug|x64
		{12345678-1234-1234-1234-123456789012}.Release|x64.ActiveCfg = Release|x64
		{12345678-1234-1234-1234-123456789012}.Release|x64.Build.0 = Release|x64
	EndGlobalSection
EndGlobal
"@

$solutionContent | Out-File -FilePath "PokemonExpedition.sln" -Encoding ASCII

# Executar build
& $msbuild "PokemonExpedition.sln" /p:Configuration=Release

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build realizado com sucesso!" -ForegroundColor Green
    Write-Host "Executando sistema..." -ForegroundColor Yellow
    Start-Process -FilePath ".\Release\master_controller.exe"
} else {
    Write-Host "Erro no build!" -ForegroundColor Red
}
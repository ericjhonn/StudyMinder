@echo off
echo.
echo ================================================
echo  Gerando o StudyMinder (Single-File App)
echo ================================================
echo.

REM Entra na pasta onde este arquivo script esta salvo (a raiz do projeto)
cd /d "%~dp0"

REM Limpar compilações anteriores
echo Limpando compilacoes anteriores...
dotnet clean -c Release >nul 2>&1

REM Comando de publicacao para single-file autossuficiente
echo Publicando aplicacao...
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=true -o "bin\Publish\win-x64"

if errorlevel 1 (
    echo.
    echo ==========================================
    echo ERRO NA PUBLICACAO!
    echo Verifique se ha erros acima.
    echo ==========================================
    pause
    exit /b 1
)

echo.
echo ================================================
echo  SUCESSO! 
echo  O arquivo StudyMinder.exe foi gerado em:
echo  %~dp0bin\Publish\win-x64
echo ================================================
echo.

REM Exibir tamanho do arquivo gerado
for %%F in ("bin\Publish\win-x64\StudyMinder.exe") do (
    set "size=%%~zF"
    for /F %%A in ('powershell -NoProfile -Command "Write-Host ([math]::Round(%%~zF / 1MB, 2)) + ' MB'"') do set "size=%%A"
)
echo Tamanho do arquivo: %size%
echo.
pause
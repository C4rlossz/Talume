@echo off
cd /d "%~dp0"
docker info >nul 2>&1
if errorlevel 1 (
  echo Abra o Docker Desktop e aguarde ele iniciar. Depois execute este arquivo novamente.
  pause
  exit /b 1
)
echo Preparando o Talume. O primeiro inicio pode levar alguns minutos.
docker compose up --build -d
if errorlevel 1 (
  echo Nao foi possivel iniciar. Veja a mensagem acima.
  pause
  exit /b 1
)
echo.
echo Aplicacao: http://localhost:8081
echo E-mails locais: http://localhost:8025
echo Aguarde a aplicacao iniciar antes de abrir o navegador.
echo Para acompanhar: docker compose logs --tail=100 app
pause

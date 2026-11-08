@echo off
rem Запуск Unity 6.3 в DirectX 11 (обход падений D3D12)
start "" "C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe" -projectPath "%~dp0." -force-d3d11

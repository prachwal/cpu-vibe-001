#!/bin/bash
set -e

echo "=== Building all source projects ==="
dotnet build src/Cpu.Core/Cpu.Core.csproj -nologo
dotnet build src/Cpu.Board/Cpu.Board.csproj -nologo
dotnet build src/Cpu.Module.Abstractions/Cpu.Module.Abstractions.csproj -nologo
dotnet build src/Cpu.Tui.Abstractions/Cpu.Tui.Abstractions.csproj -nologo
dotnet build src/Cpu.Tui.Media/Cpu.Tui.Media.csproj -nologo

echo "=== Building Tui ==="
dotnet build src/Cpu.Tui/Cpu.Tui.csproj -nologo

echo "=== Building modules ==="
dotnet build src/Cpu.Help/Cpu.Help.csproj -nologo
dotnet build src/Cpu.Screen/Cpu.Screen.csproj -nologo
dotnet build src/Cpu.DemoMenu/Cpu.DemoMenu.csproj -nologo
dotnet build src/Cpu.Image/Cpu.Image.csproj -nologo
dotnet build src/Cpu.Canvas/Cpu.Canvas.csproj -nologo
dotnet build src/Cpu.Apple1/Cpu.Apple1.csproj -nologo

echo "=== Starting Cpu.Tui ==="
dotnet run --project src/Cpu.Tui --no-build "$@"

#!/usr/bin/env bash
# One-time bootstrap: adds NuGet packages (versions resolve into Directory.Packages.props), then builds + tests.
# Re-runnable. Requires .NET SDK 10+. A failing package is reported, not fatal.
set -u
cd "$(dirname "$0")"
command -v dotnet >/dev/null || { echo "Install the .NET 10 SDK first: https://dotnet.microsoft.com/download"; exit 1; }
FAILED=()
add() { # project package...
  local proj=$1; shift
  for pkg in "$@"; do
    echo "+ $proj <- $pkg"
    if ! dotnet add "$proj" package "$pkg" >/dev/null 2>&1; then
      # some packages (e.g. Agent Framework pieces) may only have prerelease builds
      dotnet add "$proj" package "$pkg" --prerelease >/dev/null 2>&1 && echo "  (prerelease: $pkg)" || FAILED+=("$proj:$pkg")
    fi
  done
}
S=src; T=tests
add $S/VocalEye.Application/VocalEye.Application.csproj Microsoft.Extensions.AI.Abstractions Microsoft.Extensions.DependencyInjection.Abstractions Microsoft.Extensions.Logging.Abstractions
add $S/VocalEye.Agents/VocalEye.Agents.csproj           Microsoft.Agents.AI Microsoft.Extensions.AI Microsoft.Extensions.DependencyInjection.Abstractions Microsoft.Extensions.Logging.Abstractions
add $S/VocalEye.Tools/VocalEye.Tools.csproj             DocumentFormat.OpenXml MailKit QuestPDF Microsoft.Extensions.DependencyInjection.Abstractions Microsoft.Extensions.Logging.Abstractions
add $S/VocalEye.Rag/VocalEye.Rag.csproj                 Microsoft.Extensions.VectorData.Abstractions UglyToad.PdfPig DocumentFormat.OpenXml Microsoft.ML.Tokenizers Microsoft.Extensions.DependencyInjection.Abstractions
add $S/VocalEye.Infrastructure/VocalEye.Infrastructure.csproj Microsoft.Extensions.AI Microsoft.Extensions.AI.OpenAI Azure.AI.OpenAI OllamaSharp Anthropic Npgsql.EntityFrameworkCore.PostgreSQL Pgvector.EntityFrameworkCore Microsoft.EntityFrameworkCore.Design Microsoft.Extensions.Caching.StackExchangeRedis Microsoft.Extensions.Http.Resilience OpenTelemetry.Extensions.Hosting OpenTelemetry.Exporter.OpenTelemetryProtocol OpenTelemetry.Instrumentation.AspNetCore OpenTelemetry.Instrumentation.Http
add $S/VocalEye.Api/VocalEye.Api.csproj                 Microsoft.AspNetCore.OpenApi
add $T/VocalEye.UnitTests/VocalEye.UnitTests.csproj     Microsoft.NET.Test.Sdk xunit xunit.runner.visualstudio NSubstitute
add $T/VocalEye.IntegrationTests/VocalEye.IntegrationTests.csproj Microsoft.NET.Test.Sdk xunit xunit.runner.visualstudio Microsoft.AspNetCore.Mvc.Testing Testcontainers.PostgreSql
add $T/VocalEye.AgentEvals/VocalEye.AgentEvals.csproj   Microsoft.NET.Test.Sdk xunit xunit.runner.visualstudio

echo "== build =="; dotnet build VocalEye.slnx || FAILED+=("build")
echo "== unit tests =="; dotnet test tests/VocalEye.UnitTests --no-build || FAILED+=("unit-tests")
if [ ${#FAILED[@]} -gt 0 ]; then echo; echo "Needs attention:"; printf '  - %s\n' "${FAILED[@]}"; exit 1; fi
echo "All good. Next: docker compose up -d && dotnet run --project src/VocalEye.Api"

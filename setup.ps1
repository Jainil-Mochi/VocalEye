# One-time bootstrap (Windows). Adds NuGet packages, builds, runs unit tests. Re-runnable.
Set-Location $PSScriptRoot
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { Write-Host "Install the .NET 10 SDK first: https://dotnet.microsoft.com/download"; exit 1 }
$failed = @()
function Add-Pkgs($proj, [string[]]$pkgs) {
  foreach ($p in $pkgs) {
    Write-Host "+ $proj <- $p"
    dotnet add $proj package $p *> $null
    if ($LASTEXITCODE -ne 0) {
      # some packages (e.g. Agent Framework pieces) may only have prerelease builds
      dotnet add $proj package $p --prerelease *> $null
      if ($LASTEXITCODE -ne 0) { $script:failed += "${proj}:$p" } else { Write-Host "  (prerelease: $p)" }
    }
  }
}
$di = "Microsoft.Extensions.DependencyInjection.Abstractions"; $log = "Microsoft.Extensions.Logging.Abstractions"
Add-Pkgs "src/VocalEye.Application/VocalEye.Application.csproj" @("Microsoft.Extensions.AI.Abstractions",$di,$log)
Add-Pkgs "src/VocalEye.Agents/VocalEye.Agents.csproj" @("Microsoft.Agents.AI","Microsoft.Extensions.AI",$di,$log)
Add-Pkgs "src/VocalEye.Tools/VocalEye.Tools.csproj" @("DocumentFormat.OpenXml","MailKit","QuestPDF",$di,$log)
Add-Pkgs "src/VocalEye.Rag/VocalEye.Rag.csproj" @("Microsoft.Extensions.VectorData.Abstractions","UglyToad.PdfPig","DocumentFormat.OpenXml","Microsoft.ML.Tokenizers",$di)
Add-Pkgs "src/VocalEye.Infrastructure/VocalEye.Infrastructure.csproj" @("Microsoft.Extensions.AI","Microsoft.Extensions.AI.OpenAI","Azure.AI.OpenAI","OllamaSharp","Anthropic","Npgsql.EntityFrameworkCore.PostgreSQL","Pgvector.EntityFrameworkCore","Microsoft.EntityFrameworkCore.Design","Microsoft.Extensions.Caching.StackExchangeRedis","Microsoft.Extensions.Http.Resilience","OpenTelemetry.Extensions.Hosting","OpenTelemetry.Exporter.OpenTelemetryProtocol","OpenTelemetry.Instrumentation.AspNetCore","OpenTelemetry.Instrumentation.Http")
Add-Pkgs "src/VocalEye.Api/VocalEye.Api.csproj" @("Microsoft.AspNetCore.OpenApi")
Add-Pkgs "tests/VocalEye.UnitTests/VocalEye.UnitTests.csproj" @("Microsoft.NET.Test.Sdk","xunit","xunit.runner.visualstudio","NSubstitute")
Add-Pkgs "tests/VocalEye.IntegrationTests/VocalEye.IntegrationTests.csproj" @("Microsoft.NET.Test.Sdk","xunit","xunit.runner.visualstudio","Microsoft.AspNetCore.Mvc.Testing","Testcontainers.PostgreSql")
Add-Pkgs "tests/VocalEye.AgentEvals/VocalEye.AgentEvals.csproj" @("Microsoft.NET.Test.Sdk","xunit","xunit.runner.visualstudio")
Write-Host "== build =="; dotnet build VocalEye.slnx; if ($LASTEXITCODE -ne 0) { $failed += "build" }
Write-Host "== unit tests =="; dotnet test tests/VocalEye.UnitTests --no-build; if ($LASTEXITCODE -ne 0) { $failed += "unit-tests" }
if ($failed.Count -gt 0) { Write-Host "`nNeeds attention:"; $failed | ForEach-Object { Write-Host "  - $_" }; exit 1 }
Write-Host "All good. Next: docker compose up -d ; dotnet run --project src/VocalEye.Api"

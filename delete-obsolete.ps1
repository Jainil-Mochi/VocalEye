# Run from the repository root (the folder containing VocalEye.slnx).
Remove-Item -LiteralPath 'src/VocalEye.Application/Abstractions/Agents/ModelCallFailedException.cs' -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath 'src/VocalEye.Application/Abstractions/Voice/IVoice.cs' -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath 'src/VocalEye.Contracts/Chat/ChatContracts.cs' -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath 'tests/VocalEye.AgentEvals/PlaceholderEvals.cs' -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath 'tests/VocalEye.UnitTests/Fakes/FakeChatClientFactory.cs' -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath 'tests/VocalEye.UnitTests/Fakes/FakeTool.cs' -Force -ErrorAction SilentlyContinue

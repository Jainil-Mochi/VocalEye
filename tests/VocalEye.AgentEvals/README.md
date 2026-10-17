# Agent & RAG evals
Datasets live in `/evals/datasets` (JSONL: input, expected tool calls / expected answer facts).
Evals run on demand (`dotnet test tests/VocalEye.AgentEvals`), not on every commit: they cost tokens.
Track: tool-selection accuracy, retrieval recall@k, answer groundedness, latency, cost.

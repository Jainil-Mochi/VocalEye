# ADR-0001: Modular monolith with Clean Architecture
Status: Accepted
Context: Solo/small team, needs fast learning loop, but must scale and add services easily.
Decision: One API + one Worker, modules with interface boundaries, dependencies inward only.
Consequences: Simple deploy/debug now; modules can be extracted to services later. Boundaries enforced by project references and (Phase 4) architecture tests.

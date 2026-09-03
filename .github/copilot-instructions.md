# GitHub Copilot repository instructions

Repository-level custom instructions for Copilot code review. Mirrors
`.pr_agent.toml`'s `[pr_reviewer].extra_instructions` -- keep the two in sync.

The list below covers baseline conventions that bot reviewers reliably misread
as defects. Add project-specific exemptions underneath it, each with the
reason and, where one exists, the issue or PR that decided it -- an
unexplained exemption is indistinguishable from a bug being waved through.

Respect these deliberate baseline conventions and do not flag them as defects:

- Any DI-injected constructor dependency (HttpClient, DbContext, ILogger<T>,
  and application services) is trusted and intentionally not null-guarded. Only
  non-DI public/internal entry points guard their reference-type parameters --
  flag a missing guard there, never on an injected dependency.
- xUnit test classes are public sealed on purpose. The baseline defaults types
  to internal sealed but names this exact exception: a type a framework must
  discover. Do not suggest making test classes internal.
- *.Logging.cs partial void Log... methods are [LoggerMessage] source-generated;
  the generator supplies the bodies, so they are not unimplemented partials.
- Classes and structs use explicit constructors with _field backing fields, not
  primary constructors (IDE0290 is off). Do not suggest converting them. This
  rule does NOT apply to records (sealed record, readonly record struct):
  CLAUDE-baseline.md and CLAUDE-domain-driven-design.md explicitly call for
  record positional syntax for pure data carriers with no validation and
  default equality -- DTOs, value objects, and discriminated-union outcome
  types (e.g. CreateXOutcome/XSucceeded/XFailed hierarchies). Do not flag a
  record's positional constructor as a primary-constructor violation, and do
  not suggest adding underscore-prefixed backing fields to one.
- EF Core-generated migration classes under Migrations/ (including the model
  snapshot) are public partial by generator design; regeneration reverts
  hand-edits, so do not flag their public surface or suggest sealing them.
- EF Core read-only queries use AsNoTracking by convention, but write/upsert
  paths that load an entity to modify and save it require change tracking; do
  not flag those as missing AsNoTracking.
- Trainfree is a self-hosted, single-user app (see CLAUDE.md). Race conditions,
  concurrent-request handling, and multi-user locking/isolation concerns are
  not a priority -- do not flag missing concurrency guards, optimistic
  locking, or request-ordering races as defects.

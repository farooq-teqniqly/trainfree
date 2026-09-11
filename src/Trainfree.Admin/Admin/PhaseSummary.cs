using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Admin;

/// <summary>A phase as displayed in the admin UI.</summary>
public sealed record PhaseSummary(PhaseId Id, string Name);

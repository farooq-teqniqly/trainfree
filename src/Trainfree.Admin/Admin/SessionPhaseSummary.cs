using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Admin;

/// <summary>A session phase as displayed in the admin UI, nested under its session.</summary>
internal sealed record SessionPhaseSummary(SessionPhaseId Id, SessionId SessionId, PhaseId PhaseId);

using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Admin;

/// <summary>A session as displayed in the admin UI, nested under its program.</summary>
internal sealed record SessionSummary(SessionId Id, ProgramId ProgramId, string Name);

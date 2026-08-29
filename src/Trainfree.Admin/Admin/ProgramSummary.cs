using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Admin;

/// <summary>A program as displayed in the admin UI.</summary>
internal sealed record ProgramSummary(ProgramId Id, string Name);

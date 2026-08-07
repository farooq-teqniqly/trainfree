using Trainfree.Web.Ids;

namespace Trainfree.Web.Admin;

/// <summary>A program as displayed in the admin UI.</summary>
internal sealed record ProgramSummary(ProgramId Id, string Name);

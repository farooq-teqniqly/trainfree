using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Admin;

/// <summary>A category as displayed in the admin UI.</summary>
internal sealed record CategorySummary(CategoryId Id, string Name);

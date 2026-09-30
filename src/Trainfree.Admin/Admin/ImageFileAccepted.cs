namespace Trainfree.Admin.Admin;

/// <summary>The file is a supported type within the size limit; carries its content type as given.</summary>
internal sealed record ImageFileAccepted(string ContentType) : ImageFileValidationOutcome;

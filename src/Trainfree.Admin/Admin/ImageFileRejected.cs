namespace Trainfree.Admin.Admin;

/// <summary>The file was refused; carries a message suitable for showing on the row.</summary>
internal sealed record ImageFileRejected(string Error) : ImageFileValidationOutcome;

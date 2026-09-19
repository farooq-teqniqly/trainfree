using Trainfree.Domain.Users;

namespace Trainfree.Admin.Admin;

/// <summary>
/// The signed-in caller, as reported by <c>GET api/me</c>. Public because
/// <see cref="Components.AccessGate"/> cascades it to components, and a component
/// parameter's type must be at least as accessible as the component.
/// </summary>
public sealed record CurrentUser
{
    /// <summary>Initializes a new instance of the <see cref="CurrentUser"/> record.</summary>
    /// <param name="email">The caller's email address.</param>
    /// <param name="role">The caller's role name, as displayed to the user.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="role"/> is null, empty, or whitespace.</exception>
    public CurrentUser(EmailAddress email, string role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(role);

        Email = email;
        Role = role;
    }

    /// <summary>Gets the caller's email address.</summary>
    public EmailAddress Email { get; }

    /// <summary>Gets the caller's role name.</summary>
    public string Role { get; }
}

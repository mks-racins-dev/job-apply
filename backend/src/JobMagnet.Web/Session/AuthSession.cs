using System.Security.Cryptography;

using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace JobMagnet.Web.Session;

// One instance per Blazor circuit. The token is kept in encrypted browser storage so a reload stays signed in.
public class AuthSession(ProtectedLocalStorage storage)
{
    private const string StorageKey = "jobmagnet.session";

    private record StoredSession(string Token, string Email);

    public string? Token { get; private set; }

    public string? Email { get; private set; }

    // Not persisted: MainLayout reads it from the API after the session loads.
    public int AvatarId { get; private set; }

    public bool IsSignedIn => Token is not null;

    public event Action? Changed;

    public async Task LoadAsync()
    {
        try
        {
            var stored = await storage.GetAsync<StoredSession>(StorageKey);
            if (stored is { Success: true, Value: { } session })
            {
                Token = session.Token;
                Email = session.Email;
            }
        }
        catch (CryptographicException)
        {
            // Data protection keys rotated (e.g. container rebuilt): treat as signed out.
            await storage.DeleteAsync(StorageKey);
        }
    }

    public async Task SignInAsync(string token, string email)
    {
        Token = token;
        Email = email;
        await storage.SetAsync(StorageKey, new StoredSession(token, email));
        Changed?.Invoke();
    }

    public void SetAvatar(int avatarId)
    {
        AvatarId = avatarId;
        Changed?.Invoke();
    }

    public async Task SignOutAsync()
    {
        Token = null;
        Email = null;
        AvatarId = 0;
        await storage.DeleteAsync(StorageKey);
        Changed?.Invoke();
    }
}

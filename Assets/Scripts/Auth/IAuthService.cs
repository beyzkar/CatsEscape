using System;
using System.Threading.Tasks;

namespace CatsEscape.Auth
{
    public interface IAuthService
    {
        bool IsLoggedIn { get; }
        bool IsInitialized { get; }
        string ErrorMessage { get; }
        string UserDisplayName { get; }
        string UserEmail { get; }
        string UserId { get; }
        string UserPhotoUrl { get; }

        Task InitAsync(string webClientId);
        Task<bool> SignInWithGoogleAsync();
        Task<bool> SignInAnonymouslyAsync();
        void SignOut();
        Task<bool> TrySilentLoginAsync();
        Task<string> GetIdTokenAsync(bool forceRefresh = false);
    }
}

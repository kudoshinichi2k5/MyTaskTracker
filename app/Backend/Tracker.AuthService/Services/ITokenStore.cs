using Tracker.AuthService.Models;

namespace Tracker.AuthService.Services;

public interface ITokenStore
{
    Task StoreAccessTokenAsync(string rawToken, TokenInfo info, TimeSpan timeToLive);
    Task<TokenInfo?> GetAccessTokenAsync(string rawToken);
    Task RevokeAccessTokenAsync(string rawToken);

    Task StoreRefreshSessionAsync(string rawRefreshToken, RefreshSession session, TimeSpan timeToLive);
    Task<RefreshSession?> GetRefreshSessionAsync(string rawRefreshToken);
    Task<RefreshSession?> ConsumeRefreshSessionAsync(string rawRefreshToken);
    Task RevokeRefreshSessionAsync(string rawRefreshToken);

    Task RevokeAccessTokensForRefreshTokenAsync(string username, string rawRefreshToken);
    Task RevokeAllSessionsForUserAsync(string userId, string username);
}

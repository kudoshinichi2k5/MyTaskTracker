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

    Task StoreAuthorizationRequestAsync(string transactionId, AuthorizationRequest request, TimeSpan timeToLive);
    Task<AuthorizationRequest?> GetAuthorizationRequestAsync(string transactionId);
    Task<AuthorizationRequest?> ConsumeAuthorizationRequestAsync(string transactionId);

    Task StoreAuthorizationCodeAsync(string code, AuthorizationCode authorizationCode, TimeSpan timeToLive);
    Task<AuthorizationCode?> ConsumeAuthorizationCodeAsync(string code);
}

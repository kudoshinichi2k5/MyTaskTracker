using System.Collections.Concurrent;
using Tracker.AuthService.Models;
using Tracker.AuthService.Services;

namespace Tracker.AuthService.Tests;

public sealed class InMemoryTokenStore : ITokenStore
{
    private readonly ConcurrentDictionary<string, TokenInfo> _accessTokens = new();
    private readonly ConcurrentDictionary<string, RefreshSession> _refreshSessions = new();

    public Task StoreAccessTokenAsync(string rawToken, TokenInfo info, TimeSpan timeToLive)
    {
        _accessTokens[rawToken] = info;
        return Task.CompletedTask;
    }

    public Task<TokenInfo?> GetAccessTokenAsync(string rawToken) =>
        Task.FromResult(_accessTokens.TryGetValue(rawToken, out var info) ? info : null);

    public Task RevokeAccessTokenAsync(string rawToken)
    {
        _accessTokens.TryRemove(rawToken, out _);
        return Task.CompletedTask;
    }

    public Task StoreRefreshSessionAsync(string rawRefreshToken, RefreshSession session, TimeSpan timeToLive)
    {
        _refreshSessions[rawRefreshToken] = session;
        return Task.CompletedTask;
    }

    public Task<RefreshSession?> GetRefreshSessionAsync(string rawRefreshToken) =>
        Task.FromResult(_refreshSessions.TryGetValue(rawRefreshToken, out var session) ? session : null);

    public Task<RefreshSession?> ConsumeRefreshSessionAsync(string rawRefreshToken) =>
        Task.FromResult(_refreshSessions.TryRemove(rawRefreshToken, out var session) ? session : null);

    public Task RevokeRefreshSessionAsync(string rawRefreshToken)
    {
        _refreshSessions.TryRemove(rawRefreshToken, out _);
        return Task.CompletedTask;
    }

    public Task RevokeAccessTokensForRefreshTokenAsync(string username, string rawRefreshToken)
    {
        var refreshTokenHash = TokenHashing.Compute(rawRefreshToken);
        foreach (var (accessToken, info) in _accessTokens)
        {
            if (info.Username == username && info.RefreshTokenHash == refreshTokenHash)
            {
                _accessTokens.TryRemove(accessToken, out _);
            }
        }

        return Task.CompletedTask;
    }

    public Task RevokeAllSessionsForUserAsync(string userId, string username)
    {
        foreach (var (refreshToken, session) in _refreshSessions)
        {
            if (session.UserId == userId)
            {
                _refreshSessions.TryRemove(refreshToken, out _);
            }
        }

        foreach (var (accessToken, info) in _accessTokens)
        {
            if (info.Username == username)
            {
                _accessTokens.TryRemove(accessToken, out _);
            }
        }

        return Task.CompletedTask;
    }
}

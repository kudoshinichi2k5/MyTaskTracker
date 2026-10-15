using System.Text.Json;
using StackExchange.Redis;
using Tracker.AuthService.Models;

namespace Tracker.AuthService.Services;

public class ValkeyTokenStore : ITokenStore
{
    private readonly IDatabase _db;

    public ValkeyTokenStore(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task StoreAccessTokenAsync(string rawToken, TokenInfo info, TimeSpan timeToLive)
    {
        var hash = TokenHashing.Compute(rawToken);
        var json = JsonSerializer.Serialize(info);
        await _db.StringSetAsync($"access:{hash}", json, timeToLive);
        await _db.SetAddAsync($"user:{info.Username}:access", hash);
        await _db.KeyExpireAsync($"user:{info.Username}:access", timeToLive);
    }

    public async Task<TokenInfo?> GetAccessTokenAsync(string rawToken)
    {
        var hash = TokenHashing.Compute(rawToken);
        var val = await _db.StringGetAsync($"access:{hash}");
        return val.HasValue ? JsonSerializer.Deserialize<TokenInfo>(val!) : null;
    }

    public async Task RevokeAccessTokenAsync(string rawToken)
    {
        var hash = TokenHashing.Compute(rawToken);
        var info = await GetAccessTokenAsync(rawToken);
        if (info != null)
        {
            await _db.KeyDeleteAsync($"access:{hash}");
            await _db.SetRemoveAsync($"user:{info.Username}:access", hash);
        }
    }

    public async Task StoreRefreshSessionAsync(string rawRefreshToken, RefreshSession session, TimeSpan timeToLive)
    {
        var hash = TokenHashing.Compute(rawRefreshToken);
        var json = JsonSerializer.Serialize(session);
        await _db.StringSetAsync($"refresh:{hash}", json, timeToLive);
        await _db.SetAddAsync($"user:{session.UserId}:refresh", hash);
        await _db.KeyExpireAsync($"user:{session.UserId}:refresh", timeToLive);
    }

    public async Task<RefreshSession?> GetRefreshSessionAsync(string rawRefreshToken)
    {
        var hash = TokenHashing.Compute(rawRefreshToken);
        var val = await _db.StringGetAsync($"refresh:{hash}");
        return val.HasValue ? JsonSerializer.Deserialize<RefreshSession>(val!) : null;
    }

    public async Task RevokeRefreshSessionAsync(string rawRefreshToken)
    {
        var hash = TokenHashing.Compute(rawRefreshToken);
        var session = await GetRefreshSessionAsync(rawRefreshToken);
        if (session != null)
        {
            await _db.KeyDeleteAsync($"refresh:{hash}");
            await _db.SetRemoveAsync($"user:{session.UserId}:refresh", hash);
        }
    }

    public async Task<RefreshSession?> ConsumeRefreshSessionAsync(string rawRefreshToken)
    {
        var hash = TokenHashing.Compute(rawRefreshToken);
        var value = await _db.StringGetDeleteAsync($"refresh:{hash}");

        if (!value.HasValue)
        {
            return null;
        }

        var session = JsonSerializer.Deserialize<RefreshSession>(value!);
        if (session is not null)
        {
            await _db.SetRemoveAsync($"user:{session.UserId}:refresh", hash);
        }

        return session;
    }

    public async Task RevokeAccessTokensForRefreshTokenAsync(string username, string rawRefreshToken)
    {
        var refreshTokenHash = TokenHashing.Compute(rawRefreshToken);
        var accessKey = $"user:{username}:access";
        var accessTokens = await _db.SetMembersAsync(accessKey);

        foreach (var accessTokenHash in accessTokens)
        {
            var tokenKey = $"access:{accessTokenHash}";
            var value = await _db.StringGetAsync(tokenKey);

            if (!value.HasValue)
            {
                await _db.SetRemoveAsync(accessKey, accessTokenHash);
                continue;
            }

            var info = JsonSerializer.Deserialize<TokenInfo>(value!);
            if (info?.RefreshTokenHash == refreshTokenHash)
            {
                await _db.KeyDeleteAsync(tokenKey);
                await _db.SetRemoveAsync(accessKey, accessTokenHash);
            }
        }
    }

    public async Task RevokeAllSessionsForUserAsync(string userId, string username)
    {
        var refreshTokens = await _db.SetMembersAsync($"user:{userId}:refresh");
        foreach (var rt in refreshTokens)
            await _db.KeyDeleteAsync($"refresh:{rt}");
        await _db.KeyDeleteAsync($"user:{userId}:refresh");

        var accessTokens = await _db.SetMembersAsync($"user:{username}:access");
        foreach (var at in accessTokens)
            await _db.KeyDeleteAsync($"access:{at}");
        await _db.KeyDeleteAsync($"user:{username}:access");
    }
}

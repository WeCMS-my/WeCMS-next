using System.Collections.Concurrent;
using System.Security.Cryptography;
using WeCms.Shared.Security;
using WeCms.Shared.Time;

namespace WeCms.Infrastructure.Security;

public sealed class InMemoryTwoFactorLoginService : ITwoFactorLoginService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);
    private readonly ConcurrentDictionary<string, TwoFactorEntry> _entries = new(StringComparer.Ordinal);
    private readonly IClock _clock;
    private readonly ITokenGenerator _tokenGenerator;

    public InMemoryTwoFactorLoginService(IClock clock, ITokenGenerator tokenGenerator)
    {
        _clock = clock;
        _tokenGenerator = tokenGenerator;
    }

    public Task<TwoFactorLoginChallenge> CreateChallengeAsync(
        long userId,
        string username,
        CancellationToken cancellationToken)
    {
        CleanupExpired();

        var challengeId = _tokenGenerator.GenerateRefreshToken();
        var code = RandomNumberGenerator.GetInt32(100000, 1000000)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
        _entries[challengeId] = new TwoFactorEntry(userId, code, _clock.UtcNow.Add(Lifetime));

        return Task.FromResult(new TwoFactorLoginChallenge(challengeId, "one_time_code", (int)Lifetime.TotalSeconds));
    }

    public Task<TwoFactorLoginVerification> VerifyChallengeAsync(
        string challengeId,
        string code,
        CancellationToken cancellationToken)
    {
        if (!_entries.TryRemove(challengeId, out var entry))
        {
            return Task.FromResult(new TwoFactorLoginVerification(false, 0));
        }

        if (entry.ExpiresAt <= _clock.UtcNow)
        {
            return Task.FromResult(new TwoFactorLoginVerification(false, 0));
        }

        var valid = string.Equals(entry.Code, code.Trim(), StringComparison.Ordinal);
        return Task.FromResult(new TwoFactorLoginVerification(valid, valid ? entry.UserId : 0));
    }

    private void CleanupExpired()
    {
        var now = _clock.UtcNow;
        foreach (var item in _entries)
        {
            if (item.Value.ExpiresAt <= now)
            {
                _entries.TryRemove(item.Key, out _);
            }
        }
    }

    private sealed record TwoFactorEntry(long UserId, string Code, DateTimeOffset ExpiresAt);
}

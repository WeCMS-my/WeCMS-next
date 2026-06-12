using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using WeCms.Shared.Security;
using WeCms.Shared.Time;

namespace WeCms.Infrastructure.Security;

public sealed class InMemoryCaptchaService : ICaptchaService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);
    private readonly ConcurrentDictionary<string, CaptchaEntry> _entries = new(StringComparer.Ordinal);
    private readonly IClock _clock;
    private readonly ITokenGenerator _tokenGenerator;

    public InMemoryCaptchaService(IClock clock, ITokenGenerator tokenGenerator)
    {
        _clock = clock;
        _tokenGenerator = tokenGenerator;
    }

    public Task<CaptchaChallenge> CreateChallengeAsync(CancellationToken cancellationToken)
    {
        CleanupExpired();

        var left = RandomNumberGenerator.GetInt32(1, 10);
        var right = RandomNumberGenerator.GetInt32(1, 10);
        var answer = (left + right).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var challengeId = _tokenGenerator.GenerateRefreshToken();
        var expiresAt = _clock.UtcNow.Add(Lifetime);

        _entries[challengeId] = new CaptchaEntry(answer, expiresAt);

        var imageData = CreateSvgDataUri($"{left} + {right} = ?");
        return Task.FromResult(new CaptchaChallenge(challengeId, imageData, (int)Lifetime.TotalSeconds));
    }

    public Task<bool> VerifyAsync(
        string challengeId,
        string code,
        CancellationToken cancellationToken)
    {
        if (!_entries.TryRemove(challengeId, out var entry))
        {
            return Task.FromResult(false);
        }

        if (entry.ExpiresAt <= _clock.UtcNow)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(string.Equals(
            entry.Code,
            code.Trim(),
            StringComparison.OrdinalIgnoreCase));
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

    private static string CreateSvgDataUri(string text)
    {
        var escaped = WebUtility.HtmlEncode(text);
        var svg = $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="160" height="48" viewBox="0 0 160 48">
              <rect width="160" height="48" fill="#f8fafc"/>
              <path d="M8 34 C36 10 72 42 152 14" stroke="#94a3b8" stroke-width="2" fill="none"/>
              <text x="80" y="30" text-anchor="middle" font-family="Arial, sans-serif" font-size="20" fill="#0f172a">{escaped}</text>
            </svg>
            """;
        return "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));
    }

    private sealed record CaptchaEntry(string Code, DateTimeOffset ExpiresAt);
}

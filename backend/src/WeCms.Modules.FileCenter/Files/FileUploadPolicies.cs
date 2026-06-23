using WeCms.Shared;

namespace WeCms.Modules.FileCenter.Files;

public interface IFileUploadPolicy
{
    string Code { get; }
    IReadOnlySet<string> AllowedExtensions { get; }
    IReadOnlySet<string> AllowedMimeTypes { get; }
    long MaxSizeBytes { get; }
    bool RequireImageSignatureValidation { get; }
    bool ReencodeImage { get; }
    bool AllowPreview { get; }
    string StorageScope { get; }
    Task ValidateContentAsync(Stream content, long sizeBytes, string declaredMimeType, string fileExt, CancellationToken cancellationToken);
}

public interface IFileUploadPolicyResolver
{
    IFileUploadPolicy Resolve(string? policyCode);
}

public sealed class AvatarUploadPolicy : FileUploadPolicyBase
{
    public AvatarUploadPolicy()
        : base("avatar", [".jpg", ".jpeg", ".png", ".webp"], ["image/jpeg", "image/png", "image/webp"], 512 * 1024, true, false, true, "avatars")
    {
    }
}

public sealed class ImageUploadPolicy : FileUploadPolicyBase
{
    public ImageUploadPolicy()
        : base("image", [".jpg", ".jpeg", ".png", ".webp"], ["image/jpeg", "image/png", "image/webp"], 10 * 1024 * 1024, true, false, true, "images")
    {
    }
}

public sealed class DocumentUploadPolicy : FileUploadPolicyBase
{
    public DocumentUploadPolicy()
        : base("document", [".pdf", ".txt"], ["application/pdf", "text/plain"], 10 * 1024 * 1024, false, false, false, "documents")
    {
    }
}

public sealed class FileUploadPolicyResolver : IFileUploadPolicyResolver
{
    private readonly IReadOnlyDictionary<string, IFileUploadPolicy> _policies;

    public FileUploadPolicyResolver(IEnumerable<IFileUploadPolicy> policies)
    {
        _policies = policies.ToDictionary(policy => policy.Code, StringComparer.OrdinalIgnoreCase);
    }

    public IFileUploadPolicy Resolve(string? policyCode)
    {
        var normalized = string.IsNullOrWhiteSpace(policyCode) ? "document" : policyCode.Trim();
        return _policies.TryGetValue(normalized, out var policy)
            ? policy
            : throw new DomainException(ApiCodes.ValidationError, "file upload policy is not defined.");
    }
}

public abstract class FileUploadPolicyBase : IFileUploadPolicy
{
    protected FileUploadPolicyBase(
        string code,
        string[] allowedExtensions,
        string[] allowedMimeTypes,
        long maxSizeBytes,
        bool requireImageSignatureValidation,
        bool reencodeImage,
        bool allowPreview,
        string storageScope)
    {
        Code = code;
        AllowedExtensions = allowedExtensions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        AllowedMimeTypes = allowedMimeTypes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        MaxSizeBytes = maxSizeBytes;
        RequireImageSignatureValidation = requireImageSignatureValidation;
        ReencodeImage = reencodeImage;
        AllowPreview = allowPreview;
        StorageScope = storageScope;
    }

    public string Code { get; }
    public IReadOnlySet<string> AllowedExtensions { get; }
    public IReadOnlySet<string> AllowedMimeTypes { get; }
    public long MaxSizeBytes { get; }
    public bool RequireImageSignatureValidation { get; }
    public bool ReencodeImage { get; }
    public bool AllowPreview { get; }
    public string StorageScope { get; }

    public async Task ValidateContentAsync(Stream content, long sizeBytes, string declaredMimeType, string fileExt, CancellationToken cancellationToken)
    {
        if (content is null || sizeBytes <= 0)
        {
            throw Validation("file is required and must not be empty.");
        }

        if (sizeBytes > MaxSizeBytes)
        {
            throw Validation($"sizeBytes must be between 1 and {MaxSizeBytes}.");
        }

        if (!AllowedMimeTypes.Contains(declaredMimeType))
        {
            throw Validation("mimeType is not allowed for the selected upload policy.");
        }

        if (string.IsNullOrWhiteSpace(fileExt) || !AllowedExtensions.Contains(fileExt))
        {
            throw Validation("file extension is not allowed for the selected upload policy.");
        }

        if (RequireImageSignatureValidation)
        {
            await ValidateImageStructureAsync(content, declaredMimeType, cancellationToken);
        }
    }

    private static async Task ValidateImageStructureAsync(Stream content, string declaredMimeType, CancellationToken cancellationToken)
    {
        if (!content.CanSeek)
        {
            throw Validation("file content stream must be seekable.");
        }

        var valid = declaredMimeType.ToLowerInvariant() switch
        {
            "image/png" => await IsPngAsync(content, cancellationToken),
            "image/jpeg" => await IsJpegAsync(content, cancellationToken),
            "image/webp" => await IsWebpAsync(content, cancellationToken),
            _ => false
        };
        content.Position = 0;

        if (!valid)
        {
            throw Validation("image content is invalid.");
        }
    }

    private static async Task<bool> IsPngAsync(Stream content, CancellationToken cancellationToken)
    {
        if (content.Length < 12)
        {
            return false;
        }

        var head = await ReadAtAsync(content, 0, 8, cancellationToken);
        var tail = await ReadAtAsync(content, content.Length - 12, 8, cancellationToken);
        return head.Count == 8 &&
               tail.Count == 8 &&
               head.Buffer[0] == 0x89 &&
               head.Buffer[1] == 0x50 &&
               head.Buffer[2] == 0x4E &&
               head.Buffer[3] == 0x47 &&
               head.Buffer[4] == 0x0D &&
               head.Buffer[5] == 0x0A &&
               head.Buffer[6] == 0x1A &&
               head.Buffer[7] == 0x0A &&
               tail.Buffer[0] == 0x00 &&
               tail.Buffer[1] == 0x00 &&
               tail.Buffer[2] == 0x00 &&
               tail.Buffer[3] == 0x00 &&
               tail.Buffer[4] == 0x49 &&
               tail.Buffer[5] == 0x45 &&
               tail.Buffer[6] == 0x4E &&
               tail.Buffer[7] == 0x44;
    }

    private static async Task<bool> IsJpegAsync(Stream content, CancellationToken cancellationToken)
    {
        if (content.Length < 4)
        {
            return false;
        }

        var head = await ReadAtAsync(content, 0, 2, cancellationToken);
        var tail = await ReadAtAsync(content, content.Length - 2, 2, cancellationToken);
        return head.Count == 2 &&
               tail.Count == 2 &&
               head.Buffer[0] == 0xFF &&
               head.Buffer[1] == 0xD8 &&
               tail.Buffer[0] == 0xFF &&
               tail.Buffer[1] == 0xD9;
    }

    private static async Task<bool> IsWebpAsync(Stream content, CancellationToken cancellationToken)
    {
        if (content.Length < 12)
        {
            return false;
        }

        var head = await ReadAtAsync(content, 0, 12, cancellationToken);
        return head.Count == 12 &&
               head.Buffer[0] == 0x52 &&
               head.Buffer[1] == 0x49 &&
               head.Buffer[2] == 0x46 &&
               head.Buffer[3] == 0x46 &&
               head.Buffer[8] == 0x57 &&
               head.Buffer[9] == 0x45 &&
               head.Buffer[10] == 0x42 &&
               head.Buffer[11] == 0x50;
    }

    private static async Task<(byte[] Buffer, int Count)> ReadAtAsync(Stream content, long offset, int count, CancellationToken cancellationToken)
    {
        content.Position = offset;
        var buffer = new byte[count];
        var total = 0;
        while (total < count)
        {
            var read = await content.ReadAsync(buffer.AsMemory(total, count - total), cancellationToken);
            if (read == 0)
            {
                break;
            }

            total += read;
        }

        return (buffer, total);
    }

    private static DomainException Validation(string message) => new(ApiCodes.ValidationError, message);
}

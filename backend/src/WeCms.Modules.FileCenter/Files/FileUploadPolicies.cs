using Microsoft.AspNetCore.Http;
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
    Task ValidateContentAsync(IFormFile file, string declaredMimeType, string fileExt, CancellationToken cancellationToken);
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

    public async Task ValidateContentAsync(IFormFile file, string declaredMimeType, string fileExt, CancellationToken cancellationToken)
    {
        if (file is null || file.Length <= 0)
        {
            throw Validation("file is required and must not be empty.");
        }

        if (file.Length > MaxSizeBytes)
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
            await ValidateImageStructureAsync(file, declaredMimeType, cancellationToken);
        }
    }

    private static async Task ValidateImageStructureAsync(IFormFile file, string declaredMimeType, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        await using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);
        var bytes = memory.ToArray();
        var valid = declaredMimeType.ToLowerInvariant() switch
        {
            "image/png" => IsPng(bytes),
            "image/jpeg" => IsJpeg(bytes),
            "image/webp" => IsWebp(bytes),
            _ => false
        };

        if (!valid)
        {
            throw Validation("image content is invalid.");
        }
    }

    private static bool IsPng(byte[] bytes)
    {
        return bytes.Length >= 12 &&
               bytes[0] == 0x89 &&
               bytes[1] == 0x50 &&
               bytes[2] == 0x4E &&
               bytes[3] == 0x47 &&
               bytes[4] == 0x0D &&
               bytes[5] == 0x0A &&
               bytes[6] == 0x1A &&
               bytes[7] == 0x0A &&
               bytes[^12] == 0x00 &&
               bytes[^11] == 0x00 &&
               bytes[^10] == 0x00 &&
               bytes[^9] == 0x00 &&
               bytes[^8] == 0x49 &&
               bytes[^7] == 0x45 &&
               bytes[^6] == 0x4E &&
               bytes[^5] == 0x44;
    }

    private static bool IsJpeg(byte[] bytes)
    {
        return bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[^2] == 0xFF && bytes[^1] == 0xD9;
    }

    private static bool IsWebp(byte[] bytes)
    {
        return bytes.Length >= 12 &&
               bytes[0] == 0x52 &&
               bytes[1] == 0x49 &&
               bytes[2] == 0x46 &&
               bytes[3] == 0x46 &&
               bytes[8] == 0x57 &&
               bytes[9] == 0x45 &&
               bytes[10] == 0x42 &&
               bytes[11] == 0x50;
    }

    private static DomainException Validation(string message) => new(ApiCodes.ValidationError, message);
}

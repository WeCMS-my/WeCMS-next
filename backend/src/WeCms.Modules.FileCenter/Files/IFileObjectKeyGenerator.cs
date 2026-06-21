namespace WeCms.Modules.FileCenter.Files;

public interface IFileObjectKeyGenerator
{
    string GenerateObjectKey(DateTimeOffset now, string fileExt);
}

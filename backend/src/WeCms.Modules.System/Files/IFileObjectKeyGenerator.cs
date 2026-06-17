namespace WeCms.Modules.System.Files;

public interface IFileObjectKeyGenerator
{
    string GenerateObjectKey(DateTimeOffset now, string fileExt);
}

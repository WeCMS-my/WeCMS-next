using System.Data.Common;

namespace WeCms.Shared.Data;

/// <summary>
/// MySQL 连接工厂抽象。
/// </summary>
public interface IDbConnectionFactory
{
    Task<DbConnection> OpenAsync(CancellationToken cancellationToken = default);
}


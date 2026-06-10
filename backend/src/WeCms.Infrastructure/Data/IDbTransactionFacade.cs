using System.Data.Common;

namespace WeCms.Infrastructure.Data;

public interface IDbTransactionFacade
{
    DbConnection Connection { get; }
    DbTransaction? Inner { get; }
}

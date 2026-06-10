using System.Data.Common;

namespace WeCms.Shared.Data;

public interface IDbTransactionFacade
{
    DbConnection Connection { get; }
    DbTransaction? Inner { get; }
}


using System.Data.Common;
using WeCms.Shared.Data;

namespace WeCms.Persistence.Data;

public sealed record DbTransactionFacade(DbConnection Connection, DbTransaction? Inner) : IDbTransactionFacade;

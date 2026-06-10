using System.Data.Common;

namespace WeCms.Infrastructure.Data;

public sealed record DbTransactionFacade(DbConnection Connection, DbTransaction? Inner) : IDbTransactionFacade;

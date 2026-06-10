using System.Data.Common;
using WeCms.Shared.Data;

namespace WeCms.Infrastructure.Data;

public sealed record DbTransactionFacade(DbConnection Connection, DbTransaction? Inner) : IDbTransactionFacade;

 using System.Data.Common;
 
 namespace WeCms.Shared.Contracts;
 
 public interface IDbConnectionFactory
 {
     Task<DbConnection> OpenAsync(CancellationToken cancellationToken);
 }

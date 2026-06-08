 namespace WeCms.Api.Filters;
 
 public static class PermissionEndpointExtensions
 {
     public static RouteHandlerBuilder RequirePermission(
         this RouteHandlerBuilder builder,
         string permissionCode)
     {
         return builder
             .RequireAuthorization()
             .WithMetadata(new PermissionMetadata(permissionCode))
             .AddEndpointFilter<PermissionEndpointFilter>();
     }
 }

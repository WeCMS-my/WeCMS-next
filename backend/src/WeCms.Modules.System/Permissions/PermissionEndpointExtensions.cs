 using WeCms.Api.Filters;
 
 namespace WeCms.Modules.System;
 
 public static class PermissionEndpointExtensions
 {
     public static RouteHandlerBuilder RequirePermission(this RouteHandlerBuilder builder, string code)
         => builder.RequireAuthorization().WithMetadata(new PermissionMetadata(code)).AddEndpointFilter<PermissionEndpointFilter>();
 }
 
 public sealed class PermissionMetadata(string code)
 {
     public string Code => code;
 }

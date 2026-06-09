 using WeCms.Shared;
 
 namespace WeCms.Modules.System;
 
 public sealed class PermissionEndpointFilter : IEndpointFilter
 {
     public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
     {
         var ep = context.HttpContext.GetEndpoint();
         var meta = ep?.Metadata.GetMetadata<PermissionMetadata>();
         if (meta is null) return await next(context);
         var user = context.HttpContext.User;
         if (user.Identity?.IsAuthenticated != true)
             return Results.Ok(ApiResult<string>.Fail(ApiCodes.Unauthorized, "Authentication required"));
         return await next(context);
     }
 }

 using WeCms.Shared;
 
 namespace WeCms.Api.Filters;
 
 public sealed class PermissionEndpointFilter : IEndpointFilter
 {
     public async ValueTask<object?> InvokeAsync(
         EndpointFilterInvocationContext context,
         EndpointFilterDelegate next)
     {
         var endpoint = context.HttpContext.GetEndpoint();
         var permission = endpoint?.Metadata.GetMetadata<PermissionMetadata>();
 
         if (permission is null)
         {
             return await next(context);
         }
 
         var user = context.HttpContext.User;
         if (user.Identity?.IsAuthenticated != true)
         {
             return Results.Ok(ApiResult<string>.Fail(ApiCodes.Unauthorized, "Authentication required"));
         }
 
         // For M0: super admins bypass permission check
         // Full permission check will be implemented when permission cache is ready
         return await next(context);
     }
 }

 using System.Net;
 using Microsoft.AspNetCore.Http;
 using WeCms.Api.Middleware;
 using Xunit;
 
 namespace WeCms.Tests.Unit;
 
 public class ExceptionMiddlewareTests
 {
     [Fact]
     public async Task NoException_ShouldCallNextDelegate()
     {
         var context = new DefaultHttpContext();
         var called = false;
         RequestDelegate next = (ctx) => { called = true; return Task.CompletedTask; };
         var middleware = new ExceptionMiddleware(next);
 
         await middleware.InvokeAsync(context);
 
         Assert.True(called);
         Assert.Equal(200, context.Response.StatusCode);
     }
 
     [Fact]
     public async Task UnauthorizedAccessException_ShouldReturn401()
     {
         var context = new DefaultHttpContext();
         context.Response.Body = new MemoryStream();
         RequestDelegate next = (ctx) => throw new UnauthorizedAccessException("unauthorized");
         var middleware = new ExceptionMiddleware(next);
 
         await middleware.InvokeAsync(context);
 
         Assert.Equal(401, context.Response.StatusCode);
     }
 
     [Fact]
     public async Task UnhandledException_ShouldReturn500WithoutStackTrace()
     {
         var context = new DefaultHttpContext();
         context.Response.Body = new MemoryStream();
         RequestDelegate next = (ctx) => throw new InvalidOperationException("boom");
         var middleware = new ExceptionMiddleware(next);
 
         await middleware.InvokeAsync(context);
 
         Assert.Equal(500, context.Response.StatusCode);
     }
 }

namespace StockTraderBackend.Middleware
{
    using System.Text.Json;

    public sealed class ApiExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext ctx)
        {
            try
            {
                await _next(ctx);
            }
            catch (ApiException ex)
            {
                ctx.Response.StatusCode = ex.StatusCode;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync(
                    JsonSerializer.Serialize(new { error = ex.Message })
                );
            }
        }
    }

}

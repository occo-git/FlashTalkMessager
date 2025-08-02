namespace GatewayApi.Middleware
{
    public class ApiExceptionHandler(
        RequestDelegate next,
        ILogger<ApiExceptionHandler> logger)
    {

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unhandled exception occurred.");

                if (!context.Response.HasStarted)
                {
                    context.Response.Clear();
                    context.Response.ContentType = "application/json";

                    context.Response.StatusCode = ex switch
                    {
                        ApplicationException => StatusCodes.Status400BadRequest,
                        UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                        KeyNotFoundException => StatusCodes.Status404NotFound,
                        OperationCanceledException => StatusCodes.Status408RequestTimeout,
                        _ => StatusCodes.Status500InternalServerError
                    };

                    await context.Response.WriteAsJsonAsync(
                        new
                        {
                            Type = ex.GetType().Name,
                            Title = "An error occurred.",
                            Detail = ex.Message,
                            //StackTrace = ex.StackTrace
                        });
                }
            }
        }
    }
}

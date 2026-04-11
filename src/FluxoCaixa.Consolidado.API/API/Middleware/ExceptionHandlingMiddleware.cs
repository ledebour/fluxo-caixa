using System.Net;
using System.Text.Json;
using FluxoCaixa.Consolidado.API.Application.DTOs;

namespace FluxoCaixa.Consolidado.API.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (statusCode, mensagem) = ex switch
            {
                ArgumentException ae        => (HttpStatusCode.BadRequest, ae.Message),
                OperationCanceledException  => (HttpStatusCode.ServiceUnavailable, "Requisição cancelada."),
                _                           => (HttpStatusCode.InternalServerError, "Erro interno no servidor.")
            };

            if (statusCode == HttpStatusCode.InternalServerError)
                _logger.LogError(ex, "Erro não tratado: {Mensagem}", ex.Message);

            var resposta = new ErrorResponse
            {
                Mensagem = mensagem,
                Detalhe = context.RequestServices
                    .GetRequiredService<IHostEnvironment>().IsDevelopment() ? ex.ToString() : null
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;
            await context.Response.WriteAsync(JsonSerializer.Serialize(resposta,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        }
    }
}

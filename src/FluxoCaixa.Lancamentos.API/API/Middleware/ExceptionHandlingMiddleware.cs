using System.Net;
using System.Text.Json;
using FluxoCaixa.Lancamentos.API.Application.DTOs;
using FluxoCaixa.Lancamentos.API.Domain.Exceptions;

namespace FluxoCaixa.Lancamentos.API.API.Middleware;

/// <summary>
/// Intercepta todas as exceções não tratadas e retorna respostas HTTP padronizadas.
/// Evita vazar stack traces para o cliente em produção.
/// </summary>
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
            await TratarExcecaoAsync(context, ex);
        }
    }

    private async Task TratarExcecaoAsync(HttpContext context, Exception ex)
    {
        var (statusCode, mensagem) = ex switch
        {
            NotFoundException nfe       => (HttpStatusCode.NotFound, nfe.Message),
            DomainException de          => (HttpStatusCode.UnprocessableEntity, de.Message),
            ArgumentException ae        => (HttpStatusCode.BadRequest, ae.Message),
            OperationCanceledException  => (HttpStatusCode.ServiceUnavailable, "Requisição cancelada."),
            _                           => (HttpStatusCode.InternalServerError, "Ocorreu um erro interno. Tente novamente.")
        };

        // Loga erros inesperados com stack trace completo
        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(ex, "Erro não tratado: {Mensagem}", ex.Message);
        else
            _logger.LogWarning("Erro de negócio [{Status}]: {Mensagem}", (int)statusCode, ex.Message);

        var resposta = new ErrorResponse
        {
            Mensagem = mensagem,
            Detalhe = context.RequestServices
                .GetRequiredService<IHostEnvironment>().IsDevelopment()
                    ? ex.ToString()
                    : null
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(resposta, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}

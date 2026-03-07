namespace SauronSheet.Infrastructure.Monitoring;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sentry;
using SauronSheet.Application.Common;

/// <summary>
/// Pipeline behavior for MediatR that adds Sentry tracing (breadcrumbs and error capture) to all requests.
/// Automatically captures operation details and errors for observability.
/// </summary>
public class SentryTracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestTypeName = typeof(TRequest).Name;

        // Add breadcrumb with operation details
        var breadcrumbData = new Dictionary<string, string>
        {
            { "requestType", requestTypeName },
            { "timestamp", DateTime.UtcNow.ToString("o") }
        };

        // Add request-specific data if it's a transaction operation
        if (request is ITransactionRequest transactionRequest)
        {
            breadcrumbData["operationDetails"] = BuildTransactionDetails(transactionRequest);
        }

        SentrySdk.AddBreadcrumb(
            $"Executing {requestTypeName}",
            "request",
            data: breadcrumbData
        );

        try
        {
            var response = await next();
            
            SentrySdk.AddBreadcrumb(
                $"Completed {requestTypeName}",
                "request",
                level: BreadcrumbLevel.Info
            );

            return response;
        }
        catch (Exception ex)
        {
            // Capture the exception with command context
            SentrySdk.ConfigureScope(scope =>
            {
                scope.SetTag("requestType", requestTypeName);
            });

            // Add error details as breadcrumb before capturing
            SentrySdk.AddBreadcrumb(
                $"Error in {requestTypeName}: {ex.Message}",
                "error",
                level: BreadcrumbLevel.Error,
                data: breadcrumbData
            );

            SentrySdk.CaptureException(ex);
            throw;
        }
    }

    /// <summary>
    /// Builds transaction-specific details for breadcrumb from operation requests.
    /// </summary>
    private string BuildTransactionDetails(ITransactionRequest request)
    {
        try
        {
            // This method can be extended to handle different transaction operations
            // For now, it provides a basic toString representation
            return request.GetType().Name;
        }
        catch
        {
            return "unknown";
        }
    }
}

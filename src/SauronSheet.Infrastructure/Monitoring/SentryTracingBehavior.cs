namespace SauronSheet.Infrastructure.Monitoring;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sentry;
using Sentry.Extensibility;
using SauronSheet.Domain.Common;

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

        SentrySdk.Logger?.LogDebug("MediatR handler starting: {0}", requestTypeName);

        SentrySdk.AddBreadcrumb(
            $"Executing {requestTypeName}",
            "request",
            data: breadcrumbData
        );

        var sw = Stopwatch.StartNew();

        try
        {
            var response = await next();
            sw.Stop();

            SentrySdk.Logger?.LogInfo("MediatR handler completed in {0}ms: {1}", (int)sw.Elapsed.TotalMilliseconds, requestTypeName);

            SentrySdk.AddBreadcrumb(
                $"Completed {requestTypeName}",
                "request",
                level: BreadcrumbLevel.Info
            );

            // Metrics: count successful handler executions and record duration
            SentrySdk.Experimental.Metrics.EmitCounter("app.handler.success", 1.0,
                new KeyValuePair<string, object>[] { new("handler", requestTypeName) });
            SentrySdk.Experimental.Metrics.EmitDistribution("app.handler.duration_ms",
                sw.Elapsed.TotalMilliseconds,
                MeasurementUnit.Duration.Millisecond,
                new KeyValuePair<string, object>[] { new("handler", requestTypeName) });

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
            sw.Stop();

            SentrySdk.Logger?.LogError("MediatR handler failed: {0} — {1}", requestTypeName, ex.GetType().Name);

            SentrySdk.AddBreadcrumb(
                $"Error in {requestTypeName}: {ex.Message}",
                "error",
                level: BreadcrumbLevel.Error,
                data: breadcrumbData
            );

            // Metrics: count handler failures
            SentrySdk.Experimental.Metrics.EmitCounter("app.handler.error", 1.0,
                new KeyValuePair<string, object>[]
                {
                    new("handler", requestTypeName),
                    new("exception", ex.GetType().Name)
                });

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

using System.Net;

namespace SauronSheet.Frontend.Tests.Fixtures;

/// <summary>
/// HttpMessageHandler that returns canned responses for test scenarios.
/// Used by the web application factory to avoid real network calls.
/// </summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, HttpResponseMessage> _responses;

    public FakeHttpMessageHandler(Dictionary<string, HttpResponseMessage> responses)
    {
        _responses = responses;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        string key = request.RequestUri?.ToString() ?? string.Empty;

        foreach (KeyValuePair<string, HttpResponseMessage> response in _responses)
        {
            if (key.Contains(response.Key, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(Clone(response.Value));
            }
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    private static HttpResponseMessage Clone(HttpResponseMessage source)
    {
        HttpResponseMessage clone = new(source.StatusCode)
        {
            Content = source.Content
        };

        foreach (KeyValuePair<string, IEnumerable<string>> header in source.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }
}

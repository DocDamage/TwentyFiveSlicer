using System.Net.Http;
using System.Text.Json;
using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop.Services;

public sealed class CloudAiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly CloudAiSecretStore? _secretStore;

    public CloudAiClient()
        : this(new HttpClient { Timeout = TimeSpan.FromSeconds(45) }, ownsHttpClient: true, secretStore: null)
    {
    }

    public CloudAiClient(CloudAiSecretStore secretStore)
        : this(new HttpClient { Timeout = TimeSpan.FromSeconds(45) }, ownsHttpClient: true, secretStore)
    {
    }

    public CloudAiClient(HttpClient httpClient, CloudAiSecretStore? secretStore = null)
        : this(httpClient, ownsHttpClient: false, secretStore)
    {
    }

    private CloudAiClient(HttpClient httpClient, bool ownsHttpClient, CloudAiSecretStore? secretStore)
    {
        _httpClient = httpClient;
        _ownsHttpClient = ownsHttpClient;
        _secretStore = secretStore;
    }

    public async Task<CloudAiResult> AskAsync(
        CloudAiProviderDescriptor provider,
        CloudAiSettings settings,
        string prompt,
        CloudAiImageInput? image = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpRequestMessage request = CloudAiRequestBuilder.BuildAnalysisRequest(provider, settings, prompt, image, _secretStore);
            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return CloudAiResult.Failed($"Cloud AI request failed with HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {TrimForDisplay(body)}");
            }

            string advice = CloudAiResponseParser.Parse(provider, body).Trim();
            return string.IsNullOrWhiteSpace(advice)
                ? CloudAiResult.Failed("Cloud AI response did not contain advice text.")
                : CloudAiResult.Succeeded(advice);
        }
        catch (Exception exception) when (exception is InvalidOperationException or HttpRequestException or TaskCanceledException or JsonException)
        {
            return CloudAiResult.Failed(exception.Message);
        }
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private static string TrimForDisplay(string value)
    {
        const int maxLength = 300;
        if (string.IsNullOrWhiteSpace(value))
        {
            return "No response body.";
        }

        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }
}

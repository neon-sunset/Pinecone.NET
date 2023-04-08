using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;
using Pinecone.Transport;
using Pinecone.Transport.Rest;

namespace Pinecone;

public sealed class PineconeClient : IDisposable
{
    private readonly HttpClient Http;

    public PineconeClient(string apiKey, string environment)
    {
        Guard.IsNotNullOrWhiteSpace(apiKey);
        Guard.IsNotNullOrWhiteSpace(environment);

        Http = new() { BaseAddress = new Uri($"https://controller.{environment}.pinecone.io") };
        Http.DefaultRequestHeaders.Add("Api-Key", apiKey);
    }

    public PineconeClient(string apiKey, Uri baseUrl)
    {
        Guard.IsNotNullOrWhiteSpace(apiKey);
        Guard.IsNotNull(baseUrl);

        Http = new() { BaseAddress = baseUrl };
        Http.DefaultRequestHeaders.Add("Api-Key", apiKey);
    }

    public async Task<PineconeIndexName[]> ListIndexes()
    {
        var response = await Http.GetFromJsonAsync("/databases", SerializerContext.Default.StringArray);
        if (response is null or { Length: 0 })
        {
            return Array.Empty<PineconeIndexName>();
        }

        var indexes = new PineconeIndexName[response.Length];
        foreach (var i in 0..response.Length)
        {
            indexes[i] = new(response[i]);
        }

        return indexes;
    }

    public async Task CreateIndex(
        PineconeIndexDetails indexDetails,
        Dictionary<string, string[]>? metadataConfig = null,
        string? sourceCollection = null)
    {
        var request = CreateIndexRequest.From(indexDetails, metadataConfig, sourceCollection);
        var response = await Http.PostAsJsonAsync(
            "/databases", request, SerializerContext.Default.CreateIndexRequest);

        await CheckStatusCode(response);
    }

    public async Task<PineconeIndex<TTransport>> GetIndex<TTransport>(PineconeIndexName name)
        where TTransport : struct, ITransport<TTransport>
    {
        var response = await Http.GetFromJsonAsync(
            $"/databases/{name.Value}",
            typeof(PineconeIndex<TTransport>),
            SerializerContext.Default) ?? throw new HttpRequestException("GetIndex request has failed.");

        var index = (PineconeIndex<TTransport>)response;
        var host = index.Status.Host;
        var apiKey = Http.DefaultRequestHeaders.GetValues("Api-Key").First();

        index.Transport = TTransport.Create(host, apiKey);
        return index;
    }

    public async Task ConfigureIndex(PineconeIndexName name, int replicas, string podType)
    {
        var request = new ConfigureIndexRequest { Replicas = replicas, PodType = podType };
        var response = await Http.PatchAsJsonAsync(
            $"/databases/{name.Value}", request, SerializerContext.Default.ConfigureIndexRequest);

        await CheckStatusCode(response);
    }

    public async Task DeleteIndex(PineconeIndexName name) =>
        await CheckStatusCode(await Http.DeleteAsync($"/databases/{name.Value}"));

    public void Dispose() => Http.Dispose();

    private static ValueTask CheckStatusCode(HttpResponseMessage response, [CallerMemberName] string requestName = "")
    {
        return response.IsSuccessStatusCode ? ValueTask.CompletedTask : ThrowOnFailedResponse();

        async ValueTask ThrowOnFailedResponse()
        {
            throw new HttpRequestException($"{requestName} request has failed. Message: {await response.Content.ReadAsStringAsync()}");
        }
    }
}

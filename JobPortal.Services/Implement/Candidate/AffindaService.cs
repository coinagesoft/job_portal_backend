using JobPortal.Services.IImplement.ICandidate;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;

public class AffindaService : IAffindaService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public AffindaService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<string> ParseResumeAsync(IFormFile file)
    {
        try
        {
            var apiKey = _configuration["Affinda:ApiKey"];

            Console.WriteLine($"Affinda Key Found: {!string.IsNullOrEmpty(apiKey)}");

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            using var form = new MultipartFormDataContent();

            using var stream = file.OpenReadStream();

            form.Add(
                new StreamContent(stream),
                "file",
                file.FileName);

            var response =
                await _httpClient.PostAsync(
                    "https://api.affinda.com/v3/documents",
                    form);

            var responseBody =
                await response.Content.ReadAsStringAsync();

            Console.WriteLine("STATUS: " + response.StatusCode);
            Console.WriteLine("BODY: " + responseBody);

            return responseBody;
        }
        catch (Exception ex)
        {
            Console.WriteLine("AFFINDA ERROR:");
            Console.WriteLine(ex.ToString());

            throw;
        }
    }
}
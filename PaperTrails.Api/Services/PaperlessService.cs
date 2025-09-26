using PaperTrails.Api.DTOs.Paperless;

namespace PaperTrails.Api.Services
{
    public class PaperlessService
    {
        private readonly HttpClient _httpClient;
        private readonly string _paperlessUrl;
        private readonly string _token;

        public PaperlessService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _paperlessUrl = configuration["Paperless:Url"];
            _token = configuration["Paperless:ApiKey"];
        }

        public async Task<CreateCategoryResult> CreateCategory(string name)
        {
            var payload = new
            {
                name,
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_paperlessUrl}/document_types/");
            request.Content = JsonContent.Create( payload );
            request.Headers.Add( "Authorization", $"Token {_token}" );

            var response = await _httpClient.SendAsync( request );

            if(!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new CreateCategoryResult
                {
                    Error = errorContent,
                };
            }

            var category = await response.Content.ReadFromJsonAsync<CreateCategoryResult>();

            return new CreateCategoryResult
            {
                Id = category.Id,
                Name = category.Name,
            };
        }
    }
}

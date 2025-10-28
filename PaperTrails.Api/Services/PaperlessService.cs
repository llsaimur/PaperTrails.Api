using PaperTrails.Api.DTOs.Paperless;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;


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
            _token = configuration["Paperless:Token"];
        }


        public async Task<string> CreateDocument(
            Stream fileStream,
            string fileName,
            string? title = null,
            int? documentTypeId = null,
            string? description = null)
        {
            using var formData = new MultipartFormDataContent();

            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            formData.Add(fileContent, "document", fileName);

            StringContent titleValue = new StringContent(title ?? fileName);
            formData.Add(titleValue, "title");

            if (documentTypeId.HasValue)
            {
                StringContent documentTypeIdValue = new StringContent(documentTypeId.Value.ToString());
                formData.Add(documentTypeIdValue, "document_type");
            }

            if (!string.IsNullOrWhiteSpace(description))
            {
                StringContent descriptionValue = new StringContent(description);
                formData.Add(descriptionValue, "description");
            }

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_paperlessUrl}/documents/post_document/");
            request.Headers.Authorization = new AuthenticationHeaderValue("Token", _token);
            request.Content = formData;

            try
            {
                HttpResponseMessage response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Paperless upload failed: {err}");
                }

                return await response.Content.ReadFromJsonAsync<string>();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Network error while uploading document", ex);
            }
        }

        public async Task<UpdateDocumentResult> UpdateDocumentAsync(
            int paperlessDocumentId,
            Stream? newFileStream = null,     
            string? fileName = null,          
            string? title = null,
            int? documentTypeId = null)
        {
            string updatedTaskId = null;
            int? updatedPaperlessId = paperlessDocumentId;
            string status = "PENDING";

            try
            {

                if (newFileStream != null && !string.IsNullOrWhiteSpace(fileName))
                {
                    using var formData = new MultipartFormDataContent();

                    var fileContent = new StreamContent(newFileStream);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    formData.Add(fileContent, "document", fileName);

                    formData.Add(new StringContent(title ?? fileName), "title");

                    if (documentTypeId.HasValue)
                    {
                        formData.Add(new StringContent(documentTypeId.Value.ToString()), "document_type");
                    }

                    var request = new HttpRequestMessage(HttpMethod.Post, $"{_paperlessUrl}/documents/post_document/");
                    request.Headers.Authorization = new AuthenticationHeaderValue("Token", _token);
                    request.Content = formData;

                    HttpResponseMessage response = await _httpClient.SendAsync(request);
                    response.EnsureSuccessStatusCode();

                    updatedTaskId = await response.Content.ReadFromJsonAsync<string>();
                    updatedPaperlessId = -1;
                    status = "PENDING";
                }
                else if (documentTypeId.HasValue)
                {
                    var payload = new
                    {
                        documents = new[] { paperlessDocumentId }, // keep as int, not string
                        method = "set_document_type",
                        parameters = new { document_type = documentTypeId.Value }
                    };
                    var jsonString = JsonSerializer.Serialize(payload);
                    var request = new HttpRequestMessage(HttpMethod.Post, $"{_paperlessUrl}/documents/bulk_edit/");

                    request.Headers.Authorization = new AuthenticationHeaderValue("Token", _token);
                    request.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await _httpClient.SendAsync(request);
                    response.EnsureSuccessStatusCode();

                    status = "SUCCESS"; // Paperless updates status immediately for bulk edit
                }

                return new UpdateDocumentResult
                {
                    TaskId = updatedTaskId,
                    PaperlessDocumentId = updatedPaperlessId,
                    Status = status
                };
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Network error while updating document", ex);
            }
        }

        public async Task<PaperlessDocumentResponse?> GetDocumentAsync(int documentId)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_paperlessUrl}/documents/{documentId}/");
                request.Headers.Authorization = new AuthenticationHeaderValue("Token", _token);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var document = await response.Content.ReadFromJsonAsync<PaperlessDocumentResponse>();
                return document;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Network error while fetching document", ex);
            }
        }

        public async Task<IEnumerable<PaperlessDocumentResponse>> GetAllDocumentsAsync(IEnumerable<int> documentIds)
        {
            if (!documentIds.Any())
            {
                return Enumerable.Empty<PaperlessDocumentResponse>();
            }

            string idsParam = string.Join(",", documentIds);

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_paperlessUrl}/documents/?id__in={idsParam}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Token", _token);

                HttpResponseMessage response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                AllPaperlessDocumentsResponse result = await response.Content.ReadFromJsonAsync<AllPaperlessDocumentsResponse>();

                return result?.Results ?? Enumerable.Empty<PaperlessDocumentResponse>();
            }
            catch(HttpRequestException ex) 
            {
                throw new Exception("Network error while fetching documents", ex);
            }

            
        }

        public async Task<TaskStatusResponse?> GetDocumentStatusAsync(string taskId)
        {
            if (string.IsNullOrWhiteSpace(taskId))
            {
                throw new ArgumentException("taskId is required", nameof(taskId));
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_paperlessUrl}/tasks/?task_id={taskId}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Token", _token);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var taskList = await response.Content.ReadFromJsonAsync<List<TaskStatusResponse>>();

                var result = taskList?.FirstOrDefault();

                if (result == null)
                {
                    return null;
                }

                return result;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Network error while fetching document status", ex);
            }
        }


        public async Task DeleteDocumentAsync(int documentId)
        {
            if (documentId <= 0)
                throw new ArgumentException("Invalid documentId", nameof(documentId));

            var request = new HttpRequestMessage(HttpMethod.Delete, $"{_paperlessUrl}/documents/{documentId}/");
            request.Headers.Authorization = new AuthenticationHeaderValue("Token", _token);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to delete document in Paperless: {error}");
            }
        }


        public async Task<CreateCategoryResult> CreateCategory(string name)
        {
            var payload = new
            {
                name,
            };
            var jsonString = JsonSerializer.Serialize(payload);

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_paperlessUrl}/document_types/");
            request.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            request.Headers.Add("Authorization", $"Token {_token}");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
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

        public async Task<UpdateCategoryResult> UpdateCategory(string name, int id)
        {
            var payload = new
            {
                name,
            };
            var jsonString = JsonSerializer.Serialize(payload);

            var request = new HttpRequestMessage(HttpMethod.Put, $"{_paperlessUrl}/document_types/{id}/");
            request.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            request.Headers.Add("Authorization", $"Token {_token}");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new UpdateCategoryResult
                {
                    Error = errorContent,
                };
            }

            var category = await response.Content.ReadFromJsonAsync<UpdateCategoryResult>();

            return new UpdateCategoryResult
            {
                Name = category.Name,
            };
        }

        public async Task<Stream> GetDocumentPdfAsync(string contentUrl)
        {
            if (string.IsNullOrWhiteSpace(contentUrl))
                throw new ArgumentException("Content URL cannot be null or empty.", nameof(contentUrl));

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, contentUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Token", _token);

                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to fetch PDF from Paperless: {errorContent}");
                }

                return await response.Content.ReadAsStreamAsync();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Network error while fetching document PDF from Paperless.", ex);
            }
        }


        public async Task<DeleteCategoryResult> DeleteCategory(int id)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, $"{_paperlessUrl}/document_types/{id}/");
            request.Headers.Add("Authorization", $"Token {_token}");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new DeleteCategoryResult
                {
                    Success = false,
                    Error = errorContent,
                };
            }

            return new DeleteCategoryResult
            {
                Success = true
            };
        }
    }
}

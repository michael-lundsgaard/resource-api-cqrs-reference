using System.Net;
using System.Net.Http.Json;
using ResourceCatalog.Api.Features.Resources;

namespace ResourceCatalog.Tests.Features.Resources
{
    public class ResourceEndpointTests(ResourcesApiFactory factory)
        : IClassFixture<ResourcesApiFactory>
    {
        private readonly HttpClient _client = factory.CreateClient();

        [Fact]
        public async Task POST_creates_resource_and_GET_returns_it()
        {
            // Arrange
            var body = new CreateResourceRequest("My Resource", "A test description");

            // Act — Create
            var createResponse = await _client.PostAsJsonAsync("/api/v1/resources", body);
            createResponse.EnsureSuccessStatusCode();
            var created = await createResponse.Content.ReadFromJsonAsync<ResourceDto>();

            // Assert — Create
            Assert.NotNull(created);
            Assert.NotEqual(Guid.Empty, created.Id);
            Assert.Equal("My Resource", created.Name);

            // Act — Get
            var getResponse = await _client.GetAsync($"/api/v1/resources/{created.Id}");
            var fetched = await getResponse.Content.ReadFromJsonAsync<ResourceDto>();

            // Assert — Get
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.Equal(created.Id, fetched!.Id);
        }

        [Fact]
        public async Task POST_returns_validation_problem_for_empty_name()
        {
            var body = new CreateResourceRequest("", null);
            var response = await _client.PostAsJsonAsync("/api/v1/resources", body);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GET_returns_404_for_unknown_id()
        {
            var response = await _client.GetAsync($"/api/v1/resources/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PUT_updates_resource()
        {
            var created = await CreateResourceAsync("Original");
            var update = new UpdateResourceRequest("Updated", "New description");

            var response = await _client.PutAsJsonAsync($"/api/v1/resources/{created.Id}", update);
            var updated = await response.Content.ReadFromJsonAsync<ResourceDto>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Updated", updated!.Name);
        }

        [Fact]
        public async Task DELETE_removes_resource()
        {
            var created = await CreateResourceAsync("To Delete");

            var deleteResponse = await _client.DeleteAsync($"/api/v1/resources/{created.Id}");
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            var getResponse = await _client.GetAsync($"/api/v1/resources/{created.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        private async Task<ResourceDto> CreateResourceAsync(string name)
        {
            var response = await _client.PostAsJsonAsync("/api/v1/resources",
                new CreateResourceRequest(name, null));
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<ResourceDto>())!;
        }
    }
}
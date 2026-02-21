using System.Net;
using System.Net.Http.Json;
using ResourceCatalog.Api.Features.Resources;

namespace ResourceCatalog.Tests.Features.Resources
{
    public class ResourceEndpointTests(ResourcesApiFactory factory)
        : IClassFixture<ResourcesApiFactory>, IAsyncLifetime
    {
        private HttpClient _client = null!;

        public async Task InitializeAsync()
        {
            await factory.ResetDatabaseAsync(); // Reset database to clean state before each test
            _client = factory.CreateClient();   // Create a new HttpClient for each test to ensure isolation
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task POST_creates_resource_and_GET_returns_it()
        {
            // Arrange
            var body = new CreateResourceRequest("My Resource", "A test description", null);

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
            var body = new CreateResourceRequest("", null, null);
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
            var update = new UpdateResourceRequest("Updated", "New description", null);

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

        // Tag Tests - Happy Path

        [Fact]
        public async Task POST_creates_resource_with_tags()
        {
            var body = new CreateResourceRequest("My Resource", null, ["React", "Learning"]);

            var response = await _client.PostAsJsonAsync("/api/v1/resources", body);
            response.EnsureSuccessStatusCode();
            var created = await response.Content.ReadFromJsonAsync<ResourceDto>();

            Assert.NotNull(created);
            Assert.Null(created.Tags); // Tags not returned by default
        }

        [Fact]
        public async Task POST_creates_resource_without_tags()
        {
            var body = new CreateResourceRequest("My Resource", null, null);

            var response = await _client.PostAsJsonAsync("/api/v1/resources", body);
            response.EnsureSuccessStatusCode();
            var created = await response.Content.ReadFromJsonAsync<ResourceDto>();

            Assert.NotNull(created);
            Assert.Null(created.Tags);
        }

        [Fact]
        public async Task GET_with_expand_tags_returns_tags()
        {
            // Create resource with tags
            var body = new CreateResourceRequest("My Resource", null, ["React", "dotnet"]);
            var createResponse = await _client.PostAsJsonAsync("/api/v1/resources", body);
            var created = await createResponse.Content.ReadFromJsonAsync<ResourceDto>();

            // Get with expand=tags
            var getResponse = await _client.GetAsync($"/api/v1/resources/{created!.Id}?expand=tags");
            var fetched = await getResponse.Content.ReadFromJsonAsync<ResourceDto>();

            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.NotNull(fetched!.Tags);
            Assert.Equal(2, fetched.Tags.Count);
            Assert.Contains(fetched.Tags, t => t.Label == "React");
            Assert.Contains(fetched.Tags, t => t.Label == "dotnet");
        }

        [Fact]
        public async Task GET_without_expand_does_not_return_tags()
        {
            // Create resource with tags
            var body = new CreateResourceRequest("My Resource", null, ["React"]);
            var createResponse = await _client.PostAsJsonAsync("/api/v1/resources", body);
            var created = await createResponse.Content.ReadFromJsonAsync<ResourceDto>();

            // Get without expand
            var getResponse = await _client.GetAsync($"/api/v1/resources/{created!.Id}");
            var fetched = await getResponse.Content.ReadFromJsonAsync<ResourceDto>();

            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.Null(fetched!.Tags);
        }

        [Fact]
        public async Task LIST_with_expand_tags_returns_tags_for_all_resources()
        {
            // Create two resources with tags
            await _client.PostAsJsonAsync("/api/v1/resources", new CreateResourceRequest("Resource1", null, ["React"]));
            await _client.PostAsJsonAsync("/api/v1/resources", new CreateResourceRequest("Resource2", null, ["dotnet"]));

            // List with expand=tags
            var response = await _client.GetAsync("/api/v1/resources?expand=tags");
            var resources = await response.Content.ReadFromJsonAsync<List<ResourceDto>>();

            Assert.NotNull(resources);
            Assert.All(resources, r => Assert.NotNull(r.Tags));
        }

        [Fact]
        public async Task LIST_without_expand_does_not_return_tags()
        {
            // Create resource with tags
            await _client.PostAsJsonAsync("/api/v1/resources", new CreateResourceRequest("Resource1", null, ["React"]));

            // List without expand
            var response = await _client.GetAsync("/api/v1/resources");
            var resources = await response.Content.ReadFromJsonAsync<List<ResourceDto>>();

            Assert.NotNull(resources);
            Assert.All(resources, r => Assert.Null(r.Tags));
        }

        [Fact]
        public async Task PUT_updates_resource_tags()
        {
            // Create resource with initial tags
            var body = new CreateResourceRequest("My Resource", null, ["React"]);
            var createResponse = await _client.PostAsJsonAsync("/api/v1/resources", body);
            var created = await createResponse.Content.ReadFromJsonAsync<ResourceDto>();

            // Update with new tags
            var update = new UpdateResourceRequest("My Resource", null, ["dotnet", "Learning"]);
            var updateResponse = await _client.PutAsJsonAsync($"/api/v1/resources/{created!.Id}", update);
            updateResponse.EnsureSuccessStatusCode();

            // Verify tags were updated
            var getResponse = await _client.GetAsync($"/api/v1/resources/{created.Id}?expand=tags");
            var fetched = await getResponse.Content.ReadFromJsonAsync<ResourceDto>();

            Assert.NotNull(fetched!.Tags);
            Assert.Equal(2, fetched.Tags.Count);
            Assert.Contains(fetched.Tags, t => t.Label == "dotnet");
            Assert.Contains(fetched.Tags, t => t.Label == "Learning");
            Assert.DoesNotContain(fetched.Tags, t => t.Label == "React");
        }

        [Fact]
        public async Task PUT_with_empty_tags_clears_all_tags()
        {
            // Create resource with tags
            var body = new CreateResourceRequest("My Resource", null, ["React", "dotnet"]);
            var createResponse = await _client.PostAsJsonAsync("/api/v1/resources", body);
            var created = await createResponse.Content.ReadFromJsonAsync<ResourceDto>();

            // Update with empty tags array
            var update = new UpdateResourceRequest("My Resource", null, []);
            await _client.PutAsJsonAsync($"/api/v1/resources/{created!.Id}", update);

            // Verify tags were cleared
            var getResponse = await _client.GetAsync($"/api/v1/resources/{created.Id}?expand=tags");
            var fetched = await getResponse.Content.ReadFromJsonAsync<ResourceDto>();

            Assert.NotNull(fetched!.Tags);
            Assert.Empty(fetched.Tags);
        }

        [Fact]
        public async Task PUT_without_tags_field_preserves_existing_tags()
        {
            // Create resource with tags
            var body = new CreateResourceRequest("My Resource", null, ["React"]);
            var createResponse = await _client.PostAsJsonAsync("/api/v1/resources", body);
            var created = await createResponse.Content.ReadFromJsonAsync<ResourceDto>();

            // Update without tags field (null)
            var update = new UpdateResourceRequest("Updated Name", "New description", null);
            await _client.PutAsJsonAsync($"/api/v1/resources/{created!.Id}", update);

            // Verify tags were preserved
            var getResponse = await _client.GetAsync($"/api/v1/resources/{created.Id}?expand=tags");
            var fetched = await getResponse.Content.ReadFromJsonAsync<ResourceDto>();

            Assert.NotNull(fetched!.Tags);
            Assert.Single(fetched.Tags);
            Assert.Equal("React", fetched.Tags[0].Label);
        }

        [Fact]
        public async Task LIST_filters_by_single_tag()
        {
            // Create resources with different tags
            await _client.PostAsJsonAsync("/api/v1/resources", new CreateResourceRequest("Resource1", null, ["React"]));
            await _client.PostAsJsonAsync("/api/v1/resources", new CreateResourceRequest("Resource2", null, ["dotnet"]));
            await _client.PostAsJsonAsync("/api/v1/resources", new CreateResourceRequest("Resource3", null, ["React", "dotnet"]));

            // Filter by React tag
            var response = await _client.GetAsync("/api/v1/resources?tags=React");
            var resources = await response.Content.ReadFromJsonAsync<List<ResourceDto>>();

            Assert.NotNull(resources);
            Assert.Equal(2, resources.Count);
            Assert.All(resources, r => Assert.Contains("Resource", r.Name));
        }

        [Fact]
        public async Task LIST_filters_by_multiple_tags_with_OR_logic()
        {
            // Create resources
            await _client.PostAsJsonAsync("/api/v1/resources", new CreateResourceRequest("Resource1", null, ["React"]));
            await _client.PostAsJsonAsync("/api/v1/resources", new CreateResourceRequest("Resource2", null, ["dotnet"]));
            await _client.PostAsJsonAsync("/api/v1/resources", new CreateResourceRequest("Resource3", null, ["Learning"]));

            // Filter by React OR dotnet
            var response = await _client.GetAsync("/api/v1/resources?tags=React,dotnet");
            var resources = await response.Content.ReadFromJsonAsync<List<ResourceDto>>();

            Assert.NotNull(resources);
            Assert.Equal(2, resources.Count);
        }

        [Fact]
        public async Task Tag_reuse_across_resources()
        {
            // Create two resources with same tag
            await _client.PostAsJsonAsync("/api/v1/resources", new CreateResourceRequest("Resource1", null, ["React"]));
            await _client.PostAsJsonAsync("/api/v1/resources", new CreateResourceRequest("Resource2", null, ["React"]));

            // Get both with expand
            var response = await _client.GetAsync("/api/v1/resources?expand=tags&tags=React");
            var resources = await response.Content.ReadFromJsonAsync<List<ResourceDto>>();

            Assert.NotNull(resources);
            Assert.Equal(2, resources.Count);

            // Both should have the same tag ID (reused)
            var tag1 = resources[0].Tags!.First(t => t.Label == "React");
            var tag2 = resources[1].Tags!.First(t => t.Label == "React");
            Assert.Equal(tag1.Id, tag2.Id);
        }

        [Fact]
        public async Task GET_resource_with_no_tags_and_expand_returns_empty_array()
        {
            // Create resource without tags
            var body = new CreateResourceRequest("My Resource", null, null);
            var createResponse = await _client.PostAsJsonAsync("/api/v1/resources", body);
            var created = await createResponse.Content.ReadFromJsonAsync<ResourceDto>();

            // Get with expand=tags
            var getResponse = await _client.GetAsync($"/api/v1/resources/{created!.Id}?expand=tags");
            var fetched = await getResponse.Content.ReadFromJsonAsync<ResourceDto>();

            Assert.NotNull(fetched!.Tags);
            Assert.Empty(fetched.Tags);
        }

        // Tag Validation Tests

        [Fact]
        public async Task POST_returns_400_for_duplicate_tag_labels()
        {
            var body = new CreateResourceRequest("My Resource", null, ["React", "React"]);

            var response = await _client.PostAsJsonAsync("/api/v1/resources", body);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task POST_returns_400_for_more_than_10_tags()
        {
            var tags = Enumerable.Range(1, 11).Select(i => $"Tag{i}").ToList();
            var body = new CreateResourceRequest("My Resource", null, tags);

            var response = await _client.PostAsJsonAsync("/api/v1/resources", body);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task POST_returns_400_for_empty_tag_label()
        {
            var body = new CreateResourceRequest("My Resource", null, ["React", ""]);

            var response = await _client.PostAsJsonAsync("/api/v1/resources", body);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task POST_returns_400_for_whitespace_only_tag_label()
        {
            var body = new CreateResourceRequest("My Resource", null, ["React", "   "]);

            var response = await _client.PostAsJsonAsync("/api/v1/resources", body);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task POST_returns_400_for_tag_label_exceeding_50_characters()
        {
            var longLabel = new string('a', 51);
            var body = new CreateResourceRequest("My Resource", null, [longLabel]);

            var response = await _client.PostAsJsonAsync("/api/v1/resources", body);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task LIST_with_non_existent_tag_returns_empty_list()
        {
            // Create resource with a known tag
            await _client.PostAsJsonAsync("/api/v1/resources",
                new CreateResourceRequest("Resource1", null, ["React"]));

            // Filter by non-existent tag
            var response = await _client.GetAsync("/api/v1/resources?tags=NonExistent");
            var resources = await response.Content.ReadFromJsonAsync<List<ResourceDto>>();

            Assert.NotNull(resources);
            Assert.Empty(resources);
        }

        [Fact]
        public async Task Expand_parameter_is_case_insensitive()
        {
            // Create resource with tags
            var body = new CreateResourceRequest("My Resource", null, ["React"]);
            var createResponse = await _client.PostAsJsonAsync("/api/v1/resources", body);
            var created = await createResponse.Content.ReadFromJsonAsync<ResourceDto>();

            // Get with expand=Tags (capital T)
            var getResponse = await _client.GetAsync($"/api/v1/resources/{created!.Id}?expand=Tags");
            var fetched = await getResponse.Content.ReadFromJsonAsync<ResourceDto>();

            Assert.NotNull(fetched!.Tags);
            Assert.Single(fetched.Tags);
        }

        [Fact]
        public async Task Multiple_expand_values_are_parsed_correctly()
        {
            // Create resource with tags
            var body = new CreateResourceRequest("My Resource", null, ["React"]);
            var createResponse = await _client.PostAsJsonAsync("/api/v1/resources", body);
            var created = await createResponse.Content.ReadFromJsonAsync<ResourceDto>();

            // Get with expand=tags,other
            var getResponse = await _client.GetAsync($"/api/v1/resources/{created!.Id}?expand=tags,other");
            var fetched = await getResponse.Content.ReadFromJsonAsync<ResourceDto>();

            Assert.NotNull(fetched!.Tags);
            Assert.Single(fetched.Tags);
        }

        [Fact]
        public async Task GET_non_existent_resource_with_expand_returns_404()
        {
            var response = await _client.GetAsync($"/api/v1/resources/{Guid.NewGuid()}?expand=tags");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PUT_non_existent_resource_with_tags_returns_404()
        {
            var update = new UpdateResourceRequest("Updated", null, ["React"]);
            var response = await _client.PutAsJsonAsync($"/api/v1/resources/{Guid.NewGuid()}", update);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        private async Task<ResourceDto> CreateResourceAsync(string name)
        {
            var response = await _client.PostAsJsonAsync("/api/v1/resources", new CreateResourceRequest(name, null, null));
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<ResourceDto>())!;
        }
    }
}
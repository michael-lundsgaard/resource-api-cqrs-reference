namespace ResourceCatalog.Api.Entities
{
    public class Resource
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
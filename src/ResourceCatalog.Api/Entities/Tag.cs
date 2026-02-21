namespace ResourceCatalog.Api.Entities
{
    public class Tag
    {
        public Guid Id { get; set; }
        public required string Label { get; set; }

        // Navigation property for many-to-many
        public List<Resource> Resources { get; set; } = new();
    }
}

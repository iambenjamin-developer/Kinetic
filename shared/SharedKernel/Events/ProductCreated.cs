namespace SharedKernel.Events
{
    public record ProductCreated(Guid Id, string Name, string Description, decimal Price, int Stock, string Category);
}

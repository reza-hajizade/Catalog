namespace Catalog.Infrastructure.IntegrationEvent;

    public record CatalogItemChangedEvent(
            string Name,
            string Description,
            string CatalogCategory,
            string CatalogBrand,
            string Slug);


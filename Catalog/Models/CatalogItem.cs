namespace Catalog.Models;

public class CatalogItem
{
    public const string TableName = "CatalogItems";

    public int Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public decimal Price { get; private set; }

    public int AvailableStock { get; private set; }

    public string Slug { get; private set; } = null!;

    public int MaxStockThreshold { get; private set; }
    public ICollection<CatalogMedia> Medias { get; private set; } = [];


    public static CatalogItem Create(string name, string description, int maxStockThreshold, int brandId, int categoryId, decimal price = default)
    {
        var newItem = new CatalogItem
        {
            Name = name,
            CatalogBrandId = brandId,
            CatalogCategoryId = categoryId,
            Description = description,
            Slug = name.ToKebabCase(),
            Price = price,
            MaxStockThreshold = maxStockThreshold

        };


        return newItem;
    }

    public void Update(string name, string description, int brandId, int categoryId)
    {
        Name = name;
        CatalogBrandId = brandId;
        CatalogCategoryId = categoryId;
        Description = description;
        Slug = name.ToKebabCase();
    }


    public CatalogBrand CatalogBrand { get; private set; } = null!;

    public int CatalogBrandId { get; private set; }

    public CatalogCategory CatalogCategory { get; private set; } = null!;

    public int CatalogCategoryId { get; private set; }
 
}


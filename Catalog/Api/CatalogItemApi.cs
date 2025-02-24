﻿using Catalog.Api.Contracts;
using Catalog.Infrastructure.IntegrationEvent;

namespace Catalog.Api;

public static class CatalogItemApi
{

    public static IEndpointRouteBuilder MapCatalogItemApis(this IEndpointRouteBuilder app)
    {
        app.MapPost("/", CreateItem);
        app.MapPut("/", UpdateItem);
        app.MapPatch("/MaxStockThreshold", UpdateMaxStockThreshold);
        app.MapDelete("/{id:int:required}", DeleteItemById);
        app.MapGet("/{id:int:required}", GetItemById);
        app.MapGet("/", GetItems);


        return app;

    }


    public static async Task<Results<Created, ValidationProblem, BadRequest<string>>> CreateItem(
        [AsParameters] CatalogServices services,
        CreateCatalogItemRequest itemToCreate,
        IValidator<CreateCatalogItemRequest> validator,
        CancellationToken cancellationToken)
    {

        var validate = validator.Validate(itemToCreate);

        if (!validate.IsValid)
        {
            return TypedResults.ValidationProblem(validate.ToDictionary());
        }

        var hasCategory = await services.Context.CatalogCategories.AnyAsync(x => x.Id == itemToCreate.CatalogId, cancellationToken);
        if (!hasCategory)
        {
            return TypedResults.BadRequest($"A category Id is not valid.");
        }

        var hasBrand = await services.Context.CatalogBrands.AnyAsync(x => x.Id == itemToCreate.BrandId, cancellationToken);
        if (!hasBrand)
        {
            return TypedResults.BadRequest($"A brand Id is not valid.");
        }

        var hasItemSlug = await services.Context.CatalogItems.AnyAsync(x => x.Slug == itemToCreate.Name.ToKebabCase(), cancellationToken);

        if (hasItemSlug)
        {
            return TypedResults.BadRequest($"A Item with the slug '{itemToCreate.Name.ToKebabCase()}' already exists.");
        }

        var item = CatalogItem.Create(
             itemToCreate.Name,
             itemToCreate.Description,
             itemToCreate.MaxStockThreshold,
             itemToCreate.BrandId,
             itemToCreate.CatalogId
             );

        services.Context.CatalogItems.Add(item);
        await services.Context.SaveChangesAsync(cancellationToken);

        var hintUrl = $"/api/v1/items/{item.Id}";

        var LoadedItem = await services.Context.CatalogItems
            .Include(x => x.CatalogBrand)
            .Include(x => x.CatalogCategory)
            .FirstAsync(x => x.Id == item.Id);

        await services.Publish.Publish(new CatalogItemAddedEvent(
            LoadedItem.Name,
            LoadedItem.Description,
            LoadedItem.CatalogCategory.Category,
            LoadedItem.CatalogBrand.Brand,
            LoadedItem.Slug,
            hintUrl));

        return TypedResults.Created(hintUrl);
    }

    public static async Task<Results<Created, ValidationProblem, NotFound<string>, BadRequest<string>>> UpdateItem(
        [AsParameters] CatalogServices services,
        UpdateCatalogItemRequest itemToUpdate,
        IValidator<UpdateCatalogItemRequest> validator,
        CancellationToken cancellationToken)
    {
        var validate = validator.Validate(itemToUpdate);
        if (validate is null)
        {

            return TypedResults.ValidationProblem(validate.ToDictionary());
        }

        var Item = await services.Context.CatalogItems.FirstOrDefaultAsync(x => x.Id == itemToUpdate.Id, cancellationToken);

        if (Item is null)
        {
            return TypedResults.NotFound($"Item with id {Item.Id} is not valid");
        }

        var hasCategory = await services.Context.CatalogCategories.AnyAsync(x => x.Id == itemToUpdate.CatalogId, cancellationToken);
        if (!hasCategory)
        {
            return TypedResults.BadRequest($"A category Id is not valid.");
        }


        var hasBrand = await services.Context.CatalogBrands.AnyAsync(x => x.Id == itemToUpdate.BrandId, cancellationToken);
        if (!hasBrand)
        {
            return TypedResults.BadRequest($"A brand Id is not valid.");
        }

        var hasItemSlug = await services.Context.CatalogItems.AnyAsync(x => x.Id != Item.Id &&
                                                                            x.Slug == itemToUpdate.Name.ToKebabCase(), cancellationToken);

        if (hasItemSlug)
        {
            return TypedResults.BadRequest($"A Item with the slug '{itemToUpdate.Name.ToKebabCase()}' already exists.");
        }


        Item.Update(itemToUpdate.Name,
           itemToUpdate.Description,
           itemToUpdate.BrandId,
           itemToUpdate.CatalogId);
        await services.Context.SaveChangesAsync(cancellationToken);



        var LoadedItem = await services.Context.CatalogItems
            .Include(x => x.CatalogBrand)
            .Include(x => x.CatalogCategory)
            .FirstAsync(x => x.Id == Item.Id);


        await services.Publish.Publish(
            new CatalogItemChangedEvent(
                LoadedItem.Name,
                LoadedItem.Description,
                LoadedItem.CatalogCategory.Category,
                LoadedItem.CatalogBrand.Brand,
                LoadedItem.Slug));




        return TypedResults.Created($"/api/v1/items/{Item.Id}");
    }

    public static async Task<Results<Created, ValidationProblem, NotFound<string>, BadRequest<string>>> UpdateMaxStockThreshold(
[AsParameters] CatalogServices services,
UpdateCatalogItemMaxStockThresholdRequest itemToUpdate,
IValidator<UpdateCatalogItemMaxStockThresholdRequest> validator,
CancellationToken cancellationToken)
    {

        var validate = validator.Validate(itemToUpdate);
        if (!validate.IsValid)
        {
            return TypedResults.ValidationProblem(validate.ToDictionary());
        }

        var Item = await services.Context.CatalogItems.FirstOrDefaultAsync(i => i.Id == itemToUpdate.Id, cancellationToken);
        if (Item is null)
        {
            return TypedResults.NotFound($"Item with id {itemToUpdate.Id} not found.");
        }


        await services.Context.SaveChangesAsync(cancellationToken);

        return TypedResults.Created($"/api/v1/items/{Item.Id}");

    }

    public static async Task<Results<NoContent, NotFound, BadRequest<string>>> DeleteItemById
   ([AsParameters] CatalogServices services, int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return TypedResults.BadRequest("Id is not valid.");
        }

        var item = await services.Context.CatalogItems.FirstOrDefaultAsync(x => x.Id == id);
        if (item is null)
        {
            return TypedResults.NotFound();
        }

        services.Context.CatalogItems.Remove(item);
        await services.Context.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    public static async Task<Results<Ok<CatalogItemResponse>, NotFound, BadRequest<string>>> GetItemById(
[AsParameters] CatalogServices services,
int id)
    {
        if (id <= 0)
        {
            return TypedResults.BadRequest("Id is not valid.");
        }
        var item = await services.Context.CatalogItems
                                         .Include(x => x.CatalogBrand)
                                         .Include(x => x.CatalogCategory)
                                         .FirstOrDefaultAsync(ci => ci.Id == id);
        if (item is null)
        {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(
            new CatalogItemResponse(
                item.Id,
                item.Name,
                item.Slug,
                item.Description,
                item.CatalogBrandId,
                item.CatalogBrand.Brand,
                item.CatalogCategoryId,
                item.CatalogCategory.Category,
                item.Price,
                item.AvailableStock,
                item.MaxStockThreshold));
    }

    public static async Task<Results<Ok<IEnumerable<CatalogItemResponse>>, BadRequest<string>>> GetItems(
    [AsParameters] CatalogServices services, CancellationToken cancellationToken)
    {


        var items = await services.Context.CatalogItems
                          .Include(x => x.CatalogBrand)
                          .Include(x => x.CatalogCategory)
                          .Select(x => new CatalogItemResponse(x.Id,
                                                               x.Name,
                                                               x.Slug,
                                                               x.Description,
                                                               x.CatalogBrandId,
                                                               x.CatalogBrand.Brand,
                                                               x.CatalogCategoryId,
                                                               x.CatalogCategory.Category,
                                                               x.Price,
                                                               x.AvailableStock,
                                                               x.MaxStockThreshold))
                          .OrderBy(c => c.Id)
                          .ToListAsync(cancellationToken);

        return TypedResults.Ok<IEnumerable<CatalogItemResponse>>(items);
    }

}


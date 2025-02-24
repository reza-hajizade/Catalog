

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.AddApplicationServices();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGroup("/api/v1/brands")
   .WithTags("Brand APIs")
   .MapCatalogBrandEndpoints();


app.MapGroup("/api/v1/categories")
   .WithTags("Category APIs")
   .MapCatalogCategoryEndpoints();

app.MapGroup("/api/v1/items")
   .WithTags("Item APIs")
   .MapCatalogItemEndpoints();

app.Run();



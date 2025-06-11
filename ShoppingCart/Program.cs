using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using ShoppingCart.Repositories;
using ShoppingCart.Services;
using ShoppingCart.Events;
using MediatR;
using System.Reflection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();

builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(builder.Configuration.GetConnectionString("MongoDb")));

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase("shoppingcartdb2"));

ConfigureBsonSerialization();

builder.Services.AddSingleton<EventStore>();
builder.Services.AddSingleton<CartAggregateRepository>();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
});

builder.Services.AddHttpClient<ProductService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        var exceptionHandlerFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();

        if (exceptionHandlerFeature?.Error != null)
        {
            logger.LogError(exceptionHandlerFeature.Error, "Unhandled exception occurred");
        }

        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred" });
    });
});

app.Run();

static void ConfigureBsonSerialization()
{
    if (!BsonClassMap.IsClassMapRegistered(typeof(CartEvent)))
    {
        BsonClassMap.RegisterClassMap<CartEvent>(cm =>
        {
            cm.AutoMap();
            cm.SetIsRootClass(true);
            // Usuñ discriminator - nie u¿ywamy _t
            cm.SetDiscriminator("EventType");
            cm.SetDiscriminatorIsRequired(true);
            cm.AddKnownType(typeof(CartCreatedEvent));
            cm.AddKnownType(typeof(ProductAddedToCartEvent));
            cm.AddKnownType(typeof(ProductRemovedFromCartEvent));
            cm.AddKnownType(typeof(CartCheckedOutEvent));
        });
    }

    if (!BsonClassMap.IsClassMapRegistered(typeof(CartCreatedEvent)))
    {
        BsonClassMap.RegisterClassMap<CartCreatedEvent>();
    }

    if (!BsonClassMap.IsClassMapRegistered(typeof(ProductAddedToCartEvent)))
    {
        BsonClassMap.RegisterClassMap<ProductAddedToCartEvent>();
    }

    if (!BsonClassMap.IsClassMapRegistered(typeof(ProductRemovedFromCartEvent)))
    {
        BsonClassMap.RegisterClassMap<ProductRemovedFromCartEvent>();
    }

    if (!BsonClassMap.IsClassMapRegistered(typeof(CartCheckedOutEvent)))
    {
        BsonClassMap.RegisterClassMap<CartCheckedOutEvent>();
    }
}
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using ShoppingCart.Repositories;
using ShoppingCart.Services;
using MediatR;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;


var builder = WebApplication.CreateBuilder(args);

// Dodanie logowania
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Us³ugi podstawowe
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Memory Cache
builder.Services.AddMemoryCache();

// MongoDB
builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(builder.Configuration.GetConnectionString("MongoDb")));

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase("shoppingcartdb"));

builder.Services.AddSingleton<CartRepository>();

// MediatR dla wzorca CQRS
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
});

// Konfiguracja HTTP Client z Polly dla obs³ugi awarii
builder.Services.AddHttpClient<ProductService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});
//    .AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(3, retryAttempt =>
//    TimeSpan.FromMilliseconds(100 * Math.Pow(2, retryAttempt))));

var app = builder.Build();

// Konfiguracja potoku HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Dodanie obs³ugi wyj¹tków middleware
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred" });
    });
});

app.Run();
using Broker;
using Domain.Interfaces;
using Infrastructure;
using WebApi;
using WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<BrokerSettings>(builder.Configuration.GetSection("BrokerSettings"));
builder.Services.Configure<SquashSettings>(builder.Configuration.GetSection("SquashSettings"));
builder.Services.AddScoped<IBroker, Broker.Broker>();
builder.Services.AddScoped<ISquashService, SquashService>();
builder.Services.AddSingleton<IKeyComputeService, KeyComputeService>();
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();
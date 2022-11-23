using Microsoft.Extensions.Options;
using QuickbooksApi;
using Swashbuckle.AspNetCore.SwaggerUI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.WebHost.UseUrls("http://localhost:5000", "https://localhost:5001", "https://192.168.1.111:5001", "http://192.168.1.111:5000");

builder.Services.AddControllers();

IQbXMLConnection CreateConnection()
{
    var connection = SessionConnectionFactory.Create(SessionCreationMode.OneConnectionAndSession);
    connection.Connect();
    return connection;
}
// var qbXmlConnection = CreateConnection();

builder.Services.AddSingleton<IQbXMLConnection>(_ => CreateConnection());
builder.Services.AddScoped(s =>
{
    var connection = s.GetRequiredService<IQbXMLConnection>();
    return new QbXmlQueriesFactory(connection);
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
app.UseSwagger();

app.UseSwaggerUI();
//}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

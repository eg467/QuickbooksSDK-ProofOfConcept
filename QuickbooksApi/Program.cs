using QuickbooksApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

//builder.WebHost.UseUrls("http://localhost:5000", "https://localhost:5001", "https://192.168.1.111:5001", "http://192.168.1.111:5000");

builder.Services.AddControllers();

IQbXMLConnection CreateConnection()
{
    /*
     * Request timings as measured by postman...
     * SessionCreationMode.OneConnectionAndSession ~900ms
     * SessionCreationMode.IndividualConnectionsAndSessions ~1100-1200ms
     * SessionCreationMode.OneConnectionIndividualSessions ~950ms
     */
    var connection = SessionConnectionFactory.Create(SessionCreationMode.OneConnectionIndividualSessions);
    connection.Connect();
    return connection;
}
var qbXmlConnection = CreateConnection();

builder.Services.AddSingleton(qbXmlConnection);
builder.Services.AddScoped(_ => new QbXmlQueriesFactory(qbXmlConnection));

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

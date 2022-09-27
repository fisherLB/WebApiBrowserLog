using Lavspent.BrowserLogger.Extensions;
using Lavspent.BrowserLogger.Options;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Host.UseSerilog((context, logger) => { 

    logger.WriteTo.Console();

    logger.WriteTo.File("Logs/log.txt");

    logger.WriteTo.Browser();
});
builder.Services.Configure<BrowserLoggerOptions>(builder.Configuration.GetSection("BrowserLog"));
builder.Services.AddBrowserLogger();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseWebSockets();
app.UseBrowserLogger();

app.Run();

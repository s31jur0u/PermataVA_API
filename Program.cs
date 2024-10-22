
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
// Load configuration from appsettings.json

string? tokenSecret = builder.Configuration.GetValue<string>("TOKEN:SECRET");
// Register the SqlConnectionFactory with a Scoped lifetime
builder.Services.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();

// Register the security key (example)

if (tokenSecret != null)
    builder.Services.AddSingleton<SecurityKey>(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenSecret)));


// Register the JWT token generator service
builder.Services.AddScoped<IJwtTokenGeneratorService, JwtTokenGeneratorService>();

builder.Services.AddCors(options =>
   {
       options.AddPolicy("AllowAllOrigin",
           corsPolicyBuilder => corsPolicyBuilder.AllowAnyOrigin()
                             .AllowAnyHeader()
                             .AllowAnyMethod());
   });

builder.Services.AddControllers(options =>
{

    // options.Filters.Add<JwtAuthorizeAttribute>(); // Add the custom authorize filter globally
});
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Konfigurasi Serilog untuk menulis log ke file

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AllowAllOrigin");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

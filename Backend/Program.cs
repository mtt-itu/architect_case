using Backend.Data;
using Backend.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});
builder.Services.AddSingleton<AppDateService>();
builder.Services.AddScoped<DebtService>();
var thirdPartyBaseUrl = builder.Configuration["ThirdPartyServices:BaseUrl"] ?? "http://localhost:5041/";
builder.Services.AddHttpClient<MockPaymentService>(client => client.BaseAddress = new Uri(thirdPartyBaseUrl));
builder.Services.AddHttpClient<MockSubscriptionProviderService>(client => client.BaseAddress = new Uri(thirdPartyBaseUrl));
builder.Services.AddScoped<ReminderService>();
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
app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();

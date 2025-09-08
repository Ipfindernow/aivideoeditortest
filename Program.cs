using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Allow huge uploads (up to 120GB)
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 1024L * 1024 * 1024 * 120;
});

builder.Services.AddControllers();

var app = builder.Build();

// Serve static files for preview
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(@"D:\VideoClips"),
    RequestPath = "/VideoClips"
});

app.UseHttpsRedirection();
app.MapControllers();
app.Run();

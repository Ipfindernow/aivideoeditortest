using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Allow large file uploads
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 1024L*1024L*1024*120; // 120GB
});

builder.Services.AddControllers();

var app = builder.Build();

// Serve static files from storage folder for preview
app.UseStaticFiles(new StaticFileOptions {
    FileProvider = new PhysicalFileProvider(@"D:\VideoClips"), // your drive
    RequestPath = "/VideoClips"
});

app.UseHttpsRedirection();
app.MapControllers();
app.Run();

using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AutoVideoEditor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VideoUploadController : ControllerBase
    {
        private readonly string _storagePath = @"D:\VideoClips\"; // Change to your large storage drive

        [HttpPost("upload")]
        public async Task<IActionResult> UploadVideos([FromForm] List<IFormFile> clips)
        {
            if (clips == null || clips.Count < 4)
                return BadRequest($"Please upload at least 4 clips. You sent {clips?.Count ?? 0}.");

            Directory.CreateDirectory(_storagePath);
            var savedFiles = new List<string>();

            foreach (var clip in clips)
            {
                if (clip.Length > 0)
                {
                    var filePath = Path.Combine(_storagePath, clip.FileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await clip.CopyToAsync(stream);

                    savedFiles.Add(filePath);
                }
            }

            var pythonResult = AnalyzeClips(savedFiles);

            return Ok(new
            {
                message = "Videos uploaded successfully",
                files = savedFiles.Select(f => Path.GetFileName(f)).ToList(),
                analysis = pythonResult
            });
        }

        private object AnalyzeClips(List<string> files)
        {
            try
            {
                var args = string.Join(" ", files.Select(f => $"\"{f}\""));

                var psi = new ProcessStartInfo
                {
                    FileName = "python3",
                    Arguments = $"analyze_clips.py {args}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var process = Process.Start(psi);
                string result = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                    return new { error };

                return Newtonsoft.Json.JsonConvert.DeserializeObject(result);
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }
    }
}

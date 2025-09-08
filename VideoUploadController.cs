using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AutoVideoEditor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VideoUploadController : ControllerBase
    {
        private readonly string _storagePath = @"D:\VideoClips\";

        [HttpPost("upload")]
        public async Task<IActionResult> UploadVideos([FromForm] List<IFormFile> clips)
        {
            if (clips == null || clips.Count < 4)
                return BadRequest($"Upload at least 4 clips. You sent {clips?.Count ?? 0}");

            Directory.CreateDirectory(_storagePath);

            var savedFiles = new List<string>();
            foreach (var clip in clips)
            {
                if (clip.Length > 0)
                {
                    var fileName = Path.GetFileName(clip.FileName);
                    var filePath = Path.Combine(_storagePath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await clip.CopyToAsync(stream); // save permanently

                    savedFiles.Add(filePath); // full path for editing
                }
            }

            // Call Python analysis
            var analysis = AnalyzeClips(savedFiles);

            return Ok(new
            {
                message = "Videos uploaded successfully",
                count = savedFiles.Count,
                files = savedFiles.Select(f => System.IO.Path.GetFileName(f)).ToList(),
                analysis
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

                if (process.ExitCode != 0) return new { error };
                return Newtonsoft.Json.JsonConvert.DeserializeObject(result);
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using System.Text;
using YoutubeExplode;
using System.IO;
using YoutubeExplode.Common;
using System.Diagnostics;

namespace Backend.Controllers
{
    using Microsoft.EntityFrameworkCore;
    using Model;
    using YoutubeExplode.Videos.Streams;

    [ApiController]
    [Route("content")]
    [EnableCors("main")]
    public class ContentController : ControllerBase
    {
        private readonly YoutubeClient youtube;
        private readonly StreamingDBContext ctx;

        public ContentController(YoutubeClient youtube, StreamingDBContext ctx)
        {
            this.youtube = youtube;
            this.ctx = ctx;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetContent(Guid id)
        {
            var content = await ctx.Contents.FindAsync(id);

            if (content == null)
                return NotFound();

            return File(content.Bytes, "application/octet-stream");
        }

        [HttpPost("{id}")]
        public async Task<IActionResult> UploadContent(string id)
        {
            string url = $"www.youtube.com/watch?v={id}";
            if (string.IsNullOrWhiteSpace(url))
                return BadRequest("URL do vídeo é inválida.");

            await ProcessYoutubeVideo(url, id);
            return Ok("Vídeo processado e armazenado com sucesso.");
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var contents = await ctx.Contents.ToListAsync(); // Busca todos os conteúdos
            return Ok(contents); // Retorna os conteúdos em formato JSON
        }

        public async Task ProcessYoutubeVideo(string videoUrl, string videoId)
        {
            var video = await youtube.Videos.GetAsync(videoId);
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoId);
            var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestBitrate();

            var videoFilePath = Path.Combine(Path.GetTempPath(), $"{video.Title}.mp4");

            await youtube.Videos.Streams.DownloadAsync(streamInfo, videoFilePath);

            var hlsDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(hlsDirectory);
            var playlistPath = Path.Combine(hlsDirectory, "playlist.m3u8");

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{videoFilePath}\" -hls_time 10 -hls_list_size 0 -f hls \"{playlistPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processStartInfo))
            {
                var error = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"Erro ao processar o vídeo: {error}");
                    return;
                }
            }

            var m3u8Lines = System.IO.File.ReadAllLines(playlistPath);
            foreach (var line in m3u8Lines)
            {
                if (line.EndsWith(".ts"))
                {
                    var tsFilePath = Path.Combine(hlsDirectory, line.Trim());

                    if (System.IO.File.Exists(tsFilePath))
                    {
                        var content = new Content
                        {
                            Bytes = System.IO.File.ReadAllBytes(tsFilePath)
                        };
                        ctx.Add(content);
                    }
                }
            }
            await ctx.SaveChangesAsync();

            System.IO.File.Delete(videoFilePath);
            Directory.Delete(hlsDirectory, true);
        }

        private string GetVideoId(string url)
        {
            var uri = new Uri(url);
            var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
            return queryParams["v"] ?? uri.Segments.Last();
        }
    }
}

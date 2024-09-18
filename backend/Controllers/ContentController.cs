using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using System.Text;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace Backend.Controllers;

using System.Diagnostics;
using Model;
using YoutubeExplode.Videos.Streams;

[ApiController]
[Route("content")]
[EnableCors("main")]
public class ContentController : ControllerBase
{
    StreamingDBContext ctx;
    public ContentController(StreamingDBContext ctx)
        => this.ctx = ctx;

    [HttpGet("{id}")]
    public async Task<IActionResult> getContent(Guid id)
    {
        var content = await ctx.Contents.FindAsync(id);

        if (content == null)
            return NotFound();

        return File(content.Bytes, "application/octet-stream");
    }

    [HttpPost]
    public async Task<IActionResult> UploadContent([FromBody] VideoUploadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.VideoUrl))
            return BadRequest("URL do vídeo é inválida.");

        await ProcessYoutubeVideo(request.VideoUrl);
        return Ok("Vídeo processado e armazenado com sucesso.");
    }

    public async Task ProcessYoutubeVideo(string videoUrl)
    {
        var youtube = new YoutubeClient();
        string videoId = GetVideoId(videoUrl);
        var video = await youtube.Videos.GetAsync(videoId);
        var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoId);
        var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestBitrate();

        // Define o caminho para salvar o vídeo
        var videoFilePath = Path.Combine(Path.GetTempPath(), $"{video.Title}.mp4");

        // Baixa o vídeo
        await youtube.Videos.Streams.DownloadAsync(streamInfo, videoFilePath);

        // Cria um diretório temporário para os arquivos HLS
        var hlsDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(hlsDirectory);
        var playlistPath = Path.Combine(hlsDirectory, "playlist.m3u8");

        // Chama o FFmpeg para gerar HLS
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-i \"{videoFilePath}\" -hls_time 10 -hls_list_size 0 -f hls \"{playlistPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = Process.Start(processStartInfo)!)
        {
            var error = await process.StandardError.ReadToEndAsync();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                Console.WriteLine($"Erro ao processar o vídeo: {error}");
                return;
            }
        }

        // Salvar a playlist no banco de dados
        var contentHeader = new Content
        {
            Bytes = Encoding.UTF8.GetBytes(System.IO.File.ReadAllText(playlistPath))
        };

        using var ctx = new StreamingDBContext();
        ctx.Add(contentHeader);
        await ctx.SaveChangesAsync();

        // Limpeza dos arquivos temporários
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
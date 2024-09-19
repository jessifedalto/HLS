using System.Text;
using Microsoft.EntityFrameworkCore;

using Backend.Model;
using System.Linq.Expressions;
using System.Net;

using YoutubeExplode;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

builder.Services.AddScoped<HttpClient>(p =>
{
    var proxy = new WebProxy
    {
        Address = new Uri($"http://@rb-proxy-ca1.bosch.com:8080"),
        BypassProxyOnLocal = false,
        UseDefaultCredentials = false,

        // *** These creds are given to the proxy server, not the web server ***
        Credentials = new NetworkCredential(
            userName: "disrct",
            password: "etsps2024401")
    };

    // Now create a client handler which uses that proxy
    var httpClientHandler = new HttpClientHandler
    {
        Proxy = proxy,
    };

    // Finally, create the HTTP client object
    var client = new HttpClient(handler: httpClientHandler, disposeHandler: true);
    return client;
});

builder.Services.AddScoped(p =>
{
    var client = p.GetService<HttpClient>();
    if (client is null)
        throw new Exception();

    return new YoutubeClient(client);
});

builder.Services.AddDbContext<StreamingDBContext>();

builder.Services.AddCors(op => op
    .AddPolicy("main", policy => policy
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowAnyOrigin()
    )
);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.MapControllers();


// addData();

app.Run();

// static void addData()
// {
//     var path = "..\\data";

//     if (!Directory.Exists(path))
//     {
//         Console.WriteLine($"Diretório não encontrado: {path}");
//         return;
//     }

//     var files = Directory.GetFiles(path);

//     var header = files
//         .FirstOrDefault(f => f.EndsWith(".m3u8"))!;

//     if (header == null)
//     {
//         Console.WriteLine("Arquivo de cabeçalho .m3u8 não encontrado.");
//         return;
//     }


//     var parts = files
//         .Where(f => f != header);

//     using var ctx = new StreamingDBContext();
//     var dict = new Dictionary<string, Guid>();

//     foreach (var part in parts)
//     {
//         var content = new Content
//         {
//             Bytes = File.ReadAllBytes(part)
//         };
//         ctx.Add(content);

//         var fileName = Path.GetFileName(part);
//         dict.Add(fileName, content.Id);
//     }
//     ctx.SaveChanges();

//     var lines = File.ReadAllLines(header);
//     var sb = new StringBuilder();
//     foreach (var line in lines)
//     {
//         if (!dict.TryGetValue(line, out var id))
//         {
//             sb.AppendLine(line);
//             continue;
//         }

//         sb.AppendLine(id.ToString());
//     }
//     var processedHeader = sb.ToString();

//     var contentHeader = new Content
//     {
//         Bytes = Encoding.UTF8.GetBytes(processedHeader)
//     };
//     ctx.Add(contentHeader);
//     ctx.SaveChanges();

//     Console.WriteLine(contentHeader.Id);

// }

using (var context = new StreamingDBContext())
{
    var contents = context.Contents.ToList();
    foreach (var content in contents)
    {
        Console.WriteLine(content); // Ajuste para exibir as propriedades relevantes
    }
}
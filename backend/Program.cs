using System.Text;
using Microsoft.EntityFrameworkCore;

using Backend.Model;
using System.Linq.Expressions;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

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

app.MapControllers();

app.UseCors();

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
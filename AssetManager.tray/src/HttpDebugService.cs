using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace AssetManager.Tray
{
    public class HttpDebugService
    {
        private static string _latestJson = "{ \"status\": \"aguardando dados\" }";
        public static void UpdateSnapshot(string json)
        {
            _latestJson = json;
        }
        public async Task StartAsync()
        {
            var port = Program.Configuration.GetValue<int>("HttpDebug:Port", 6767);

            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls($"http://*:{port}");

            var app = builder.Build();

            app.MapGet("/", () =>
            {
                return Results.Text(_latestJson, "application/json");
            });

            await app.RunAsync();
        }
    }
}
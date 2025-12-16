using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AssetManager.Tray
{
    public class MetricsWebSocketClient
    {
        private ClientWebSocket? _ws;
        private readonly string _url;
        private readonly string _token;

        public MetricsWebSocketClient(string url, string token)
        {
            _url = url;
            _token = token;
        }

        public async Task EnsureConnectedAsync()
        {
            if (_ws != null && _ws.State == WebSocketState.Open)
                return;

            _ws?.Dispose();
            _ws = new ClientWebSocket();

            _ws.Options.SetRequestHeader(
                "Authorization",
                $"Bearer {_token}"
            );

            await _ws.ConnectAsync(
                new Uri(_url),
                CancellationToken.None
            );
        }

        public async Task SendAsync(string json)
        {
            await EnsureConnectedAsync();

            if (_ws?.State != WebSocketState.Open)
                return;

            var buffer = Encoding.UTF8.GetBytes(json);

            await _ws.SendAsync(
                buffer,
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
        }
    }
}

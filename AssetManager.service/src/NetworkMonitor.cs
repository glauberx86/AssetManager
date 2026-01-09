// Criar opção para escolher qual endereço mac usar em caso de múltiplos adaptadores

using System;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace AssetManager.Service
{
    public interface INetworkMonitor
    {
        (string ip, string mac) GetNetworkInfo();
    }

    public class NetworkMonitor : INetworkMonitor
    {
        private readonly TimeSpan _refreshInterval = TimeSpan.FromSeconds(60);
        private string? _lastIp;
        private DateTime _lastFetch = DateTime.MinValue;
        private string? _mac;

        public (string ip, string mac) GetNetworkInfo()
        {
            string ip = GetCachedPublicIp();

            _mac ??= GetMacAddress();
            if (string.IsNullOrWhiteSpace(_mac))
                _mac = "Not Found";

            string mac = _mac;

            return (ip, mac);
        }
        private string? GetPublicIp()
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                var response = client.GetStringAsync("https://api.ipify.org").GetAwaiter().GetResult();
                return string.IsNullOrWhiteSpace(response) ? null : response.Trim();
            }
            catch
            {
                return null; // TODO: Logar erro no visualizador de eventos
            }
        }
        private string GetCachedPublicIp()
        {
            var now = DateTime.UtcNow;

            if (_lastIp != null && _lastIp != "Not Found" && now - _lastFetch < _refreshInterval)
                return _lastIp;

            string? newIp = GetPublicIp();

            if (newIp != null)
            {
                _lastIp = newIp;
                _lastFetch = now;
                return newIp;
            }

            if (_lastIp != null && _lastIp != "Not Found")
            {
                return _lastIp;
            }

            return "Not Found";
        }

        private static string? GetMacAddress()
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus == OperationalStatus.Up &&
                    nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    var bytes = nic.GetPhysicalAddress().GetAddressBytes();
                    // Ignora adaptadores sem endereço físico
                    if (bytes == null || bytes.Length == 0)
                        continue;

                    var mac = BitConverter.ToString(bytes);
                    if (string.IsNullOrWhiteSpace(mac))
                        continue;

                    return mac;
                }
            }
            return null;
        }
    }
}

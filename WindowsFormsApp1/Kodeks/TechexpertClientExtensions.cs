using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Kodeks
{
    public static class TechexpertClientExtensions
    {
        public static async Task<string> GetbparserAsync(this TechexpertClient client, string text)
        {
            string url = $"{client.Endpoint.Address.Uri.AbsoluteUri}/bparser?parse={Uri.EscapeDataString(text)}";
            using (var httpClient = new HttpClient())
            using (var response = await httpClient.GetAsync(url))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }

        public static async Task<string> KClientRemotePath(this TechexpertClient client)
        {
            string url = $"{client.Endpoint.Address.Uri.AbsoluteUri}";
            using (var httpClient = new HttpClient())
            using (var response = await httpClient.GetAsync(url))
            {
                var data = await response.Content.ReadAsStringAsync();
                string pattern = @"Клиент, который следует использовать с этим сервером - (?<path>.[^\s]*)\s";
                return Regex.IsMatch(data, pattern)
                    ? Regex.Match(data, pattern).Groups["path"].Value
                    : null;
            }
        }

        public static async Task RunKodeks(this TechexpertClient client, string nd = null, string mark = null)
        {
            //var startInfo = new ProcessStartInfo();
            //startInfo.FileName = await client.KClientRemotePath();
            //startInfo.WorkingDirectory = Path.GetDirectoryName(startInfo.FileName);
            //startInfo.Arguments = "client.docs.ini";
            //startInfo.UseShellExecute = false;
            //Process.Start(startInfo);
            //d?nd={nd}&mark={mark}

            var kClient = await client.KClientRemotePath();
            var workingDirectory = Path.Combine(Path.GetDirectoryName(kClient), "kassist");
            var fileName = Path.Combine(workingDirectory, "link.exe");

            var arguments = $"kodeks://link/d?nd={nd}";
            if (!string.IsNullOrEmpty(mark))
            {
                arguments += $"&mark={mark}";
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                Arguments = arguments
            };
            Process.Start(startInfo);
        }
    }
}

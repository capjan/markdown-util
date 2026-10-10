using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;

namespace MarkdownUtilTest;

public class ServeCommandIntegrationTest
{
    [Fact]
    public async Task UpdatesMarkdownAssetsAndRemovesGeneratedFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var docs = Path.Combine(root, "Docs");
        var site = Path.Combine(root, "site");
        Directory.CreateDirectory(docs);
        var markdownPath = Path.Combine(docs, "README.md");
        var assetPath = Path.Combine(docs, "diagram.svg");
        File.WriteAllText(markdownPath, "# Before\n\n![Diagram](diagram.svg)");
        File.WriteAllText(assetPath, "asset-v1");

        var port = FindAvailablePort();
        using var process = StartServer(docs, site, port);
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var pageUrl = $"http://127.0.0.1:{port}/index.html";
        var assetUrl = $"http://127.0.0.1:{port}/diagram.svg";

        try
        {
            await WaitUntilAsync(async () =>
            {
                var response = await client.GetAsync(pageUrl);
                if (!response.IsSuccessStatusCode) return false;
                return (await response.Content.ReadAsStringAsync()).Contains("Before", StringComparison.Ordinal);
            }, process);

            Assert.Equal("asset-v1", await client.GetStringAsync(assetUrl));

            File.WriteAllText(markdownPath, "# After\n\n![Diagram](diagram.svg)");
            File.WriteAllText(assetPath, "asset-v2");

            await WaitUntilAsync(async () =>
            {
                var response = await client.GetAsync(pageUrl);
                if (!response.IsSuccessStatusCode) return false;
                return (await response.Content.ReadAsStringAsync()).Contains("After", StringComparison.Ordinal);
            }, process);
            await WaitUntilAsync(async () => await client.GetStringAsync(assetUrl) == "asset-v2", process);

            File.Delete(markdownPath);
            await WaitUntilAsync(async () =>
            {
                using var response = await client.GetAsync(pageUrl);
                return response.StatusCode == HttpStatusCode.NotFound;
            }, process);
            await WaitUntilAsync(async () =>
            {
                using var response = await client.GetAsync(assetUrl);
                return response.StatusCode == HttpStatusCode.NotFound;
            }, process);
        }
        finally
        {
            StopServer(process);
            Directory.Delete(root, recursive: true);
        }
    }

    private static Process StartServer(string docs, string site, int port)
    {
        var projectPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../MarkdownUtil/MarkdownUtil.csproj"));
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = Path.GetDirectoryName(projectPath)!,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add("Release");
        startInfo.ArgumentList.Add("--no-build");
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add("serve");
        startInfo.ArgumentList.Add(docs);
        startInfo.ArgumentList.Add(site);
        startInfo.ArgumentList.Add("--port");
        startInfo.ArgumentList.Add(port.ToString());
        startInfo.ArgumentList.Add("--no-browser");

        var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start the preview process.");
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition, Process process)
    {
        var deadline = DateTime.UtcNow.AddSeconds(12);
        while (DateTime.UtcNow < deadline)
        {
            if (process.HasExited)
                throw new InvalidOperationException($"Preview process exited with code {process.ExitCode}.");

            try
            {
                if (await condition()) return;
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException)
            {
            }

            await Task.Delay(100);
        }

        throw new TimeoutException("Timed out waiting for the preview site to update.");
    }

    private static int FindAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static void StopServer(Process process)
    {
        if (process.HasExited) return;
        process.Kill(entireProcessTree: true);
        process.WaitForExit();
    }
}

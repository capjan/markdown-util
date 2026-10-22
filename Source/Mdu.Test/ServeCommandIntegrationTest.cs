using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;

namespace Mdu.Test;

public class ServeCommandIntegrationTest
{
    [Fact]
    public async Task KeepsAliasPageWhenItsFolderContainsOnlyAssets()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var docs = Path.Combine(root, "Docs");
        var site = Path.Combine(root, "site");
        Directory.CreateDirectory(docs);
        File.WriteAllText(Path.Combine(docs, "README.md"), "# Home\n\n[Guide](Guide.md)");
        File.WriteAllText(Path.Combine(docs, "Guide.md"), "# Guide\n");

        var port = FindAvailablePort();
        using var process = StartServer(docs, site, port);
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var aliasUrl = $"http://127.0.0.1:{port}/Guide/index.html";

        try
        {
            await WaitUntilAsync(async () =>
            {
                using var response = await client.GetAsync(aliasUrl);
                return response.IsSuccessStatusCode;
            }, process);

            Directory.CreateDirectory(Path.Combine(docs, "Guide"));
            File.WriteAllText(Path.Combine(docs, "Guide", "asset.svg"), "<svg />");
            File.WriteAllText(Path.Combine(docs, "Another.md"), "# Another\n");

            await WaitUntilAsync(async () =>
            {
                using var response = await client.GetAsync($"http://127.0.0.1:{port}/Another.html");
                return response.IsSuccessStatusCode &&
                       (await response.Content.ReadAsStringAsync()).Contains("Another", StringComparison.Ordinal);
            }, process);

            using var aliasResponse = await client.GetAsync(aliasUrl);
            Assert.True(aliasResponse.IsSuccessStatusCode);
            Assert.Contains("Guide.html", await aliasResponse.Content.ReadAsStringAsync());
        }
        finally
        {
            StopServer(process);
            Directory.Delete(root, recursive: true);
        }
    }

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

    [Fact]
    public async Task UpdatesPagesSavedThroughTemporaryFilesAndFindsNewPages()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var docs = Path.Combine(root, "Docs");
        var site = Path.Combine(root, "site");
        Directory.CreateDirectory(docs);
        var markdownPath = Path.Combine(docs, "README.md");
        File.WriteAllText(markdownPath, "# Home\n\nBefore");

        var port = FindAvailablePort();
        using var process = StartServer(docs, site, port);
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var pageUrl = $"http://127.0.0.1:{port}/index.html";

        try
        {
            await WaitUntilAsync(async () => await PageContainsAsync(client, pageUrl, "Before"), process);

            // Saves the way JetBrains IDEs do with "safe write": a temporary file replaces the page.
            File.WriteAllText(markdownPath + "___jb_tmp___", "# Home\n\nAfter");
            File.Move(markdownPath, markdownPath + "___jb_old___");
            File.Move(markdownPath + "___jb_tmp___", markdownPath);
            File.Delete(markdownPath + "___jb_old___");
            await WaitUntilAsync(async () => await PageContainsAsync(client, pageUrl, "After"), process);

            Directory.CreateDirectory(Path.Combine(docs, "Guide"));
            File.WriteAllText(Path.Combine(docs, "Guide", "README.md"), "# Guide\n\nNew page");
            await WaitUntilAsync(async () =>
                await PageContainsAsync(client, $"http://127.0.0.1:{port}/Guide/index.html", "New page") &&
                await PageContainsAsync(client, pageUrl, "Guide/index.html"), process);
        }
        finally
        {
            StopServer(process);
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task WatchesOpenApiSpecificationsOutsideTheMarkdownRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var docs = Path.Combine(root, "Docs");
        var api = Path.Combine(root, "api");
        var site = Path.Combine(root, "site");
        Directory.CreateDirectory(docs);
        Directory.CreateDirectory(api);
        File.WriteAllText(Path.Combine(docs, "README.md"), "# Orders\n\n```openapi\n../api/orders.json\n```\n");
        var specificationPath = Path.Combine(api, "orders.json");
        File.WriteAllText(specificationPath, OpenApiSpecification("API Before"));

        var port = FindAvailablePort();
        using var process = StartServer(docs, site, port);
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var pageUrl = $"http://127.0.0.1:{port}/index.html";

        try
        {
            await WaitUntilAsync(async () => await PageContainsAsync(client, pageUrl, "API Before"), process);

            File.WriteAllText(specificationPath, OpenApiSpecification("API After"));

            await WaitUntilAsync(async () => await PageContainsAsync(client, pageUrl, "API After"), process);

            var secondApi = Path.Combine(root, "second-api");
            Directory.CreateDirectory(secondApi);
            var secondSpecificationPath = Path.Combine(secondApi, "orders.json");
            File.WriteAllText(secondSpecificationPath, OpenApiSpecification("API Switched"));
            File.WriteAllText(Path.Combine(docs, "README.md"), "# Orders\n\n```openapi\n../second-api/orders.json\n```\n");
            await WaitUntilAsync(async () => await PageContainsAsync(client, pageUrl, "API Switched"), process);

            File.WriteAllText(secondSpecificationPath, OpenApiSpecification("API Latest"));
            await WaitUntilAsync(async () => await PageContainsAsync(client, pageUrl, "API Latest"), process);
        }
        finally
        {
            StopServer(process);
            Directory.Delete(root, recursive: true);
        }
    }

    private static async Task<bool> PageContainsAsync(HttpClient client, string url, string text)
    {
        using var response = await client.GetAsync(url);
        return response.IsSuccessStatusCode &&
               (await response.Content.ReadAsStringAsync()).Contains(text, StringComparison.Ordinal);
    }

    private static string OpenApiSpecification(string title) => $$"""
        {
          "openapi": "3.1.0",
          "info": { "title": "{{title}}", "version": "1.0" },
          "paths": {}
        }
        """;

    private static Process StartServer(string docs, string site, int port)
    {
        var projectPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../Mdu/Mdu.csproj"));
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

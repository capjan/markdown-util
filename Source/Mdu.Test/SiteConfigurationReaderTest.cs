using Mdu.Service;

namespace Mdu.Test;

public class SiteConfigurationReaderTest
{
    [Fact]
    public void ReadsBrandingColorsAndCopiesConfiguredAssets()
    {
        var root = Path.Combine(Path.GetTempPath(), $"mdu-site-config-test-{Guid.NewGuid():N}");
        var assetDirectory = Path.Combine(root, "assets");
        var output = Path.Combine(root, "site");
        Directory.CreateDirectory(assetDirectory);
        File.WriteAllText(Path.Combine(root, "mdu.yml"), """
            site_name: My Wiki
            logo:
              light: assets/logo-light.svg
              dark: assets/logo-dark.svg
            favicon:
              light: assets/favicon.svg
            colors:
              light:
                background: "#ffffff"
                text: "#222222"
                primary: "#0066cc"
            """);
        foreach (var asset in new[] { "logo-light.svg", "logo-dark.svg", "favicon.svg" })
            File.WriteAllText(Path.Combine(assetDirectory, asset), "<svg />");

        try
        {
            var configuration = SiteConfigurationReader.Read(root);
            SiteConfigurationReader.CopyAssets(root, output, configuration);

            Assert.Equal("My Wiki", configuration.SiteName);
            Assert.Equal("assets/logo-light.svg", configuration.LogoLight);
            Assert.Equal("#0066cc", configuration.Light?.Primary);
            Assert.Equal("assets/logo-dark.svg", configuration.LogoDark);
            Assert.Equal("assets/favicon.svg", configuration.FaviconDark);
            Assert.True(File.Exists(Path.Combine(output, "assets", "logo-light.svg")));
            Assert.True(File.Exists(Path.Combine(output, "assets", "logo-dark.svg")));
            Assert.True(File.Exists(Path.Combine(output, "assets", "favicon.svg")));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}

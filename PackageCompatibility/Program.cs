using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class Program
{
    public static async Task Main(string[] args)
    {
        string[] packages = File.ReadAllLines("packages.txt");

        using (var writer = new StreamWriter("output.csv"))
        {
            writer.WriteLine("Package,CompatibleWithNet8,CompatibleWithNetStandard2,CompatibleWithNetStandard2.1,CompatibleWithNet5,CompatibleWithNet6,LatestDotNetCompatibility");

            foreach (var package in packages)
            {
                try
                {
                    await CheckPackageCompatibility(package, writer);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error checking package {package}: {ex.Message}");
                }
            }
        }
    }

    public static async Task CheckPackageCompatibility(string packageName, StreamWriter writer)
    {
        var url = packageName.StartsWith("DevExpress") 
            ? $"https://api.nuget.org/v3/registration5-semver1/{packageName.ToLowerInvariant()}/index.json"
            : $"https://api.nuget.org/v3/registration5-semver1/{packageName.ToLowerInvariant()}/index.json";

        using (var client = new HttpClient())
        {
            var response = await client.GetStringAsync(url);
            var jObject = JObject.Parse(response);
            var items = jObject["items"][0]["items"].ToObject<JArray>();
            var latestItem = items[items.Count - 1]; // Get the latest version
            var catalogEntry = latestItem["catalogEntry"];

            var version = (string)catalogEntry["version"];
            var packageUrl = (string)catalogEntry["@id"];
            var dependencyGroups = catalogEntry["dependencyGroups"].ToObject<JArray>();

            var compatibleWithNet8 = "No";
            var compatibleWithNetStandard2 = "No";
            var compatibleWithNetStandard2_1 = "No";
            var compatibleWithNet5 = "No";
            var compatibleWithNet6 = "No";
            var latestDotNetCompatibility = "Unknown";

            foreach (var dependencyGroup in dependencyGroups)
            {
                var targetFramework = (string)dependencyGroup["targetFramework"];

                if (targetFramework == "net8.0")
                {
                    compatibleWithNet8 = "Yes";
                }
                if (targetFramework == "netstandard2.0")
                {
                    compatibleWithNetStandard2 = "Yes";
                }
                if (targetFramework == "netstandard2.1")
                {
                    compatibleWithNetStandard2_1 = "Yes";
                }
                if (targetFramework == "net5.0")
                {
                    compatibleWithNet5 = "Yes";
                }
                if (targetFramework == "net6.0")
                {
                    compatibleWithNet6 = "Yes";
                }

                if (targetFramework.StartsWith("net"))
                {
                    latestDotNetCompatibility = targetFramework.Substring(3);
                }
            }

            writer.WriteLine($"{packageName},{compatibleWithNet8},{compatibleWithNetStandard2},{compatibleWithNetStandard2_1},{compatibleWithNet5},{compatibleWithNet6},{latestDotNetCompatibility},{packageUrl}");
        }
    }
}

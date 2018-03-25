using Polly;
using Polly.Wrap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using static Common.Functions.DownloadFile.FileDownloader;
using static DevOps.Build.AppVeyor.GetBuildRecord.BuildRecordGetter;
using static System.IO.Path;

namespace DevOps.Build.Tools.LocalNuGetSource.BeforeBuild
{
    public static class Program
    {
        private static readonly PolicyWrap _pollyWrapper = Policy.Wrap(new Policy[]
            {
                Policy.Handle<HttpRequestException>()
                    // Retry 4 times (exponential backoff for ~30 seconds)
                    .WaitAndRetry(4, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
            });

        public static async Task Main(string[] args)
        {
            var token = args.FirstOrDefault() ?? throw new ArgumentNullException("NamespacePrefix");
            var cacheDirectory = args[1] ?? throw new ArgumentNullException("CacheDirectory");
            var packageUri = args[2] ?? throw new ArgumentNullException("PackageUri");

            // Get list of required NuGet packages
            var packages = new Dictionary<string, string>();

            Console.WriteLine("Finding .csproj...");
            var dir = Environment.GetEnvironmentVariable("APPVEYOR_BUILD_FOLDER");
            var files = Directory.EnumerateFiles(dir, "*.csproj", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                Console.WriteLine($"Found .csproj: {file}");
                Console.WriteLine($"Getting {token} package references...");
                var lines = File.ReadAllLines(file);
                foreach (var line in lines.Where(ln
                    => ln.Contains("PackageReference") && ln.Contains($"\"{token}.")))
                    packages.Add(line);
            }

            // Save each package in local NuGet cache
            await PopulatePackageCache(cacheDirectory, packageUri, packages);
        }

        private static async Task PopulatePackageCache(string cacheDirectory, string packageUri, Dictionary<string, string> packages)
        {
            foreach (var package in packages)
            {
                var name = package.GetFileName();
                var path = Combine(cacheDirectory, name);
                Console.WriteLine($"Looking for package: {name}...");
                if (File.Exists(path)) continue;

                Console.WriteLine($"Caching package: {name}...");
                _pollyWrapper.Execute(() =>
                {
                    try
                    {
                        Download(new Uri($"{packageUri}/{name}"), path);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}...");
                        if (!ex.Message.Contains("404")) throw ex; // and retry
                        // else, swallow exception and continue on
                    }
                });

                Console.WriteLine($"Looking for package dependencies: {package.Key} {package.Value}...");
                var record = await GetBuildRecordAsync(package.Key, package.Value);
                var deps = record?.Dependencies;
                if (!string.IsNullOrEmpty(deps))
                {
                    Console.WriteLine($"Parsing dependencies: {deps}...");
                    var dependencyDict = new Dictionary<string, string>(
                        deps.Split(',').Select(d => d.Split('|'))
                            .Select(each => new KeyValuePair<string, string>(each.First(), each.Last())));
                    await PopulatePackageCache(cacheDirectory, packageUri, dependencyDict);
                }
            }
        }
    }
}

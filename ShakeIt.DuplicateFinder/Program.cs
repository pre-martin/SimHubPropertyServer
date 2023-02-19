using System.Text.Json;
using ShakeIt.DuplicateFinder;

if (args.Length != 1)
{
    Console.WriteLine("Please specify a JSON file to check.");
    Environment.Exit(1);
}

var file = args[0];

try
{
    using var stream = File.OpenRead(file);
    var shakeItSettings = JsonSerializer.Deserialize<ShakeItSettings>(stream);
    if (shakeItSettings == null)
    {
        Console.WriteLine($"Could not deserialize file {file}");
        Environment.Exit(1);
    }

    Console.WriteLine($"Analyzing {shakeItSettings.Profiles.Count} profiles");
    var finder = new DuplicateFinder();
    finder.Analyze(shakeItSettings.Profiles);
}
catch (Exception e)
{
    Console.WriteLine($"Exception while reading file {file}: {e}");
    Environment.Exit(1);
}
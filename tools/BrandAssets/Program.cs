using BrandAssets;

string command = args.Length > 0 ? args[0] : "all";
string repoRoot = FindRepoRoot();
string workDir = Path.Combine(Path.GetTempPath(), "BrandAssets-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(workDir);
try
{
    switch (command)
    {
        case "icons":
            Icons.Build(repoRoot, workDir);
            break;
        case "hero":
            Hero.Build(repoRoot, workDir);
            break;
        case "all":
            Icons.Build(repoRoot, workDir);
            Hero.Build(repoRoot, workDir);
            break;
        default:
            Console.Error.WriteLine("usage: dotnet run --project tools/BrandAssets -- [icons|hero|all]");
            return 2;
    }
    return 0;
}
finally
{
    try { Directory.Delete(workDir, recursive: true); } catch (IOException) { }
}

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AudioCarousel.sln")))
        dir = dir.Parent;
    return dir?.FullName ?? throw new InvalidOperationException("AudioCarousel.sln not found above the tool.");
}

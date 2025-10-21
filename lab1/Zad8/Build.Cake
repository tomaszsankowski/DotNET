var target = Argument("target", "Publish");
var configuration = Argument("configuration", "Release");
var publishDir = Directory("./publish");

Task("Clean")
    .Does(() =>
{
    Information("Czyszczenie katalogów bin i obj...");
    CleanDirectory("./bin");
    CleanDirectory("./obj");
});

Task("Restore")
    .Does(() =>
{
    Information("Przywracanie paczek NuGet...");
    DotNetRestore("./Zad8.csproj");
    DotNetRestore("../Ztm.Tests/Ztm.Tests.csproj");
});

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    Information("Budowanie projektu głównego...");
    DotNetBuild("./Zad8.csproj", new DotNetBuildSettings
    {
        Configuration = configuration
    });

    Information("Budowanie projektu testowego...");
    DotNetBuild("../Ztm.Tests/Ztm.Tests.csproj", new DotNetBuildSettings
    {
        Configuration = configuration
    });
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    Information("Uruchamianie testów jednostkowych...");
    DotNetTest("../Ztm.Tests/Ztm.Tests.csproj", new DotNetTestSettings
    {
        Configuration = configuration
    });
});

Task("Clean-Publish")
    .IsDependentOn("Test")
    .Does(() =>
{
    Information("Czyszczenie katalogu publikacji...");
    CleanDirectory(publishDir);
});

Task("Publish")
    .IsDependentOn("Clean-Publish")
    .Does(() =>
{
    CreateDirectory(publishDir);

    Information("Publikacja projektu głównego do {0}...", publishDir);
    DotNetPublish("./Zad8.csproj", new DotNetPublishSettings
    {
        Configuration = configuration,
        OutputDirectory = publishDir
    });

    Information("Projekt został opublikowany pomyślnie!");
});

RunTarget(target);

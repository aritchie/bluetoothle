//#tool XamarinComponent
//#addin Cake.Xamarin
#addin Cake.FileHelpers
#tool nunit.consolerunner
#tool gitlink

var target = Argument("target", "publish");
var version = Argument("version", "1.0.1");

Setup(x =>
{
    DeleteFiles("./*.nupkg");
    DeleteFiles("./output/*.*");

	if (!DirectoryExists("./output"))
		CreateDirectory("./output");
});

Task("build")
	.Does(() =>
{
	NuGetRestore("./lib.sln");
	DotNetBuild("./lib.sln", x => x
        .SetConfiguration("Release")
        .SetVerbosity(Verbosity.Minimal)
        .WithProperty("TreatWarningsAsErrors", "false")
    );
});


Task("nuget")
	.IsDependentOn("build")
	.Does(() =>
{
    NuGetPack(new FilePath("./nuspec/Acr.Ble.nuspec"), new NuGetPackSettings());
	MoveFiles("./*.nupkg", "./output");
});


Task("tests")
    .IsDependentOn("build")
    .Does(() =>
{
    //NUnit3("./**/*.Tests.dll");
});


Task("publish")
    .IsDependentOn("tests")
    .IsDependentOn("nuget")
    .Does(() =>
{
    GitLink("./", new GitLinkSettings
    {
         RepositoryUrl = "https://github.com/aritchie/bluetoothle",
         Branch = "master"
    });
    NuGetPush("./output/*.nupkg", new NuGetPushSettings
    {
        Source = "http://www.nuget.org/api/v2/package",
        Verbosity = NuGetVerbosity.Detailed
    });
    CopyFiles("./output/*.nupkg", "c:\\users\\allan.ritchie\\dropbox\\nuget");
});

RunTarget(target);
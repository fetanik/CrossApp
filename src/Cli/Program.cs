using System.Runtime.InteropServices;
using System.Text.Json;

var osDescription = RuntimeInformation.OSDescription;
var osVersion = Environment.OSVersion.ToString();
var processArchitecture = RuntimeInformation.ProcessArchitecture.ToString();
var clrVersion = Environment.Version.ToString();
var runtime = RuntimeInformation.FrameworkDescription;
var appDirectory = AppContext.BaseDirectory;
var currentDirectory = Environment.CurrentDirectory;
var domain = "Бібліотека (видання, примірники, читачі, видачі)";

if (args.Contains("--json"))
{
    var info = new
    {
        Student = "Фесюк Тетяна, група ФЕІ-32",
        OSDescription = osDescription,
        OSVersion = osVersion,
        ProcessArchitecture = processArchitecture,
        CLRVersion = clrVersion,
        Runtime = runtime,
        AppDirectory = appDirectory,
        CurrentDirectory = currentDirectory,
        Domain = domain
    };

    Console.WriteLine(JsonSerializer.Serialize(info));
}
else
{
    Console.WriteLine("CrossApp – практикум з крос-платформного програмування");
    Console.WriteLine("Студент: Фесюк Тетяна, група ФЕІ-32");
    Console.WriteLine(new string('-', 52));

    Console.WriteLine($"ОС (OSDescription) : {osDescription}");
    Console.WriteLine($"ОС (Environment) : {osVersion}");
    Console.WriteLine($"Архітектура процесу : {processArchitecture}");
    Console.WriteLine($"Версія .NET (CLR) : {clrVersion}");
    Console.WriteLine($"Runtime : {runtime}");
    Console.WriteLine($"Каталог застосунку : {appDirectory}");
    Console.WriteLine($"Поточний каталог : {currentDirectory}");

    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"Предметна область: {domain}");
}
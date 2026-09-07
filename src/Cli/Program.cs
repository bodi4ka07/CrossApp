using System.Runtime.InteropServices;
using System.Text.Json;

var info = new
{
    Greeting = "CrossApp – практикум з крос-платформного програмування",
    Student = "Жегістовський Богдан, група ФЕІ-33",
    OSDescription = RuntimeInformation.OSDescription,
    OSVersion = Environment.OSVersion.ToString(),
    ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
    ClrVersion = Environment.Version.ToString(),
    Runtime = RuntimeInformation.FrameworkDescription,
    AppBaseDirectory = AppContext.BaseDirectory,
    CurrentDirectory = Environment.CurrentDirectory,
    Domain = "Бібліотека (видання, примірники, читачі, видачі)"
};

if (args.Contains("--json"))
{
    Console.WriteLine(JsonSerializer.Serialize(info));
}
else
{
    Console.WriteLine(info.Greeting);
    Console.WriteLine($"Студент: {info.Student}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"ОС (OSDescription)  : {info.OSDescription}");
    Console.WriteLine($"ОС (Environment)    : {info.OSVersion}");
    Console.WriteLine($"Архітектура процесу : {info.ProcessArchitecture}");
    Console.WriteLine($"Версія .NET (CLR)   : {info.ClrVersion}");
    Console.WriteLine($"Runtime             : {info.Runtime}");
    Console.WriteLine($"Каталог застосунку  : {info.AppBaseDirectory}");
    Console.WriteLine($"Поточний каталог    : {info.CurrentDirectory}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"Предметна область: {info.Domain}");
}
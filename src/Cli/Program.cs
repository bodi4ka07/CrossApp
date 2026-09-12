using Core;
using System.Text.Json;

Console.OutputEncoding = System.Text.Encoding.UTF8;

EnvironmentReport report = EnvironmentInfo.Collect();

if (args.Contains("--json"))
{
    Console.WriteLine(JsonSerializer.Serialize(report));
}
else
{
    Console.WriteLine("CrossApp – практикум з крос-платформного програмування");
    Console.WriteLine("Студент: Жегістовський Богдан, група ФЕІ-33");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"ОС              : {report.OsDescription}");
    Console.WriteLine($"Runtime         : {report.FrameworkDescription}");
    Console.WriteLine($"Архітектура     : {report.ProcessArchitecture}");
    Console.WriteLine($"RID (визначено) : {report.DetectedRid}");
    Console.WriteLine($"RID (від .NET)  : {report.ReportedRid}");
    Console.WriteLine($"Каталог         : {report.BaseDirectory}");
    Console.WriteLine($"Поточний каталог: {report.CurrentDirectory}");
    Console.WriteLine($"Нотатка збірки  : {report.BuildNote}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"Предметна область: {report.Domain}");
}
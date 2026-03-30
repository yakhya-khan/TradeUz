using System;
using System.IO;
using System.Linq;
using System.Reflection;

var baseDir = @"C:\Users\YakhYa\.nuget\packages";
var deps = new[]
{
    Path.Combine(baseDir, "avalonia", "11.3.12", "lib", "net8.0", "Avalonia.Base.dll"),
    Path.Combine(baseDir, "avalonia", "11.3.12", "lib", "net8.0", "Avalonia.Controls.dll"),
    Path.Combine(baseDir, "avalonia", "11.3.12", "lib", "net8.0", "Avalonia.Markup.Xaml.dll"),
    Path.Combine(baseDir, "avalonia.skia", "11.3.12", "lib", "net8.0", "Avalonia.Skia.dll"),
    Path.Combine(baseDir, "svg.controls.skia.avalonia", "11.3.9.2", "lib", "net8.0", "Svg.Controls.Skia.Avalonia.dll")
};

foreach (var dep in deps)
    Assembly.LoadFrom(dep);

var asm = Assembly.LoadFrom(deps.Last());
foreach (var type in asm.GetExportedTypes().Where(t => t.FullName != null && t.FullName.Contains("Svg")))
    Console.WriteLine(type.FullName);

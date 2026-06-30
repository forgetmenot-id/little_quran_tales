using System;
using System.IO;

namespace LittleQuranTales.Services;

public static class LogHelper
{
    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LittleQuranTales");
    private static readonly string TracePath = Path.Combine(Dir, "trace.log");
    private static readonly string CrashPath = Path.Combine(Dir, "crash.log");

    public static void Trace(string msg)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            var line = $"{DateTime.Now:HH:mm:ss.fff} {msg}";
            File.AppendAllText(TracePath, line + "\n");
            Console.Error.WriteLine("[TRACE] " + line);
        }
        catch { }
    }

    public static void WriteCrash(Exception ex)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            var text = $"=== CRASH at {DateTime.Now} ===\n{ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}\n";
            File.WriteAllText(CrashPath, text);
            Console.Error.WriteLine("[CRASH] " + text);
        }
        catch { }
    }
}

using System;
using System.IO;
using System.Text;

public class Logger
{
    private readonly string _logFilePath;
    private readonly StringBuilder _logBuilder;

    public Logger(string logFilePath)
    {
        _logFilePath = logFilePath;
        Directory.CreateDirectory(_logFilePath);

        _logBuilder = new StringBuilder();
    }

    // Method untuk mencatat log
    public void Log(string message, string title)
    {

        try
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{title}] {message}";
            _logBuilder.AppendLine(logEntry);

            // Simpan log ke file setiap kali ada log baru
            SaveLog();
        }
        catch (Exception ex)
        { }


    }

    // Method untuk menyimpan log ke file
    private void SaveLog()
    {
        try
        {
            File.AppendAllText(_logFilePath, _logBuilder.ToString());
            _logBuilder.Clear(); // Kosongkan StringBuilder setelah menulis ke file
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Gagal menulis log ke file: {ex.Message}");
        }
    }
}

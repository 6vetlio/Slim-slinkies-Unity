using System;
using System.IO;
using UnityEngine;

public static class RuntimeFileLogger
{
    private static readonly object SyncRoot = new object();
    private static string logFilePath;
    private static StreamWriter writer;
    private static bool initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;

        string logsDirectory = Path.Combine(Application.persistentDataPath, "Logs");
        Directory.CreateDirectory(logsDirectory);

        logFilePath = Path.Combine(logsDirectory, "runtime-log-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");
        writer = new StreamWriter(logFilePath, true)
        {
            AutoFlush = true
        };

        Application.logMessageReceivedThreaded += HandleLogMessage;
        AppDomain.CurrentDomain.ProcessExit += HandleProcessExit;

        WriteLine("SESSION START " + DateTime.Now.ToString("O"));
        WriteLine("persistentDataPath=" + Application.persistentDataPath);
        Debug.Log("RuntimeFileLogger: writing logs to " + logFilePath);
    }

    private static void HandleLogMessage(string condition, string stackTrace, LogType type)
    {
        string timestamp = DateTime.Now.ToString("O");
        string entry = "[" + timestamp + "] [" + type + "] " + condition;

        if (!string.IsNullOrWhiteSpace(stackTrace))
        {
            entry += Environment.NewLine + stackTrace;
        }

        WriteLine(entry);
    }

    private static void HandleProcessExit(object sender, EventArgs e)
    {
        Shutdown();
    }

    private static void WriteLine(string message)
    {
        lock (SyncRoot)
        {
            if (writer == null)
            {
                return;
            }

            writer.WriteLine(message);
        }
    }

    private static void Shutdown()
    {
        lock (SyncRoot)
        {
            if (writer == null)
            {
                return;
            }

            writer.WriteLine("SESSION END " + DateTime.Now.ToString("O"));
            writer.Flush();
            writer.Dispose();
            writer = null;
        }
    }
}

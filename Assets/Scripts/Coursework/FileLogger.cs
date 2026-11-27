// ШІ: ChatGPT – Створив FileLogger.
// Змінено: фікс кода.
using System;
using System.IO;
using UnityEngine;

namespace Coursework2D
{
    public static class FileLogger
    {
        private static readonly object Sync = new object();
        private static string _logPath;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitOnMainThread()
        {
            CaptureMainThreadPath();
        }

        public static string CaptureMainThreadPath()
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, "coursework.log");
                lock (Sync)
                {
                    _logPath = path;
                }
                return path;
            }
            catch
            {
                return _logPath;
            }
        }

        public static void Write(string message, Exception exception = null)
        {
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}" + (exception == null ? "" : " | " + exception);
            string path;
            lock (Sync)
            {
                path = _logPath;
            }

            if (!string.IsNullOrEmpty(path))
            {
                lock (Sync)
                {
                    try
                    {
                        string dir = Path.GetDirectoryName(path);
                        if (!string.IsNullOrEmpty(dir))
                            Directory.CreateDirectory(dir);
                        File.AppendAllText(path, line + Environment.NewLine);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Не вдалося записати журнал: " + e.Message);
                    }
                }
            }

            Debug.Log(line);
        }
    }
}
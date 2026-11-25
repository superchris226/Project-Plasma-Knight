using System;
using System.IO;
using UnityEngine;

namespace Coursework2D
{
    public static class FileLogger
    {
        private static readonly object Sync = new object();
        private static string LogPath => Path.Combine(Application.persistentDataPath, "coursework.log");
        public static void Write(string message, Exception exception = null)
        {
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}" + (exception == null ? "" : " | " + exception);
            lock (Sync) { try { File.AppendAllText(LogPath, line + Environment.NewLine); } catch (Exception e) { Debug.LogError("Не удалось записать журнал: " + e.Message); } }
            Debug.Log(line);
        }
    }
}

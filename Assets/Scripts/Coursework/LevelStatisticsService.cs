using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace Coursework2D
{
    [Serializable]
    public class LevelRunStatistic
    {
        public string LevelName;
        public float TimeSeconds;
        public int Attempts;
        public int DefeatedEnemies;
        public string CompletedAt;
    }

    [Serializable, XmlRoot("LevelStatistics")]
    public class LevelStatisticsDocument
    {
        [XmlElement("Run")] public List<LevelRunStatistic> Runs = new List<LevelRunStatistic>();
    }

    public static class LevelStatisticsService
    {
        public static string DefaultPath => Path.Combine(Application.persistentDataPath, "level-statistics.xml");
        public static void Export(LevelStatisticsDocument document, string path = null)
        {
            path = string.IsNullOrWhiteSpace(path) ? DefaultPath : path;
            try
            {
                if (document == null) throw new ArgumentNullException(nameof(document));
                var serializer = new XmlSerializer(typeof(LevelStatisticsDocument));
                using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write)) serializer.Serialize(stream, document);
            }
            catch (Exception ex) { FileLogger.Write("Помилка експорту статистики XML.", ex); throw; }
        }

        public static LevelStatisticsDocument Import(string path = null)
        {
            path = string.IsNullOrWhiteSpace(path) ? DefaultPath : path;
            try
            {
                var serializer = new XmlSerializer(typeof(LevelStatisticsDocument));
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                    return (LevelStatisticsDocument)serializer.Deserialize(stream);
            }
            catch (Exception ex) { FileLogger.Write("Помилка імпорту статистики XML.", ex); throw; }
        }
    }
}
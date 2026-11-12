#nullable enable
using System.IO;
using System.Text;
using AnomalyPath.Core.Data;
using UnityEditor;

namespace AnomalyPath.Editor
{
    /// <summary>
    /// Готовит пакет данных для сборки: один JSON в <c>Assets/Resources</c>. На Android папки
    /// данных в APK нет, поэтому <c>GameBootstrap</c> читает игру из этого пакета.
    /// </summary>
    public static class DataPackBuilder
    {
        /// <summary>Папка ресурсов сборки.</summary>
        public const string ResourceDirectory = "Assets/Resources";

        /// <summary>Путь файла пакета внутри проекта.</summary>
        public const string PackPath = ResourceDirectory + "/" + DataPack.ResourceName + ".json";

        /// <summary>Собирает пакет из папки данных и импортирует его в проект.</summary>
        /// <param name="dataRoot">Папка данных проекта, обычно <c>Assets/Data</c>.</param>
        /// <returns>Путь к записанному файлу пакета.</returns>
        /// <exception cref="DirectoryNotFoundException">Папка данных не найдена.</exception>
        public static string Write(string dataRoot)
        {
            if (!Directory.Exists(dataRoot))
            {
                throw new DirectoryNotFoundException($"Папка данных не найдена: {dataRoot}");
            }

            Directory.CreateDirectory(ResourceDirectory);
            var text = DataPack.Serialize(new FileSystemDataSource(dataRoot));
            File.WriteAllText(PackPath, text, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(PackPath, ImportAssetOptions.ForceUpdate);
            return PackPath;
        }

        /// <summary>Удаляет пакет из проекта: в редакторе и Windows-сборке данные берутся из папки.</summary>
        /// <returns><c>true</c>, если пакет был и удалён.</returns>
        public static bool Delete()
        {
            if (!File.Exists(PackPath))
            {
                return false;
            }

            AssetDatabase.DeleteAsset(PackPath);
            if (Directory.Exists(ResourceDirectory) && Directory.GetFileSystemEntries(ResourceDirectory).Length == 0)
            {
                Directory.Delete(ResourceDirectory);
                if (File.Exists(ResourceDirectory + ".meta"))
                {
                    File.Delete(ResourceDirectory + ".meta");
                }

                AssetDatabase.Refresh();
            }

            return true;
        }

        /// <summary>Проверяет, что пакет лежит в проекте.</summary>
        /// <returns><c>true</c>, если файл пакета существует.</returns>
        public static bool Exists() => File.Exists(PackPath);
    }
}

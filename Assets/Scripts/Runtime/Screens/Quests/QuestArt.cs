#nullable enable
using AnomalyPath.Core.Quests;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;

namespace AnomalyPath.Runtime.Screens.Quests
{
    /// <summary>
    /// Картинка задания для доски объявлений: сначала арт из поля <c>art</c>, затем заглушка
    /// по типу цели. Коды заглушек — имена файлов в папке <c>Assets/Art/Quest</c>
    /// (каталог спрайтов даёт им id с префиксом <c>quests/</c>).
    /// </summary>
    public static class QuestArt
    {
        /// <summary>Префикс id в каталоге спрайтов для картинок заданий.</summary>
        public const string Prefix = "quests/";

        /// <summary>Заглушка, когда по типу цели картинки тоже нет.</summary>
        public const string Default = Prefix + "default";

        /// <summary>Ищет картинку задания.</summary>
        /// <param name="host">Хост интерфейса с каталогом спрайтов.</param>
        /// <param name="template">Шаблон задания.</param>
        /// <returns>Спрайт или <c>null</c>, если в каталоге нет ни арта, ни заглушки.</returns>
        public static Sprite? Resolve(IUiHost host, QuestTemplate template)
        {
            if (!string.IsNullOrWhiteSpace(template.Art) &&
                host.Sprites.TryGet(template.Art!, out var art) &&
                art != null)
            {
                return art;
            }

            if (host.Sprites.TryGet(IdFor(template.Type), out var placeholder) && placeholder != null)
            {
                return placeholder;
            }

            return host.Sprites.TryGet(Default, out var fallback) ? fallback : null;
        }

        /// <summary>Id заглушки по типу цели задания.</summary>
        /// <param name="type">Тип цели.</param>
        /// <returns>Id в каталоге спрайтов вида <c>quests/killMutants</c>.</returns>
        public static string IdFor(QuestType type) => Prefix + QuestTypeCodec.ToCode(type);
    }
}

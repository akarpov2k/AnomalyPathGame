#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Runtime.Screens.Menu;
using UnityEngine;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>Часть класса <see cref="MenuPrefabsScreenshot"/>: кадр экрана авторов.</summary>
    public static partial class MenuPrefabsScreenshot
    {
        /// <summary>
        /// Ставит титры так, чтобы в кадр попали их первые строки: в игре они идут сами,
        /// а в batch-режиме Update не работает, поэтому смещение задаётся вручную.
        /// </summary>
        /// <param name="panel">Экземпляр префаба экрана авторов.</param>
        /// <param name="texts">Каталог текстов.</param>
        /// <param name="pageSize">Размер страницы сохранений; экрану авторов не нужен.</param>
        private static void FillAuthors(GameObject panel, IGameTexts texts, int pageSize)
        {
            var scroller = panel.GetComponentInChildren<CreditsScroller>(true);
            if (scroller == null || scroller.Track == null || scroller.Viewport == null)
            {
                throw new InvalidOperationException("В префабе авторов нет титров");
            }

            scroller.Restart();
            scroller.Seek(scroller.Viewport.rect.height - scroller.Track.sizeDelta.y);
            scroller.Stop();
            Rebuild(panel);
        }
    }
}

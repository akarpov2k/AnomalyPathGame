#nullable enable
using System;
using UnityEditor;
using UnityEngine;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Пересобирает все статические скриншоты экранов в одном batch-сеансе.
    /// Кадры игрового похода снимаются PlayMode-тестом отдельно.
    /// </summary>
    public static class ScreenshotRecache
    {
        /// <summary>Перекеширует все скриншоты из меню.</summary>
        [MenuItem("Tools/AnomalyPath/UI/Перекешировать все скриншоты")]
        private static void CaptureFromMenu() => CaptureAll();

        /// <summary>Точка входа batch-режима.</summary>
        public static void CaptureFromBatch()
        {
            try
            {
                CaptureAll();
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Снимает все статические экраны.</summary>
        public static void CaptureAll()
        {
            MenuPrefabsScreenshot.CaptureAll();
            LocationSceneScreenshot.CaptureAll();
            LocationSceneScreenshot.CaptureStages();
            HubScreenScreenshot.CaptureAll();
            MapScreenScreenshot.CaptureAll();
            TradeScreenScreenshot.CaptureAll();
            TraderDialogScreenshot.CaptureAll();
            CombatScreenScreenshot.CaptureAll();
            DialogPrefabsScreenshot.CaptureAll();
            MinigameScreensScreenshot.CaptureAll();
            QuestScreensScreenshot.CaptureAll();
            RewardScreenScreenshot.CaptureAll();
            TalentScreenScreenshot.CaptureAll();
            Debug.Log("AnomalyPath: скриншоты экранов перекешированы");
        }
    }
}

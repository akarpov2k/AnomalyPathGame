#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using AnomalyPath.Core;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Dialog;
using AnomalyPath.Runtime.Ui.Core;
using AnomalyPath.Runtime.Ui.Dialogs;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Снимает префабы модальных окон в PNG: затемнение, окна, подписи и динамические строки
    /// заполняются образцами из каталога. Файлы — `Logs/Screenshots`.
    /// </summary>
    public static class DialogPrefabsScreenshot
    {
        private const string Folder = "Assets/Prefabs/Dialogs";
        private const string OutputFolder = "Logs/Screenshots";
        private const string DataRoot = "Assets/Data";
        private const int Width = 1920;
        private const int Height = 1080;

        /// <summary>Пункт меню: снять скриншоты модальных окон.</summary>
        [MenuItem("Tools/AnomalyPath/UI/Скриншоты модальных окон")]
        public static void CaptureFromMenu() => CaptureAll();

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

        /// <summary>Снимает все модальные окна в <c>Logs/Screenshots</c>.</summary>
        public static void CaptureAll()
        {
            Directory.CreateDirectory(OutputFolder);
            var result = Preflight.Run(DataRoot);
            if (!result.CanStart || result.Services == null)
            {
                throw new InvalidOperationException(result.Report.Summary);
            }

            var texts = result.Services.Resolve<IGameTexts>();
            Capture(Folder + "/ConfirmDialog.prefab", "dialog-confirm", texts, instance =>
            {
                SetText(instance, "Window/Title", UiText.Get(texts, UiTextKey.Load_Delete_Title));
                SetText(instance, "Window/Message",
                    UiText.Format(texts, UiTextKey.Load_Delete_Question, "Новое сохранение 1"));
            });
            Capture(Folder + "/ExitDialog.prefab", "dialog-exit", texts, _ => { });
            Capture(Folder + "/RestDialog.prefab", "dialog-rest", texts, instance => FillRest(instance, texts));
            Capture(Folder + "/DialogOverlay.prefab", "dialog-menu", texts, instance =>
                Overlay(instance, texts).ShowMenu(new[]
                {
                    DialogOverlayView.MenuTalk,
                    DialogOverlayView.MenuTrade,
                    DialogOverlayView.MenuFarewell
                }));
            Capture(Folder + "/DialogOverlay.prefab", "dialog-talk", texts, instance =>
            {
                var view = Overlay(instance, texts);
                view.ShowTalk(
                    texts.Get("actor.trd_hub_smuggler.name"),
                    texts.Get("dialog.dlg_trd_hub_smuggler.root"),
                    SampleOptions());
                PrepareScroll(view);
            });
            Debug.Log($"AnomalyPath: кадры модальных окон в {OutputFolder}");
        }

        private static DialogOverlayView Overlay(GameObject instance, IGameTexts texts)
        {
            var view = instance.GetComponent<DialogOverlayView>();
            if (view == null)
            {
                throw new InvalidOperationException("В префабе окна диалога нет DialogOverlayView");
            }

            view.Bind(
                texts,
                new[]
                {
                    new DialogOverlayView.MenuEntry(DialogOverlayView.MenuTalk, UiTextKey.Dialog_Talk, () => { }),
                    new DialogOverlayView.MenuEntry(DialogOverlayView.MenuTrade, UiTextKey.Dialog_Trade, () => { }),
                    new DialogOverlayView.MenuEntry(DialogOverlayView.MenuFarewell, UiTextKey.Dialog_Farewell, () => { })
                },
                _ => { },
                blockClicks: false);
            return view;
        }

        private static IReadOnlyList<DialogOptionView> SampleOptions()
        {
            return new List<DialogOptionView>
            {
                new DialogOptionView("s1_std", "dialog.dlg_trd_hub_smuggler.opt.s1_std", true),
                new DialogOptionView("s1_hard", "dialog.dlg_trd_hub_smuggler.opt.s1_hard", true),
                new DialogOptionView("s1_ask", "dialog.dlg_trd_hub_smuggler.opt.s1_ask", true),
                new DialogOptionView("s1_no", "dialog.dlg_trd_hub_smuggler.opt.s1_no", true),
                new DialogOptionView("bye", "dialog.common.bye", true)
            };
        }

        private static void FillRest(GameObject instance, IGameTexts texts)
        {
            SetText(instance, "Window/RestStaminaText",
                UiText.Format(texts, UiTextKey.Rest_Stamina, 42, 100));
            SetText(instance, "Window/RestHoursText",
                UiText.Format(texts, UiTextKey.Rest_Hours, 6));
            SetText(instance, "Window/RestRecoveryText",
                UiText.Format(texts, UiTextKey.Rest_Recovery, 60));
            SetText(instance, "Window/RestRiskText",
                UiText.Format(texts, UiTextKey.Rest_Risk, 20));
            var slider = Find(instance, "Window/RestHoursSlider")?.GetComponent<Slider>();
            if (slider != null)
            {
                slider.minValue = 0f;
                slider.maxValue = 10f;
                slider.value = 6f;
            }
        }

        private static void SetText(GameObject instance, string path, string value)
        {
            var target = Find(instance, path);
            var label = target?.GetComponent<Text>();
            if (label != null)
            {
                label.text = value;
            }
        }

        private static GameObject? Find(GameObject instance, string path)
        {
            var target = instance.transform.Find(path);
            return target != null ? target.gameObject : null;
        }

        private static void PrepareScroll(DialogOverlayView view)
        {
            Canvas.ForceUpdateCanvases();
            var scroll = view.PhraseScroll;
            if (scroll.Content == null || scroll.Bar == null)
            {
                return;
            }

            var content = scroll.Content.rect.height;
            var viewport = Mathf.Max(1f, scroll.ViewportHeight);
            var visible = content > viewport;
            scroll.Bar.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            scroll.Bar.size = Mathf.Clamp01(viewport / content);
            scroll.Bar.value = 1f;
        }

        private static void Capture(
            string prefabPath,
            string fileName,
            IGameTexts texts,
            Action<GameObject> fill)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Нет префаба {prefabPath}");
            }

            var cameraObject = new GameObject("CaptureCamera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = Height / 2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.23f, 0.28f, 0.26f);
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;

            var canvasObject = new GameObject("CaptureCanvas", typeof(Canvas));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.sizeDelta = new Vector2(Width, Height);
            canvasRect.position = Vector3.zero;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvasObject.transform);
            instance.SetActive(true);
            foreach (var label in instance.GetComponentsInChildren<LocalizedText>(true))
            {
                label.Apply(texts);
            }

            fill(instance);
            Rebuild(instance);

            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
            texture.Apply();
            RenderTexture.active = null;
            camera.targetTexture = null;

            var path = Path.Combine(OutputFolder, fileName + ".png");
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(canvasObject);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            Debug.Log($"AnomalyPath: скриншот {path}");
        }

        private static void Rebuild(GameObject panel)
        {
            foreach (var group in panel.GetComponentsInChildren<LayoutGroup>(true))
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)group.transform);
            }

            foreach (var fitter in panel.GetComponentsInChildren<ContentSizeFitter>(true))
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)fitter.transform);
            }

            Canvas.ForceUpdateCanvases();
        }
    }
}

#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using AnomalyPath.Core;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Dialog;
using AnomalyPath.Runtime.Screens.Game.Hub;
using AnomalyPath.Runtime.Ui.Core;
using AnomalyPath.Runtime.Ui.Dialogs;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Снимает кадры диалога торговца по прототипу `.prototypes/диалог.penpot`:
    /// меню разделов и окно разговора с ответом торговца и фразами игрока.
    /// Кадры ставят префаб `Assets/Prefabs/Dialogs/DialogOverlay.prefab` поверх картинки комнаты.
    /// </summary>
    public static class TraderDialogScreenshot
    {
        private const string OutputFolder = "Logs/Screenshots";
        private const string DataRoot = "Assets/Data";
        private const string InteriorPath = "Assets/Art/Backgrounds/bg_hub_house.png";
        private const string MerchantPath = "Assets/Art/Portraits/trd_hub_smuggler_full.png";
        private const string CounterPath = "Assets/Art/Hub/hub_counter.png";
        private const string IconsFolder = "Assets/Art/Icons";
        private const string OverlayPrefabPath = "Assets/Prefabs/Dialogs/DialogOverlay.prefab";
        private const int Width = 1920;
        private const int Height = 1080;

        /// <summary>Значки нижнего ряда комнаты: слева шестерёнка и выбор торговца, справа ремонт и рюкзак.</summary>
        private const float IconBottom = 40f;
        private const float IconSide = 40f;
        private const float IconSize = 112f;
        private const float TradersLeft = IconSide + IconSize + 16f;
        private const float CounterHeight = 155.5f;

        /// <summary>Размер и верх фигуры торговца — как в префабе комнаты.</summary>
        private const float MerchantWidth = 700f;
        private const float MerchantHeight = 1262f;
        private const float MerchantTop = 200f;

        /// <summary>Пункт меню: снять кадры диалога торговца.</summary>
        [MenuItem("Tools/AnomalyPath/UI/Скриншоты диалога торговца")]
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

        /// <summary>Снимает меню разделов и окно разговора в <c>Logs/Screenshots</c>.</summary>
        public static void CaptureAll()
        {
            Directory.CreateDirectory(OutputFolder);
            var result = Preflight.Run(DataRoot);
            if (!result.CanStart || result.Services == null)
            {
                throw new InvalidOperationException(result.Report.Summary);
            }

            var texts = result.Services.Resolve<IGameTexts>();
            Capture("hub-dialog", texts, view =>
                view.ShowMenu(new[] { "DialogTalk", "DialogTrade", "DialogFarewell" }));
            Capture("hub-dialog-talk", texts, view =>
            {
                view.ShowTalk(
                    texts.Get("actor.trd_hub_smuggler.name"),
                    texts.Get("dialog.dlg_trd_hub_smuggler.root"),
                    SampleOptions());
                PrepareScroll(view);
            });
            Debug.Log($"AnomalyPath: кадры диалога торговца в {OutputFolder}");
        }

        private static IReadOnlyList<DialogOptionView> SampleOptions()
        {
            return new List<DialogOptionView>
            {
                new DialogOptionView("s1_std", "dialog.dlg_trd_hub_smuggler.opt.s1_std", true),
                new DialogOptionView("s1_hard", "dialog.dlg_trd_hub_smuggler.opt.s1_hard", true),
                new DialogOptionView("s1_ask", "dialog.dlg_trd_hub_smuggler.opt.s1_ask", true),
                new DialogOptionView("s1_no", "dialog.dlg_trd_hub_smuggler.opt.s1_no", true),
                new DialogOptionView("s1_active", "dialog.dlg_trd_hub_smuggler.opt.s1_active", true),
                new DialogOptionView("s1_ready", "dialog.dlg_trd_hub_smuggler.opt.s1_ready", true),
                new DialogOptionView("s2", "dialog.dlg_trd_hub_smuggler.opt.s2", true),
                new DialogOptionView("bye", "dialog.common.bye", true)
            };
        }

        private static void AddCounter(Transform canvas)
        {
            var counter = new GameObject("Counter", typeof(RectTransform), typeof(Image));
            counter.transform.SetParent(canvas, false);
            var rect = (RectTransform)counter.transform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = new Vector2(0f, CounterHeight);
            var image = counter.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CounterPath);
            image.preserveAspect = false;
            image.raycastTarget = false;
        }

        private static void AddIcon(Transform canvas, string name, string spriteId, Vector2 position, bool left)
        {
            var icon = new GameObject(name, typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(canvas, false);
            var rect = (RectTransform)icon.transform;
            var anchor = left ? new Vector2(0f, 0f) : new Vector2(1f, 0f);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(IconSize, IconSize);
            var image = icon.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(IconsFolder + "/" + spriteId + ".png");
            image.preserveAspect = true;
            image.raycastTarget = false;
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

        private static void Capture(string fileName, IGameTexts texts, Action<DialogOverlayView> fill)
        {
            var cameraObject = new GameObject("CaptureCamera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = Height / 2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;

            var canvasObject = new GameObject("CaptureCanvas", typeof(Canvas));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.sizeDelta = new Vector2(Width, Height);
            canvasRect.position = Vector3.zero;

            var background = new GameObject("Interior", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(canvasObject.transform, false);
            var backgroundRect = (RectTransform)background.transform;
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            var backgroundImage = background.GetComponent<Image>();
            backgroundImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(InteriorPath);
            backgroundImage.color = Color.white;
            backgroundImage.preserveAspect = false;

            var merchant = new GameObject("Merchant", typeof(RectTransform), typeof(Image));
            merchant.transform.SetParent(canvasObject.transform, false);
            var merchantRect = (RectTransform)merchant.transform;
            merchantRect.anchorMin = new Vector2(0.5f, 1f);
            merchantRect.anchorMax = new Vector2(0.5f, 1f);
            merchantRect.pivot = new Vector2(0.5f, 1f);
            merchantRect.anchoredPosition = new Vector2(0f, -MerchantTop);
            merchantRect.sizeDelta = new Vector2(MerchantWidth, MerchantHeight);
            var merchantImage = merchant.GetComponent<Image>();
            merchantImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(MerchantPath);
            merchantImage.color = HubPalette.MerchantTint;
            merchantImage.preserveAspect = true;
            merchantImage.raycastTarget = false;

            AddCounter(canvasObject.transform);
            AddIcon(canvasObject.transform, "Gear", "ui_hub_gear", new Vector2(IconSide, IconBottom), left: true);
            AddIcon(canvasObject.transform, "Traders", "ui_hub_bag", new Vector2(TradersLeft, IconBottom), left: true);
            AddIcon(
                canvasObject.transform,
                "Repair",
                "ui_hub_wrench",
                new Vector2(-(IconSide + IconSize + 16f), IconBottom),
                left: false);
            AddIcon(
                canvasObject.transform,
                "Backpack",
                "ui_hub_backpack",
                new Vector2(-IconSide, IconBottom),
                left: false);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OverlayPrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Нет префаба {OverlayPrefabPath}");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvasObject.transform);
            instance.name = TraderDialogOverlay.RootName;
            instance.SetActive(true);
            var view = instance.GetComponent<DialogOverlayView>();
            if (view == null)
            {
                throw new InvalidOperationException("В префабе окна диалога нет DialogOverlayView");
            }

            view.Bind(
                texts,
                new[]
                {
                    new DialogOverlayView.MenuEntry(
                        DialogOverlayView.MenuTalk,
                        UiTextKey.Dialog_Talk,
                        () => { }),
                    new DialogOverlayView.MenuEntry(
                        DialogOverlayView.MenuTrade,
                        UiTextKey.Dialog_Trade,
                        () => { }),
                    new DialogOverlayView.MenuEntry(
                        DialogOverlayView.MenuFarewell,
                        UiTextKey.Dialog_Farewell,
                        () => { })
                },
                _ => { },
                blockClicks: false);
            fill(view);
            Canvas.ForceUpdateCanvases();

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
    }
}

#nullable enable
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Runtime.Screens.Menu;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>Часть класса <see cref="NewGameScreenPrefabBuilder"/>: панель превью.</summary>
    public static partial class NewGameScreenPrefabBuilder
    {
        private static void BuildPreviewPanel(Transform parent, UiFactory ui, IGameTexts texts, NewGameScreen screen)
        {
            var panel = new GameObject(
                "NewGameInventoryFrame",
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement),
                typeof(NewGameInventoryFrame));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = MenuPalette.Frame;
            var element = panel.GetComponent<LayoutElement>();
            element.flexibleWidth = 1;
            element.flexibleHeight = 1;
            element.minWidth = MenuMetrics.PreviewMinWidth;
            element.minHeight = 0;

            var scrollGo = new GameObject("PreviewScroll", typeof(RectTransform), typeof(ScrollArea));
            scrollGo.transform.SetParent(panel.transform, false);
            var scrollRect = (RectTransform)scrollGo.transform;
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = new Vector2(1f, MenuMetrics.PreviewPanelTop);
            scrollRect.offsetMin = new Vector2(MenuMetrics.PreviewPadding, MenuMetrics.PreviewPadding);
            scrollRect.offsetMax = new Vector2(-MenuMetrics.PreviewPadding, -MenuMetrics.PreviewPadding);
            var scroll = scrollGo.GetComponent<ScrollArea>();
            scroll.Configure(MenuMetrics.PreviewScrollbarWidth);

            var money = ui.CreateText(panel.transform, string.Empty, 24, TextAnchor.MiddleCenter, UiFactory.TextColor);
            money.gameObject.name = "PreviewMoney";
            money.raycastTarget = false;
            var moneyRect = money.rectTransform;
            var plate = InventoryPanelArt.CurrencyPlate;
            moneyRect.anchorMin = new Vector2(plate.xMin, plate.yMin);
            moneyRect.anchorMax = new Vector2(plate.xMax, plate.yMax);
            moneyRect.pivot = new Vector2(0.5f, 0.5f);
            moneyRect.offsetMin = Vector2.zero;
            moneyRect.offsetMax = Vector2.zero;
            money.gameObject.SetActive(false);

            var weight = ui.CreateText(panel.transform, string.Empty, 20, TextAnchor.MiddleLeft, UiFactory.TextColor);
            weight.gameObject.name = "PreviewWeight";
            weight.raycastTarget = false;
            var weightRect = weight.rectTransform;
            weightRect.anchorMin = new Vector2(0f, 1f);
            weightRect.anchorMax = new Vector2(0f, 1f);
            weightRect.pivot = new Vector2(0f, 1f);
            weightRect.anchoredPosition = new Vector2(MenuMetrics.PreviewPadding + 8f, -MenuMetrics.PreviewPadding - 8f);
            weightRect.sizeDelta = new Vector2(300f, 36f);
            weight.gameObject.SetActive(false);

            var empty = ui.CreateText(
                panel.transform,
                UiText.Get(texts, UiTextKey.NewGame_Preview_Empty),
                18,
                TextAnchor.MiddleCenter,
                UiFactory.ToastColor);
            empty.gameObject.name = "PreviewEmpty";
            empty.raycastTarget = false;
            var emptyRect = empty.rectTransform;
            emptyRect.anchorMin = Vector2.zero;
            emptyRect.anchorMax = Vector2.one;
            emptyRect.offsetMin = new Vector2(MenuMetrics.PreviewPadding, MenuMetrics.PreviewPadding);
            emptyRect.offsetMax = new Vector2(-MenuMetrics.PreviewPadding, -MenuMetrics.PreviewPadding);
            AddLocalized(empty, UiTextKey.NewGame_Preview_Empty);
            empty.gameObject.SetActive(false);

            var frame = panel.GetComponent<NewGameInventoryFrame>();
            SetObject(frame, "scroll", scroll);
            SetObject(frame, "gridRoot", scroll.Content);
            SetObject(frame, "moneyLabel", money);
            SetObject(frame, "weightLabel", weight);
            SetObject(frame, "emptyLabel", empty);
            SetObject(frame, "panelTexture", AssetDatabase.LoadAssetAtPath<Texture2D>(InventoryPanelArt.TexturePath));
            SetFloat(frame, "gridGap", MenuMetrics.PreviewGridGap);
            SetFloat(frame, "minCell", MenuMetrics.PreviewMinCell);
            SetFloat(frame, "padding", MenuMetrics.PreviewPadding);
            SetObject(screen, "preview", frame);
        }

        private static void BuildStatus(Transform content, UiFactory ui, NewGameScreen screen)
        {
            var status = ui.CreateText(content, string.Empty, 16, TextAnchor.MiddleLeft, UiFactory.ToastColor);
            status.gameObject.name = "Status";
            status.raycastTarget = false;
            var element = status.GetComponent<LayoutElement>();
            element.preferredHeight = MenuMetrics.StatusHeight;
            element.flexibleHeight = 0;
            status.gameObject.SetActive(false);
            SetObject(screen, "status", status);
        }

        private static void BuildButtons(Transform content, UiFactory ui, IGameTexts texts, NewGameScreen screen)
        {
            var row = new GameObject("ButtonsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(content, false);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = MenuMetrics.ColumnGap;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;
            RowHeight(row, MenuMetrics.ButtonsHeight);

            var start = ui.CreateActionButton(row.transform, UiText.Get(texts, UiTextKey.NewGame_Start));
            start.gameObject.name = "Начать";
            var startElement = start.GetComponent<LayoutElement>();
            startElement.preferredHeight = MenuMetrics.ButtonsHeight;
            startElement.minHeight = MenuMetrics.ButtonsHeight;
            startElement.preferredWidth = MenuMetrics.ButtonsWidth;
            startElement.minWidth = MenuMetrics.ButtonsWidth;
            startElement.flexibleWidth = 0f;
            GetButtonLabel(start).fontSize = MenuMetrics.ButtonsFontSize;
            AddLocalized(GetButtonLabel(start), UiTextKey.NewGame_Start);
            SetObject(screen, "start", start);

            var back = ui.CreateActionButton(row.transform, UiText.Get(texts, UiTextKey.NewGame_Back));
            back.gameObject.name = "Назад";
            var backElement = back.GetComponent<LayoutElement>();
            backElement.preferredHeight = MenuMetrics.ButtonsHeight;
            backElement.minHeight = MenuMetrics.ButtonsHeight;
            backElement.preferredWidth = MenuMetrics.ButtonsWidth;
            backElement.minWidth = MenuMetrics.ButtonsWidth;
            backElement.flexibleWidth = 0f;
            GetButtonLabel(back).fontSize = MenuMetrics.ButtonsFontSize;
            AddLocalized(GetButtonLabel(back), UiTextKey.NewGame_Back);
            SetObject(screen, "back", back);
        }
    }
}

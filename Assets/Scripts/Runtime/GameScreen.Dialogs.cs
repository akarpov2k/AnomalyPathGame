#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Localization;
using AnomalyPath.Runtime.Ui.Dialogs;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AnomalyPath.Runtime
{
    /// <summary>Часть класса <see cref="GameScreen"/>: создание модальных диалогов из префабов.</summary>
    public sealed partial class GameScreen
    {
        public ConfirmDialog CreateConfirmDialog(Transform parent)
        {
            var dialog = CreateDialog<ConfirmDialog>(confirmDialogPrefab, parent, "ConfirmDialog");
            dialog.Bind(this);
            return dialog;
        }

        /// <inheritdoc />
        public DialogOverlayView CreateDialogOverlay(
            Transform parent,
            string rootName,
            IReadOnlyList<DialogOverlayView.MenuEntry> menu,
            Action<string> choose,
            bool blockClicks)
        {
            var view = CreateDialog<DialogOverlayView>(dialogOverlayPrefab, parent, rootName);
            view.Bind(Texts, menu, choose, blockClicks);
            return view;
        }

        /// <summary>Создаёт модальное окно из префаба; окно остаётся скрытым до показа.</summary>
        /// <typeparam name="T">Тип компонента окна.</typeparam>
        /// <param name="prefab">Префаб окна.</param>
        /// <param name="parent">Родитель.</param>
        /// <param name="name">Имя объекта в иерархии.</param>
        /// <returns>Готовый компонент окна.</returns>
        /// <exception cref="InvalidOperationException">Префаб не задан или не содержит нужного компонента.</exception>
        private T CreateDialog<T>(GameObject? prefab, Transform parent, string name)
            where T : MonoBehaviour
        {
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    CoreText.Format(CoreErrors.DialogPrefabMissing, typeof(T).Name));
            }

            var instance = Instantiate(prefab, parent);
            instance.name = name;
            var component = instance.GetComponent<T>();
            if (component == null)
            {
                throw new InvalidOperationException(
                    CoreText.Format(CoreErrors.DialogPrefabMissing, prefab.name));
            }

            return component;
        }

        /// <summary>Создаёт экран из префаба и связывает его с хостом.</summary>
        /// <typeparam name="T">Тип компонента экрана.</typeparam>
        /// <param name="prefab">Префаб экрана.</param>
        /// <param name="canvas">Родительский канвас.</param>
        /// <param name="panelName">Имя объекта панели в иерархии.</param>
        /// <returns>Готовый экран.</returns>
        /// <exception cref="InvalidOperationException">Префаб не задан или не содержит нужного компонента.</exception>
    }
}

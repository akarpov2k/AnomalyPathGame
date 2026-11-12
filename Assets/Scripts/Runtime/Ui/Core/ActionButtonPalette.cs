#nullable enable
using UnityEngine;

namespace AnomalyPath.Runtime.Ui.Core
{
    /// <summary>
    /// Цвета общего кадра кнопки-действия <c>ui_btn_mp_ind_empty/green</c>. Кадр один на все
    /// экраны, поэтому и цвета подписи общие: песочная надпись с тёмной обводкой читается
    /// и на светлой металлической табличке, и на тёмной кромке кадра.
    /// </summary>
    public static class ActionButtonPalette
    {
        /// <summary>Подпись кнопки-действия: песочный, тёплый тон.</summary>
        public static readonly Color Label = new Color(0.84f, 0.68f, 0.42f, 1f);

        /// <summary>Обводка подписи: тёмно-коричневая, держит буквы на светлой табличке.</summary>
        public static readonly Color LabelOutline = new Color(0.13f, 0.08f, 0.04f, 0.9f);
    }
}

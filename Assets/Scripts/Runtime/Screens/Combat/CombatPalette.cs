#nullable enable
using UnityEngine;

namespace AnomalyPath.Runtime.Screens.Combat
{
    /// <summary>
    /// Цвета боевого экрана: фон поля, полосы здоровья, очки действия и подсветка зон.
    /// Взяты из темы игры, а не из прототипа: цвета прототипа обозначают зоны, а не палитру.
    /// </summary>
    public static class CombatPalette
    {
        /// <summary>Фон поля боя поверх задника экрана.</summary>
        public static readonly Color Field = new Color(0.06f, 0.08f, 0.07f, 0.38f);

        /// <summary>Фон формы действий.</summary>
        public static readonly Color ActionForm = new Color(0.05f, 0.07f, 0.06f, 0.82f);

        /// <summary>Фон плашки бойца с полосой здоровья.</summary>
        public static readonly Color CombatantPlate = new Color(0.10f, 0.13f, 0.12f, 0.85f);

        /// <summary>Фон полосы здоровья.</summary>
        public static readonly Color HealthBack = new Color(0.05f, 0.06f, 0.06f, 0.95f);

        /// <summary>Заполнение полосы здоровья игрока.</summary>
        public static readonly Color HealthPlayer = new Color(0.45f, 0.70f, 0.45f, 1f);

        /// <summary>Заполнение полосы здоровья противника.</summary>
        public static readonly Color HealthEnemy = new Color(0.72f, 0.32f, 0.28f, 1f);

        /// <summary>Фон плашки оружия и патронов.</summary>
        public static readonly Color WeaponPlate = new Color(0.09f, 0.12f, 0.11f, 0.85f);

        /// <summary>Доступное очко действия.</summary>
        public static readonly Color ApOn = new Color(0.55f, 0.85f, 0.45f, 1f);

        /// <summary>Потраченное очко действия.</summary>
        public static readonly Color ApOff = new Color(0.20f, 0.24f, 0.22f, 1f);

        /// <summary>Зона поля без бойца.</summary>
        public static readonly Color ZoneIdle = new Color(0.12f, 0.16f, 0.14f, 0.70f);

        /// <summary>Зона поля, в которой стоит боец.</summary>
        public static readonly Color ZoneActive = new Color(0.24f, 0.34f, 0.24f, 0.95f);

        /// <summary>Зона, куда боец перейдёт по наведённому шагу: ярче обычной и с тёплым тоном.</summary>
        public static readonly Color ZonePreview = new Color(0.62f, 0.66f, 0.24f, 0.98f);

        /// <summary>Всплывающее число урона над фигурой.</summary>
        public static readonly Color DamageText = new Color(1f, 0.55f, 0.38f, 1f);

        /// <summary>Всплывающее слово «Промах» над фигурой.</summary>
        public static readonly Color MissText = new Color(0.82f, 0.86f, 0.84f, 1f);

        /// <summary>Плашка игрока на поле.</summary>
        public static readonly Color FigurePlayer = new Color(0.28f, 0.44f, 0.30f, 0.60f);

        /// <summary>Плашка противника на поле.</summary>
        public static readonly Color FigureEnemy = new Color(0.46f, 0.24f, 0.22f, 0.60f);

        /// <summary>Фон выпадашки с действиями.</summary>
        public static readonly Color Menu = new Color(0.04f, 0.06f, 0.05f, 0.97f);

        /// <summary>Фон окна журнала: полупрозрачный, сквозь него видно поле.</summary>
        public static readonly Color Journal = new Color(0.03f, 0.04f, 0.04f, 0.80f);

        /// <summary>Фон подсказки при наведении.</summary>
        public static readonly Color Tooltip = new Color(0.02f, 0.03f, 0.03f, 0.95f);

        /// <summary>Фон полосы опыта.</summary>
        public static readonly Color XpBack = new Color(0.10f, 0.12f, 0.11f, 0.92f);

        /// <summary>Заполнение полосы опыта.</summary>
        public static readonly Color XpFill = new Color(0.55f, 0.75f, 0.45f, 1f);
    }
}

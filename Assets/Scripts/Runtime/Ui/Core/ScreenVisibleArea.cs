#nullable enable
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace AnomalyPath.Runtime.Ui.Core
{
    /// <summary>
    /// Видимая часть клиентской области окна на Windows: окно без рамки может заходить низом
    /// под панель задач, и нижняя кромка интерфейса (полоса опыта, нижний ряд кнопок) тогда
    /// не видна. Здесь считается, какая часть клиента реально видна, чтобы разметка поднялась
    /// выше панели задач. На других платформах возвращается весь экран.
    /// </summary>
    public static class ScreenVisibleArea
    {
        /// <summary>
        /// Видимая часть клиента в пикселях, начало координат — левый нижний угол, как у
        /// <see cref="Screen.safeArea"/>. Никогда не меньше одного пикселя.
        /// </summary>
        public static Rect Current
        {
            get
            {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
                if (TryRead(out var rect) && rect.width >= 1f && rect.height >= 1f)
                {
                    return rect;
                }
#endif
                return new Rect(0f, 0f, Screen.width, Screen.height);
            }
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private const uint MonitorDefaultToNearest = 2;

        /// <summary>Пересечение клиентской области с рабочей областью монитора.</summary>
        /// <param name="rect">Видимый прямоугольник в координатах клиента.</param>
        /// <returns><c>true</c>, если рамки окна и монитора удалось прочитать.</returns>
        private static bool TryRead(out Rect rect)
        {
            rect = default;
            try
            {
                // Окно игрока: у процесса плеера это главное окно. GetActiveWindow и
                // GetForegroundWindow зависят от фокуса и в фоновом запуске отдают чужое окно.
                var window = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                if (window == IntPtr.Zero)
                {
                    window = GetActiveWindow();
                }

                if (window == IntPtr.Zero)
                {
                    window = GetForegroundWindow();
                }

                if (window == IntPtr.Zero)
                {
                    return false;
                }

                if (!GetClientRect(window, out var client))
                {
                    return false;
                }

                var monitor = MonitorFromWindow(window, MonitorDefaultToNearest);
                if (monitor == IntPtr.Zero)
                {
                    return false;
                }

                var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
                if (!GetMonitorInfo(monitor, ref info))
                {
                    return false;
                }

                var origin = new PointI();
                if (!ClientToScreen(window, ref origin))
                {
                    return false;
                }

                var width = client.Right - client.Left;
                var height = client.Bottom - client.Top;
                if (width <= 0 || height <= 0)
                {
                    return false;
                }

                var left = Math.Max(origin.X, info.Work.Left) - origin.X;
                var right = Math.Min(origin.X + width, info.Work.Right) - origin.X;
                var top = Math.Max(origin.Y, info.Work.Top) - origin.Y;
                var bottom = Math.Min(origin.Y + height, info.Work.Bottom) - origin.Y;
                if (right <= left || bottom <= top)
                {
                    return false;
                }

                // Экранные координаты идут сверху вниз, Rect — снизу вверх.
                rect = new Rect(left, height - bottom, right - left, bottom - top);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>Точка в экранных координатах.</summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct PointI
        {
            /// <summary>Горизонтальная координата.</summary>
            public int X;

            /// <summary>Вертикальная координата.</summary>
            public int Y;
        }

        /// <summary>Прямоугольник в экранных координатах.</summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct RectI
        {
            /// <summary>Левый край.</summary>
            public int Left;

            /// <summary>Верхний край.</summary>
            public int Top;

            /// <summary>Правый край.</summary>
            public int Right;

            /// <summary>Нижний край.</summary>
            public int Bottom;
        }

        /// <summary>Монитор и его рабочая область без панели задач.</summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct MonitorInfo
        {
            /// <summary>Размер структуры.</summary>
            public int Size;

            /// <summary>Полный прямоугольник монитора.</summary>
            public RectI Monitor;

            /// <summary>Рабочая область без панели задач.</summary>
            public RectI Work;

            /// <summary>Флаги монитора.</summary>
            public uint Flags;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr window, out RectI rect);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr window, ref PointI point);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);

        [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
        private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
#endif
    }
}

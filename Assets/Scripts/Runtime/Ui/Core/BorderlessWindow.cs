#nullable enable
using AnomalyPath.Core.Ui;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace AnomalyPath.Runtime.Ui.Core
{
    /// <summary>
    /// Окно игры без рамки на Windows: убирает заголовок и не-клиентскую область окна плеера,
    /// оставляя изменение размера за края и перетаскивание за верхнюю полосу. Стартовый размер
    /// подгоняется под монитор. В редакторе и на других площадках ничего не делает.
    /// </summary>
    public static class BorderlessWindow
    {
        /// <summary>Применяет оформление при старте плеера, до показа сплэш-экрана.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void ApplyOnStartup() => Apply();

        /// <summary>Повторяет оформление после загрузки сцены: окно к этому моменту точно создано.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void ApplyAfterSceneLoad() => Apply();

        /// <summary>
        /// Снимает рамку окна и подгоняет его размер под монитор. Повторный вызов безопасен:
        /// уже снятая рамка и уже нужный размер ничего не меняют.
        /// </summary>
        public static void Apply()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            try
            {
                ApplySize();
                ApplyStyle();
                PlaceWindow();
                BorderlessWindowKeeper.Ensure();
            }
            catch (Exception error)
            {
                Debug.LogWarning($"AnomalyPath: borderless window failed: {error.Message}");
            }
#endif
        }

        /// <summary>
        /// Возвращает безрамочный вид, если Unity вернула заголовок после смены разрешения
        /// или возврата из полноэкранного режима. Размер и позицию окна не меняет.
        /// </summary>
        public static void EnforceStyle()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            try
            {
                ApplyStyle();
            }
            catch (Exception error)
            {
                Debug.LogWarning($"AnomalyPath: borderless window failed: {error.Message}");
            }
#endif
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        /// <summary>Стартовый размер окна: опорный размер разметки, если монитор позволяет.</summary>
        private static readonly WindowSize DefaultSize = new WindowSize(1920, 1080);

        private const int StyleIndex = -16;
        private const int ProcIndex = -4;
        private const int CaptionBits = 0x00C00000;
        private const uint SwpNoSize = 0x0001;
        private const uint SwpNoMove = 0x0002;
        private const uint SwpNoZOrder = 0x0004;
        private const uint SwpNoActivate = 0x0010;
        private const uint SwpFrameChanged = 0x0020;
        private const int DwmBorderColor = 34;
        private const int DwmCornerPreference = 33;
        private const uint DwmColorNone = 0xFFFFFFFE;
        private const uint DwmCornerDoNotRound = 1;
        private const uint WmNcCalcSize = 0x0083;
        private const uint WmNcHitTest = 0x0084;
        private const uint WmGetMinMaxInfo = 0x0024;
        private const int HtClient = 1;
        private const int HtCaption = 2;
        private const int HtLeft = 10;
        private const int HtRight = 11;
        private const int HtTop = 12;
        private const int HtTopLeft = 13;
        private const int HtTopRight = 14;
        private const int HtBottom = 15;
        private const int HtBottomLeft = 16;
        private const int HtBottomRight = 17;
        private const int GripSize = 6;
        private const int DragStrip = 24;
        private const uint MonitorDefaultToNearest = 2;

        private delegate IntPtr WindowProc(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

        private static WindowProc? _windowProc;
        private static IntPtr _previousProc;
        private static IntPtr _hookedWindow;

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        private static extern IntPtr SetWindowLongPtr(IntPtr window, int index, IntPtr value);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(
            IntPtr window,
            IntPtr insertAfter,
            int x,
            int y,
            int width,
            int height,
            uint flags);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr window, out WindowRect rect);

        [DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
        private static extern IntPtr CallWindowProc(
            IntPtr previous,
            IntPtr window,
            uint message,
            IntPtr wParam,
            IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "DefWindowProcW")]
        private static extern IntPtr DefWindowProc(
            IntPtr window,
            uint message,
            IntPtr wParam,
            IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsZoomed(IntPtr window);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);

        [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
        private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref uint value, int size);

        /// <summary>Прямоугольник в пикселях экрана.</summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct WindowRect
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

        /// <summary>Размеры монитора и его рабочей области без панели задач.</summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct MonitorInfo
        {
            /// <summary>Размер структуры.</summary>
            public int Size;

            /// <summary>Полный прямоугольник монитора.</summary>
            public WindowRect Monitor;

            /// <summary>Рабочая область без панели задач.</summary>
            public WindowRect Work;

            /// <summary>Флаги монитора.</summary>
            public uint Flags;
        }

        /// <summary>Параметры пересчёта клиентской области при изменении размера окна.</summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct NcCalcSizeParams
        {
            /// <summary>Предлагаемый прямоугольник клиентской области.</summary>
            public WindowRect Client;

            /// <summary>Прямоугольник окна до изменения.</summary>
            public WindowRect Window;

            /// <summary>Прямоугольник клиентской области до изменения.</summary>
            public WindowRect PreviousClient;

            /// <summary>Позиция окна после изменения.</summary>
            public IntPtr WindowPos;
        }

        /// <summary>Точка в координатах монитора.</summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct WindowPoint
        {
            /// <summary>Горизонтальная координата.</summary>
            public int X;

            /// <summary>Вертикальная координата.</summary>
            public int Y;
        }

        /// <summary>Ограничения развёрнутого окна: размер и положение не больше рабочей области.</summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct MinMaxInfo
        {
            /// <summary>Зарезервированное поле.</summary>
            public WindowPoint Reserved;

            /// <summary>Размер развёрнутого окна.</summary>
            public WindowPoint MaxSize;

            /// <summary>Положение развёрнутого окна.</summary>
            public WindowPoint MaxPosition;

            /// <summary>Минимальный размер при перетаскивании.</summary>
            public WindowPoint MinTrackSize;

            /// <summary>Максимальный размер при перетаскивании.</summary>
            public WindowPoint MaxTrackSize;
        }

        private static void ApplySize()
        {
            var displayWidth = Display.main.systemWidth > 0 ? Display.main.systemWidth : Screen.currentResolution.width;
            var displayHeight = Display.main.systemHeight > 0 ? Display.main.systemHeight : Screen.currentResolution.height;
            if (displayWidth <= 0 || displayHeight <= 0)
            {
                return;
            }

            var fitted = WindowLayout.Fit(new WindowSize(displayWidth, displayHeight), DesiredSize());
            if (Screen.fullScreenMode == FullScreenMode.Windowed &&
                Screen.width == fitted.Width &&
                Screen.height == fitted.Height)
            {
                return;
            }

            Screen.SetResolution(fitted.Width, fitted.Height, FullScreenMode.Windowed);
        }

        private static WindowSize DesiredSize()
        {
            var args = Environment.GetCommandLineArgs();
            var width = ArgumentValue(args, "-screen-width");
            var height = ArgumentValue(args, "-screen-height");
            return width > 0 && height > 0 ? new WindowSize(width, height) : DefaultSize;
        }

        private static int ArgumentValue(string[] args, string name)
        {
            for (var index = 0; index + 1 < args.Length; index++)
            {
                if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(args[index + 1], out var value))
                {
                    return value;
                }
            }

            return 0;
        }

        private static void ApplyStyle()
        {
            var window = CurrentWindow();
            if (window == IntPtr.Zero)
            {
                return;
            }

            var style = GetWindowLongPtr(window, StyleIndex).ToInt64();
            var borderless = style & ~(long)CaptionBits;
            if (borderless != style)
            {
                SetWindowLongPtr(window, StyleIndex, new IntPtr(borderless));
                SetWindowPos(
                    window,
                    IntPtr.Zero,
                    0,
                    0,
                    0,
                    0,
                    SwpNoSize | SwpNoMove | SwpNoZOrder | SwpNoActivate | SwpFrameChanged);
            }

            Hook(window);
            HideBorderAndRoundCorners(window);
        }

        private static IntPtr CurrentWindow()
        {
            var window = GetActiveWindow();
            return window != IntPtr.Zero ? window : GetForegroundWindow();
        }

        private static void Hook(IntPtr window)
        {
            if (_hookedWindow == window)
            {
                return;
            }

            _windowProc = WindowProcImpl;
            var pointer = Marshal.GetFunctionPointerForDelegate(_windowProc);
            _previousProc = SetWindowLongPtr(window, ProcIndex, pointer);
            _hookedWindow = window;
        }

        private static IntPtr WindowProcImpl(IntPtr window, uint message, IntPtr wParam, IntPtr lParam)
        {
            if (message == WmNcHitTest)
            {
                return new IntPtr(HitTest(window, lParam));
            }

            if (message == WmNcCalcSize && wParam != IntPtr.Zero)
            {
                ClipClientToWindow(window, lParam);
                return IntPtr.Zero;
            }

            var result = _previousProc != IntPtr.Zero
                ? CallWindowProc(_previousProc, window, message, wParam, lParam)
                : DefWindowProc(window, message, wParam, lParam);

            if (message == WmGetMinMaxInfo)
            {
                ClampMaximizedSize(window, lParam);
            }

            return result;
        }

        /// <summary>
        /// Развёрнутое окно занимает рабочую область монитора, а не рабочую область плюс
        /// невидимую рамку: иначе окно накрывает панель задач и выходит за края экрана.
        /// </summary>
        private static void ClampMaximizedSize(IntPtr window, IntPtr lParam)
        {
            var monitor = MonitorFromWindow(window, MonitorDefaultToNearest);
            if (monitor == IntPtr.Zero)
            {
                return;
            }

            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (!GetMonitorInfo(monitor, ref info))
            {
                return;
            }

            var minMax = Marshal.PtrToStructure<MinMaxInfo>(lParam);
            minMax.MaxPosition = new WindowPoint
            {
                X = info.Work.Left - info.Monitor.Left,
                Y = info.Work.Top - info.Monitor.Top
            };
            minMax.MaxSize = new WindowPoint
            {
                X = info.Work.Right - info.Work.Left,
                Y = info.Work.Bottom - info.Work.Top
            };
            Marshal.StructureToPtr(minMax, lParam, false);
        }

        /// <summary>
        /// Клиентская область занимает всё окно. В развёрнутом окне она ограничивается рабочей
        /// областью монитора, иначе окно накрыло бы панель задач.
        /// </summary>
        private static void ClipClientToWindow(IntPtr window, IntPtr lParam)
        {
            if (!IsZoomed(window))
            {
                return;
            }

            var monitor = MonitorFromWindow(window, MonitorDefaultToNearest);
            if (monitor == IntPtr.Zero)
            {
                return;
            }

            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (!GetMonitorInfo(monitor, ref info))
            {
                return;
            }

            var parameters = Marshal.PtrToStructure<NcCalcSizeParams>(lParam);
            parameters.Client = info.Work;
            Marshal.StructureToPtr(parameters, lParam, false);
        }

        private static int HitTest(IntPtr window, IntPtr lParam)
        {
            if (!GetWindowRect(window, out var rect))
            {
                return HtClient;
            }

            var screen = lParam.ToInt64();
            var x = unchecked((short)(screen & 0xFFFF));
            var y = unchecked((short)((screen >> 16) & 0xFFFF));

            var left = x - rect.Left <= GripSize;
            var right = rect.Right - x <= GripSize;
            var top = y - rect.Top <= GripSize;
            var bottom = rect.Bottom - y <= GripSize;

            if (top && left)
            {
                return HtTopLeft;
            }

            if (top && right)
            {
                return HtTopRight;
            }

            if (bottom && left)
            {
                return HtBottomLeft;
            }

            if (bottom && right)
            {
                return HtBottomRight;
            }

            if (left)
            {
                return HtLeft;
            }

            if (right)
            {
                return HtRight;
            }

            if (top)
            {
                return HtTop;
            }

            if (bottom)
            {
                return HtBottom;
            }

            return y - rect.Top <= DragStrip ? HtCaption : HtClient;
        }

        private static void HideBorderAndRoundCorners(IntPtr window)
        {
            var border = DwmColorNone;
            DwmSetWindowAttribute(window, DwmBorderColor, ref border, sizeof(uint));
            var corners = (uint)DwmCornerDoNotRound;
            DwmSetWindowAttribute(window, DwmCornerPreference, ref corners, sizeof(uint));
        }

        private static void PlaceWindow()
        {
            if (Screen.fullScreenMode != FullScreenMode.Windowed)
            {
                return;
            }

            var displayWidth = Display.main.systemWidth > 0 ? Display.main.systemWidth : Screen.currentResolution.width;
            var displayHeight = Display.main.systemHeight > 0 ? Display.main.systemHeight : Screen.currentResolution.height;
            if (displayWidth <= 0 || displayHeight <= 0)
            {
                return;
            }

            var window = CurrentWindow();
            if (window == IntPtr.Zero)
            {
                return;
            }

            var fitted = WindowLayout.Fit(new WindowSize(displayWidth, displayHeight), DesiredSize());
            var x = Math.Max(0, (displayWidth - fitted.Width) / 2);
            var y = Math.Max(0, (displayHeight - fitted.Height) / 2);
            SetWindowPos(window, IntPtr.Zero, x, y, fitted.Width, fitted.Height, SwpNoZOrder | SwpNoActivate);
        }
#endif
    }
}

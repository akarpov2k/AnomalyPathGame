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

        /// <summary>
        /// Повторно вписывает окно в рабочую область после старта. Unity применяет разрешение
        /// из командной строки и настроек уже после первого кадра и возвращает окно за край
        /// экрана: низ интерфейса, где лежит полоса опыта, уходит под панель задач.
        /// </summary>
        public static void EnforceWindow()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            try
            {
                ApplyStyle();
                PlaceWindow();
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
        private const uint MonitorDefaultToPrimary = 1;

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

        [DllImport("user32.dll", EntryPoint = "GetClientRect")]
        private static extern bool GetClientRect(IntPtr window, out WindowRect rect);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr window, ref WindowPoint point);

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

            // Рабочая область, а не весь монитор: окно во весь экран заходит низом под панель
            // задач, и нижняя полоса интерфейса (полоса опыта) оказывается закрытой.
            var areaWidth = displayWidth;
            var areaHeight = displayHeight;
            if (TryPrimaryWorkArea(out _, out _, out var workWidth, out var workHeight))
            {
                areaWidth = Math.Min(areaWidth, workWidth);
                areaHeight = Math.Min(areaHeight, workHeight);
            }

            var fitted = WindowLayout.Fit(new WindowSize(areaWidth, areaHeight), DesiredSize());
            if (Screen.fullScreenMode == FullScreenMode.Windowed &&
                Screen.width == fitted.Width &&
                Screen.height == fitted.Height)
            {
                return;
            }

            Screen.SetResolution(fitted.Width, fitted.Height, FullScreenMode.Windowed);
        }

        /// <summary>
        /// Рабочая область основного монитора без панели задач и док-панелей. Окно, посаженное
        /// в неё, не теряет нижнюю кромку интерфейса за системной панелью.
        /// </summary>
        /// <param name="left">Левый край рабочей области.</param>
        /// <param name="top">Верхний край рабочей области.</param>
        /// <param name="width">Ширина рабочей области.</param>
        /// <param name="height">Высота рабочей области.</param>
        /// <returns><c>true</c>, если размеры удалось прочитать.</returns>
        private static bool TryPrimaryWorkArea(out int left, out int top, out int width, out int height)
        {
            left = 0;
            top = 0;
            width = 0;
            height = 0;
            var monitor = MonitorFromWindow(IntPtr.Zero, MonitorDefaultToPrimary);
            if (monitor == IntPtr.Zero)
            {
                return false;
            }

            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (!GetMonitorInfo(monitor, ref info))
            {
                return false;
            }

            left = info.Work.Left;
            top = info.Work.Top;
            width = info.Work.Right - info.Work.Left;
            height = info.Work.Bottom - info.Work.Top;
            return width > 0 && height > 0;
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

        /// <summary>
        /// Оконная процедура хука. Атрибут обязателен для IL2CPP: без него AOT не создаёт
        /// обратный P/Invoke-переход, и <see cref="Marshal.GetFunctionPointerForDelegate"/> падает
        /// с «To marshal a managed method, please add an attribute named 'MonoPInvokeCallback'».
        /// </summary>
        [AOT.MonoPInvokeCallback(typeof(WindowProc))]
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

            // Центрируем в рабочей области: иначе низ окна на полном мониторе уходит под панель задач.
            var areaLeft = 0;
            var areaTop = 0;
            var areaWidth = displayWidth;
            var areaHeight = displayHeight;
            if (TryPrimaryWorkArea(out var workLeft, out var workTop, out var workWidth, out var workHeight))
            {
                areaLeft = workLeft;
                areaTop = workTop;
                areaWidth = Math.Min(areaWidth, workWidth);
                areaHeight = Math.Min(areaHeight, workHeight);
            }

            // Текущий размер, вписанный в рабочую область: разрешение, выбранное в настройках
            // или заданное командной строкой, сохраняется, но окно не выходит за край экрана.
            var fitted = WindowLayout.Fit(
                new WindowSize(areaWidth, areaHeight),
                new WindowSize(Screen.width, Screen.height));
            var left = areaLeft + Math.Max(0, (areaWidth - fitted.Width) / 2);
            var top = areaTop + Math.Max(0, (areaHeight - fitted.Height) / 2);
            FitClientToArea(window, left, top, fitted.Width, fitted.Height);
        }

        /// <summary>
        /// Ставит клиентскую область окна в заданный прямоугольник. Windows добавляет окну
        /// невидимую рамку изменения размера, поэтому <see cref="SetWindowPos"/> по размерам
        /// клиентской области сдвигает кадр вниз: на полном мониторе низ интерфейса уходит за
        /// край экрана. Здесь рамка измеряется и вычитается.
        /// </summary>
        /// <param name="window">Окно игрока.</param>
        /// <param name="left">Левый край клиентской области на экране.</param>
        /// <param name="top">Верхний край клиентской области на экране.</param>
        /// <param name="width">Ширина клиентской области.</param>
        /// <param name="height">Высота клиентской области.</param>
        private static void FitClientToArea(IntPtr window, int left, int top, int width, int height)
        {
            for (var pass = 0; pass < 2; pass++)
            {
                if (!GetWindowRect(window, out var outer) || !GetClientRect(window, out var client))
                {
                    return;
                }

                var origin = new WindowPoint();
                if (!ClientToScreen(window, ref origin))
                {
                    return;
                }

                var frameWidth = (outer.Right - outer.Left) - (client.Right - client.Left);
                var frameHeight = (outer.Bottom - outer.Top) - (client.Bottom - client.Top);
                var offsetX = origin.X - outer.Left;
                var offsetY = origin.Y - outer.Top;
                var x = left - offsetX;
                var y = top - offsetY;
                var targetWidth = width + frameWidth;
                var targetHeight = height + frameHeight;
                if (outer.Left == x && outer.Top == y &&
                    outer.Right - outer.Left == targetWidth &&
                    outer.Bottom - outer.Top == targetHeight)
                {
                    return;
                }

                SetWindowPos(window, IntPtr.Zero, x, y, targetWidth, targetHeight, SwpNoZOrder | SwpNoActivate);
            }
        }
#endif
    }
}

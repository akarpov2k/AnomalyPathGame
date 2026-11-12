#nullable enable
using UnityEngine;

namespace AnomalyPath.Runtime.Ui.Core
{
    /// <summary>
    /// Следит за окном плеера: Unity возвращает заголовок окна после смены разрешения,
    /// поэтому безрамочный вид возвращается повторно. Первые секунды после старта
    /// дополнительно вписывает окно в рабочую область — разрешение из командной строки и
    /// настроек применяется после первого кадра и может вынести окно за край экрана.
    /// </summary>
    public sealed class BorderlessWindowKeeper : MonoBehaviour
    {
        private const float CheckInterval = 0.5f;
        private const int SettleChecks = 20;

        private float _nextCheck;
        private int _settleLeft = SettleChecks;

        /// <summary>Создаёт держатель окна, если его ещё нет.</summary>
        internal static void Ensure()
        {
            if (FindAnyObjectByType<BorderlessWindowKeeper>() != null)
            {
                return;
            }

            var go = new GameObject("BorderlessWindow");
            DontDestroyOnLoad(go);
            go.AddComponent<BorderlessWindowKeeper>();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextCheck)
            {
                return;
            }

            _nextCheck = Time.unscaledTime + CheckInterval;
            BorderlessWindow.EnforceStyle();
            if (_settleLeft > 0)
            {
                _settleLeft--;
                BorderlessWindow.EnforceWindow();
            }
        }
    }
}

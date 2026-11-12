#nullable enable
using UnityEngine;

namespace AnomalyPath.Runtime.Ui.Core
{
    /// <summary>
    /// Следит за окном плеера: Unity возвращает заголовок окна после смены разрешения,
    /// поэтому безрамочный вид возвращается повторно. Размер и позицию окна не меняет.
    /// </summary>
    public sealed class BorderlessWindowKeeper : MonoBehaviour
    {
        private const float CheckInterval = 0.5f;

        private float _nextCheck;

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
        }
    }
}

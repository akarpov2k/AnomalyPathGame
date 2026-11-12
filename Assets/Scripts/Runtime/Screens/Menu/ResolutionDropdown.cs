#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Menu
{
    /// <summary>
    /// Выпадающий список разрешений окна: кнопка с текущим разрешением и список вариантов под ней.
    /// Варианты строятся из неактивного образца строки, который лежит в префабе.
    /// </summary>
    public sealed class ResolutionDropdown : MonoBehaviour
    {
        [Tooltip("Кнопка, раскрывающая список")]
        [SerializeField]
        private Button toggle = null!;

        [Tooltip("Подпись текущего разрешения на кнопке")]
        [SerializeField]
        private Text value = null!;

        [Tooltip("Панель выпадающего списка")]
        [SerializeField]
        private RectTransform options = null!;

        [Tooltip("Неактивный образец строки списка")]
        [SerializeField]
        private Button optionTemplate = null!;

        private readonly List<Vector2Int> _built = new List<Vector2Int>();
        private Action<int, int>? _onPicked;
        private bool _bound;

        /// <summary><c>true</c>, если список раскрыт.</summary>
        public bool IsOpen => options != null && options.gameObject.activeSelf;

        /// <summary>Заполняет список вариантами и показывает текущее разрешение.</summary>
        /// <param name="width">Текущая ширина окна.</param>
        /// <param name="height">Текущая высота окна.</param>
        /// <param name="resolutions">Доступные разрешения.</param>
        /// <param name="onPicked">Что выполнить при выборе разрешения.</param>
        public void Bind(
            int width,
            int height,
            IReadOnlyList<Vector2Int> resolutions,
            Action<int, int> onPicked)
        {
            _onPicked = onPicked;
            if (!_bound)
            {
                toggle.onClick.AddListener(ToggleOptions);
                optionTemplate.gameObject.SetActive(false);
                _bound = true;
            }

            if (!SameOptions(resolutions))
            {
                BuildOptions(resolutions);
            }

            SetCurrent(width, height);
            Close();
        }

        /// <summary>Показывает текущее разрешение на кнопке.</summary>
        /// <param name="width">Ширина окна.</param>
        /// <param name="height">Высота окна.</param>
        public void SetCurrent(int width, int height) => value.text = Format(width, height);

        /// <summary>Закрывает список.</summary>
        public void Close() => options.gameObject.SetActive(false);

        private static string Format(int width, int height) => width + "×" + height;

        private bool SameOptions(IReadOnlyList<Vector2Int> resolutions)
        {
            if (_built.Count != resolutions.Count)
            {
                return false;
            }

            for (var index = 0; index < resolutions.Count; index++)
            {
                if (_built[index] != resolutions[index])
                {
                    return false;
                }
            }

            return true;
        }

        private void BuildOptions(IReadOnlyList<Vector2Int> resolutions)
        {
            foreach (Transform child in options)
            {
                if (child.gameObject != optionTemplate.gameObject)
                {
                    Destroy(child.gameObject);
                }
            }

            _built.Clear();
            foreach (var resolution in resolutions)
            {
                _built.Add(resolution);
                var row = Instantiate(optionTemplate, options);
                row.gameObject.name = "Resolution_" + Format(resolution.x, resolution.y);
                row.gameObject.SetActive(true);
                var label = row.GetComponentInChildren<Text>(true);
                if (label != null)
                {
                    label.text = Format(resolution.x, resolution.y);
                }

                var width = resolution.x;
                var height = resolution.y;
                row.onClick.AddListener(() => Pick(width, height));
            }
        }

        private void Pick(int width, int height)
        {
            SetCurrent(width, height);
            Close();
            _onPicked?.Invoke(width, height);
        }

        private void ToggleOptions()
        {
            if (IsOpen)
            {
                Close();
                return;
            }

            options.gameObject.SetActive(true);
            options.SetAsLastSibling();
            var rect = (RectTransform)toggle.transform;
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            options.position = new Vector3(corners[0].x, corners[0].y, corners[0].z);
        }
    }
}

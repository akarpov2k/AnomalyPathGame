#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Combat
{
    /// <summary>
    /// Фигура бойца на боевом экране: тело в профиль, текущее оружие у кисти
    /// и вспышка у ствола на каждый выстрел. Фигура противника зеркалится и смотрит влево.
    /// </summary>
    public sealed class CombatFigureView : MonoBehaviour
    {
        /// <summary>Тело бойца: спрайт костюма.</summary>
        [SerializeField]
        private Image? body = null;

        /// <summary>Оружие в руках: спрайт иконки предмета.</summary>
        [SerializeField]
        private Image? weapon = null;

        /// <summary>Вспышка у среза ствола.</summary>
        [SerializeField]
        private Image? muzzle = null;

        /// <summary>Всплывающая подпись урона или промаха над фигурой.</summary>
        [SerializeField]
        private Text? floatingText = null;

        /// <summary>Насколько подпись поднимается за время показа, единицы канваса.</summary>
        [SerializeField]
        private float floatingRise = 44f;

        /// <summary>Сколько секунд живёт всплывающая подпись.</summary>
        [SerializeField]
        private float floatingSeconds = 0.9f;

        /// <summary>Точка хвата оружия в долях тела: 0 — низ, 1 — верх; снята по кулакам профилей.</summary>
        [SerializeField]
        private Vector2 weaponMount = new Vector2(0.90f, 0.69f);

        /// <summary>Насколько сдвинуть центр оружия от кисти к стволу: доля длины иконки.</summary>
        [SerializeField]
        private float weaponGripOffset = 0.28f;

        /// <summary>Высота тела в пикселях при полном кадре.</summary>
        [SerializeField]
        private float bodyHeight = 250f;

        /// <summary>Предельная ширина тела: у зверя она больше, чем у человека.</summary>
        [SerializeField]
        private float bodyMaxWidth = 150f;

        /// <summary>Высота оружия в пикселях; длина считается по пропорции иконки.</summary>
        [SerializeField]
        private float weaponHeight = 34f;

        /// <summary>Сторона вспышки у ствола.</summary>
        [SerializeField]
        private float muzzleSize = 56f;

        /// <summary>Цвет тела при попадании.</summary>
        [SerializeField]
        private Color hitTint = new Color(1f, 0.66f, 0.55f, 1f);

        /// <summary>Отдача оружия назад, в пикселях.</summary>
        [SerializeField]
        private float recoil = 12f;

        /// <summary>Длительность одной вспышки: рост, затухание и возврат оружия.</summary>
        [SerializeField]
        private float shotSeconds = 0.12f;

        /// <summary>Пауза между выстрелами очереди: вспышки идут одна за другой, но не сливаются.</summary>
        [SerializeField]
        private float shotInterval = 0.1f;

        /// <summary>Размер вспышки в начале кадра, в долях от <see cref="muzzleSize"/>.</summary>
        [SerializeField]
        private float flashStartScale = 0.7f;

        /// <summary>Размер вспышки в конце кадра: вспышка раскрывается и гаснет.</summary>
        [SerializeField]
        private float flashEndScale = 1.35f;

        /// <summary>Наклон вспышки в градусах; чётные и нечётные выстрелы встают в разнобой.</summary>
        [SerializeField]
        private float flashTilt = 14f;

        private Sprite? _bodySprite;
        private Sprite? _weaponSprite;
        private Vector2 _weaponHome;
        private Vector2 _lungeHome;
        private bool _firing;
        private bool _flashing;
        private bool _flipped;
        private bool _lunging;
        private int _shotIndex;
        private Coroutine? _shot;
        private Coroutine? _lunge;
        private Coroutine? _floating;
        private Vector2 _floatingHome;
        private bool _floatingHomeCaptured;

        /// <summary>Идёт ли сейчас анимация выстрела.</summary>
        public bool IsFiring => _firing;

        /// <summary>Сколько вспышек фигура проиграла: по одной на каждый выстрел.</summary>
        public int ShotsPlayed { get; private set; }

        /// <summary>Есть ли у фигуры оружие в руках.</summary>
        public bool HasWeapon => weapon != null && weapon.enabled;

        /// <summary>Текст последней всплывающей подписи: число урона или «Промах».</summary>
        public string FloatingTextValue => floatingText != null ? floatingText.text : string.Empty;

        /// <summary><c>true</c>, если всплывающая подпись сейчас видна.</summary>
        public bool IsFloatingVisible => floatingText != null && floatingText.gameObject.activeSelf;

        /// <summary>
        /// Показывает над фигурой всплывающую подпись: число урона или «Промах».
        /// Подпись поднимается и гаснет, не перекрывая следующие выстрелы.
        /// </summary>
        /// <param name="text">Готовый текст подписи.</param>
        /// <param name="color">Цвет: урон или промах.</param>
        public void ShowFloating(string text, Color color)
        {
            if (floatingText == null)
            {
                return;
            }

            if (_floating != null)
            {
                StopCoroutine(_floating);
                _floating = null;
            }

            if (!_floatingHomeCaptured)
            {
                _floatingHome = floatingText.rectTransform.anchoredPosition;
                _floatingHomeCaptured = true;
            }

            floatingText.text = text;
            floatingText.color = color;
            floatingText.rectTransform.anchoredPosition = _floatingHome;
            floatingText.gameObject.SetActive(true);
            _floating = StartCoroutine(FloatingRoutine());
        }

        private IEnumerator FloatingRoutine()
        {
            var rect = floatingText!.rectTransform;
            var duration = Mathf.Max(0.2f, floatingSeconds);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                rect.anchoredPosition = _floatingHome + new Vector2(0f, floatingRise * progress);
                var color = floatingText.color;
                color.a = progress < 0.65f
                    ? 1f
                    : Mathf.Lerp(1f, 0f, (progress - 0.65f) / 0.35f);
                floatingText.color = color;
                yield return null;
            }

            floatingText.gameObject.SetActive(false);
            rect.anchoredPosition = _floatingHome;
            floatingText.color = new Color(floatingText.color.r, floatingText.color.g, floatingText.color.b, 1f);
            _floating = null;
        }

        private void HideFloating()
        {
            if (_floating != null)
            {
                StopCoroutine(_floating);
                _floating = null;
            }

            if (floatingText != null)
            {
                floatingText.gameObject.SetActive(false);
            }
        }

        /// <summary>Ставит тело бойца и подгоняет размер по пропорциям спрайта.</summary>
        /// <param name="sprite">Спрайт костюма или <c>null</c>.</param>
        /// <param name="fallback">Цвет плашки, если спрайта нет.</param>
        public void Show(Sprite? sprite, Color fallback)
        {
            if (body == null)
            {
                return;
            }

            if (sprite != null && _bodySprite == sprite)
            {
                return;
            }

            StopActions();
            _bodySprite = sprite;
            _weaponSprite = null;
            var rect = (RectTransform)transform;
            if (sprite == null)
            {
                body.sprite = null;
                body.color = fallback;
                body.preserveAspect = false;
                rect.sizeDelta = new Vector2(135f, bodyHeight);
                return;
            }

            body.sprite = sprite;
            body.color = Color.white;
            body.preserveAspect = true;
            var aspect = sprite.rect.width / sprite.rect.height;
            var height = bodyHeight;
            var width = height * aspect;
            if (width > bodyMaxWidth)
            {
                width = bodyMaxWidth;
                height = width / aspect;
            }

            rect.sizeDelta = new Vector2(width, height);
        }

        /// <summary>Поворачивает фигуру влево для противника.</summary>
        /// <param name="flip"><c>true</c> — смотреть влево.</param>
        public void SetFlip(bool flip)
        {
            if (_flipped == flip)
            {
                return;
            }

            _flipped = flip;
            transform.localScale = new Vector3(flip ? -1f : 1f, 1f, 1f);
            if (floatingText != null)
            {
                // Фигура противника зеркалится, а подпись должна остаться читаемой.
                floatingText.rectTransform.localScale = new Vector3(flip ? -1f : 1f, 1f, 1f);
            }
        }

        /// <summary>Кладёт текущее оружие в руки; <c>null</c> убирает его.</summary>
        /// <param name="icon">Спрайт иконки оружия.</param>
        public void SetWeapon(Sprite? icon)
        {
            if (weapon == null)
            {
                return;
            }

            if (icon == null)
            {
                _weaponSprite = null;
                weapon.enabled = false;
                if (muzzle != null)
                {
                    muzzle.enabled = false;
                }

                return;
            }

            if (_weaponSprite == icon && weapon.enabled)
            {
                return;
            }

            _weaponSprite = icon;
            weapon.enabled = true;

            weapon.sprite = icon;
            weapon.color = Color.white;
            var aspect = icon.rect.width / icon.rect.height;
            var height = weaponHeight;
            var width = height * aspect;
            weapon.rectTransform.sizeDelta = new Vector2(width, height);
            weapon.rectTransform.anchorMin = Vector2.zero;
            weapon.rectTransform.anchorMax = Vector2.zero;
            weapon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            weapon.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            var bodyRect = (RectTransform)transform;
            var handX = weaponMount.x * bodyRect.rect.width;
            var handY = weaponMount.y * bodyRect.rect.height;
            _weaponHome = new Vector2(handX + (width * weaponGripOffset), handY);
            weapon.rectTransform.anchoredPosition = _weaponHome;
            PlaceMuzzle(width, handY);
        }

        /// <summary>
        /// Ставит фигуру над клеткой своей зоны боя: центр клетки в долях поля становится
        /// якорем фигуры, линия ступней остаётся прежней. Клетка и фигура — дети поля,
        /// обе расставлены якорями, поэтому расчёт не зависит от того, прошла ли раскладка.
        /// </summary>
        /// <param name="zoneCell">Клетка зоны, в которой стоит боец.</param>
        public void StandOver(RectTransform? zoneCell)
        {
            var row = zoneCell != null ? zoneCell.parent as RectTransform : null;
            if (zoneCell == null || row == null)
            {
                return;
            }

            var cellCenter = (zoneCell.anchorMin.x + zoneCell.anchorMax.x) * 0.5f;
            var x = row.anchorMin.x + ((row.anchorMax.x - row.anchorMin.x) * cellCenter);
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(x, rect.anchorMin.y);
            rect.anchorMax = new Vector2(x, rect.anchorMax.y);
            rect.anchoredPosition = new Vector2(0f, rect.anchoredPosition.y);
        }

        /// <summary>Выстрел: вспышка у ствола, отдача оружия и вспышка попадания.</summary>
        /// <param name="target">Цель выстрела.</param>
        /// <param name="hit">Попал ли выстрел.</param>
        public void Fire(CombatFigureView? target, bool hit) => FireVolley(target, new[] { hit });

        /// <summary>
        /// Очередь выстрелов: на каждый выстрел своя вспышка, между ними — небольшая пауза.
        /// </summary>
        /// <param name="target">Цель очереди.</param>
        /// <param name="hits">Попадания по выстрелам в порядке выполнения.</param>
        /// <param name="onShot">Что выполнить после каждого выстрела: номер выстрела; <c>null</c> — ничего.</param>
        public void FireVolley(
            CombatFigureView? target,
            IReadOnlyList<bool>? hits,
            Action<int>? onShot = null)
        {
            if (hits == null || hits.Count == 0)
            {
                return;
            }

            FireShots(target, hits, onShot);
        }

        /// <summary>Атака зверя: короткий выпад вместо выстрела.</summary>
        /// <param name="target">Цель выпада.</param>
        /// <param name="hit">Попал ли удар.</param>
        public void Lunge(CombatFigureView? target, bool hit)
        {
            if (!isActiveAndEnabled || target == null)
            {
                return;
            }

            StopActions();
            var rect = (RectTransform)transform;
            _lungeHome = rect.anchoredPosition;
            _lunging = true;
            _lunge = StartCoroutine(LungeRoutine(target, hit));
        }

        /// <summary>Подсвечивает тело на время попадания.</summary>
        public void FlashHit()
        {
            if (body == null || _flashing || body.sprite == null)
            {
                return;
            }

            StartCoroutine(HitRoutine());
        }

        /// <summary>Останавливает текущую анимацию и возвращает фигуру в исходное положение.</summary>
        private void StopActions()
        {
            if (_shot != null)
            {
                StopCoroutine(_shot);
                _shot = null;
            }

            if (_lunge != null)
            {
                StopCoroutine(_lunge);
                _lunge = null;
            }

            if (_lunging)
            {
                ((RectTransform)transform).anchoredPosition = _lungeHome;
                _lunging = false;
            }

            _firing = false;
            if (weapon != null)
            {
                weapon.rectTransform.anchoredPosition = _weaponHome;
            }

            HideFlash();
            HideFloating();
        }

        private void PlaceMuzzle(float weaponWidth, float handY)
        {
            if (muzzle == null)
            {
                return;
            }

            muzzle.rectTransform.anchorMin = Vector2.zero;
            muzzle.rectTransform.anchorMax = Vector2.zero;
            muzzle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            muzzle.rectTransform.sizeDelta = new Vector2(muzzleSize, muzzleSize);
            muzzle.rectTransform.anchoredPosition = new Vector2(_weaponHome.x + (weaponWidth * 0.5f), handY);
            muzzle.enabled = false;
        }

        private void FireShots(CombatFigureView? target, IReadOnlyList<bool> hits, Action<int>? onShot)
        {
            if (!isActiveAndEnabled || target == null || weapon == null || !weapon.enabled)
            {
                return;
            }

            StopActions();
            _shot = StartCoroutine(VolleyRoutine(target, hits, onShot));
        }

        /// <summary>
        /// Проигрывает очередь: столько вспышек, сколько выстрелов, с паузой между ними.
        /// </summary>
        private IEnumerator VolleyRoutine(
            CombatFigureView target,
            IReadOnlyList<bool> hits,
            Action<int>? onShot)
        {
            _firing = true;
            for (var i = 0; i < hits.Count; i++)
            {
                if (i > 0)
                {
                    yield return new WaitForSeconds(Mathf.Max(0.02f, shotInterval));
                }

                yield return ShotRoutine(target, hits[i]);
                onShot?.Invoke(i);
            }

            _firing = false;
            _shot = null;
        }

        /// <summary>Один выстрел: вспышка раскрывается и гаснет, оружие даёт отдачу.</summary>
        private IEnumerator ShotRoutine(CombatFigureView target, bool hit)
        {
            _shotIndex++;
            ShotsPlayed++;
            if (muzzle != null)
            {
                muzzle.enabled = true;
                var tilt = flashTilt * (_shotIndex % 2 == 0 ? -1f : 1f);
                muzzle.rectTransform.localRotation = Quaternion.Euler(0f, 0f, tilt);
            }

            if (weapon != null)
            {
                weapon.rectTransform.anchoredPosition = _weaponHome + new Vector2(-recoil, 0f);
            }

            var duration = Mathf.Max(0.04f, shotSeconds);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                yield return null;
                elapsed += Time.deltaTime;
                AnimateFlash(Mathf.Clamp01(elapsed / duration));
            }

            HideFlash();
            if (weapon != null)
            {
                weapon.rectTransform.anchoredPosition = _weaponHome;
            }

            if (hit)
            {
                target.FlashHit();
            }
        }

        /// <summary>Кадр вспышки: раскрывается от начального размера и гаснет к концу.</summary>
        /// <param name="progress">Ход вспышки от 0 до 1.</param>
        private void AnimateFlash(float progress)
        {
            if (muzzle == null)
            {
                return;
            }

            var scale = Mathf.Lerp(flashStartScale, flashEndScale, progress);
            muzzle.rectTransform.localScale = new Vector3(scale, scale, 1f);
            muzzle.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - (progress * progress)));
        }

        /// <summary>Гасит вспышку и возвращает ей исходный вид.</summary>
        private void HideFlash()
        {
            if (muzzle == null)
            {
                return;
            }

            muzzle.enabled = false;
            muzzle.rectTransform.localScale = Vector3.one;
            muzzle.color = Color.white;
        }

        private IEnumerator LungeRoutine(CombatFigureView target, bool hit)
        {
            _firing = true;
            var rect = (RectTransform)transform;
            var forward = _flipped ? -1f : 1f;
            rect.anchoredPosition = _lungeHome + new Vector2(forward * 22f, 0f);
            yield return new WaitForSeconds(0.08f);
            if (hit)
            {
                target.FlashHit();
            }

            yield return new WaitForSeconds(0.08f);
            rect.anchoredPosition = _lungeHome;
            _firing = false;
            _lunging = false;
            _lunge = null;
        }

        private IEnumerator HitRoutine()
        {
            _flashing = true;
            body!.color = hitTint;
            yield return new WaitForSeconds(0.1f);
            body.color = Color.white;
            _flashing = false;
        }

        /// <summary>Экран погасил фигуру посреди очереди: корутина снята, вид возвращается в покой.</summary>
        private void OnDisable()
        {
            _shot = null;
            _lunge = null;
            _firing = false;
            _lunging = false;
            if (weapon != null)
            {
                weapon.rectTransform.anchoredPosition = _weaponHome;
            }

            HideFlash();
            HideFloating();
        }
    }
}

#nullable enable
using System.Collections.Generic;
using System.Globalization;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Talents;
using AnomalyPath.Runtime.Ui.Core;
using AnomalyPath.Runtime.Ui.Dialogs;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Talents
{
    /// <summary>
    /// Дерево талантов по прототипу: карточки-картинки в координатах row/column, связи между
    /// соседними талантами, подсказка по наведению с задержкой и отложенное вложение очков.
    /// Клик копит очко на экране, «Сохранить» применяет накопленное, «Назад» спрашивает
    /// подтверждение, если очки не сохранены. Разметка и карточки приходят из префаба.
    /// </summary>
    public sealed class TalentScreen : PrefabScreenBase<ITalentScreenHost>
    {
        [Tooltip("Заголовок окна")]
        [SerializeField]
        private Text? title = null;

        [Tooltip("Строка текущего уровня")]
        [SerializeField]
        private Text? level = null;

        [Tooltip("Строка свободных очков")]
        [SerializeField]
        private Text? points = null;

        [Tooltip("Прокручиваемое дерево талантов")]
        [SerializeField]
        private ScrollArea? tree = null;

        [Tooltip("Неактивный образец карточки таланта")]
        [SerializeField]
        private TalentNodeView? nodeTemplate = null;

        [Tooltip("Кнопка «Сохранить»: применяет накопленные очки")]
        [SerializeField]
        private Button? save = null;

        [Tooltip("Кнопка «Назад»")]
        [SerializeField]
        private Button? back = null;

        [Tooltip("Полоса сообщений")]
        [SerializeField]
        private Text? toast = null;

        private readonly Dictionary<string, int> _pending = new Dictionary<string, int>();
        private TalentTooltip? _tooltip;
        private ConfirmDialog? _confirm;

        /// <summary>Сколько очков накоплено на экране, но ещё не применено.</summary>
        public int PendingPoints
        {
            get
            {
                var total = 0;
                foreach (var pair in _pending)
                {
                    total += pair.Value;
                }

                return total;
            }
        }

        /// <summary>Открывает дерево талантов и перестраивает его по текущему состоянию игрока.</summary>
        public void Show()
        {
            if (!Panel.activeSelf)
            {
                _pending.Clear();
            }

            ApplyTheme();
            Host.Router.Show(Panel);
            Canvas.ForceUpdateCanvases();
            Refresh();
        }

        /// <summary>Esc спрашивает подтверждение при несохранённых очках и возвращает в инвентарь.</summary>
        /// <returns><c>true</c>, если Esc обработан.</returns>
        public override bool HandleEscape()
        {
            if (_confirm != null && _confirm.HandleEscape())
            {
                return true;
            }

            RequestBack();
            return true;
        }

        /// <inheritdoc />
        protected override void OnBound()
        {
            _tooltip = new TalentTooltip(WidgetHost, Panel.transform);
            _confirm = Host.CreateConfirmDialog(Panel.transform);
            if (save != null)
            {
                save.onClick.AddListener(SavePending);
            }

            if (back != null)
            {
                back.onClick.AddListener(RequestBack);
            }
        }

        private void Refresh()
        {
            var player = Host.Session?.State.Player;
            var texts = Host.Texts;
            _tooltip?.Hide();
            if (title != null)
            {
                title.text = UiText.Get(texts, UiTextKey.Talent_Title);
            }

            if (level != null)
            {
                level.text = string.Empty;
            }

            if (points != null)
            {
                points.text = string.Empty;
            }

            if (save != null)
            {
                save.interactable = false;
            }

            ClearTree();
            if (player == null)
            {
                ShowToast(UiText.Get(texts, UiTextKey.Common_No_Game), true);
                return;
            }

            if (level != null)
            {
                level.text = UiText.Format(texts, UiTextKey.Game_Level, player.Level);
            }

            if (points != null)
            {
                points.text = UiText.Format(texts, UiTextKey.Talent_Points, PendingAvailable(player));
            }

            if (save != null)
            {
                save.interactable = PendingPoints > 0;
            }

            var treeData = Host.Talents.Tree;
            var content = tree?.Content;
            if (content == null || nodeTemplate == null)
            {
                return;
            }

            if (treeData.Nodes.Count == 0)
            {
                ShowToast(UiText.Get(texts, UiTextKey.Talent_Empty), true);
                return;
            }

            BuildLinks(treeData.Nodes, content);
            BuildNodes(treeData.Nodes, player, content);
            tree.SetContentHeight(Mathf.Max(TalentMetrics.TreeHeight, TalentLayout.ContentHeight(treeData.Nodes)));
        }

        private void BuildLinks(IReadOnlyList<TalentNodeTemplate> nodes, RectTransform content)
        {
            foreach (var node in nodes)
            {
                foreach (var parent in TalentLayout.Parents(nodes, node))
                {
                    foreach (var segment in TalentLayout.LinkSegments(parent, node))
                    {
                        var link = Host.Ui.CreateImage("TalentLink", content, TalentPalette.Link);
                        link.raycastTarget = false;
                        var rect = link.rectTransform;
                        rect.anchorMin = new Vector2(0f, 1f);
                        rect.anchorMax = new Vector2(0f, 1f);
                        rect.pivot = new Vector2(0.5f, 0.5f);
                        rect.anchoredPosition = segment.Center;
                        rect.sizeDelta = new Vector2(segment.Length, TalentMetrics.LinkWidth);
                        rect.localRotation = Quaternion.Euler(0f, 0f, segment.Angle);
                    }
                }
            }
        }

        private void BuildNodes(
            IReadOnlyList<TalentNodeTemplate> nodes,
            PlayerState player,
            RectTransform content)
        {
            var texts = Host.Texts;
            foreach (var node in nodes)
            {
                var view = Instantiate(nodeTemplate, content);
                var go = view.gameObject;
                go.SetActive(true);
                go.name = "TalentNode_" + node.Id;
                var rect = (RectTransform)go.transform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = TalentLayout.NodeTopLeft(node);
                rect.sizeDelta = new Vector2(TalentMetrics.NodeWidth, TalentMetrics.NodeHeight);

                var rank = PendingRank(player, node);
                var unlocked = PendingSpent(player) >= node.RequiredPoints;
                view.Show(
                    node,
                    rank,
                    unlocked,
                    UiText.Format(texts, UiTextKey.Talent_Rank, rank, node.MaxRank),
                    Host.Sprites);

                if (view.Button != null)
                {
                    view.Button.onClick.AddListener(() => Stage(node));
                }

                if (view.Hover != null)
                {
                    view.Hover.Configure(
                        TalentMetrics.TooltipDelay,
                        (position, camera) => ShowTooltip(node, position, camera),
                        (position, camera) => _tooltip?.Move(position, camera),
                        () => _tooltip?.Hide());
                }
            }
        }

        private void Stage(TalentNodeTemplate node)
        {
            var player = Host.Session?.State.Player;
            if (player == null)
            {
                return;
            }

            if (PendingRank(player, node) >= node.MaxRank)
            {
                ShowToast(UiText.Get(Host.Texts, UiTextKey.Talent_Max), true);
                return;
            }

            if (PendingSpent(player) < node.RequiredPoints)
            {
                ShowToast(UiText.Get(Host.Texts, UiTextKey.Talent_Result_Locked), true);
                return;
            }

            if (PendingAvailable(player) <= 0)
            {
                ShowToast(UiText.Get(Host.Texts, UiTextKey.Talent_Result_No_Points), true);
                return;
            }

            _pending[node.Id] = PendingRankRaw(node.Id) + 1;
            _tooltip?.Hide();
            Refresh();
        }

        private void SavePending()
        {
            if (_pending.Count == 0)
            {
                return;
            }

            var player = Host.Session?.State.Player;
            if (player == null)
            {
                return;
            }

            var nodes = new List<TalentNodeTemplate>(Host.Talents.Tree.Nodes);
            nodes.Sort((left, right) =>
            {
                var row = left.Row.CompareTo(right.Row);
                return row != 0 ? row : left.Column.CompareTo(right.Column);
            });

            foreach (var node in nodes)
            {
                if (!_pending.TryGetValue(node.Id, out var count))
                {
                    continue;
                }

                for (var index = 0; index < count; index++)
                {
                    var result = Host.Talents.Learn(player, node.Id);
                    if (result.Succeeded)
                    {
                        continue;
                    }

                    _pending.Clear();
                    ShowToast(UiText.Get(Host.Texts, KeyFor(result.Status)), true);
                    Refresh();
                    return;
                }
            }

            _pending.Clear();
            ShowToast(UiText.Get(Host.Texts, UiTextKey.Talent_Saved));
            Refresh();
        }

        private void RequestBack()
        {
            var texts = Host.Texts;
            if (_pending.Count > 0 && _confirm != null)
            {
                _confirm.Show(
                    UiText.Get(texts, UiTextKey.Common_Confirm_Title),
                    UiText.Get(texts, UiTextKey.Talent_Unsaved),
                    UiText.Get(texts, UiTextKey.Talent_Unsaved_Confirm),
                    () =>
                    {
                        _pending.Clear();
                        Leave();
                    });
                return;
            }

            Leave();
        }

        private void Leave() => Host.ShowInventory();

        private void ShowTooltip(TalentNodeTemplate node, Vector2 position, Camera? camera)
        {
            var player = Host.Session?.State.Player;
            if (player == null)
            {
                return;
            }

            var texts = Host.Texts;
            var rank = PendingRank(player, node);
            var unlocked = PendingSpent(player) >= node.RequiredPoints;
            var maxed = rank >= node.MaxRank;
            var available = PendingAvailable(player);
            var status = maxed
                ? UiText.Get(texts, UiTextKey.Talent_Max)
                : !unlocked
                    ? UiText.Format(texts, UiTextKey.Talent_Locked, node.RequiredPoints)
                    : available > 0
                        ? UiText.Get(texts, UiTextKey.Talent_Tooltip_Click)
                        : UiText.Get(texts, UiTextKey.Talent_Result_No_Points);
            var statusColor = maxed
                ? TalentPalette.MaxIcon
                : unlocked && available > 0 ? UiFactory.ToastColor : UiFactory.MutedTextColor;
            _tooltip?.Show(
                texts.Get(node.TitleKey),
                UiText.Format(texts, UiTextKey.Talent_Rank, rank, node.MaxRank),
                texts.Get(node.DescriptionKey),
                EffectsLine(node),
                status,
                statusColor,
                position,
                camera);
        }

        private string EffectsLine(TalentNodeTemplate node)
        {
            var parts = new List<string>();
            foreach (var effect in node.Effects)
            {
                var value = effect.PerRank.ToString("0.###", CultureInfo.InvariantCulture);
                var key = effect.Stat switch
                {
                    TalentStat.ApMax => UiTextKey.Talent_Effect_Ap_Max,
                    TalentStat.HealthMax => UiTextKey.Talent_Effect_Health,
                    TalentStat.Accuracy => UiTextKey.Talent_Effect_Accuracy,
                    TalentStat.StaminaMax => UiTextKey.Talent_Effect_Stamina,
                    TalentStat.StaminaRecovery => UiTextKey.Talent_Effect_Stamina_Recovery,
                    TalentStat.MaxFood => UiTextKey.Talent_Effect_Max_Food,
                    TalentStat.MaxWater => UiTextKey.Talent_Effect_Max_Water,
                    _ => UiTextKey.Talent_Effect_Ap_Max
                };
                parts.Add(UiText.Format(
                    Host.Texts,
                    UiTextKey.Talent_Effect_Per_Rank,
                    UiText.Format(Host.Texts, key, value)));
            }

            return string.Join("\n", parts);
        }

        private void ShowToast(string text, bool error = false)
        {
            if (toast == null)
            {
                return;
            }

            toast.text = text;
            toast.color = error ? TalentPalette.Error : UiFactory.ToastColor;
        }

        private int PendingRank(PlayerState player, TalentNodeTemplate node) =>
            Mathf.Min(Host.Talents.RankOf(player, node.Id) + PendingRankRaw(node.Id), node.MaxRank);

        private int PendingRankRaw(string talentId) =>
            _pending.TryGetValue(talentId, out var rank) ? rank : 0;

        private int PendingSpent(PlayerState player) =>
            Host.Talents.SpentPoints(player) + PendingPoints;

        private int PendingAvailable(PlayerState player) =>
            Mathf.Max(0, Host.Talents.AvailablePoints(player) - PendingPoints);

        private void ClearTree()
        {
            var content = tree?.Content;
            if (content == null)
            {
                return;
            }

            var keep = nodeTemplate != null ? nodeTemplate.gameObject : null;
            for (var index = content.childCount - 1; index >= 0; index--)
            {
                var child = content.GetChild(index).gameObject;
                if (child == keep)
                {
                    continue;
                }

                child.SetActive(false);
                Destroy(child);
            }
        }

        private static UiTextKey KeyFor(TalentResultStatus status) => status switch
        {
            TalentResultStatus.UnknownTalent => UiTextKey.Talent_Result_Unknown,
            TalentResultStatus.MaxRank => UiTextKey.Talent_Result_Max,
            TalentResultStatus.Locked => UiTextKey.Talent_Result_Locked,
            TalentResultStatus.NoPoints => UiTextKey.Talent_Result_No_Points,
            _ => UiTextKey.Talent_Result_Invalid
        };
    }
}

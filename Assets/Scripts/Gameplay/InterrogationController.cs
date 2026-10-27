using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeaOfMemories
{
    // 質問→判定→反応→返答をつなぐ。PhaseはFlow、数値はGameSessionだけが更新する。
    public sealed class InterrogationController : MonoBehaviour
    {
        [SerializeField] private GameFlowController flow;
        [SerializeField] private ContentCatalog catalog;
        [SerializeField] private DialoguePlayer dialogue;
        [SerializeField] private UiInputRouter input;
        [SerializeField] private GameObject optionsRoot;
        [SerializeField] private Button[] buttons;
        [SerializeField] private TMP_Text[] labels;
        [SerializeField] private Image[] icons;
        [SerializeField] private TMP_Text[] markers;
        [SerializeField] private TMP_Text questionCount;
        [SerializeField] private TMP_Text comboCount;
        [SerializeField] private TMP_Text agitation;
        [SerializeField] private RectTransform gaugeFill;
        [SerializeField] private ScreenEffects effects;
        [SerializeField] private GuardPortraitView portrait;
        [SerializeField] private AudioController audioController;
        public void ConfigureAudio(AudioController controller) { audioController = controller; }
        private bool warning;
        private bool presentingImmediateLine;
        private int playbackVersion;
        private string currentItemId = "";
        public bool HasVisualSetup => effects != null || portrait != null;
        public bool HasQuestionVisuals => effects != null && effects.IsConfigured && effects.HasVignette
            && portrait != null && portrait.IsConfigured;

        public void ConfigureVisuals(ScreenEffects screenEffects, GuardPortraitView guard)
        {
            effects = screenEffects;
            portrait = guard;
        }
        public bool IsConfigured
        {
            get
            {
                if (flow == null || catalog == null || dialogue == null || input == null || optionsRoot == null
                    || questionCount == null || comboCount == null || agitation == null || gaugeFill == null
                    || buttons == null || labels == null || icons == null || markers == null
                    || buttons.Length != 6 || labels.Length != 6 || icons.Length != 6 || markers.Length != 6) return false;
                for (int i = 0; i < 6; i++)
                    if (buttons[i] == null || labels[i] == null || icons[i] == null || markers[i] == null) return false;
                return true;
            }
        }

        public void Configure(GameFlowController gameFlow, ContentCatalog content, DialoguePlayer player,
            UiInputRouter router, GameObject options, Button[] questionButtons, TMP_Text[] questionLabels,
            Image[] itemIcons, TMP_Text[] itemMarkers, TMP_Text count, TMP_Text combo, TMP_Text score, RectTransform fill)
        {
            flow = gameFlow;
            catalog = content;
            dialogue = player;
            input = router;
            optionsRoot = options;
            buttons = questionButtons;
            labels = questionLabels;
            icons = itemIcons;
            markers = itemMarkers;
            questionCount = count;
            comboCount = combo;
            agitation = score;
            gaugeFill = fill;
        }

        public void ResetView()
        {
            playbackVersion++;
            currentItemId = "";
            warning = false;
            presentingImmediateLine = false;
            if (effects != null) effects.SetComboLevel(0);
            if (portrait != null) portrait.ResetView();
        }

        public void BeginQuestionPhase()
        {
            if (!IsConfigured || flow.Phase != GamePhase.QuestionSelection || flow.Session.QuestionCount >= 3) return;
            currentItemId = "";
            warning = false;
            ShowImmediateLine("QUESTION_PROMPT");
            RefreshQuestionOptions();
        }

        public void RefreshQuestionOptions()
        {
            if (!IsConfigured) return;
            GameSession session = flow.Session;
            questionCount.text = "質問 " + session.QuestionCount + "/3";
            comboCount.text = "コンボ " + session.ComboCount;
            agitation.text = "動揺度 " + session.Agitation + "/100";
            gaugeFill.anchorMax = new Vector2(Mathf.Clamp01(session.Agitation / 100f), 1f);
            optionsRoot.SetActive(session.QuestionCount < 3);
            bool canSelect = flow.Phase == GamePhase.QuestionSelection && !flow.IsItemDetailsOpen && !warning && !presentingImmediateLine
                && session.QuestionCount < 3;
            for (int i = 0; i < buttons.Length; i++)
            {
                string id = "ITEM_0" + (i + 1);
                ItemDefinition item = catalog.GetItem(id);
                bool owned = session.HasItem(id);
                bool asked = session.HasAsked(id);
                labels[i].text = !owned ? "？？？" : item == null ? id : item.QuestionLabel;
                labels[i].color = asked ? new Color(0.5f, 0.5f, 0.5f) : Color.white;
                icons[i].sprite = owned && item != null ? item.Icon : null;
                icons[i].enabled = icons[i].sprite != null;
                markers[i].gameObject.SetActive(!icons[i].enabled);
                markers[i].text = !owned ? "LOCK" : asked ? "質問済み" : id;
                buttons[i].interactable = canSelect && item != null && !asked;
            }
        }

        public void SelectSlot(int index)
        {
            if (index >= 0 && index < 6) SelectQuestion("ITEM_0" + (index + 1));
        }

        public void SelectQuestion(string itemId)
        {
            if (!IsConfigured || input.IsConsumedThisFrame || flow.IsItemDetailsOpen || warning || presentingImmediateLine
                || flow.Phase != GamePhase.QuestionSelection || flow.Session.QuestionCount >= 3) return;
            ItemDefinition item = catalog.GetItem(itemId);
            if (item == null || flow.Session.HasAsked(itemId)) return;
            if (audioController != null) audioController.PlaySe("SE_01");
            if (!flow.HasItem(itemId))
            {
                input.SuppressForCurrentFrame();
                warning = true;
                ShowImmediateLine("QUESTION_MISSING_INFO");
                RefreshQuestionOptions();
                return;
            }
            PlayQuestion(itemId);
        }

        public bool TryDismissWarning()
        {
            if (flow.Phase != GamePhase.QuestionSelection || flow.IsItemDetailsOpen) return false;
            if (dialogue.TryAdvanceImmediatePage())
            {
                input.SuppressForCurrentFrame();
                return true;
            }
            if (!warning || presentingImmediateLine) return false;
            warning = false;
            if (audioController != null) audioController.PlaySe("SE_01");
            input.SuppressForCurrentFrame();
            ShowImmediateLine("QUESTION_PROMPT");
            RefreshQuestionOptions();
            return true;
        }

        private void ShowImmediateLine(string sequenceId)
        {
            presentingImmediateLine = true;
            RefreshQuestionOptions();
            int version = playbackVersion;
            dialogue.PlayImmediate(sequenceId, () =>
            {
                if (version != playbackVersion || flow.Phase != GamePhase.QuestionSelection) return;
                presentingImmediateLine = false;
                RefreshQuestionOptions();
            });
        }

        public void PlayQuestion(string itemId)
        {
            if (!IsConfigured || input.IsConsumedThisFrame || warning || presentingImmediateLine || flow.IsItemDetailsOpen
                || flow.Phase != GamePhase.QuestionSelection || !flow.HasItem(itemId) || flow.Session.HasAsked(itemId)) return;
            ItemDefinition item = catalog.GetItem(itemId);
            if (item == null || !flow.TryStartQuestionPlayback()) return;
            currentItemId = itemId;
            int version = ++playbackVersion;
            dialogue.Play(item.QuestionSequenceId, () =>
            {
                if (!IsCurrent(version, itemId)) return;
                QuestionResult result = flow.Session.TryApplyQuestion(item);
                if (result.status != QuestionStatus.Accepted)
                {
                    Debug.LogError("[SeaOfMemories] Question result was rejected: " + result.status, this);
                    return;
                }
                // 3問目はこの時点で所持品と選択肢を止め、返答終了までEndingに移らない。
                flow.RefreshQuestionState();
                if (item.IsValid)
                {
                    if (HasQuestionVisuals)
                    {
                        effects.SetComboLevel(result.comboAfter);
                        portrait.SetExpression(result.comboAfter == 1 ? "shaken1" : result.comboAfter == 2 ? "shaken2" : "limit");
                        portrait.Shake(portrait.ShakeSeconds, portrait.ShakePixels);
                    }
                    dialogue.Play("QUESTION_VALID_REACTION", () => PlayReply(item, result, version));
                }
                else
                {
                    if (audioController != null) audioController.PlaySe("SE_06");
                    if (HasQuestionVisuals)
                    {
                        portrait.StopShake();
                        effects.FadeOutVignette(effects.DummyFadeSeconds);
                    }
                    PlayReply(item, result, version);
                }
            });
        }

        private bool IsCurrent(int version, string itemId)
        {
            return version == playbackVersion && currentItemId == itemId && flow.Phase == GamePhase.QuestionPlayback;
        }

        private void PlayReply(ItemDefinition item, QuestionResult result, int version)
        {
            if (!IsCurrent(version, item.Id)) return;
            dialogue.Play(item.ReplySequenceId, () =>
            {
                if (IsCurrent(version, item.Id)) OnReplyFinished(result);
            });
        }

        public void OnReplyFinished(QuestionResult result)
        {
            if (result.status != QuestionStatus.Accepted || flow.Phase != GamePhase.QuestionPlayback) return;
            currentItemId = "";
            if (result.isFinalQuestion) flow.StartEnding(result.ending);
            else flow.EnterQuestionSelection();
        }
    }
}


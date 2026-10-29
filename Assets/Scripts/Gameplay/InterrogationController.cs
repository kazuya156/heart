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
        private ScenarioRepository QuestionRepository => GetComponent<ScenarioRepository>();
        private bool UsesAuthoredQuestions => QuestionRepository != null && QuestionRepository.HasSequence("QUESTION_SELECTION");
        private string QuestionLabel(ItemDefinition item)
        {
            if (UsesAuthoredQuestions)
                foreach (ChoiceOptionData option in QuestionRepository.Find("QUESTION_SELECTION").nodes[0].options)
                    if (option.id == item.Id) return option.label;
            return item.QuestionLabel;
        }
        private bool awaitingAuthoredResult;
        private QuestionResult authoredResult;
        private int authoredVersion;
        private bool statusPanelReady;
        private bool warning;
        private float warningSecondsRemaining = -1f;
        private const float WarningDuration = 1.5f;
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
            awaitingAuthoredResult = false;
            currentItemId = "";
            warning = false;
            warningSecondsRemaining = -1f;
            presentingImmediateLine = false;
            if (effects != null) effects.SetComboLevel(0);
            if (portrait != null) portrait.ResetView();
        }

        public void BeginQuestionPhase()
        {
            if (!IsConfigured || flow.Phase != GamePhase.QuestionSelection || flow.Session.QuestionCount >= 3) return;
            currentItemId = "";
            warning = false;
            warningSecondsRemaining = -1f;
            ShowImmediateLine("QUESTION_PROMPT");
            RefreshQuestionOptions();
        }

        public void RefreshQuestionOptions()
        {
            if (!IsConfigured) return;
            EnsureStatusPanel();
            GameSession session = flow.Session;
            questionCount.text = "質問 " + session.QuestionCount + "/3";
            comboCount.text = "コンボ " + session.ComboCount;
            agitation.text = "動揺度 " + session.Agitation + "/100";
            gaugeFill.anchorMax = new Vector2(Mathf.Clamp01(session.Agitation / 100f), 1f);
            optionsRoot.SetActive(flow.Phase == GamePhase.QuestionSelection && session.QuestionCount < 3);
            bool canSelect = flow.Phase == GamePhase.QuestionSelection && !flow.IsModalOpen && !warning && !presentingImmediateLine
                && session.QuestionCount < 3;
            for (int i = 0; i < buttons.Length; i++)
            {
                string id = "ITEM_0" + (i + 1);
                ItemDefinition item = catalog.GetItem(id);
                bool owned = session.HasItem(id);
                bool asked = session.HasAsked(id);
                RectTransform optionRect = buttons[i].GetComponent<RectTransform>();
                optionRect.anchorMin = new Vector2(0.64f, optionRect.anchorMin.y);
                optionRect.anchorMax = new Vector2(0.99f, optionRect.anchorMax.y);
                labels[i].text = !owned ? "？？？" : item == null ? id : QuestionLabel(item);
                labels[i].color = asked ? new Color(0.5f, 0.5f, 0.5f) : Color.white;
                icons[i].sprite = owned && item != null ? item.Icon : null;
                icons[i].enabled = icons[i].sprite != null;
                markers[i].gameObject.SetActive(!icons[i].enabled);
                markers[i].text = !owned ? "LOCK" : asked ? "質問済み" : id;
                buttons[i].interactable = canSelect && item != null && !asked;
            }
        }

        private void EnsureStatusPanel()
        {
            if (statusPanelReady || optionsRoot == null || optionsRoot.transform.parent == null) return;
            Transform parent = optionsRoot.transform.parent;
            var panel = new GameObject("QuestionStatusBacking", typeof(RectTransform), typeof(Image));
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.025f, 0.68f);
            rect.anchorMax = new Vector2(0.39f, 0.998f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.SetAsFirstSibling();
            Image image = panel.GetComponent<Image>();
            image.color = new Color(0.015f, 0.055f, 0.07f, 0.94f);
            image.raycastTarget = false;
            Transform heading = parent.Find("Title");
            if (heading != null)
            {
                var title = heading.GetComponent<TMP_Text>();
                if (title != null)
                {
                    title.color = new Color(0.60f, 0.91f, 0.94f);
                    title.fontSize = 28f;
                    title.alignment = TextAlignmentOptions.MidlineLeft;
                    title.raycastTarget = false;
                    title.rectTransform.anchorMin = new Vector2(0.05f, 0.94f);
                    title.rectTransform.anchorMax = new Vector2(0.37f, 0.995f);
                    title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;
                }
            }
            foreach (TMP_Text label in new[] { questionCount, agitation, comboCount })
            {
                label.color = Color.white;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.fontSize = 28f;
                label.raycastTarget = false;
                RectTransform textRect = label.rectTransform;
                textRect.anchorMin = new Vector2(0.05f, textRect.anchorMin.y);
                textRect.anchorMax = new Vector2(0.37f, textRect.anchorMax.y);
                textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            }
            if (gaugeFill != null && gaugeFill.parent != null)
            {
                var trackRect = gaugeFill.parent as RectTransform;
                if (trackRect != null)
                {
                    trackRect.anchorMin = new Vector2(0.05f, 0.775f);
                    trackRect.anchorMax = new Vector2(0.37f, 0.795f);
                    trackRect.offsetMin = trackRect.offsetMax = Vector2.zero;
                }
                Image track = gaugeFill.parent.GetComponent<Image>();
                if (track != null) { track.color = new Color(0.14f, 0.30f, 0.34f, 1f); track.raycastTarget = false; }
            }
            statusPanelReady = true;
        }

        public void SelectSlot(int index)
        {
            if (index >= 0 && index < 6) SelectQuestion("ITEM_0" + (index + 1));
        }

        public void SelectQuestion(string itemId)
        {
            if (!IsConfigured || input.IsConsumedThisFrame || flow.IsModalOpen || warning || presentingImmediateLine
                || flow.Phase != GamePhase.QuestionSelection || flow.Session.QuestionCount >= 3) return;
            ItemDefinition item = catalog.GetItem(itemId);
            if (item == null || flow.Session.HasAsked(itemId)) return;
            if (audioController != null) audioController.PlaySe("SE_01");
            if (!flow.HasItem(itemId))
            {
                input.SuppressForCurrentFrame();
                warning = true;
                warningSecondsRemaining = -1f;
                ShowImmediateLine("QUESTION_MISSING_INFO");
                RefreshQuestionOptions();
                return;
            }
            PlayQuestion(itemId);
        }

        public bool TryDismissWarning()
        {
            if (flow.Phase != GamePhase.QuestionSelection || flow.IsModalOpen) return false;
            if (dialogue.TryAdvanceImmediatePage())
            {
                input.SuppressForCurrentFrame();
                return true;
            }
            if (!warning) return false;
            if (!presentingImmediateLine) RestoreQuestionPrompt();
            else input.SuppressForCurrentFrame();
            return true;
        }

        private void Update()
        {
            if (!warning || presentingImmediateLine || warningSecondsRemaining < 0f
                || flow == null || dialogue == null || flow.Phase != GamePhase.QuestionSelection
                || flow.IsTransitioning || flow.IsModalOpen || dialogue.IsPaused) return;
            warningSecondsRemaining -= Time.unscaledDeltaTime;
            if (warningSecondsRemaining > 0f) return;
            RestoreQuestionPrompt();
        }

        private void RestoreQuestionPrompt()
        {
            warning = false;
            warningSecondsRemaining = -1f;
            input.SuppressForCurrentFrame();
            ShowImmediateLine("QUESTION_PROMPT");
            RefreshQuestionOptions();
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
                // Start only after the complete warning (including any extra pages) is visible.
                if (warning) warningSecondsRemaining = WarningDuration;
                RefreshQuestionOptions();
            });
        }

        public void PlayQuestion(string itemId)
        {
            if (!IsConfigured || input.IsConsumedThisFrame || warning || presentingImmediateLine || flow.IsModalOpen
                || flow.Phase != GamePhase.QuestionSelection || !flow.HasItem(itemId) || flow.Session.HasAsked(itemId)) return;
            ItemDefinition item = catalog.GetItem(itemId);
            if (item == null || !flow.TryStartQuestionPlayback()) return;
            currentItemId = itemId;
            int version = ++playbackVersion;
            if (UsesAuthoredQuestions)
            {
                authoredResult = flow.Session.TryBeginQuestion(item);
                if (authoredResult.status != QuestionStatus.Accepted) return;
                authoredVersion = version;
                awaitingAuthoredResult = true;
                dialogue.SequencePresented -= OnAuthoredSequencePresented;
                dialogue.SequencePresented += OnAuthoredSequencePresented;
                flow.RefreshQuestionState();
                dialogue.Play(item.QuestionSequenceId, () =>
                {
                    if (!IsCurrent(version, itemId)) return;
                    if (awaitingAuthoredResult)
                    {
                        Debug.LogError("[SeaOfMemories] Question completed without its RESULT: " + itemId, this);
                        return;
                    }
                    QuestionResult result = authoredResult;
                    if (item.IsValid && !(result.isFinalQuestion && result.ending == EndingType.Bad))
                        dialogue.Play("QUESTION_COMBO_" + result.comboAfter, () =>
                        {
                            if (IsCurrent(version, itemId)) OnReplyFinished(result);
                        });
                    else OnReplyFinished(result);
                });
                return;
            }
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
                    if (audioController != null) audioController.PlaySe("SE_05");
                    if (HasQuestionVisuals)
                    {
                        effects.SetComboLevel(result.comboAfter);
                        portrait.SetExpression(result.comboAfter == 1 ? "GUARD_03" : result.comboAfter == 2 ? "GUARD_04" : "GUARD_05");
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

        private void OnAuthoredSequencePresented(string sequenceId)
        {
            if (!awaitingAuthoredResult || !IsCurrent(authoredVersion, currentItemId)
                || sequenceId != "QUESTION_" + currentItemId + "_RESULT") return;
            awaitingAuthoredResult = false;
            ItemDefinition item = catalog.GetItem(currentItemId);
            authoredResult = flow.Session.CompleteQuestion(item);
            if (authoredResult.status != QuestionStatus.Accepted)
            {
                Debug.LogError("[SeaOfMemories] Authored result could not be applied.", this);
                return;
            }
            flow.RefreshQuestionState();
            if (audioController != null) audioController.PlaySe(item.IsValid ? "SE_05" : "SE_06");
            if (!HasQuestionVisuals) return;
            if (item.IsValid)
            {
                effects.SetComboLevel(authoredResult.comboAfter);
                portrait.SetExpression(authoredResult.comboAfter == 1 ? "GUARD_03" : authoredResult.comboAfter == 2 ? "GUARD_04" : "GUARD_05");
                portrait.Shake(portrait.ShakeSeconds, portrait.ShakePixels);
            }
            else { portrait.StopShake(); effects.FadeOutVignette(effects.DummyFadeSeconds); }
        }

        private void OnDestroy()
        {
            if (dialogue != null) dialogue.SequencePresented -= OnAuthoredSequencePresented;
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


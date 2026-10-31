using System;
using UnityEngine;

namespace SeaOfMemories
{
    public sealed class GameFlowController : MonoBehaviour
    {
        [SerializeField] private ScenarioRepository repository;
        [SerializeField] private TextAsset scenario; // legacy single-file test/setup fallback
        [SerializeField] private TextAsset[] scenarios;
        private bool explorationIntroduced;
        private NameEntryController nameEntry;
        private BacklogController backlog;
        [SerializeField] private DialoguePlayer dialogue;
        [SerializeField] private UiInputRouter input;
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private GameObject roomPanel;
        [SerializeField] private GameObject closeupPanel;
        [SerializeField] private ExplorationController exploration;
        [SerializeField] private ContentCatalog catalog;
        [SerializeField] private GameObject inspectionPanel;
        [SerializeField] private GameObject introNote;
        [SerializeField] private InventoryPanel inventory;
        [SerializeField] private GameObject questionPanel;
        [SerializeField] private InterrogationController interrogation;
        [SerializeField] private GameObject badEndingPanel;
        [SerializeField] private GameObject trueEndingPanel;
        [SerializeField] private ScreenEffects effects;
        [SerializeField] private AudioController audioController;
        [SerializeField] private BackgroundView backgroundView;
        public void ConfigureBackground(BackgroundView view) { backgroundView = view; }
        public void ConfigureAudio(AudioController controller) { audioController = controller; }

        private readonly GameSession session = new GameSession();
        private bool ready;
        private string transitionTarget;
        private Action transitionCoveredAction;
        private Action transitionFinishedAction;
        private bool transitionCovered;
        private bool effectsSubscribed;
        public GamePhase Phase => session.Phase;
        public string PlayerName => session.PlayerName;
        internal GameSession Session => session;
        public string CurrentFurnitureId => session.CurrentFurnitureId;
        public bool HasItem(string itemId) { return session.HasItem(itemId); }
        public bool IsInventoryFull() { return session.IsInventoryFull(); }
        public bool IsItemDetailsOpen => inventory != null && inventory.IsDetailsOpen;
        public bool IsBacklogOpen => backlog != null && backlog.IsOpen;
        public bool IsModalOpen => IsItemDetailsOpen || IsBacklogOpen;
        public bool CanOpenBacklog => ready && !IsTransitioning && !IsModalOpen
            && session.Phase != GamePhase.Title && session.Phase != GamePhase.NameEntry;
        public bool IsTransitioning => session.Phase == GamePhase.Transition || (effects != null && effects.IsBusy);
        public void ConfigureFades(ScreenEffects screenEffects) { effects = screenEffects; }

        public bool CanReadInventory()
        {
            if (!ready || IsTransitioning || IsModalOpen || session.QuestionCount >= 3) return false;
            switch (session.Phase)
            {
                case GamePhase.Intro:
                case GamePhase.Room:
                case GamePhase.Closeup:
                case GamePhase.FurnitureDescription:
                case GamePhase.Inspecting:
                case GamePhase.ItemChoice:
                case GamePhase.PickupReaction:
                case GamePhase.GuardReturn:
                case GamePhase.InterrogationIntro:
                case GamePhase.QuestionSelection:
                case GamePhase.QuestionPlayback:
                    return true;
                default:
                    return false;
            }
        }

        // 詳細はPhaseを変えない。背後のクリック可否だけを更新する。
        public void RefreshInteraction()
        {
            if (exploration != null) exploration.RefreshHotspots();
            if (interrogation != null) interrogation.RefreshQuestionOptions();
        }

        public void ConfigureInterrogation(InterrogationController controller, GameObject badEnd, GameObject trueEnd)
        {
            interrogation = controller;
            badEndingPanel = badEnd;
            trueEndingPanel = trueEnd;
        }

        public void ConfigureAcquisition(InventoryPanel slots, GameObject interrogationPreview)
        {
            inventory = slots;
            questionPanel = interrogationPreview;
        }

        public void ConfigureInspection(ContentCatalog content, GameObject popup, GameObject introductionNote)
        {
            catalog = content;
            inspectionPanel = popup;
            introNote = introductionNote;
        }

        public void ConfigureExploration(ExplorationController controller, GameObject closeup)
        {
            exploration = controller;
            closeupPanel = closeup;
        }

        public void Configure(ScenarioRepository source, TextAsset json, DialoguePlayer player,
            UiInputRouter router, GameObject title, GameObject conversation, GameObject room)
        {
            repository = source;
            scenario = json;
            dialogue = player;
            input = router;
            titlePanel = title;
            dialoguePanel = conversation;
            roomPanel = room;
        }

        private void Start()
        {
            ready = repository != null && ((scenarios != null && scenarios.Length > 0) || scenario != null) && dialogue != null && input != null
                && titlePanel != null && dialoguePanel != null && roomPanel != null;
            if (!ready)
            {
                Debug.LogError("[SeaOfMemories] Scene references are missing. Run the starter setup.", this);
                enabled = false;
                return;
            }
            ready = (scenarios != null && scenarios.Length > 0 ? repository.LoadAll(scenarios) : repository.Load(scenario)) && repository.Find("INTRO_START") != null;
            if (ready && catalog != null && (inspectionPanel == null || exploration == null
                || !exploration.HasInspectionUI || !catalog.Validate()))
            {
                Debug.LogError("[SeaOfMemories] Item inspection references or catalog data are incomplete.", this);
                ready = false;
            }
            if (ready && exploration != null && (closeupPanel == null || !exploration.IsConfigured))
            {
                Debug.LogError("[SeaOfMemories] Exploration references are incomplete. Check GameSystems and CloseupPanel.", this);
                ready = false;
            }
            if (ready && repository.HasChoices && !dialogue.HasChoiceUI)
            {
                Debug.LogError("[SeaOfMemories] Choice UI is missing. Stop Play mode, run Sea of Memories > 02 - Add Intro Choices, save MainScene, then play again.", this);
                ready = false;
            }
            if (ready && inventory != null)
            {
                if (!inventory.IsConfigured || questionPanel == null || exploration == null || !exploration.HasAcquisitionUI)
                {
                    Debug.LogError("[SeaOfMemories] Acquisition UI references are incomplete.", this);
                    ready = false;
                }
                string[] required = { "PICKUP_REACTION_1", "PICKUP_REACTION_2", "PICKUP_REACTION_3",
                    "FURNITURE_EMPTY", "GUARD_RETURN", "INTERROGATION_INTRO" };
                foreach (string id in required) if (repository.Find(id) == null) ready = false;
                if (inventory.HasDetailsSetup && !inventory.IsDetailsConfigured)
                {
                    Debug.LogError("[SeaOfMemories] Inventory reread references are incomplete. Check step 06.", this);
                    ready = false;
                }
            }
            if (ready && interrogation != null && (!interrogation.IsConfigured || badEndingPanel == null
                || trueEndingPanel == null || catalog == null || !catalog.ValidateInterrogation()))
            {
                Debug.LogError("[SeaOfMemories] Interrogation references or data are incomplete. Check step 07.", this);
                ready = false;
            }
            if (ready && interrogation != null && interrogation.HasVisualSetup
                && (!interrogation.HasQuestionVisuals || !dialogue.HasPortraitUI || !catalog.ValidatePortraits()))
            {
                Debug.LogError("[SeaOfMemories] Question visual references or portrait IDs are incomplete. Check step 10.", this);
                ready = false;
            }
            if (ready && repository.HasExpressions && (!dialogue.HasPortraitUI || catalog == null || !catalog.ValidatePortraits()))
            {
                Debug.LogError("[SeaOfMemories] Scenario expressions need the portrait setup and sprite catalog.", this);
                ready = false;
            }
            if (ready && (audioController != null || repository.HasAudioCommands)
                && (audioController == null || !audioController.IsConfigured || !dialogue.HasAudioUI
                    || catalog == null || !catalog.ValidateAudio()))
            {
                Debug.LogError("[SeaOfMemories] Audio references or IDs are incomplete. Check step 11.", this);
                ready = false;
            }
            if (ready && backgroundView != null && (!backgroundView.IsConfigured || catalog == null || !catalog.ValidateBackgrounds()))
            {
                Debug.LogError("[SeaOfMemories] Background references or sprite IDs are incomplete. Check step 12.", this);
                ready = false;
            }
            if (effects != null)
            {
                if (!effects.IsConfigured)
                {
                    Debug.LogError("[SeaOfMemories] Fade references are incomplete. Check step 08.", this);
                    ready = false;
                }
                effects.Covered += OnFadeCovered;
                effects.Completed += OnFadeCompleted;
                effects.Cancelled += OnFadeCancelled;
                effectsSubscribed = true;
            }
            if (ready && repository.HasBackgroundCommands && (backgroundView == null || !backgroundView.IsConfigured
                || catalog == null || !catalog.ValidateBackgrounds()))
            {
                Debug.LogError("[SeaOfMemories] Scenario backgrounds need the background view and catalog.", this);
                ready = false;
            }
            dialogue.ConfigureScenarioFades(this);
            Canvas canvas = titlePanel.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                foreach (Transform element in canvas.GetComponentsInChildren<Transform>(true))
                    if (element.name == "DevelopmentReturnButton" || element.name == "DevelopmentNote"
                        || element.name == "RoomNote" || element.name == "IntroNote"
                        || element.name == "Milestone" || element.name == "ProjectLabel")
                        element.gameObject.SetActive(false);
                backlog = BacklogController.Create(canvas, this, dialogue, input);
            }
            EnterTitle();
        }

        public void EnterTitle()
        {
            if (backlog != null) backlog.Close();
            if (dialogue != null) dialogue.History.Clear();
            ClearTransition();
            if (effects != null) effects.CancelFade();
            if (inventory != null) inventory.DismissDetails();
            input.SuppressForCurrentFrame();
            dialogue.StopPlayback();
            session.Reset();
            explorationIntroduced = false;
            if (exploration != null) exploration.ResetView();
            if (interrogation != null) interrogation.ResetView();
            if (ready && backgroundView != null) backgroundView.SetBackground("BG_02");
            TitleScreenController title = GetComponent<TitleScreenController>();
            if (title != null) title.ConfigureArtwork(titlePanel.transform, catalog);
            SetPhase(GamePhase.Title);
            if (ready && audioController != null)
            {
                audioController.StopAll();
                audioController.PlayBgm("BGM_01");
            }
        }

        public void StartNewGame()
        {
            if (!ready || session.Phase != GamePhase.Title) return;
            dialogue.History.Clear();
            input.SuppressForCurrentFrame();
            dialogue.StopPlayback();
            session.Reset();
            explorationIntroduced = false;
            if (exploration != null) exploration.ResetView();
            if (interrogation != null) interrogation.ResetView();
            if (nameEntry == null)
            {
                Canvas canvas = titlePanel.GetComponentInParent<Canvas>();
                if (canvas == null)
                {
                    Debug.LogError("[SeaOfMemories] Name entry needs the title Canvas.", this);
                    EnterTitle();
                    return;
                }
                nameEntry = NameEntryController.Create(canvas, dialogue.DialogueFont, ConfirmPlayerName);
            }
            nameEntry.ResetInput();
            SetPhase(GamePhase.NameEntry);
            if (audioController != null)
            {
                audioController.PlaySe("SE_01");
                audioController.FadeOutBgm(audioController.BgmFadeSeconds, () =>
                {
                    if (session.Phase == GamePhase.NameEntry && nameEntry != null) nameEntry.EnableInput();
                });
            }
            else nameEntry.EnableInput();
        }

        public bool ConfirmPlayerName(string value)
        {
            if (!ready || session.Phase != GamePhase.NameEntry || nameEntry == null
                || !nameEntry.CanConfirm || !session.TrySetPlayerName(value)) return false;
            input.SuppressForCurrentFrame();
            nameEntry.Hide();
            if (audioController != null)
            {
                audioController.PlaySe("SE_01");
                audioController.PlayBgm("BGM_05");
            }
            if (backgroundView != null)
                TransitionTo(GamePhase.Intro, "BG_03", null, () => dialogue.Play("INTRO_START", StartExploration));
            else
            {
                // メニュー12未適用の既存シーンはこれまでの即時表示を維持する。
                SetPhase(GamePhase.Intro);
                dialogue.Play("INTRO_START", StartExploration);
            }
            return true;
        }

        public void StartExploration()
        {
            dialogue.HideNarrativePortrait();
            if (!explorationIntroduced)
            {
                explorationIntroduced = true;
                TransitionTo(GamePhase.Intro, "BG_03", () => session.CurrentFurnitureId = "",
                    () => dialogue.Play("EXPLORATION_START", () => SetPhase(GamePhase.Room)));
            }
            else TransitionTo(GamePhase.Room, "BG_03", () => session.CurrentFurnitureId = "");
        }

        public bool CanInteractWithWorld()
        {
            return ready && !IsTransitioning && !IsModalOpen
                && (session.Phase == GamePhase.Room || session.Phase == GamePhase.Closeup);
        }

        public bool TryEnterCloseup(string furnitureId)
        {
            if (!CanInteractWithWorld() || session.Phase != GamePhase.Room
                || exploration == null || closeupPanel == null || string.IsNullOrWhiteSpace(furnitureId)) return false;
            string backgroundId = exploration.GetFurnitureBackgroundId(furnitureId);
            if (string.IsNullOrWhiteSpace(backgroundId)) return false;
            return TransitionTo(GamePhase.Closeup, backgroundId, () =>
            {
                session.CurrentFurnitureId = furnitureId;
                exploration.ShowFurniture(furnitureId);
            }, () =>
            {
                SetPhase(GamePhase.FurnitureDescription);
                dialogue.Play("FURNITURE_" + furnitureId + "_OPEN", () =>
                {
                    if (session.Phase != GamePhase.FurnitureDescription) return;
                    input.SuppressForCurrentFrame();
                    SetPhase(GamePhase.Closeup);
                });
            });
        }

        public bool TryStartInspection()
        {
            if (!CanInteractWithWorld() || session.Phase != GamePhase.Closeup
                || catalog == null || inspectionPanel == null || exploration == null || !exploration.HasInspectionUI) return false;
            input.SuppressForCurrentFrame();
            SetPhase(GamePhase.Inspecting);
            return true;
        }

        public void FinishInspection()
        {
            if (session.Phase != GamePhase.Inspecting) return;
            input.SuppressForCurrentFrame();
            SetPhase(GamePhase.Closeup);
        }

        public void BeginItemChoice()
        {
            if (session.Phase != GamePhase.Inspecting) return;
            input.SuppressForCurrentFrame();
            SetPhase(GamePhase.ItemChoice);
        }

        public void FinishItemChoice()
        {
            if (session.Phase != GamePhase.ItemChoice) return;
            input.SuppressForCurrentFrame();
            SetPhase(GamePhase.Closeup);
        }

        public bool TryAcquireItem(string itemId)
        {
            if (!ready || IsModalOpen || session.Phase != GamePhase.ItemChoice || catalog == null || inventory == null) return false;
            ItemDefinition item = catalog.GetItem(itemId);
            if (item == null || item.FurnitureId != session.CurrentFurnitureId || !session.TryAddItem(itemId)) return false;
            inventory.Refresh(session);
            exploration.RefreshHotspots();
            return true;
        }

        public void PlayAcquisitionReaction()
        {
            if (session.Phase != GamePhase.ItemChoice || session.ItemIds.Count == 0) return;
            input.SuppressForCurrentFrame();
            SetPhase(GamePhase.PickupReaction);
            if (session.IsInventoryFull()) { OnAcquisitionFinished(); return; }
            dialogue.Play("PICKUP_REACTION_" + session.ItemIds.Count, OnAcquisitionFinished);
        }

        public void OnAcquisitionFinished()
        {
            if (session.Phase != GamePhase.PickupReaction) return;
            // 4個目は追加の反応を挟まず、家具完了より優先して帰還へ進む。
            if (session.IsInventoryFull()) { StartGuardReturn(); return; }
            exploration.FinishAcquisitionSequence(session.CurrentFurnitureId);
        }

        public void PlayFurnitureEmpty()
        {
            if (session.Phase != GamePhase.PickupReaction) return;
            if (session.IsInventoryFull()) { StartGuardReturn(); return; }
            input.SuppressForCurrentFrame();
            dialogue.Play("FURNITURE_EMPTY", StartExploration);
        }

        public void ResumeCloseupAfterPickup()
        {
            if (session.Phase != GamePhase.PickupReaction) return;
            input.SuppressForCurrentFrame();
            SetPhase(GamePhase.Closeup);
        }

        public void StartGuardReturn()
        {
            if (session.Phase != GamePhase.PickupReaction || !session.IsInventoryFull()) return;
            TransitionTo(GamePhase.GuardReturn, "BG_04", () => session.CurrentFurnitureId = "",
                () => dialogue.Play("GUARD_RETURN", StartInterrogation));
        }

        public void StartInterrogation()
        {
            if (session.Phase != GamePhase.GuardReturn) return;
            TransitionTo(GamePhase.InterrogationIntro, "BG_04", null,
                () => dialogue.Play("INTERROGATION_INTRO", EnterQuestionSelection));
        }

        public void EnterQuestionSelection()
        {
            if (IsModalOpen || session.QuestionCount >= 3
                || (session.Phase != GamePhase.InterrogationIntro && session.Phase != GamePhase.QuestionPlayback)) return;
            input.SuppressForCurrentFrame();
            SetPhase(GamePhase.QuestionSelection);
            if (audioController != null) audioController.CrossFadeBgm("BGM_02", audioController.BgmFadeSeconds);
            if (interrogation != null) interrogation.BeginQuestionPhase();
        }

        public bool TryStartQuestionPlayback()
        {
            if (!ready || IsModalOpen || interrogation == null || session.Phase != GamePhase.QuestionSelection
                || session.QuestionCount >= 3) return false;
            input.SuppressForCurrentFrame();
            SetPhase(GamePhase.QuestionPlayback);
            return true;
        }

        public bool TryDismissQuestionWarning()
        {
            return !IsModalOpen && interrogation != null && session.Phase == GamePhase.QuestionSelection
                && interrogation.TryDismissWarning();
        }

        public void RefreshQuestionState()
        {
            if (inventory != null)
            {
                inventory.Refresh(session);
                // 最後の看守返答を再生するより前に灰色・操作不可にする。
                inventory.SetEnabled(session.QuestionCount < 3);
            }
            RefreshInteraction();
        }

        public void StartEnding(EndingType ending)
        {
            if (session.Phase != GamePhase.QuestionPlayback || session.QuestionCount != 3 || IsModalOpen) return;
            EndingType expected = session.Agitation == 100 ? EndingType.True : EndingType.Bad;
            if (ending != expected)
            {
                Debug.LogError("[SeaOfMemories] Ending does not match the accepted question results.", this);
                return;
            }
            
            bool white = ending == EndingType.True;
            string sequenceId = white ? "TRUE_END_START" : "BAD_END_START";

            input.SuppressForCurrentFrame();

            // 質問パート終了時に緊迫BGMを終了し、さざ波へ戻す
            if (audioController != null)
            {
                audioController.CrossFadeBgm(
                    "BGM_05",
                    audioController.BgmFadeSeconds
                );
            }

            SetPhase(white ? GamePhase.TrueEnding : GamePhase.BadEnding);
            dialogue.Play(sequenceId, EnterTitle);
        }

        public bool PlayGlassShake() { return backgroundView != null && backgroundView.PlayGlassShake(); }

        public bool CanAdvanceDialogue()
        {
            return !IsTransitioning && !IsModalOpen && SupportsDialogue(session.Phase);
        }

        private static bool SupportsDialogue(GamePhase phase)
        {
            return phase == GamePhase.FurnitureDescription || phase == GamePhase.Intro || phase == GamePhase.Inspecting || phase == GamePhase.PickupReaction
                || phase == GamePhase.GuardReturn || phase == GamePhase.InterrogationIntro
                || phase == GamePhase.QuestionPlayback || phase == GamePhase.BadEnding || phase == GamePhase.TrueEnding;
        }

        public bool TryPlayScenarioFade(bool white, Action completed)
        {
            if (!ready || IsTransitioning || IsModalOpen || backgroundView == null
                || effects == null || !effects.IsConfigured || !effects.isActiveAndEnabled
                || string.IsNullOrEmpty(backgroundView.CurrentBackgroundId)) return false;
            return TryPlayScenarioBackground("", white, completed);
        }

        public bool TryPlayScenarioBackground(string id, bool white, Action completed, bool forceFade = true)
        {
            if (!ready || IsTransitioning || IsModalOpen || backgroundView == null || !backgroundView.IsConfigured
                || catalog == null || effects == null || !effects.IsConfigured || !effects.isActiveAndEnabled) return false;
            string target = string.IsNullOrEmpty(id) ? backgroundView.CurrentBackgroundId : id;
            if (!ContentCatalog.IsBackgroundId(target) || !catalog.HasSpriteId(target)) return false;
            // A repeated background command is not a new scene transition.
            // Explicit BLACK_FADE / WHITE_FADE commands still play on the same image.
            if (!forceFade && target == backgroundView.CurrentBackgroundId)
            {
                completed?.Invoke();
                return true;
            }
            return TransitionTo(session.Phase, target, null, completed, false, white);
        }

        private bool TransitionTo(GamePhase destination, string targetId, Action covered, Action completed = null, bool ending = false, bool whiteFade = false)
        {
            if (IsTransitioning || IsModalOpen) return false;
            string backgroundId = ending ? "BG_04" : targetId;
            input.SuppressForCurrentFrame();
            if (effects == null || !effects.IsConfigured || !effects.isActiveAndEnabled)
            {
                // メニュー08未適用なら、前工程と同じ即時切替を維持する。
                covered?.Invoke();
                if (backgroundView != null) backgroundView.SetBackground(backgroundId);
                SetPhase(destination);
                completed?.Invoke();
                return true;
            }
            transitionTarget = targetId;
            transitionCovered = false;
            transitionCoveredAction = () =>
            {
                covered?.Invoke();
                if (backgroundView != null) backgroundView.SetBackground(backgroundId);
                ShowPanels(destination);
                // 新しい会話はフェード後に始める。直前の台詞を新画面へ持ち越さない。
                if (completed != null) dialoguePanel.SetActive(false);
            };
            transitionFinishedAction = () =>
            {
                SetPhase(destination);
                dialogue.Resume();
                completed?.Invoke();
            };
            dialogue.Pause();
            // Transition中は元の表示を保持し、暗幕が覆ってからだけ差し替える。
            SetPhase(GamePhase.Transition);
            if (ending) effects.PlayEndingFade(destination == GamePhase.TrueEnding, effects.EndingFadeSeconds);
            else if (whiteFade) effects.FadeScenario(targetId, true, effects.BackgroundFadeSeconds);
            else effects.FadeBackground(targetId, effects.BackgroundFadeSeconds);
            return true;
        }

        private void OnFadeCovered(string targetId)
        {
            if (session.Phase != GamePhase.Transition || transitionFinishedAction == null
                || targetId != transitionTarget || transitionCovered) return;
            transitionCovered = true;
            transitionCoveredAction?.Invoke();
        }

        private void OnFadeCompleted()
        {
            if (session.Phase != GamePhase.Transition || transitionFinishedAction == null) return;
            // 中断時は切替処理を一度だけ補い、入力ロックを残さない。
            if (!transitionCovered) OnFadeCovered(transitionTarget);
            Action completed = transitionFinishedAction;
            ClearTransition();
            input.SuppressForCurrentFrame();
            completed?.Invoke();
        }

        private void ClearTransition()
        {
            transitionTarget = null;
            transitionCoveredAction = null;
            transitionFinishedAction = null;
            transitionCovered = false;
        }

        private void OnFadeCancelled()
        {
            if (isActiveAndEnabled && Application.isPlaying) OnFadeCompleted();
        }

        private void OnEnable()
        {
            if (transitionFinishedAction != null && (effects == null || !effects.IsBusy)) OnFadeCompleted();
        }

        private void OnDestroy()
        {
            if (effects == null || !effectsSubscribed) return;
            effects.Covered -= OnFadeCovered;
            effects.Completed -= OnFadeCompleted;
            effects.Cancelled -= OnFadeCancelled;
            ClearTransition();
            effects.CancelFade();
        }

        private void SetPhase(GamePhase phase)
        {
            session.Phase = phase;
            if (phase != GamePhase.Transition) ShowPanels(phase);
            RefreshInteraction();
            Debug.Log("[SeaOfMemories] Phase = " + phase, this);
        }

        private static void ConfigureEndingHeading(GameObject panel, string title)
        {
            if (panel == null) return;
            Transform child = panel.transform.Find("EndingTitle");
            if (child == null) return;
            var label = child.GetComponent<TMPro.TMP_Text>();
            if (label == null) return;
            label.text = title;
            label.color = Color.white;
            label.raycastTarget = false;
            label.alignment = TMPro.TextAlignmentOptions.Center;
            RectTransform rect = label.rectTransform;
            rect.anchorMin = new Vector2(0.30f, 0.57f);
            rect.anchorMax = new Vector2(0.70f, 0.73f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var backing = new GameObject("EndingHeadingBacking", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            RectTransform plate = backing.GetComponent<RectTransform>();
            plate.SetParent(panel.transform, false);
            plate.anchorMin = rect.anchorMin; plate.anchorMax = rect.anchorMax;
            plate.offsetMin = new Vector2(-16f, -8f); plate.offsetMax = new Vector2(16f, 8f);
            var image = backing.GetComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.01f, 0.04f, 0.055f, 0.85f);
            image.raycastTarget = false;
            plate.SetSiblingIndex(child.GetSiblingIndex());
        }

        // 表示だけを差し替える。Phaseはフェード完了までTransitionのままにする。
        private void ShowPanels(GamePhase phase)
        {
            titlePanel.SetActive(phase == GamePhase.Title);
            if (nameEntry != null) nameEntry.gameObject.SetActive(phase == GamePhase.NameEntry);
            dialoguePanel.SetActive(SupportsDialogue(phase) || phase == GamePhase.ItemChoice
                || (phase == GamePhase.QuestionSelection && interrogation != null));
            if (backgroundView != null && (backgroundView.CurrentBackgroundId == "BG_08"
                || backgroundView.CurrentBackgroundId == "BG_09")) dialogue.HideNarrativePortrait();
            dialogue.ApplyPortraitStage(phase, dialoguePanel.transform);
            roomPanel.SetActive(phase == GamePhase.Room || phase == GamePhase.GuardReturn);
            if (closeupPanel != null) closeupPanel.SetActive(phase == GamePhase.FurnitureDescription || phase == GamePhase.Closeup || phase == GamePhase.Inspecting
                || phase == GamePhase.ItemChoice || phase == GamePhase.PickupReaction);
            if (inspectionPanel != null) inspectionPanel.SetActive(phase == GamePhase.Inspecting || phase == GamePhase.ItemChoice);
            if (questionPanel != null)
            {
                questionPanel.SetActive(
                    phase == GamePhase.QuestionSelection ||
                    phase == GamePhase.QuestionPlayback
                );
            }
            if (badEndingPanel != null) badEndingPanel.SetActive(false);
            if (trueEndingPanel != null) trueEndingPanel.SetActive(false);
            if (introNote != null) introNote.SetActive(false);
            // ポップアップの暗幕が台詞欄を塞がないよう、台詞欄を手前へ置く。
            if (dialoguePanel.activeSelf) dialoguePanel.transform.SetAsLastSibling();
            if (inventory != null)
            {
                inventory.Refresh(session);
                inventory.SetEnabled(session.QuestionCount < 3 && phase != GamePhase.BadEnding && phase != GamePhase.TrueEnding);
                inventory.gameObject.SetActive(phase != GamePhase.Title && phase != GamePhase.NameEntry && phase != GamePhase.BadEnding && phase != GamePhase.TrueEnding);
            }
        }
    }
}

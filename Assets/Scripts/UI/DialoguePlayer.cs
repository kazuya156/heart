using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SeaOfMemories
{
    public sealed class DialoguePlayer : MonoBehaviour
    {
        [SerializeField] private ScenarioRepository repository;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField, Min(1f)] private float charactersPerSecond = 40f;
        [SerializeField] private ChoicePanel choicePanel;
        [SerializeField] private UiInputRouter input;
        [SerializeField] private GuardPortraitView portrait;
        [SerializeField] private AudioController audioController;

        private GameFlowController scenarioFlow;
        public void ConfigureScenarioFades(GameFlowController controller) { scenarioFlow = controller; }

        private ScenarioSequence sequence;
        private Action onComplete;
        private int nodeIndex;
        private readonly PagedText pages = new PagedText();
        public int CurrentPage => pages.PageNumber;
        public int PageCount => pages.PageCount;
        private bool playing;
        private bool paused;
        private int playbackVersion;
        private bool revealOnPresentation;
        private Action onPresented;
        public bool IsWaitingForChoice { get; private set; }
        public bool HasChoiceUI => choicePanel != null && choicePanel.IsConfigured && input != null;
        public bool HasPortraitUI => portrait != null && portrait.IsConfigured;
        public bool HasAudioUI => audioController != null && audioController.IsConfigured;
        public void ConfigureAudio(AudioController controller) { audioController = controller; }
        public void ConfigurePortrait(GuardPortraitView view) { portrait = view; }

        public void Configure(ScenarioRepository source, TMP_Text speaker, TMP_Text body)
        {
            repository = source;
            speakerText = speaker;
            bodyText = body;
        }

        public void ConfigureChoices(ChoicePanel panel, UiInputRouter router)
        {
            choicePanel = panel;
            input = router;
        }

        public void Play(string sequenceId, Action completed)
        {
            BeginPlayback(sequenceId, completed, false, null);
        }

        // 特殊な1行も通常の背景/演出経路を通し、演出後に全文表示する。
        public void PlayImmediate(string sequenceId, Action presented)
        {
            BeginPlayback(sequenceId, null, true, presented);
        }

        private void BeginPlayback(string sequenceId, Action completed, bool revealImmediately, Action presented)
        {
            StopPlayback();
            if (repository == null || speakerText == null || bodyText == null)
            {
                Debug.LogError("[SeaOfMemories] DialoguePlayer references are missing.", this);
                return;
            }
            sequence = repository.Find(sequenceId);
            if (sequence == null) return;
            onComplete = completed;
            revealOnPresentation = revealImmediately;
            onPresented = presented;
            playing = true;
            nodeIndex = 0;
            ShowCurrentNode();
        }

        private void Update()
        {
            if (!playing || paused || IsWaitingForChoice) return;
            pages.Tick(Time.unscaledDeltaTime, charactersPerSecond);
        }

        public void RequestAdvance()
        {
            if (!playing || paused || IsWaitingForChoice) return;
            if (revealOnPresentation) { TryAdvanceImmediatePage(); return; }
            if (!pages.IsPageRevealed)
            {
                pages.RevealPage();
                return;
            }

            // 全文表示だけの入力は鳴らさず、実際のページ送りで一度だけ鳴らす。
            if (audioController != null) audioController.PlaySe("SE_01");
            // 同じノード内の改ページは、音・背景・表情指示を再実行しない。
            if (pages.TryNextPage()) return;
            nodeIndex++;
            if (nodeIndex < sequence.nodes.Length)
            {
                ShowCurrentNode();
                return;
            }

            if (!string.IsNullOrEmpty(sequence.nextSequenceId))
            {
                JumpToSequence(sequence.nextSequenceId);
                return;
            }

            // 呼出し元へ戻る前に再生フラグとコールバックを外し、多重完了を防ぐ。
            Action completed = onComplete;
            StopPlayback();
            completed?.Invoke();
        }

        private void JumpToSequence(string sequenceId)
        {
            sequence = repository.Find(sequenceId);
            if (sequence == null) { StopPlayback(); return; }
            nodeIndex = 0;
            ShowCurrentNode();
        }

        private void ShowCurrentNode()
        {
            pages.Clear();
            ScenarioNode node = sequence.nodes[nodeIndex];
            if (string.IsNullOrEmpty(node.backgroundId) && node.screenEffectId != "BLACK_FADE" && node.screenEffectId != "WHITE_FADE")
            { PresentCurrentNode(); return; }
            int request = playbackVersion;
            if (scenarioFlow == null || !scenarioFlow.TryPlayScenarioBackground(node.backgroundId, node.screenEffectId == "WHITE_FADE", () =>
            {
                if (playing && request == playbackVersion) PresentCurrentNode();
            }))
            {
                Debug.LogError("[SeaOfMemories] Scenario fade needs an active Flow, background and idle ScreenEffects.", this);
                StopPlayback();
            }
        }

        private void PresentCurrentNode()
        {
            ScenarioNode node = sequence.nodes[nodeIndex];
            if (!string.IsNullOrEmpty(node.bgmId) || !string.IsNullOrEmpty(node.seId))
            {
                if (!HasAudioUI)
                {
                    Debug.LogError("[SeaOfMemories] Scenario audio needs step 11 audio setup.", this);
                    StopPlayback();
                    return;
                }
                // ノードへの入場時だけ。文字更新・全文表示・Pause/Resumeでは呼ばない。
                if (!string.IsNullOrEmpty(node.bgmId)) audioController.CrossFadeBgm(node.bgmId, audioController.BgmFadeSeconds);
                if (!string.IsNullOrEmpty(node.seId)) audioController.PlaySe(node.seId);
            }
            if (node.screenEffectId == "GLASS_SHAKE")
            {
                if (!HasPortraitUI)
                {
                    Debug.LogError("[SeaOfMemories] GLASS_SHAKE needs the configured portrait view.", this);
                    StopPlayback();
                    return;
                }
                // 入場時に一度だけ。全文表示や読み返し復帰では再実行しない。
                portrait.Shake(portrait.ShakeSeconds, portrait.ShakePixels);
            }
            if (node.type == "choice")
            {
                if (!HasChoiceUI)
                {
                    Debug.LogError("[SeaOfMemories] Run Sea of Memories > 02 - Add Intro Choices, then save MainScene.", this);
                    StopPlayback();
                    return;
                }
                IsWaitingForChoice = true;
                var options = new List<ChoiceOption>();
                foreach (ChoiceOptionData option in node.options)
                    options.Add(new ChoiceOption(option.id, option.label));
                int requestVersion = playbackVersion;
                choicePanel.Show(options, id =>
                {
                    if (!playing || paused || !IsWaitingForChoice || requestVersion != playbackVersion) return;
                    // 選択したクリックとEnter/Spaceが同時でも、分岐先の台詞を飛ばさない。
                    input.SuppressForCurrentFrame();
                    IsWaitingForChoice = false;
                    foreach (ChoiceOptionData option in node.options)
                        if (option.id == id)
                        {
                            JumpToSequence(option.nextSequenceId);
                            return;
                        }
                    Debug.LogError("[SeaOfMemories] Unknown choice ID: " + id, this);
                    StopPlayback();
                });
                return;
            }
            if (!string.IsNullOrEmpty(node.expressionId))
            {
                if (!HasPortraitUI)
                {
                    Debug.LogError("[SeaOfMemories] A scenario expression needs the step 10 portrait view.", this);
                    StopPlayback();
                    return;
                }
                portrait.SetExpression(node.expressionId);
            }
            speakerText.text = node.speaker == "guard" ? "GUARD"
                : node.speaker == "hero" ? "YOU" : "";
            pages.Show(bodyText, node.text, revealOnPresentation);
            NotifyPresentationReady();
        }

        // 心情文/警告も長い場合は全ページを確認してから操作を解放する。
        public bool TryAdvanceImmediatePage()
        {
            if (!playing || paused || !revealOnPresentation || IsWaitingForChoice
                || !pages.TryNextPage(true)) return false;
            if (audioController != null) audioController.PlaySe("SE_01");
            NotifyPresentationReady();
            return true;
        }

        private void NotifyPresentationReady()
        {
            if (revealOnPresentation && pages.HasNextPage) return;
            Action presented = onPresented;
            onPresented = null;
            presented?.Invoke();
        }

        public void Pause()
        {
            paused = true;
            if (IsWaitingForChoice) choicePanel.SetInteractionEnabled(false);
        }

        public void Resume()
        {
            paused = false;
            if (IsWaitingForChoice) choicePanel.SetInteractionEnabled(true);
        }

        public void StopPlayback()
        {
            playing = false;
            paused = false;
            IsWaitingForChoice = false;
            playbackVersion++;
            if (choicePanel != null) choicePanel.Hide();
            onComplete = null;
            onPresented = null;
            revealOnPresentation = false;
            pages.Clear();
            sequence = null;
        }
    }
}


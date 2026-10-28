using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SeaOfMemories
{
    public sealed class UiInputRouter : MonoBehaviour
    {
        [SerializeField] private GameFlowController flow;
        [SerializeField] private DialoguePlayer dialogue;
        [SerializeField] private ItemDetailsViewer details;
        private int consumedFrame = -1;
        private bool gestureConsumed;
        public bool IsConsumedThisFrame => consumedFrame == Time.frameCount;
        public void ConfigureDetails(ItemDetailsViewer viewer) { details = viewer; }

        public void Configure(GameFlowController gameFlow, DialoguePlayer player)
        {
            flow = gameFlow;
            dialogue = player;
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            // Button.onClick arrives on mouse release; retain the latch through that frame.
            if (!AdvanceControlHeldOrReleased()) gestureConsumed = false;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
                RequestAdvance();
#endif
        }

        // マウスは台詞欄のButtonからここへ入る。生のMouse入力と二重に読まない。
        public void RequestAdvance()
        {
            if (IsConsumedThisFrame || gestureConsumed) return;
            if (flow != null && flow.IsBacklogOpen) { ConsumeGesture(); return; }
            if (flow != null && flow.IsTransitioning)
            {
                ConsumeGesture();
                return;
            }
            if (details != null && details.IsOpen)
            {
                ConsumeGesture();
                details.RequestAdvance();
                return;
            }
            if (flow == null || dialogue == null) return;
            if (flow.TryDismissQuestionWarning())
            {
                ConsumeGesture();
                return;
            }
            if (!flow.CanAdvanceDialogue()) return;
            if (dialogue.IsWaitingForChoice) return;
            ConsumeGesture();
            dialogue.RequestAdvance();
        }

        public void RequestCloseDetails()
        {
            if (IsConsumedThisFrame || gestureConsumed || details == null || !details.IsOpen) return;
            ConsumeGesture();
            details.Close();
        }

        private void ConsumeGesture()
        {
            consumedFrame = Time.frameCount;
#if ENABLE_INPUT_SYSTEM
            gestureConsumed = AdvanceControlHeldOrReleased();
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private static bool AdvanceControlHeldOrReleased()
        {
            Keyboard keys = Keyboard.current;
            Mouse mouse = Mouse.current;
            return (keys != null && (keys.enterKey.isPressed || keys.enterKey.wasReleasedThisFrame
                || keys.spaceKey.isPressed || keys.spaceKey.wasReleasedThisFrame))
                || (mouse != null && (mouse.leftButton.isPressed || mouse.leftButton.wasReleasedThisFrame));
        }
#endif

        public void SuppressForCurrentFrame() { ConsumeGesture(); }
    }
}

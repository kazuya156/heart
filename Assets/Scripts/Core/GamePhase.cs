namespace SeaOfMemories
{
    // 設計書の状態一覧。初回は Title / Intro / Room のみ使用する。
    public enum GamePhase
    {
        Title, Intro, Room, Closeup, Inspecting, ItemChoice,
        PickupReaction, GuardReturn, InterrogationIntro,
        QuestionSelection, QuestionPlayback,
        BadEnding, TrueEnding, Transition
    }
}

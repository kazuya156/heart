# M15c：台本背景切替の確認後

2026-10-25、KENより「できてる」と基本動作報告。確認用の導入A/B/合流の台詞と背景・フェード指定を除去済み。台本の背景切替機能は維持。

Play停止・保存後、feature/m15-scenario-backgroundsでFetch origin→Pull origin。通常の仮台本へ戻る。

backgroundIdはBG_TITLE / BG_01〜07。指定時はフェード中間点で背景変更し、完了後に本文を開始する。WHITE_FADEは白、ほかは黒。背景空欄は現状維持。即時表示専用のQUESTION_PROMPT/QUESTION_MISSING_INFOにはブロッキング背景変更を指定できない。読み返し側は演出を実行しない。

連打・読み返し・両エンド等の全条件を確認済みとは扱わない。次は仮素材での通しテストとWindows版の作成。

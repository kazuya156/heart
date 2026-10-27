# M15b：台本の黒白フェード

追加メニュー・JSON手編集は不要。feature/m15-scenario-fadesへ切り替え、MainSceneでPlay。

1. STARTから導入を読み、A「Ask directly.」を選ぶ。
2. 白フェードが入り、完了後に「[フェード確認用・仮台詞] 白いフェードが終わってから…」が表示される。
3. フェード中にクリック・Enter/Spaceを押しても、その後の台詞が飛ばない。
4. 通常どおりROOMまで進む。TEST: RETURN TO TITLEでタイトルへ戻り、再度START。
5. B「Ask politely.」では黒フェードと対応する仮台詞を確認する。
6. フェード後の全文表示、所持品の読み返しからの復帰でフェードを繰り返さないことを確認する。

選択肢A/Bの接続は変更しない。これらは確認用で、本番演出として確定しない。確認後の仮台詞・演出指示の除去はこちらで行う。

## 役割

- ScenarioRepository：BLACK_FADE / WHITE_FADEを受け入れる。backgroundIdはまだ未対応で拒否。
- DialoguePlayer：ノード開始時にFlowへ要求する。完了後にノードの音声・表情・本文表示を実行する。再生バージョンで古い完了通知を無視。
- GameFlowController：既存のTransitionと入力ロック・取消処理を再利用。遷移元Phaseへ復帰し、現在の背景を維持する。StartでDialoguePlayerへの参照を自動接続する。
- ScreenEffects：白フェードを任意背景IDで実行する入口を追加。既存Ending処理は変更しない。
- ContentCatalog：即時表示専用のQUESTION_PROMPT / QUESTION_MISSING_INFOへのブロッキングフェードを検証で拒否。これらは現在の全文即時表示と競合するため、必要なら専用対応を後続で行う。

GameSession・取得判定・質問スコア・ItemDetailsViewerは変更しない。読み返しプレイヤーは演出指示を実行しない。今回、背景を別画像に変える機能は追加していない。

静的構文・スコープ・変更範囲を確認。Unityコンパイル・実際のフェード・上の操作確認は未実行。

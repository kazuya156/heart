# M02：導入会話の選択肢

目的は、設計書T01の「導入の言葉遣いだけが変わり、どちらを選んでも同じ探索へ進む」を確認することです。本文と選択肢はすべて仮の英語です。本番シナリオ、ルート分岐、好感度の追加ではありません。

## 取り込みとUnityでの操作

1. Unityが再生中なら停止し、編集中のシーンをCtrl+Sで保存します。
2. GitHub DesktopでFetch originを押し、Current branchから`feature/m02-intro-choices`へ切り替えます。未コミットの変更について案内が出たら、内容を確認してから進めます。
3. Unityに戻り、スクリプトの読み込みが終わるのを待ちます。
4. `Assets/Scenes/MainScene.unity`を開き、上部メニュー`Sea of Memories > 02 - Add Intro Choices`を実行します。**01のシーン作成をやり直す必要はありません。**
5. Ctrl+SでMainSceneを保存します。既存オブジェクトは残り、CanvasのChoicePanelとDialoguePlayerの参照だけが追加されます。同じメニューを再実行しても重複作成しません。メニューによる追加直後はUndoも可能です。
6. Gameビューを16:9にしてPlay、STARTを押します。

02を実行する前に再生すると、ConsoleにChoice UI is missingと出て開始できません。再生を停止して02を実行し、保存後に再び再生してください。

## 正常な流れ

`INTRO_START`の最初の台詞を全文表示し、さらに送ると2つの選択肢が表示されます。

| 選ぶボタン | 表示される分岐台詞 | その後 |
| --- | --- | --- |
| Ask directly. | PLACEHOLDER A | INTRO_COMMON → ROOM |
| Ask politely. | PLACEHOLDER B | INTRO_COMMON → ROOM |

選択肢はマウスで選びます。Enter/Spaceは台詞送りに使い、選択肢を自動確定しません。文字表示中の送りは全文表示、全文表示後の送りは次の行です。

## クラスの役割

| ファイル | 役割 |
| --- | --- |
| `Assets/Scripts/UI/ChoicePanel.cs` | ボタンを作り、選択したIDを1回通知して閉じる共通UI。取得判断にも再利用できる形。台本の移動先とゲーム状態は管理しない。 |
| `Assets/Scripts/UI/DialoguePlayer.cs` | choiceノードで台詞送りを止め、選択したIDに対応する台本を再生。分岐先のnextSequenceIdで共通台本へ接続。Pause/Resumeは選択肢の操作も停止・再開する。 |
| `Assets/Scripts/Data/ScenarioRepository.cs` | 選択肢IDの重複、表示文・移動先の欠落、存在しない接続先を読込時に検出。自動接続の循環検出も維持。 |
| `Assets/Scripts/UI/UiInputRouter.cs` | 選択待ちは送り入力を通さず、選択したフレームの追加入力も抑止。 |
| `Assets/Scripts/Core/GameFlowController.cs` | 必要な選択肢UIの参照を開始時に確認。GamePhaseを変更する責務は引き続きここだけに置く。 |
| `Assets/Editor/SeaOfMemoriesChoiceSetup.cs` | 既存MainSceneに選択肢パネルと参照を追加するEditor専用ツール。ゲーム本体のビルドには含まれない。 |
| `Assets/Editor/SeaOfMemoriesSetup.cs` | 初回作成時にも選択肢UIを用意。既存MainSceneを上書きしない従来の保護は維持。 |
| `Assets/Data/Scenario/scenario.json` | START → A/B → COMMONの仮台本。選択によるGameSessionの書換えはない。 |

`ChoicePanel.Show(IReadOnlyList<ChoiceOption>, Action<string>)`は、UIへ表示するIDと文章、および「選ばれたときに呼ぶ関数」を受け取ります。DialoguePlayerは通知されたIDをJSONの選択肢と照合して移動先を決めます。これで、将来の「取得する／取得しない」にも同じUIを使えます。

## 動作確認（Unityでの確認待ち）

- [ ] Aを選び、Aの台詞 → COMMON → ROOMになる（T01）。
- [ ] ROOMのテスト用タイトル復帰ボタンで戻り、再度START。Bを選び、Bの台詞 → COMMON → ROOMになる（T01）。
- [ ] 選択肢表示中にEnter/Spaceや背景を押しても、勝手に確定・進行しない。
- [ ] 選択肢を表示させる入力と同じフレームでは、選択を確定しない。
- [ ] 選択とEnter/Spaceを同時に入力しても、選択先の台詞を飛ばさない。
- [ ] 選択肢を連打してもA/Bのどちらか一方だけが再生される。
- [ ] タイトルへ戻って再開始、またはPlay停止・再開後にも、選択肢と最初の台詞が正常に出る。
- [ ] 02をもう一度実行してもChoicePanelが増えない。
- [ ] 再生停止後に保存し、シーンを開き直しても参照が残る。
- [ ] Consoleに赤いエラーがなく、ROOM到達ログが1回だけ出る。

開発側では、C#10本の構文解析、JSONのA/B合流と全本文のプレースホルダー表示、C#スクリプトのGUID重複なしを確認しました。既存MainScene、GameSession、GamePhase、ProjectSettingsが変更されていないことも照合しています。Unityでのコンパイル、画面の収まり、実際のマウス・キーボード入力、WindowsビルドはKENの環境での確認が必要です。この変更だけでT01実機確認済みとは扱いません。

背景・看守の表情・音・フェードは後続工程です。現時点では対応していない演出コマンドをJSONに入れるとエラーとして検出し、黙って無視しません。

確認後の次の工程は家具3箇所の拡大／戻る操作です。取得ルールの実装はその後に小さく追加します。

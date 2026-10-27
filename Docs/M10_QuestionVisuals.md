# M10：質問の赤ビネット・表情・揺れ

## 目的と範囲

詳細設計書v0.1の4.4・5.3・5.8・7、T08/T09に沿って、質問の判定に視覚効果をつなぎます。M09の保存コミット3a7f8daが土台です。

| 判定 | 追加する見た目 |
| --- | --- |
| 有効1連続目 | 動揺度加算と同じタイミングで赤弱、動揺1の表情、0.18秒・最大8 Canvas単位の揺れ、既存の仮「ッ！」台詞 |
| 有効2連続目 | 赤中、動揺2の表情、同じ揺れ |
| 有効3連続目 | 赤強、限界の表情、同じ揺れ。所持欄無効化→最後の返答→True Endは従来どおり |
| ダミー | 新しい揺れと「ッ！」は出さず、残っている揺れを停止。赤だけ0.5秒で0へ。累積動揺度を消さず、固定返答へ |

ダミー時に表情をどう戻すかは資料に具体的な指定がないため、勝手な表情変更はせず現状を維持します。台本のexpressionIdで指定された場合のみ切り替えます。

ゲームルール・質問数・コンボ計算・動揺度計算・結末条件は変更しません。音響と本番素材は次工程です。「ッ！」も現段階は既存の英語プレースホルダーです。看守の表示は、既存の追及画面の仮領域を使います。

## Unityでの取り込み

1. Play停止 → Ctrl+S。
2. GitHub DesktopでFetch origin → Current branch → `feature/m10-question-visuals`。
3. Unityのコンパイルを待ち、`Assets/Scenes/MainScene.unity`を開きます。
4. **Sea of Memories → 10 - Add Question Visuals**。
5. Ctrl+S → Play → START。
6. 有効・ダミーの両方を使い、表情・赤・揺れを確認します。

メニュー10は追及パネル内にクリックを遮らないRedVignetteを追加し、GuardPortraitViewを既存の仮看守表示へ結び付けます。ContentCatalogに5個の表情IDを登録します。画像は空のままで構いません。既存の画像登録があれば保持し、必要なIDだけ補います。既存の演出設定があれば上書きしません。

本番の赤ビネットが未受領なので、中心が透明で周辺だけ赤くなる仮Spriteを起動時に生成します。画像ファイルの手作業配置や新規PNGの保存は不要です。

## 最初の確認

4個の取得はITEM_01（鉛筆）、ITEM_03（小さい服）、ITEM_05（証書）、ITEM_02（メモ）にします。

- 有効3回：ITEM_01 → ITEM_03 → ITEM_05。20→50→100、赤が弱→中→強、仮看守がSHAKEN 1→SHAKEN 2→LIMITとなり、各回で小さく揺れます。最後の返答後にTrue Endへ。
- ダミーを含む：ITEM_01 → ITEM_03 → ITEM_02。20→50→50、コンボは最後に0、赤が約0.5秒で消えます。ダミーでは「ッ！」や新しい揺れがなく、Bad Endへ。
- 途中でダミー：ITEM_01 → ITEM_02 → ITEM_03。20→20→40。赤消去後の有効質問は弱から再開します。速く進めた場合も、古い消去処理が新しい赤を後から消さないことを確認します。

有効時の効果は主人公の質問台詞を完了した瞬間に始め、仮「ッ！」と固定返答へ進みます。未取得警告・質問済み再選択・所持品読み返しでは判定も視覚効果も新規実行しません。

## ファイルと分担

| ファイル | 役割 |
| --- | --- |
| `Assets/Scripts/UI/ScreenEffects.cs` | SetComboLevel / FadeOutVignetteを追加。赤用と黒白フェード用のコルーチンを分離。赤消去ではIsBusyや入力ロックを立てない |
| `Assets/Scripts/UI/GuardPortraitView.cs`（新規） | SetExpressionで画像／仮表示を変更。Shakeで位置を一時的にずらし、終了・停止・非表示で元の位置へ戻す |
| `Assets/Scripts/Gameplay/InterrogationController.cs` | Accepted判定の直後に1回だけ見た目を更新。数値の再計算は行わない |
| `Assets/Scripts/Data/ContentCatalog.cs` | SpriteCatalogEntryとGetSpriteを追加。表情IDを画像へ対応させる。登録済みで画像nullなら仮表示、未登録IDはエラー |
| `Assets/Scripts/Data/ScenarioRepository.cs` | expressionIdを使用可能にし、台本に含まれる表情IDを列挙。背景・音など未実装の指示は引き続き拒否 |
| `Assets/Scripts/UI/DialoguePlayer.cs` | 本編のline開始時にexpressionIdをGuardPortraitViewへ渡す。空なら維持。Pause/Resumeで同じ行を再実行しない |
| `Assets/Scripts/Core/GameFlowController.cs` | 演出参照・台本の表情IDを起動時に検証。Phase更新と進行条件は維持 |
| `Assets/Editor/SeaOfMemoriesQuestionVisualsSetup.cs`（新規） | メニュー10で仮画像表示・赤パネル・表情登録・参照を設定 |
| `Assets/Editor/SeaOfMemoriesQuestionVisualChecks.cs`（新規） | 実際の演出クラスで、強度、表情ID、揺れの位置復元、赤消去、中断とリセットを確認する補助メニュー |

GameSession、scenario.json、アイテム定義、ItemDetailsViewerは変更していません。読み返しは従来の独立したline再生だけなので表情や演出を呼びません。すでに始まった揺れ・赤消去は実時間で完了し、読み返しで再発火はしません。

## 画像と表情ID

GameSystems → ContentCatalog → Spritesに、本番のSpriteを登録できます。

| ID | 用途 |
| --- | --- |
| CH_01_normal | 通常。タイトル・新しい周回へのリセットで使用 |
| shaken1 | 有効コンボ1 |
| shaken2 | 有効コンボ2 |
| limit | 有効コンボ3 |
| smile | 台本で指定されたときに使用。未指定の発火条件は作らない |

資料の短い表記に合わせた登録です。共通接頭辞付きのCH_01_shaken1 / CH_01_shaken2 / CH_01_limit / CH_01_smileも同じ枠への別名として解決します。両方を重複登録する必要はありません。

台本のlineに`"expressionId":"shaken1"`等を指定できます。空なら現状を維持します。共通の反応台本に特定の表情を指定すると、コンボによる表情をその行で上書きするので、維持したい行は空にします。今回の仮台本は変更していません。

## Inspector調整

GameSystems → ScreenEffects：Weak Alpha / Medium Alpha / Strong Alpha（初期0.18 / 0.32 / 0.48）、Dummy Fade Seconds（初期0.5）。色の強さは提出素材と合わせて実機調整します。

GameSystems → GuardPortraitView：Shake Seconds（初期0.18）、Shake Pixels（初期8）。基準1920×1080のCanvas単位です。見えにくい場合は一時的に0.5秒・24へ延ばして動きを確認し、最終的に初期値へ戻します。

赤パネルは追及画面の内部に置き、読み返し・黒白フェードより後ろで描きます。Raycast TargetはOFFなので、赤が残っていても質問・台詞送り・所持枠を操作できます。立ち絵が揺れるのは看守領域だけで、UI全体は動かしません。

## 確認項目

| 対象 | 完了条件 |
| --- | --- |
| T08 視覚効果 | 有効3回の弱/中/強、各1回の表情と揺れ、仮「ッ！」、固定返答の順序が一致 |
| T09 視覚効果 | ダミーでコンボ0、赤が0.5秒で0。累積値は維持。「ッ！」・新しい揺れは出ない |
| 入力 | 赤消去中も固定返答を進められ、読み返しも可能。演出がクリックを遮らない |
| 再入 | 全文表示・連打で同じ質問の演出が何度も始まらない。未取得・質問済みでは演出が始まらない |
| 位置 | 揺れ終了・ダミー・リセット後、看守が元の位置に戻り、周回でずれが蓄積しない |
| 読み返し | 本編を止めて説明を読み、閉じると元の行・文字位置へ。表情変更や揺れが再発火しない |
| T11/周回 | 最後の判定時の所持欄無効化、返答終了後の黒/白フェードを維持。タイトル→STARTで赤0・通常表情・揺れなし |
| M09回帰 | 家具・アイテムのホバーとOSカーソル復帰が変わらない |

Playのタイトル画面で **Sea of Memories → Checks → Question Visuals (Play mode)**を実行すると、短時間だけ一時的なCanvasで実際のクラスを確認します。強度の順序・表情IDの別名・空なら維持・timeScale=0での完了・位置復元・古い赤消去の中断・0秒・リセットを検証し、時間設定を戻して一時オブジェクトを破棄します。既存のゲームの所持・質問・台本は操作しません。

成功ログは`Question visual checks passed: intensity levels, expression IDs...`です。補助チェックとゲーム進行との結合確認は別です。作成環境では35個のC#ファイルの構文・変数スコープ・Phase更新元と既存データ未変更を静的に確認しました。Unity/C#コンパイル、補助チェックと実際の見た目は未実行です。

## 保存と次の作業

確認後はPlay停止 → Ctrl+S。GitHub DesktopのSummaryを`Save question visuals scene`として、Commit to feature/m10-question-visuals → Push origin。今回はMainSceneに表情IDの登録と演出参照を保存します。仮画像のPNG保存は不要です。

次はBGM/SEのID管理と再生を追加し、シナリオ・日本語フォント・背景・立ち絵などの本番素材をつなぎ、Windowsビルドへ進みます。

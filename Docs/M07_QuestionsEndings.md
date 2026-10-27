# M07：3回の質問と仮エンディング

## 今回の目的

6個の情報を全て質問欄に表示し、持っている情報のうち3個だけ質問できるようにします。判定をGameSessionにまとめ、最後の看守返答の後にTrue/Badの仮台本を再生し、タイトルへ戻します。

一次仕様：Unity詳細設計書v0.1の4.4・5.2・5.7・6.2とT06/T08〜T12。M06のシーン保存コミット9b4c970が土台です。ゲームルールは変更しません。

本番台詞・画像は未受領なので英語の`[PLACEHOLDER]`を使います。現状のTMPフォントで読めるように、未取得表示はASCIIの`???`と`LOCK`、警告は`[PLACEHOLDER WARNING] I do not have the required information.`、有効反応は`[PLACEHOLDER REACTION] !`です。最終台本と日本語フォント投入時に、仕様の「？？？」「必要な情報を持っていない」「ッ！」へ差し替えます。

**音・揺れ・赤い画面の演出・0.5秒の消去・フェード・BGMはこの工程では未実装です。** 論理部分が通っても演出を含む受け入れテスト全体を完了にはしません。

## Unityでの取り込み

1. Playを停止し、Ctrl+Sで現在のシーンを保存します。
2. GitHub DesktopでFetch origin → Current branch → `feature/m07-questions-endings`。
3. Unityのコンパイルを待ち、`Assets/Scenes/MainScene.unity`を開きます。
4. **Sea of Memories → 07 - Add Questions and Endings**を実行します。
5. Consoleに`Question checks passed: 8640 acquisition/question order combinations...`の成功ログが出たことを確認します。チェック失敗時はUIを追加せず止まります。
6. Ctrl+SでMainSceneを保存して、Play → START。

メニュー07は質問6個、看守の仮表示、動揺度ゲージとコンボ、黒/白の仮エンディング画面、参照を追加します。M05のINTERROGATION見出しは削除せず非表示にします。既存の6個のItemDefinitionでは質問名・質問台本ID・返答台本IDだけを設定し、アセットを保存します。所有家具・有効/ダミー・説明・画像は保持します。

二重実行時は既存UIを上書きせず停止します。メニュー06以前のシーンには先に06を適用してください。

## 最初の確認：TrueとBad

取得する4個は次の組合せにします。

| ID | 場所 | 今回の仮素材での意味 | 判定 |
| --- | --- | --- | --- |
| ITEM_01 | DESK | 不揃いの鉛筆 | 有効 |
| ITEM_03 | BED | 小さい服 | 有効 |
| ITEM_05 | WINDOW | 証書 | 有効 |
| ITEM_02 | DESK | 書き殴ったメモ | ダミー |

この順に取ると、最後のITEM_02はDESKの2品目にもなるので、M05の4個目優先の回帰確認にもなります。4個目反応 → 看守帰還 → 追及導入の後、6件の質問が表示されます。

- True：ITEM_01 → ITEM_03 → ITEM_05を質問。動揺度20 → 50 → 100、コンボ1 → 2 → 3。持っているだけのITEM_02では減点しません。
- Bad：タイトルに戻って同じ4個を取り、ITEM_01 → ITEM_03 → ITEM_02を質問。動揺度20 → 50 → 50、コンボ1 → 2 → 0。
- 途中でダミー：ITEM_01 → ITEM_02 → ITEM_03なら、動揺度20 → 20 → 40、コンボ1 → 0 → 1。蓄積値を消さないことも確認します。

質問は1クリックで開始します。台詞はいつもどおり文章欄／Enter／Spaceで進めます。有効質問は主人公 → 仮の「ッ！」反応 → 固定の看守返答、ダミーは主人公 → 固定の看守返答です。

## 判定のタイミング

1. 質問を選ぶと質問UIをロックし、主人公の質問を再生します。この途中では、まだ質問回数を増やしません。
2. 質問台詞が完了するとGameSession.TryApplyQuestionで回数・質問済み・コンボ・動揺度を1回だけ更新します。
3. **3問目はその瞬間に所持欄を灰色・クリック不可にし、質問選択肢を隠します。** 主人公の質問途中までは読み返しが可能です。
4. 有効反応と最後の固定返答を再生し終わってからGameFlowController.StartEndingを呼びます。
5. 結末の仮台本を最後まで進めるとタイトルに戻ります。STARTで取得・質問・コンボ・動揺度が初期化された新しい周回になります。

## ファイルと連携

| ファイル | 担当 |
| --- | --- |
| `Assets/Scripts/Core/GameSession.cs` | HasAsked/TryApplyQuestion。所持・未使用・3回未満を確認し、Acceptedのときだけ数値と質問済みを変更。ダミーはコンボだけ0、累積動揺度は維持。 |
| `Assets/Scripts/Core/QuestionResult.cs`（新規） | 採否、増分、変更後の数値、最終問か、結末をまとめて返す値。 |
| `Assets/Scripts/Gameplay/InterrogationController.cs`（新規） | 6質問の表示、警告、質問→判定→反応→返答の順序と完了通知。Phaseは書き換えない。 |
| `Assets/Scripts/Core/GameFlowController.cs` | QuestionSelection/QuestionPlayback/Endingの移行。3問目の即時無効化、結末からタイトルへ戻す。唯一のPhase更新元。 |
| `Assets/Scripts/Data/ItemDefinition.cs` | 質問名を追加。既存の質問・返答IDと共にデータとして設定。 |
| `Assets/Scripts/Data/ContentCatalog.cs` | 質問・返答・心情文・反応・結末の参照とline限定を検証。 |
| `Assets/Scripts/UI/UiInputRouter.cs` | 詳細画面を優先し、その次に未取得警告の解除、本編送りを処理。同一フレームの重複を防ぐ。 |
| `Assets/Data/Scenario/scenario.json` | 既存17シーケンスを保持し、質問6・返答6・心情/警告/反応3・結末2を追加。全て仮台本。 |
| `Assets/Editor/SeaOfMemoriesInterrogationSetup.cs`（新規） | メニュー07でUIと参照を作成し、既存6定義に質問情報を設定。 |
| `Assets/Editor/SeaOfMemoriesInterrogationChecks.cs`（新規） | 実際のGameSessionと一時的なItemDefinitionを使ったC#チェック。プロジェクトの素材は変更しない。 |

GameSessionは「判定」、InterrogationControllerは「会話の順番」、GameFlowControllerは「画面の状態」を受け持ちます。最後の質問を押した瞬間に結末へ飛ばず、返答完了を待つのはこの分担のためです。

## 動作確認

| 対象 | 操作・完了条件 |
| --- | --- |
| T06 未取得 | 未取得のITEM_04/06の`???`を押す。警告が全文表示され、回数・コンボ・動揺度・質問済みは変化しない。文章欄の次クリック／Enter／Spaceで通常の心情文に戻る。警告中は質問ボタンを一時停止し、所持品の読み返しは可能。 |
| T07 回帰 | 質問台詞・有効反応・看守返答・警告中に右の所持枠を開閉。元の行と文字位置に復帰。閉じたクリックで元の台詞が進まない。 |
| T08 論理 | 有効3個を使って100、Trueの仮台本を経てタイトルへ。持っているだけのダミーは無関係。 |
| T09 論理 | 有効2個とダミー1個で100未満、Badの仮台本へ。ダミーでコンボ0、累積動揺度は維持。赤消去/音の部分は後工程。 |
| T10 | 質問済みは灰色・再使用不可。連打やEnter/Space同時入力でも数値が二重更新されない。4問目は始まらない。 |
| T11 論理 | 3問目の主人公台詞を完了した時点で所持欄が灰色・操作不可、質問欄が消える。看守返答の途中でEndingに飛ばず、返答完了後にだけ結末へ。 |
| T12 | 結末後のSTARTで6品が再表示され、所持0、質問0、動揺0、コンボ0、質問済みなし。以前無効になった家具も使える。 |
| M05回帰 | 4個目が家具2品目でもFURNITURE_EMPTYを挟まず帰還へ。 |
| 表示 | 基準1920×1080で6件・メーター・会話・右の4枠が重ならず、仮文が枠内に収まる。 |

### 同梱C#チェックの範囲

6P4通りの取得順 × 4P3通りの質問順 = **8640ケース**です。仕様から独立して書いた8種類の有効/ダミーの期待表で、各質問後の増分・合計・コンボ・質問数・最終問と結末を検証します。未取得・null・質問済み・上限超過での拒否と無変更、所持順維持、Resetも検証します。

チェックはメニュー07で実行し、単独でも **Sea of Memories → Checks → Question Rules**から実行できます。作成環境では26個のC#ファイルの構文・ローカル変数のスコープ、Phase更新元と台本参照を静的に確認しました。**UnityやC#コンパイラが作成環境にないため、同梱C#チェックとUnityコンパイルはKENの環境で初めて実行されます。** 静的確認はコンパイル成功の保証ではありません。

## 保存と次の作業

確認後はPlay停止 → Ctrl+S。GitHub DesktopでMainSceneとITEM_01〜06.assetの変更を確認し、Summaryに`Save questions and endings scene` → Commit to feature/m07-questions-endings → Push origin。意図した変更は質問UIのシーン保存と6個の定義の質問情報です。

次は演出・音響・日本語フォントと本番素材をつなぎ、その後Windowsビルドと全受け入れテストへ進みます。仮素材で全編を通せることと、提出版の完成は分けて管理します。

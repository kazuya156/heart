# M05：取得判断・持ち物4枠・看守帰還

説明を読み終えると共通ChoicePanelでTAKE（取得する）／LEAVE（取得しない）を選べます。取得した情報は右の4枠へ取得順に並び、現地のボタンは消えます。取得後の台詞、家具完了、4個目での看守帰還を接続します。

今回の確認終点は、看守帰還と追及導入を読み終えた後のINTERROGATION画面です。3回の質問・結末と所持品からの説明読み返しは次工程です。画像・本文・音・フェードは引き続き仮表示または後続工程です。

## 取り込みとUnityでの操作

1. Unityの再生を停止し、Ctrl+Sで保存します。
2. GitHub DesktopでFetch origin、Current branchから`feature/m05-acquisition-loop`へ切り替えます。
3. スクリプトの読込完了後、`Assets/Scenes/MainScene.unity`を開き、`Sea of Memories > 05 - Add Acquisition Loop`を実行します。
4. Ctrl+SでMainSceneを保存します。01〜04のやり直しは不要です。
5. Play → START → 導入 → ROOM → 家具 → アイテムへ進みます。
6. 説明を最後まで送り、TAKEかLEAVEをマウスで選びます。取得後の台詞もクリック／Enter／Spaceで送ります。

メニュー05は、右4枠と追及用の仮画面を作成し、既存パネルを左80%へ調整します。既存オブジェクトや定義アセットを削除しません。InventoryPanelがすでにある場合は重複作成を止めます。メニュー実行前のM04シーンは、従来どおり説明読了で家具へ戻ります。

## 終了時の優先順位

取得した瞬間に、所持数・現地ボタン・所持欄を更新します。その後、取得数に合う`PICKUP_REACTION_1`〜`4`を再生し、**読み終えてから**次へ進みます。

| 優先 | 条件 | 次の動き |
| --- | --- | --- |
| 1 | 所持4個 | 家具完了台詞を省略し、GUARD_RETURN → INTERROGATION_INTRO → INTERROGATION画面。 |
| 2 | 所持4個未満、同じ家具の2品が取得済み | FURNITURE_EMPTY → ROOM。取り終えた家具ボタンは無効。 |
| 3 | 所持4個未満、家具に未取得品あり | 同じ家具の拡大画面へ戻る。 |

LEAVEなら取得数を変えず、アイテムを残して同じ家具へ戻ります。同じ説明を何度でも読み直せます。取得済みの品は再取得できず、捨てる操作もありません。有効／ダミーを所持しただけでは質問状態を変更しません。

## 最初の確認手順（T02〜T05の動作確認）

1. BEDで目覚まし時計を読み、LEAVE。所持0/4でアイテムが残ることを確認します。
2. 同じ目覚まし時計をもう一度調べ、TAKE。所持欄の1番目に時計が入り、現地から消えます。PICKUP 1を読了します。
3. BEDの小さな服をTAKE。2番目に服が入り、PICKUP 2 → FURNITURE EMPTY → ROOMの順に進みます。BEDが押せなくなることを確認します。
4. WINDOWの海をTAKE。3番目に海が入り、PICKUP 3の後もWINDOWに残ります。
5. WINDOWの賞状をTAKE。4番目に賞状が入り、PICKUP 4 → GUARD RETURNの看守・主人公の2行 → INTERROGATION INTROの2行へ進みます。**ここではFURNITURE EMPTYが出ない**ことを確認します。
6. INTERROGATIONの見出しが出れば今回の終点です。質問ボタンは次工程なので、ここでPlayを停止します。

表示名は英語プレースホルダーです。時計＝Alarm clock at 2 AM、服＝Small clothes、海＝Endless sea、賞状＝Certificate with brother's name。上の取得順と違う順番でも、所持欄は取得した順番を保ちます。

## 追加の確認項目

- [ ] LEAVEを繰り返しても取得数が増えず、アイテムが残る（T02）。
- [ ] 1〜3個目の取得反応が、それぞれ別のPICKUP番号で表示される（T03）。
- [ ] 2品取得済み・合計3個以下なら家具完了台詞の後でROOMへ戻り、家具が無効になる（T04）。
- [ ] 4個目が家具2品目でも家具完了台詞を出さず、4個目反応の終了後に看守帰還へ進む（T05）。
- [ ] TAKE／LEAVEの連打やEnter/Spaceとの同時入力でも、二重取得・二重遷移・次の台詞の読み飛ばしがない。
- [ ] 取得反応・看守帰還・追及導入の途中で家具やBACKを操作できない。
- [ ] Play停止・再開、またはROOMのテスト用タイトル復帰から再開始したとき、所持0/4と全アイテムが復元される。
- [ ] 05を再実行しても所持欄や仮画面が増えない。
- [ ] 保存・シーンの開き直し後も設定が残り、Consoleに赤いエラーがない。

所持欄のクリック読み返しはまだ接続していません。T07の完了扱いにはしません。TAKE／LEAVEで使う共通ChoicePanelは、導入選択肢と同じ表示部品です。

## 作成・変更ファイルと役割

| ファイル | 役割 |
| --- | --- |
| `Assets/Scripts/Core/GameSession.cs` | TryAddItem／IsInventoryFullを追加。固定6ID、重複不可、最大4個、取得順を守る。公開リストは読み取り専用にして外部からの書換えを防ぐ。 |
| `Assets/Scripts/Gameplay/ExplorationController.cs` | 説明の終了通知から取得選択肢を表示。取得・見送り、現地ボタン、家具完了を処理する。 |
| `Assets/Scripts/Core/GameFlowController.cs` | 取得後台詞の完了を待ち、4個目を最優先して看守帰還へ接続。Phase変更はここへ集約。 |
| `Assets/Scripts/UI/InventoryPanel.cs` | 所持順の4枠とアイコン／名前を描画。画像未受領時は名前を表示する。 |
| `Assets/Scripts/UI/UiInputRouter.cs` | 取得後・看守帰還・追及導入でも会話送りを受け付け、選択待ちや同一フレームの入力を抑止する。 |
| `Assets/Editor/SeaOfMemoriesAcquisitionSetup.cs` | 右4枠・仮追及画面・各参照を既存シーンへ追加。 |
| `Assets/Editor/SeaOfMemoriesAcquisitionChecks.cs` | 実際のGameSessionを使ったEditor専用の回帰チェック。 |
| `Assets/Data/Scenario/scenario.json` | 取得反応4本、FURNITURE_EMPTY、GUARD_RETURN、INTERROGATION_INTROの仮台本を追加。 |

取得処理は「データを追加する」「見た目を更新する」「台詞を再生する」を分けています。GameSessionはUnityを知らず、追加が成功したかどうかを返します。ExplorationControllerはその結果で現地表示を更新し、GameFlowControllerは台詞の完了通知を受けて次の段階を決めます。これにより4個目の処理が文字表示の途中で始まることを防ぎます。

## チェックの実行状況

メニュー05はUIを変更する前に、`SeaOfMemoriesAcquisitionChecks`を実行します。成功時はConsoleに`Acquisition checks passed: 360 ordered four-item selections...`と出ます。同じチェックは`Sea of Memories > Checks > Acquisition Rules`から単独でも実行できます。

6個から4個を選ぶ取得順360通りについて、実際のGameSessionの追加・順序・重複拒否・上限・初期化を検査します。無効IDの拒否、公開リストの外部書換え防止、取得だけでは質問数・コンボ・動揺度が変わらないことも検査します。シーンやアセットは変更しません。

開発側ではC#20本の構文解析、追加台本と既存台本の整合、4個目を先に判定する箇所、既存9定義と照合対象のGUIDの維持を静的に確認しました。こちらの環境にはUnity／C#実行環境がないため、同梱C#チェック自体と画面・実入力の実行はKENのUnityで確認します。

正常に動いたらPlay停止 → Ctrl+S。GitHub DesktopでMainSceneを確認し、Summary `Save acquisition loop scene`でコミットしてPush originします。今回は新しい定義アセットを作成しません。

次は、所持枠から説明を独立パネルで読み返し、元の会話を同じ位置から再開する処理です。その後、3質問と両エンディングへ進みます。

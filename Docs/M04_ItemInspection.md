# M04：アイテムの仮表示と説明再生

家具の拡大画面に2つずつ仮アイテムを置き、クリックすると画像ポップアップと説明文を表示します。今回の小工程では説明を読み終えると元の家具へ戻ります。取得・取得しないの判断と所持品更新は次工程です。**読了後に取得選択肢を出す最終仕様を、自動復帰へ変更するものではありません。**

## 取り込みとUnityでの操作

1. 前回のDESK・BED・WINDOWとBACKが動くことを確認します。不具合があれば先に修正します。
2. Unityの再生を停止し、Ctrl+Sで保存します。
3. GitHub DesktopでFetch origin、Current branchから`feature/m04-item-inspection`へ切り替えます。
4. Unityのスクリプト読込完了後、`Assets/Scenes/MainScene.unity`を開き、`Sea of Memories > 04 - Add Item Inspection`を実行します。
5. Ctrl+SでMainSceneを保存し、Play → START → 導入 → ROOM → 家具へ進みます。01〜03をやり直す必要はありません。
6. 2つあるアイテムのどちらかをクリックします。仮画像と説明を読み、最後のページの後にさらに送ると同じ家具へ戻ります。

説明は台詞ウィンドウのクリック、Enter、Spaceで送れます。文字表示中の入力は全文表示、表示完了後の入力は次ページです。説明中に背後のアイテムやBACKを押しても画面は切り替わりません。読み終えれば同じアイテムを何度でも調べ直せます。

## 家具とアイテム

| 家具 | アイテム1（有効） | アイテム2（ダミー） |
| --- | --- | --- |
| DESK（机） | ITEM_01：不揃いな鉛筆 / Uneven pencils | ITEM_02：落書き / Scribbles |
| BED（ベッド） | ITEM_03：小さな服 / Small clothes | ITEM_04：午前2時の目覚まし時計 / Alarm clock at 2 AM |
| WINDOW（窓辺） | ITEM_05：弟の名前の賞状 / Certificate with brother's name | ITEM_06：無限に広がる海 / Endless sea |

本文は各2行の英語プレースホルダーです。物語の正式な説明は未投入です。有効/ダミーのデータは設計書どおり設定しますが、取得・質問・動揺度の処理はこの工程では実行しません。

## 作成・変更するファイル

| ファイル | 役割 |
| --- | --- |
| `Assets/Scripts/Data/ItemDefinition.cs` | アイテムID・表示名・有効/ダミー・所属家具・画像・関連台本IDを持つデータアセットの型。 |
| `Assets/Scripts/Data/ContentCatalog.cs` | 家具とアイテムの定義を集約。固定ID、所属関係、説明台本の参照を開始時に検査する。背景・音のID検索は演出工程で追加する。 |
| `Assets/Scripts/UI/ItemInspectionView.cs` | 初回調査の画像と名前を表示。画像が未指定なら仮表示を出す。所持品の独立した読み返し用ItemDetailsViewerとは別の部品。 |
| `Assets/Scripts/Gameplay/ExplorationController.cs` | 家具に合う2アイテムを表示し、InspectItemで説明を開始。終了時に同じ家具へ戻す。 |
| `Assets/Scripts/Core/GameFlowController.cs` | Inspectingへの移行・復帰とパネルの重なり順を管理。進行状態を書き換えるのは引き続きここだけ。 |
| `Assets/Scripts/Core/GameSession.cs` | 所持確認用HasItemを追加。取得処理はまだ追加しない。 |
| `Assets/Scripts/UI/UiInputRouter.cs` | Inspecting中の台詞送りを許可。調査開始・終了の同じフレームに入力を重ねて処理しない。 |
| `Assets/Editor/SeaOfMemoriesInspectionSetup.cs` | アイテム定義6個、2つのボタン、画像ポップアップ、各参照を既存シーンへ追加。 |
| `Assets/Data/Scenario/scenario.json` | `DESC_ITEM_01`〜`DESC_ITEM_06`に各2行の仮説明を追加。導入のA/B合流は維持。 |

メニュー04の実行時には、`Assets/Data/Definitions/ITEM_01.asset`〜`ITEM_06.asset`と各.metaも作成されます。既存の家具アセットはそのまま参照します。ItemDefinitionのIcon/Large Imageへ将来素材を登録でき、Large Imageがあれば調査画面で表示します。現在の調査対象ボタンは名前による仮表示です。

`ItemDefinition`は内容を保存し、`ContentCatalog`はIDから探す窓口、`ExplorationController`は調査の手順を担当します。文章を1文字ずつ出す処理は既存の`DialoguePlayer`を再利用します。この分担により、画像や文章を替えるときに調査の手順を書き直す必要がありません。

## 動作確認（Unityでの確認待ち）

- [ ] 各家具に対応する2アイテムだけが出る。3家具、計6アイテムを確認する。
- [ ] 選んだアイテムの名前・仮画像・1ページ目が出る。
- [ ] 全文表示 → 2ページ目 → 元の家具、の順に進む。開いた入力だけで説明が飛ばない。
- [ ] 説明中に背景やBACKをクリックしても説明を中断せず、別アイテムも開かない。
- [ ] ウィンドウクリックとEnter/Spaceを同時に押しても2ページ進まない。
- [ ] 読了後に同じアイテムをもう一度開くと最初のページから読める。
- [ ] 読了後にBACKでROOMへ戻り、別の家具でも調べられる。
- [ ] ROOMからタイトルへ戻り、再開始しても導入選択肢と調査が動く。
- [ ] 04を再実行してもCatalog、ポップアップ、ボタンが増えない。
- [ ] Play停止・保存・シーンの開き直し後も設定が残り、Consoleに赤いエラーがない。

開発側ではC#17本の構文解析、6説明のページ構成、実際の家具アセットとのID対応、既存導入の合流、照合対象のGUIDの重複なしを静的に確認しました。Unityのコンパイル、描画、イベント・入力動作はKENの環境で確認します。M04だけでは取得判断のT02や所持品読み返しのT07を完了扱いにしません。

正常に動いたらPlay停止 → Ctrl+S。GitHub DesktopでMainSceneと新しい6アイテム定義、各.metaを含めて、Summary `Save item inspection scene and definitions`でコミットし、Push originで保存します。

次は説明の完了通知に共通ChoicePanelをつなぎ、「取得する／取得しない」と取得ルールを実装します。

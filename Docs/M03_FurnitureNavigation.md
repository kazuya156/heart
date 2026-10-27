# M03：家具の拡大と部屋への復帰

ROOMからDESK（机）・BED（ベッド）・WINDOW（窓辺）の仮拡大画面へ進み、BACKでROOMへ戻る小工程です。M02で保存された選択肢UI入りMainSceneを土台にしています。

## 取り込みと操作

1. Unityの再生を停止してCtrl+Sで保存します。
2. GitHub DesktopでFetch origin、Current branchから`feature/m03-furniture-navigation`へ切り替えます。
3. Unityのスクリプト読み込みが終わったら、`Assets/Scenes/MainScene.unity`を開きます。
4. 上部メニュー`Sea of Memories > 03 - Add Furniture Navigation`を1回実行します。01・02のやり直しは不要です。
5. Ctrl+SでMainSceneを保存します。
6. Play → START → 導入選択肢 → ROOMへ進みます。
7. DESKをクリックしてDESKと表示されることを確認し、BACKでROOMへ戻ります。BED、WINDOWも同様に確認します。

今回のメニューは既存MainSceneへボタンとCloseupPanelを追加します。初回の説明用RoomNote・DevelopmentNoteを非表示にし、テスト用タイトル復帰ボタンを下へ移動します。オブジェクトを削除したりシーンを再作成したりしません。すでにExplorationControllerがある場合は重複作成を止めます。

背景は色と家具名によるプレースホルダーです。本番背景、背景切替フェード、カーソル・ホバー演出、アイテムの調査・取得は未実装です。この段階の即時切替は画面移動を先に検証するための途中実装であり、仕様のフェードを廃止する変更ではありません。

## 作成・変更ファイルと役割

| ファイル | 役割 |
| --- | --- |
| `Assets/Scripts/Data/FurnitureDefinition.cs` | 家具ID、背景ID、所属する2アイテムのID、仮背景色を持つScriptableObject。 |
| `Assets/Scripts/Gameplay/ExplorationController.cs` | 家具を検索し、正しいタイミングなら拡大画面の表示をGameFlowControllerへ依頼。BACKもここで受ける。 |
| `Assets/Scripts/Core/GameFlowController.cs` | Room/Closeupの状態変更、CurrentFurnitureId、パネルの表示切替を一元管理。 |
| `Assets/Editor/SeaOfMemoriesExplorationSetup.cs` | 既存シーンへのUI追加と、家具定義3個の初期作成を行うEditor専用ツール。 |

Unityでメニュー03を実行すると、次のアセットも作成されます。

| 保存先 | 家具ID | 背景ID | 所属アイテム |
| --- | --- | --- | --- |
| `Assets/Data/Definitions/DESK.asset` | DESK | BG_03 | ITEM_01, ITEM_02 |
| `Assets/Data/Definitions/BED.asset` | BED | BG_04 | ITEM_03, ITEM_04 |
| `Assets/Data/Definitions/WINDOW.asset` | WINDOW | BG_05 | ITEM_05, ITEM_06 |

ScriptableObjectは、Inspectorで編集できるデータファイルです。シーン上のボタンとは別に保存されます。取得したかどうかのプレイ中の状態はここへ書き込まず、後でGameSessionへ実装します。ContentCatalogを追加する際にも、この家具定義を引き続き利用できます。

## 確認項目

- [ ] DESK・BED・WINDOWがそれぞれ対応する家具名の拡大画面を開く。
- [ ] BACKでROOMへ戻り、ほかの家具を開ける。
- [ ] 拡大画面でEnter/Spaceを押しても導入会話が進まない。
- [ ] 拡大中はROOMの家具ボタンを押せない。
- [ ] 家具ボタン・BACKを連打しても画面が重なったり別家具へ飛んだりしない。
- [ ] ROOMのテスト用タイトル復帰ボタンから再開始できる。
- [ ] 03を再実行しても同じボタンやコントローラーが増えない。
- [ ] 再生停止・保存後、MainSceneを開き直しても設定が残る。
- [ ] Consoleに赤いエラーが出ない。

開発側ではC#の構文解析、固定IDの対応、GUIDの重複なし、M02の保存済みシーンと会話データの維持を静的に確認しています。Unityでのコンパイル・描画・入力・永続イベントの実際の動作は未確認です。上の項目をKENのUnityで確認して進めます。

正常に動いたら、再生を停止してCtrl+S。GitHub DesktopでMainSceneと`Assets/Data/Definitions`内の3アセット、フォルダー・各ファイルの.metaを含めてコミットし、Push originで保存します。

次は、家具内の2アイテムを仮表示し、説明を読むところまで追加します。その後、取得する／取得しないの共通ChoicePanelとGameSessionの取得処理へ進みます。

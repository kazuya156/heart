# M06：所持アイテムを独立した画面で読み返す

## 今回の目的

右の取得済みスロットから、初回調査と同じ説明を何度でも読めるようにします。詳細画面はGamePhaseを変えず、本編のDialoguePlayerをPause/Resumeします。取得数・取得順・質問数・コンボ・動揺度は変更しません。

一次仕様：Unity詳細設計書v0.1の5.6、6.2、6.3と受け入れテストT07。M05の保存コミット836a486を土台にしています。質問・エンディング・音響・フェード・最終画像は後続工程です。

## Unityでの取り込み

1. Playを停止し、Ctrl+Sで保存します。
2. GitHub DesktopでFetch originを押します。
3. Current branchから`feature/m06-inventory-reread`を選びます。
4. Unityのコンパイルが終わるのを待ち、`Assets/Scenes/MainScene.unity`を開きます。
5. 上のメニューから **Sea of Memories → 06 - Add Inventory Reread** を実行します。
6. 完了の案内を閉じ、Ctrl+Sでシーンを保存します。
7. Consoleに赤いエラーがないことを確認し、Play → STARTで試します。

メニュー06はItemDetailsPanelと4枠のButton、入力と各クラスの参照をまとめて設定します。画像は未設定のままで構いません。二重実行時は既存の画面を上書きせず停止します。メニュー05以前のシーンには先に05を適用してください。

新しい詳細画面だけが全画面を覆います。既存のChoicePanelの範囲は左80%に調整し、TAKE/LEAVE中も右の所持欄を押せるようにしています。選択肢の内容・結果は変更しません。

## 操作

- 取得済みの所持枠をクリック：詳細を開きます。空き枠は押せません。
- 詳細の文章欄クリック／Enter／Space：全文表示 → 次の行。
- 最後の行から進める、またはCLOSE：詳細を閉じ、本編を同じ位置から再開します。
- 開いている間は背景・家具・戻る・TAKE/LEAVE・別の所持枠を操作できません。
- 読み返し終了後にTAKE/LEAVEを再表示したり、アイテムを再取得したりしません。

## ファイルと責務

| ファイル | 担当 |
| --- | --- |
| `Assets/Scripts/UI/ItemDetailsViewer.cs`（新規） | 本編とは別の行番号と文字数で説明のlineノードを再生。画像／仮画像を表示し、閉じたことを通知。SE・表情・取得処理を呼ばない。説明の次シーケンスにも対応。 |
| `Assets/Scripts/UI/InventoryPanel.cs` | 取得順の表示とクリックを管理。所有・状態を確認し、本編をPause → 詳細表示 → Resume。共通選択肢も一時停止。 |
| `Assets/Scripts/UI/UiInputRouter.cs` | 詳細表示中は詳細に、それ以外は本編に入力を渡す。開閉・クリック・Enter/Spaceを同じフレームで重ねない。 |
| `Assets/Scripts/Core/GameFlowController.cs` | CanReadInventoryと詳細表示中の背景操作禁止を判定。詳細の開閉ではPhaseを変更しない。 |
| `Assets/Scripts/Gameplay/ExplorationController.cs` | 詳細表示中は取得選択の古いイベントも拒否。 |
| `Assets/Editor/SeaOfMemoriesInventoryDetailsSetup.cs`（新規） | メニュー06でUIと参照・イベントを設定。 |
| `Assets/Scenes/MainScene.unity`（ユーザーが保存） | 生成した詳細UI・スロットButtonと各参照を保持。 |

Pauseは台本を最初から再生し直す操作ではありません。元のsequence・nodeIndex・文字表示数を保持したままUpdateとRequestAdvanceを止めます。詳細は別の再生位置を使い、閉じたコールバックでResumeします。

## 動作確認（T07＋入力排他）

まずDESKのITEM_01を取得し、取得反応を最後まで進めます。

| 確認 | 操作 | 正常な結果 |
| --- | --- | --- |
| 基本 | 右のITEM_01を開き、最後まで読む | 初回と同じ2行。終了後は元の家具拡大へ。所持は1/4のまま。取得反応・TAKE/LEAVEが追加されない。 |
| 途中で閉じる | 詳細の文字が途中まで出ているときCLOSE | すぐ元の画面へ。再度開くと説明の最初から読める。 |
| 文章の途中で開く | 未取得のITEM_02を調査し、その説明が途中まで表示されたとき右のITEM_01を開く。数秒待って閉じる | 元のITEM_02の同じ行・途中の文字から再開。初めからやり直したり次の行へ飛んだりしない。 |
| 選択肢の途中で開く | ITEM_02の説明を終えてTAKE/LEAVEを表示し、右のITEM_01を開閉 | 取得・見送りが勝手に実行されず、同じTAKE/LEAVEに戻る。その後LEAVEでITEM_02は未取得のまま残る。 |
| 背景への操作 | 詳細表示中、枠の外・家具の位置・右の別枠をクリック | 詳細以外の操作が起きない。ConsoleのPhaseが変わらない。 |
| 入力の重複 | 文章欄クリックとEnter/Spaceを同時に押す。最後の行／CLOSEでも試す | 1フレームに最大1回。閉じた入力で本編を次の行に進めない。 |
| 順序と空枠 | 2個目を取得し、両方の枠と空枠を押す | 各枠に対応した説明。取得順は維持。空枠は開かない。 |
| 4個目の割込み | 4個目の取得反応や看守帰還・追及導入の途中で所持品を開く | 会話が止まり、閉じて続きを読める。取得反応→看守帰還→追及導入の順序は変わらない。 |
| 追及の待機画面 | INTERROGATION到達後に4枠を開閉 | 4個とも読めて元の待機画面へ戻る。質問ボタンは次工程。 |
| 前工程の回帰 | LEAVE→再調査→TAKE、家具2品取得、4個目優先を再確認 | M05の取得ルールが維持される。 |

現時点で質問数を増やす処理は未実装です。3回目成立時の所持品無効化はM07で実装・検証します。CanReadInventoryには既にQuestionCount >= 3の禁止条件がありますが、それだけでエンディング機能が完成した扱いにはしません。

作成環境では22個のC#ファイルを構文解析し、既存の台本・定義データが未変更であることを確認しました。Unity実行環境はないため、コンパイル、文字の収まり、クリックの通過・入力タイミングは上の手順で実機確認が必要です。

## 保存と次の作業

動作確認後はPlayを停止 → Ctrl+S。GitHub DesktopのSummaryに`Save inventory reread scene`を入力し、Commit to feature/m06-inventory-reread → Push origin。MainSceneに新しいUI・Button・参照が保存されます。

次は3質問と有効／ダミーの判定、動揺度、True/Badの仮エンディングを段階的に追加します。

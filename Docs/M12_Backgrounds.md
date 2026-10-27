# M12：背景画像のID割当と表示

目的：ContentCatalogのSprite登録と、Canvas最背面の共通BackgroundImageをつなぐ。タイトル、部屋、家具、追及、結末を同じMainScene内で切り替え、本番画像は後からInspectorで差し替える。

## Unityでの取り込み

1. Playを停止し、現在のMainSceneを保存する。M11の音声設定付きシーンはコミット24fc7c0で保存を確認済み。
2. GitHub DesktopでFetch origin → `feature/m12-backgrounds`に切り替える。
3. Unityのインポートとコンパイル完了を待ち、`Assets/Scenes/MainScene.unity`を開く。
4. `Sea of Memories → 12 - Add Backgrounds`を1回実行する。
5. Ctrl+Sで保存し、Playする。

メニューは既存BackgroundImageを再利用し、子にArtwork（画像用Image）とBackgroundFallback（未受領時の表示）を追加する。GameSystemsにはBackgroundViewを追加する。既存の表情・音声登録、ボタン、イベント、所持品設定は維持する。

共通背景を覆っていたCloseupPanel・QuestionPreviewPanel・BadEndingPanel・TrueEndingPanelの**Imageコンポーネントだけ**を無効化する。子のボタン・文字・看守・ビネットは有効なまま。旧SeaBand/Horizonと家具画面の旧PlaceholderLabelも非表示にする。既存オブジェクトは削除しない。メニューはUndoに対応し、重複実行や既存画像がある場合は上書きせず停止する。

## 背景と切替時刻

| 背景ID | 表示する場面 | 未受領時の表示 |
| --- | --- | --- |
| BG_TITLE | タイトル、結末終了後 | 暗い青・TITLE |
| BG_01 | 導入の初期背景、追及導入〜質問 | 灰色・CELL BARS |
| BG_02 | 部屋全体、看守帰還 | 青緑・ROOM |
| BG_03 | DESK拡大 | 茶・DESK |
| BG_04 | BED拡大 | 青・BED |
| BG_05 | WINDOW拡大 | 青緑・WINDOW |
| BG_06 | Bad End | 黒・BAD END |
| BG_07 | True End | 白・TRUE END |

詳細設計書3.2のIDと家具対応を使用する。導入は鉄柵側のBG_01を初期配置とする。導入内で別の背景が必要になった場合は、本番台本の指定を確認して後続の台本演出対応で扱う。

タイトル表示は直ちにBG_TITLEへ戻す。START→導入は新しくBG_01への黒フェードを挟み、完了してからINTRO_STARTを再生する。以後の家具移動・帰還・追及・両エンドは既存の画面遷移を使用し、暗幕が完全に覆った時点で背景を変更する。標準0.35秒。白/黒のEndingフェードは維持する。

背景を変更する時刻はGameFlowController、フェード補間はScreenEffects、画像の表示はBackgroundViewが担当する。BackgroundViewはPhaseや質問結果を変更しない。画像未指定なら前のSpriteを外し、色と小さな仮背景表示に置き換える。

## 作成・変更したファイル

| ファイル | 役割 |
| --- | --- |
| `Assets/Scripts/Presentation/BackgroundView.cs` | IDからSpriteを取得し、背景・仮背景を表示。画像の縦横比と白色の描画色を設定 |
| `Assets/Scripts/Data/ContentCatalog.cs` | 固定背景8IDと登録の検証。表情用と同じSprites配列を使用 |
| `Assets/Scripts/Core/GameFlowController.cs` | タイトル・導入・既存フェードの中間点から背景表示を呼ぶ。結末のTRUE_END/BAD_ENDをBG_07/BG_06へ対応させる |
| `Assets/Editor/SeaOfMemoriesBackgroundSetup.cs` | メニュー12で既存シーンへ背景表示を接続 |
| `Docs/Progress.md` | M11の保存と音声未確認事項、M12の状態を更新 |

GameSession、ExplorationController、質問判定、ItemDetailsViewer、音声処理、scenario.json、定義アセットは変更していない。メニュー12未適用の旧シーンはこれまでの挙動で動作する。

## 動作確認

1. タイトルで暗い青の仮背景と画面下のTITLE表示を確認する。
2. STARTで黒フェードし、灰色のCELL BARS背景になってから導入が始まる。フェード中のSTART連打やEnter/Spaceで会話が飛ばない。
3. 導入終了後、ROOMへフェードする。DESKは茶、BEDは青、WINDOWは青緑の仮背景になり、BACKはROOMへ戻る。
4. アイテム説明中・TAKE/LEAVE中・所持品読み返し中は背景が勝手に変わらない。家具・アイテムのホバーとクリックが維持されている。
5. 4個未満で同じ家具の2品を取得すると、専用メッセージ後にROOM。4個目では専用反応→ROOM上の看守帰還→CELL BARSの追及導入。家具完了メッセージを割り込ませない。
6. 01/03/05の有効3問で白フェード→白のTrue End、ダミーを含む3問で黒フェード→黒のBad End。最後の返答より前に背景が結末へ進まない。
7. 結末最終テキスト後はBG_TITLEへ戻り、次のゲームでも前の背景・ビネット・所持品が残らない。

フェードの時間・中間点・入力ロックは既存の`Sea of Memories → Checks → Fade Timing (Play mode)`で補助確認できる。本体を通して背景対応を見る確認とは別に、ScreenEffects単体を一時Canvasで確認するもの。

こちらで確認したのは40 C#ファイルの構文解析・変数スコープ・Phase書込元、背景ID対応、変更範囲。Unityでのコンパイル・描画・操作と実画像の表示はKENの環境で確認する。

## 本番画像を入れる時の操作

画像がまだなくても、この工程の動作確認は色付き仮背景で進められる。画像を用意できた時に以下を行う。

1. Playを停止する。
2. Projectで`Assets/Art/Backgrounds`フォルダーを作り、背景PNG/JPGを入れる。
3. 画像を選択し、InspectorでTexture Typeを`Sprite (2D and UI)`、Sprite Modeを`Single`にしてApplyする。
4. HierarchyのGameSystemsを選択し、ContentCatalogの`Sprites`配列から対応するBG_IDを探す。ID文字列は変更せず、Sprite欄へ画像をドラッグする。表情用の5項目はそのまま残す。
5. Ctrl+S→Playし、その背景を表示する場面へ進む。画像が表示されると、その背景の小さな仮背景表示は消える。
6. 別の未指定背景に進んだ時に前の画像が残らないことを確認する。

ArtworkはPreserve Aspectを有効にする。1920×1080（16:9）画像が基準で、比率が異なる画像は変形させず、余白を黒で埋める。本番絵の家具位置に合わせたクリック範囲の調整は画像受領後に行う。現段階は場所を示す既存の矩形ボタン・見出しを維持する。

参照：Unity公式マニュアル [Sprite (2D and UI)への設定](https://docs.unity3d.com/jp/current/Manual/sprite/import-images-sprites/set-texture-type-imported-image-sprite-2d-ui.html)。

## 保存と次工程

動作確認後はStop→Ctrl+S→GitHub Desktopで`Save background scene`をCommit→Push origin。

次は日本語フォントと本番台本を受け入れる準備を進める。台本のbackgroundId / screenEffectIdによる任意の背景・画面効果指示は今回の範囲に含めず、ScenarioRepositoryは引き続き未対応指示を検出して止める。音声のBGM_01聞き取りは未確認として残し、本番音源への差替え時にも再確認する。家具の移動SEは未割当のまま。

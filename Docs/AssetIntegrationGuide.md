# 本番素材の投入手順

対象：MainScene / Unity 6000.3.26f1。2026-10-25時点のコードに対応。

## 作業前

Playを停止してから編集する。GitHub Desktopの未コミット変更を確認し、素材は一種類ずつ登録・確認・保存する。既存IDや有効/ダミー判定、家具とアイテムの対応を変更しない。セットアップ用01〜13メニューを素材差替えのために再実行する必要はない。

2026-10-25：KENより仮画像の退避とProjectSettings / UnityConnectSettingsの変更破棄後、ローカル変更なしとの報告。今後もブランチ切替・Pull前に未コミット変更を確認する。

## 背景・看守

MainSceneのGameFlowControllerのCatalog参照先（ContentCatalog）をInspectorで開く。Spritesの既存ID行のSpriteへ画像を割り当てる。行の追加・ID変更は不要。

| ID | 内容 |
| --- | --- |
| BG_TITLE | タイトル |
| BG_01 | 鉄柵側・追及 |
| BG_02 | 部屋全体 |
| BG_03 | 机の拡大 |
| BG_04 | ベッドの拡大 |
| BG_05 | 窓辺の拡大 |
| BG_06 | BadEnd |
| BG_07 | TrueEnd |
| CH_01_normal | 通常 |
| shaken1 / shaken2 | 動揺1 / 2 |
| limit / smile | 限界 / 笑顔 |

表情の短いIDはCH_01_shaken1などの接頭辞付き表記も同じ枠へ解決する。両方を別行として重複登録しない。

画像はProjectへ取り込み、UIで使う画像をSpriteとしてインポートする。背景の基準は1920×1080・16:9。推奨配置はAssets/Art/Backgrounds、Characters、Items、UI（必要時にUnity上でフォルダー作成）。これは整理用の推奨で、実装が固定パスから画像を検索するわけではない。背景は縦横比を保って表示するため、実画像で余白・収まりを確認する。

## アイテム

Assets/Data/Definitions/ITEM_01.asset〜ITEM_06.assetを選ぶ。Icon（所持欄・質問用）とLarge Image（説明用）を登録し、Display NameとQuestion Labelを本番の文言にする。ID・Is Valid・Furniture Idと関連Sequence Idは維持する。

| ID | 家具 | 内容 | 判定 |
| --- | --- | --- | --- |
| ITEM_01 | DESK | 不揃いな鉛筆 | 有効 |
| ITEM_02 | DESK | 落書き | ダミー |
| ITEM_03 | BED | 小さな服 | 有効 |
| ITEM_04 | BED | 午前2時の目覚まし時計 | ダミー |
| ITEM_05 | WINDOW | 弟の名前の賞状 | 有効 |
| ITEM_06 | WINDOW | 無限に広がる海 | ダミー |

家具3箇所はRoom内のButtonのRectTransformを調整する。アイテム6箇所は各ItemDefinitionのUse Custom HotspotをONにしてHotspot Rectを個別設定する（[設定・確認手順](M16_ItemHotspots.md)）。Closeup内の共通2ボタンを直接動かすだけでは家具ごとに位置を変えられない。未設定のアイテムは既存シーンの配置を使う。座標はボタンの親RectTransform基準で、背景画像のピクセル座標ではない。画像の余白や縦横比も含め実画面で範囲を確認する。

## 音

同じContentCatalogのAudio Clipsへ、既存IDに対応するAudioClipを登録する。

- BGM_01〜04
- SE_01〜03、SE_05〜09（SE_04は廃止）
- 推奨配置：Assets/Audio/BGM、Assets/Audio/SE

MainSceneのGameSystems/AudioにAudioControllerがあり、BGM Volume / Se Volumeで調整する。BGM_A/BのPitchは1で使用する。仮音は低く聞き取りにくいが、Volume 0.18のままPitch 2でかすかに、3では明瞭に聞こえるとの報告あり。本番の曲が正常に聞こえることを示す検証ではない。

必要な本番音を登録したらUse Temporary Test TonesをOFFにし、Playを開始し直して音量・ループ接続・約0.5秒の切替を確認する。未登録音は仮音OFFで代用されない。BGM_01はタイトル/導入で再生。その他の本番曲や移動SEの配置は台本・演出担当の指定を確認して追加する。受領しただけで自動的にすべての場面へ割り当たるわけではない。

## 台本

編集対象：Assets/Data/Scenario/scenario.json。UTF-8のJSONとして保存する。既存sequences / id / nodes / nextSequenceIdを保ち、まずtextと選択肢labelを差し替える。choiceの接続先とA/BからINTRO_COMMONへの合流は維持し、ルート用の新フラグを作らない。

既存台本ID：
- INTRO_START、INTRO_TONE_A/B、INTRO_COMMON
- DESC_ITEM_01〜06、PICKUP_REACTION_1〜4、FURNITURE_EMPTY
- GUARD_RETURN、INTERROGATION_INTRO
- QUESTION_ITEM_01〜06、REPLY_ITEM_01〜06
- QUESTION_PROMPT、QUESTION_MISSING_INFO、QUESTION_VALID_REACTION
- BAD_END、TRUE_END

以下は書式の説明例であり、確定した台詞・演出ではない。ファイル全体をこの例で置き換えない。

```json
{
  "type": "line",
  "speaker": "guard",
  "text": "ここに確定した本文を入れる",
  "expressionId": "CH_01_normal",
  "backgroundId": "BG_01",
  "bgmId": "BGM_01",
  "seId": "",
  "screenEffectId": "BLACK_FADE"
}
```

speakerはhero / guard / narration。背景・表情・音の空文字は現在の状態を維持。音はノード開始時に一度だけ実行する。screenEffectIdはNONE / GLASS_SHAKE / BLACK_FADE / WHITE_FADE。背景指定時はフェード中間点で差替え、終了後に本文を開始する。GLASS_SHAKEは看守立ち絵の揺れであり、画面全体の揺れではない。看守が表示されない場面では見えない。

制約：
- アイテム説明・質問・看守返答・両エンドはlineのみ。選択肢を入れない。
- QUESTION_PROMPT / QUESTION_MISSING_INFO / QUESTION_VALID_REACTIONは、空でない1行・自動接続なしを維持する。
- QUESTION_PROMPT / QUESTION_MISSING_INFOでもbackgroundId・BLACK_FADE・WHITE_FADEを指定できる。演出完了後に全文表示し、質問操作を再開する。警告から心情文へ戻すときも心情文の指示を再実行するため、本番演出の頻度を確認する。
- 有効質問の「ッ！」とコンボ演出は進行処理が実行する。台本へ同じ揺れ・音を重ねる場合は二重演出に注意する。
- 所持品の読み返しは本文だけを独立表示する。説明台本内の画像変更・音・画面効果を再実行しない。
- BadEndのSE_07/08とTrueEndのSE_09の現配置は仮台本用。最終タイミングを企画担当と照合する。

## 長い台詞の表示

本文は枠の実寸とTMPの折り返しからページ分割する。文字送り中は現在ページの全文表示、表示後は次ページ、最終ページ後に次の台詞や判定へ進む。固定文字数での上限は設けない。ページ送りで台本の音・背景・表情指定を再実行しない。読み返しも独立したページ位置を持つ。心情文/未取得警告はページごとに即時表示し、複数ページなら最後まで確認してから質問操作を再開する。

本番台詞の文の区切り・行頭/行末・読みやすさは実画面で確認する。共通選択肢は全文を表示するためボタンの高さを文章に合わせる。選択肢全体が既定範囲に収まらない場合はスクロールバー/ホイール/ドラッグで読む。取得する/しないの短い選択肢も同じ部品を使う。質問ボタンのラベルはこの共通選択肢とは別に収まりを確認する。[M19確認手順](M19_TextPagination.md)。

## 差替え後

1. Ctrl+Sでシーン・アセットを保存し、Playを開始し直す。Consoleのエラーを確認する。
2. 取得・質問・説明画面それぞれで素材と文字の収まりを確認する。
3. T01〜T14を再確認する。本番の長い文章、クリック範囲、連打、読み返し中断を重点確認する。
4. Windows版を再ビルドし、日本語・両エンド・音・QUITを実行ファイルで確認する。
5. フォルダー全体をZIP化して展開先から起動する。別PCでも確認する。

本番素材・本文は未受領。UI最終配置、音の最終割当、別PC確認、提出用ビルドは未完了。試作版の成功を提出版の完成とは扱わない。

# M13：日本語フォントの準備

目的：未対応文字が四角になる問題を防ぎ、日本語の本番台本を入れられる土台を作る。今回の確認は導入の冒頭1行だけで行う。ゲームの進行・選択肢の接続・質問判定は変更しない。

## Unity操作

1. Playを停止してMainSceneを保存する。M12の背景設定付きMainSceneは279a5afで保存を確認済み。
2. GitHub DesktopでFetch origin → `feature/m13-japanese-font`へ切り替える。
3. Unityのインポート・コンパイルを待ち、`Assets/Scenes/MainScene.unity`を開く。
4. `Sea of Memories → 13 - Add Japanese Font`を実行する。
5. 初回だけNoto Sans CJK JP Regular（約15.7 MiB）を公式リポジトリから取得する。進捗表示にはCancelがあり、通信中にキャンセルできる。取得後にSDF生成・補完設定を行い、完了ダイアログが出る。
6. Ctrl+S → Play → START。冒頭の日本語仮文を確認する。

表示する文：

> [仮台詞] 返事の言い方を選んでください。どちらを選んでも、同じ探索へ進みます。

これは従来の英語プレースホルダーの意味を日本語で示す確認文。本番の台詞が確定したわけではない。他の台詞・選択肢ラベルはこの段階では英語のまま。

## フォントの役割

これまでの英字フォントを使い、日本語など不足する文字をNoto Sans CJK JPで補完する。TextMeshProの**Fallback Font Assets**は、最初のフォントに見つからない文字を別のフォントで探す仕組み。既存の英字の見た目・文字サイズ・UIの座標を維持するため、全テキストのFontを一括置換する方法にはしていない。

日本語SDFはDynamic / Multi Atlasで作る。必要な文字を追加できるように元OTFもプロジェクト内に保存する。OSにインストールされたフォントを参照する構成にはしない。ゲーム実行中のフォント取得通信はない。

今回のメニューはアクティブ・非アクティブを含むMainSceneのTMPフォントと、TMPのデフォルトフォントへ日本語補完を追加する。既存の補完先は残し、同じフォントを重複追加しない。新しく別の英字フォントへ変えた場合は、このメニューをもう一度実行して補完先を設定できる。

## 作成・変更するファイル

| ファイル | 役割 |
| --- | --- |
| `Assets/Editor/SeaOfMemoriesJapaneseFontSetup.cs` | メニュー13。フォント取得・検証・SDF生成・フォールバック接続。Editor専用 |
| `Assets/Data/Scenario/scenario.json` | INTRO_STARTの最初のtextのみ日本語の仮文へ変更 |
| `Assets/StreamingAssets/ThirdParty/NotoSansCJKjp-OFL.txt` | 公式ライセンス原文を同梱 |
| `Assets/StreamingAssets/ThirdParty/NotoSansCJKjp-Notice.txt` | 出典と固定コミット・ファイル情報 |
| `Assets/Fonts/NotoSansCJKjp-Regular.otf` | メニュー実行時に取得する元フォント |
| `Assets/Fonts/NotoSansCJKjp-Regular-SDF.asset` | メニュー実行時に生成。MaterialとAtlasもこのアセットの内部へ保存する |
| 既存TMPフォントの.asset | Fallback Font Assetsに日本語フォントを追加する。今回のシーンでは主にLiberationSans SDF |

末尾2種類の生成物と.metaもGitへコミットする。生成フォントを既存フォントから参照するため、MainSceneだけを保存しても共有は完了しない。メニューはフォントアセットをSaveAssetsで保存する。SceneもCtrl+Sで保存する。

ダウンロードは公式ファイルを固定コミットから行い、ファイルサイズとGit blobのハッシュを照合する。同名の既存ファイルを勝手に上書き・削除しない。通信中のキャンセル・Play開始・スクリプト再コンパイル・Unity終了時は通信と進捗表示を解放する。途中で別シーンに移動した場合は補完設定を行わず、MainSceneを開いて再実行する。

## 動作確認

1. 冒頭で「仮台詞」「返事」「選んでください」「探索」などが四角や欠落にならず表示される。
2. 文字送り途中のクリックは全文表示、次のクリックで選択肢へ進む。Enter/Spaceも同様。フォント追加でT14の入力を変えない。
3. A/Bどちらの導入でもROOMへ進み、DESK/BED/WINDOW・取得・読み返しが動く。
4. 英語タイトルや既存ボタンのフォント・サイズ・位置が維持されている。
5. Play停止→Unityでシーンを開き直す→再度Playでも日本語が出る。SDFのMaterial/Atlasが一時的なメモリ上にしか存在しない状態を避けるための確認。
6. 保存・Push後、共同作業者が取得した環境でも日本語が出る。フォント生成物をコミット済みなら、共同作業者は通常ダウンロードをやり直す必要がない。

台本の分岐・定義・質問判定には変更を入れていない。？？？・警告・「ッ！」などの表示を本番に合わせて日本語へ揃える作業は、日本語フォントの表示確認後に進める。

こちらの検証はC#構文・スコープ、既存進行コードの不変、台本の変更が1フィールドだけであること、ダウンロード元とハッシュの公式メタデータ照合まで。Unityのコンパイル、実ダウンロード、FontEngineによる生成・日本語の描画・Windowsビルドは未実行。

## エラーが出た時

- 通信失敗：ネット接続を確認してメニュー13を再実行する。必要なら[固定した公式OTF](https://raw.githubusercontent.com/notofonts/noto-cjk/f8d157532fbfaeda587e826d4cd5b21a49186f7c/Sans/OTF/Japanese/NotoSansCJKjp-Regular.otf)をブラウザーで保存し、`Assets/Fonts/NotoSansCJKjp-Regular.otf`へ置いて再実行できる。
- サイズ・ハッシュ不一致：既存ファイルを自動削除しない。別の同名フォントや不完全なファイルでないか確認する。不明ならConsoleの全文を共有する。
- OTFが読み込めない / SDF生成失敗：元フォントのImport SettingsとConsoleを確認する。Include Font Dataが有効かを確認し、エラー全文を共有する。
- SDFの設定やサブアセットが不完全：既存アセットを削除せず、まずメニュー13を再実行する。元フォント・Dynamic・Multi Atlasの設定が別物の場合は自動で上書きせず停止する。

## 保存と次の作業

確認後はPlay停止→Ctrl+S。GitHub DesktopのChangesで、`Assets/Fonts`の元OTF・SDF・.meta、既存TMPフォントの変更、MainScene等の今回の変更をまとめて`Save Japanese font setup`でCommit→Push origin。

Noto Sans CJK JPはSIL OFL 1.1の原文を同梱して利用する。StreamingAssets内の文書はWindows Playerへコピーされるため、配布時にもそのフォルダーを含める。最終ビルドで実ファイルとフォント表示を確認する。

この後は、日本語UIの仕様表記と台本の残りの演出指示を段階的につなぎ、本番素材の受け取り、全受け入れテストとWindowsビルドへ進む。低い仮BGMの聞き取りは未確認事項として残す。

参考：

- [Noto公式・元フォント](https://github.com/notofonts/noto-cjk/blob/f8d157532fbfaeda587e826d4cd5b21a49186f7c/Sans/OTF/Japanese/NotoSansCJKjp-Regular.otf)
- [Noto公式・LICENSE](https://github.com/notofonts/noto-cjk/blob/f8d157532fbfaeda587e826d4cd5b21a49186f7c/Sans/LICENSE)
- [TMP CreateFontAsset API](https://docs.unity3d.com/ja/Packages/com.unity.textmeshpro%403.0/api/TMPro.TMP_FontAsset.CreateFontAsset.html)
- [TMP fallbackFontAssetTable API](https://docs.unity3d.com/ja/Packages/com.unity.textmeshpro%403.0/api/TMPro.TMP_FontAsset.fallbackFontAssetTable.html)
- [Unity 6.3 StreamingAssets](https://docs.unity3d.com/6000.3/Documentation/Manual/StreamingAssets.html)

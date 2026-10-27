# M11：BGM・効果音

目的：音声のID管理・ループ再生・合計0.5秒のBGMクロスフェード・SEの単発再生をつなぎ、素材受領前でも再生箇所を確認する。Unity 6000.3.26f1を使用する。

## 取り込みとUnity操作

1. Playを停止し、現在のMainSceneを保存する。M10のシーン保存はdc5d96dで確認済み。
2. GitHub DesktopでFetch origin → `feature/m11-audio`へ切り替える。
3. Unityでインポートとコンパイルの完了を待ち、`Assets/Scenes/MainScene.unity`を開く。
4. `Sea of Memories → 11 - Add Audio`を1回実行する。
5. Ctrl+Sで保存し、Playする。

メニューはGameSystems配下にAudioとBGM_A/BGM_B/SEを作り、AudioController・Flow・DialoguePlayer・ChoicePanel・InterrogationControllerを接続する。既存設定や素材を上書きせず、重複実行を止める。Main Cameraに有効なAudioListenerが1個必要。

**今回の音は本番素材ではなく、正弦波の確認用電子音。** メニューは`Use Temporary Test Tones`をONにする。登録済みでClipが未指定のIDだけ、実行時に仮音を生成する。タイトルは低いループ音、SEはIDごとに高さの違う短音。仮音のためにWAVファイルを作成する必要はない。音量はAudioControllerのBgm Volume / Se Volumeで調整できる。聞こえない場合はGameビューのMute Audio、Windowsの音量ミキサーも確認する。

## 確定している再生箇所と残り

| 箇所 | 今回の動作 | 根拠・未確定事項 |
| --- | --- | --- |
| タイトル・序盤 | BGM_01をループ。同じ曲の再指定では頭から再生し直さない | 詳細設計書5.1/5.8 |
| 通常の台詞送り | 全文表示後、実際の次ページへの操作でSE_01 | 詳細設計書3。文字ごと・全文表示だけの入力は無音 |
| 導入・TAKE/LEAVEの確定 | 共通ChoicePanelでSE_01を1回 | 同上。表示しただけ・無効化中・二重入力は鳴らさない |
| START・質問選択・未取得警告を戻す操作 | 有効な操作でSE_01 | 通常の選択確定・ページ送り |
| ダミー質問の判定直後 | SE_06を1回。その後、固定返答 | 詳細設計書4.4。数値・コンボ判定は変更しない |
| Bad End | 仮台本の1行目にSE_07、2行目にSE_08 | 6.2の使用IDに対応。詳細な演出タイミングは本番台本受領時に調整する仮配置 |
| True End | 仮台本の1行目にSE_09 | 同上。白フェード後に台詞が始まる現在の進行を維持 |
| 所持品の読み返し | 読み返し側はSE・BGM指示を発行しない。背後のBGMは継続 | 詳細設計書の独立した読み返し。メイン会話のPause/Resumeもノード音を再実行しない |
| BGM_02〜04、SE_02/03/05 | ID登録・APIと台本指定に対応 | 今読める詳細設計書には正確な全再生箇所がないため、自動再生箇所は追加していない。本番台本・絵コンテで確認する |

BGM_01〜04、SE_01/02/03/05/06/07/08/09の12IDを登録する。廃止された時計SE_04は登録せず、台本に指定されても検証で止める。

台本の`bgmId`と`seId`はノード開始時に1回処理する。空欄なら何もしない。`bgmId`は既定0.5秒で切り替える。通常のページ送りSEと次ノードの指定SEは別のイベントのため、同じ操作タイミングで聞こえる場合がある。詳細表示は別プレイヤーなので、台本にこれらが付いていても読み返しでは再生しない。

## コードの役割

| ファイル | 役割 |
| --- | --- |
| `Assets/Scripts/Presentation/AudioController.cs` | PlayBgm / CrossFadeBgm / PlaySe。BGM用2つのAudioSourceの音量を補間、SE用の別AudioSourceでPlayOneShot。進行・質問判定は担当しない |
| `Assets/Scripts/Data/ContentCatalog.cs` | AudioCatalogEntry配列、GetAudio、IDの整合確認。本番AudioClipはここに割り当てる |
| `Assets/Scripts/Data/ScenarioRepository.cs` | 音声指示を受け入れ、未定義・廃止IDを拒否。背景・画面効果の台本指示は引き続き別工程 |
| `Assets/Scripts/UI/DialoguePlayer.cs` | ノード入場音と通常ページ送り。全文表示・文字更新・Pause/Resumeでは鳴らさない |
| `Assets/Scripts/UI/ChoicePanel.cs` | 選択を受理した時に1回だけSE_01 |
| `Assets/Scripts/Gameplay/InterrogationController.cs` | 質問操作とダミー成立のSE。数値更新は従来どおりGameSession |
| `Assets/Scripts/Core/GameFlowController.cs` | 音声接続の起動検証とタイトル・序盤のBGM_01。タイトル復帰時はフェードとSEを止め、音の持越しを解消 |
| `Assets/Editor/SeaOfMemoriesAudioSetup.cs` | メニュー11による参照・AudioSourceの作成 |
| `Assets/Editor/SeaOfMemoriesAudioChecks.cs` | 本体と独立したPlayモード補助チェック |
| `Assets/Data/Scenario/scenario.json` | エンディング3ノードのseIdのみ追加。文章・分岐・ゲームルールは変更しない |

C#の`AudioClip`は音データ、`AudioSource`はそれを再生するコンポーネント。ContentCatalogが「どの音か」を解決し、AudioControllerが「どう再生するか」を担当する。呼出し元はゲーム上のタイミングだけを決める。

BGMのフェードはTime.unscaledDeltaTimeで進む。切替を連続指定した時は以前のCoroutineを止め、現時点で音量が大きい側から次の曲へ移る。同じ曲の再指定は再スタートしない。StopAll/無効化/タイトル復帰で古いフェードが残らない。

## 動作確認

1. Play直後、タイトルで低いループ音が鳴る。STARTから導入へ進んでも曲が毎回頭に戻らない。
2. 台詞が1文字ずつ出ている間は連続SEが鳴らない。途中のクリックは全文表示だけ。次のクリックでSE_01が1回鳴る。Enter/Spaceでも同じ。
3. 導入のA/B、取得する/しないでSE_01が1回。無効化中の選択や連打で重ならない。
4. 鉛筆(01)・服(03)・証明書(05)・メモ(02)を取得。質問01→03→02ではダミー成立時に高さの違うSE_06と赤消去が起こり、動揺度は50を維持する。Bad Endの2ページでSE_07/08を確認する。
5. タイトルへ戻り、同じ4品で質問01→03→05。True Endで白フェードとSE_09を確認する。
6. 会話中に所持品を開く。詳細ページ送りは無音で、閉じてもメイン会話の開始SEが再発しない。BGMは止まらない。
7. 3問目成立後の所持欄無効化、最後の返答完了後にのみEnding、タイトル復帰後の所持・質問・演出リセットが維持されている。

追加の補助確認：Play中に`Sea of Memories → Checks → Audio (Play mode)`。ゲームとは別の一時オブジェクトで、ノード音の単発性、Pause/Resume、全文表示とページ送り、Time.timeScale=0のBGM補間、途中切替、停止、無効化、0秒切替を確認する。数秒だけtimeScaleを変更し、成功・失敗・Play終了いずれでも元に戻す。ゲーム本体のPhase・所持品・質問数は操作しない。確認用音は本体BGMに重なって聞こえる場合がある。

成功ログ：`[SeaOfMemories] Audio checks passed: node once, pause/resume, reveal/page SE, BGM timeScale=0, interruption, stop, disable and zero duration.`

こちらではC#構文・変数スコープ・IDと台本の整合を静的確認した。Unityのコンパイル・再生・補助チェックの実行はKENの環境で確認する。

## 本番素材と次の作業

1. GameSystemsのContentCatalog → Audio Clipsで対応IDのClipへ音声素材をドラッグする。BGMはループ向きの音源を使う。
2. 全素材を割り当てた後、Audio配下AudioControllerの`Use Temporary Test Tones`をOFFにして、仮音が提出版へ入らないよう確認する。OFF時の未割当IDは無音。
3. 本番台本でBGM_02〜04・残りSEの再生箇所と結末のタイミングを確定し、bgmId / seIdに記入する。
4. 今回の確認後はStop → Ctrl+S → GitHub Desktopで`Save audio scene`をCommit → Push origin。
5. 次は本番背景のID割当・日本語フォント・台本演出指示の残りに進み、その後Windowsビルドと全受け入れ確認を行う。

参考：Unity 6.3公式API [AudioSource.PlayOneShot](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AudioSource.PlayOneShot.html)、[AudioClip.Create](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AudioClip.Create.html)。

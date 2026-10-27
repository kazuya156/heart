# M08：背景切替と結末のフェード

## 目的と範囲

詳細設計書v0.1の5.7/5.8、6.3、7に沿って、背景切替時の黒フェードとBad/Trueの黒転・白転を追加します。フェード中はTransitionとし、クリック・Enter/Space・所持品読み返しを止めます。質問判定、取得順、4個目優先、会話の順序は変更しません。

M07の保存コミット2654544が土台です。M07についてはKENより「BADENDも確認できた」と報告があり、質問UIと6定義の保存を確認しました。全受け入れ項目の完了を示すものではありません。

| 切替 | 演出 |
| --- | --- |
| 導入終了 → 部屋全体 | 黒フェード |
| 部屋全体 → 家具拡大 | 黒フェード。暗幕が完全に覆ってから背景とアイテム表示を変更 |
| BACK／家具完了後 → 部屋全体 | 黒フェード |
| 4個目専用反応完了 → 看守帰還 | 黒フェード後に帰還会話を開始。家具完了台詞は挟まない |
| 看守帰還会話完了 → 追及導入 | 黒フェード後に導入会話を開始 |
| 最後の看守返答完了 → Bad End | 黒転後に結末台本 |
| 最後の看守返答完了 → True End | 白転後に結末台本 |

結末台本完了後は従来どおりタイトルへ戻ります。タイトル→導入、アイテム説明・取得判断・読み返しの開閉には、この工程ではフェードを追加しません。

ScreenEffectsのうち背景/結末フェードまでの実装です。赤ビネット、コンボの赤消去、立ち絵揺れ・差分、カーソル、音、BGM、本番素材は後工程に残ります。

## Unityでの操作

1. Play停止 → Ctrl+S。
2. GitHub DesktopでFetch origin → Current branch → `feature/m08-screen-fades`。
3. Unityのコンパイルを待ち、`Assets/Scenes/MainScene.unity`を開きます。
4. **Sea of Memories → 08 - Add Screen Fades**。
5. Ctrl+S → Play → START。
6. DESK/BED/WINDOWとBACKで、黒くなる → 画面が変わる → 明るく戻ることを確認します。

メニュー08はGameSystemsにScreenEffectsを追加し、Canvasの最前面に使うFadeOverlay（Image＋CanvasGroup）とFlowの参照を設定します。既存のUIとデータは保持します。既存ScreenEffects/FadeOverlayがあれば上書きしません。

InspectorのGameSystems → ScreenEffectsで調整できます。

| 項目 | 初期値 | 意味 |
| --- | --- | --- |
| Background Fade Seconds | 0.35 | 黒くする半分＋戻す半分の合計。設計書の約0.35秒に対応 |
| Ending Fade Seconds | 0.35 | 黒転／白転の合計。結末用の初期調整値として設定し、演出確認時に調整可能 |

0秒なら即時切替です。端末の描画フレーム単位で時間が丸められるため、正確なフレーム数は実機に依存します。

## 作成・変更ファイルと連携

| ファイル | 担当 |
| --- | --- |
| `Assets/Scripts/UI/ScreenEffects.cs`（新規） | FadeBackground/PlayEndingFade。暗幕の色・透明度・Raycastを操作。完全に覆ったCoveredと、終了したCompletedを通知。Phaseやゲームの数値は変更しない |
| `Assets/Scripts/Core/GameFlowController.cs` | フェード開始時にTransitionへ。元の表示を維持し、Coveredで表示だけ変更。Completedで移行先のPhaseにして、新しい会話を開始 |
| `Assets/Scripts/Gameplay/ExplorationController.cs` | 家具の定義を検索し、FlowからのShowFurnitureで背景色・タイトル・調査対象を更新。背景IDはBG_03〜05のまま |
| `Assets/Scripts/UI/UiInputRouter.cs` | フェード中のキー入力を破棄。完了フレームの入力も抑制し、入力を後から実行しない |
| `Assets/Editor/SeaOfMemoriesFadeSetup.cs`（新規） | メニュー08でImage・CanvasGroup・参照を設定 |
| `Assets/Editor/SeaOfMemoriesFadeChecks.cs`（新規） | Play中に実際のScreenEffectsでゼロ秒、黒/白、時間停止、中断、暗幕解除を確認 |
| `Assets/Scenes/MainScene.unity`（Unity側で保存） | 追加したコンポーネントと暗幕・参照を保持 |

Unityのコルーチンは、処理の途中で次のフレームまで待てる仕組みです。ScreenEffectsはこれを使って透明度を少しずつ変えます。Time.unscaledDeltaTimeを使うため、ゲームのtimeScaleが0でもフェードを完了できます。

背景IDによる切替はFlowがCovered通知を受けて行います。素材が未受領なので現在は既存の仮背景色・パネルを切り替えます。将来の本番画像差し替えも暗幕の下で行える構造です。

Phase更新と表示の更新を分けた理由は、フェードの開始直後に元の画面が消えるのを防ぐためです。表示だけは中間で変わりますが、操作を許可するPhaseは最後までTransitionのままです。新しい会話が始まる切替では、直前の台詞を新しい画面へ持ち越さず、フェード完了後に表示します。

## 確認

| 確認 | 正常な結果 |
| --- | --- |
| 家具3箇所＋BACK | すべて黒フェードで正しい家具／部屋へ。切替の前に背景が瞬間変更されない |
| 連打 | 家具を押した直後からクリック・Enter・Spaceを連打しても、追加の家具選択・アイテム調査・取得・台詞送りが起きない。フェード完了後に通常操作へ戻る |
| 所持品 | 取得後の所持品は通常時に読めるが、フェード中は開けない。読み返しを閉じた後のBACKも使える |
| 家具完了 | 合計3個以下で家具2品を取得した場合、専用台詞の後に黒フェードで部屋へ。家具は無効化されたまま |
| 4個目優先 | 4個目が家具2品目でも、取得反応 → 黒フェード → 帰還会話 → 黒フェード → 追及導入。FURNITURE_EMPTYは挟まない |
| 最後の返答 | 3問目成立時の所持欄無効化はそのまま。最後の看守返答を読み終えるまで結末へのフェードが始まらない |
| 両エンド | Badは黒、Trueは白。フェード中に結末の文章が進まず、終わってから文字が出る。最後はタイトルへ戻る |
| 周回 | STARTで所持0・質問0・コンボ0・動揺0。家具と調査対象が復活し、暗幕が残らない |
| 0秒 | Play前に背景時間を0にして保存し、家具/BACKを試す。即時切替でも入力がロックしたままにならない。確認後0.35に戻す |

入力抑制を見やすくする場合はBackground Fade Secondsを一時的に1秒へ延ばして試し、最終的に0.35へ戻します。

### Play中に実行できる補助チェック

タイトルを表示したPlay状態で、**Sea of Memories → Checks → Fade Timing (Play mode)**を選びます。短時間だけ一時的なCanvasを表示して検証します。

- ゼロ秒で中間と完了が1回ずつ通知される。
- timeScale=0でも黒/白フェードがフレームをまたいで完了する。
- 中間では不透明・入力遮断中、完了後は透明・暗幕非表示・Raycast解除となる。
- 中断は完了扱いの通知をせず、暗幕を解除する。
- 時間設定を元へ戻し、一時オブジェクトを破棄する。既存の台本・所持・質問数は操作しない。

成功時はConsoleに`Fade checks passed: zero duration, black/white at timeScale=0...`と表示します。補助チェックはフェード部品自体の検証なので、上のゲーム進行との結合確認も必要です。

ScreenEffectsを実行中に無効化された場合、Flowは未実施の画面切替を1回だけ補って移行を完了し、入力ロックが残るのを防ぎます。タイトルへのリセット時は保留処理を破棄してから暗幕を解除します。

作成環境では29個のC#ファイルの構文、ローカル変数スコープ、Phase更新元、既存データ未変更を静的確認します。Unity/C#コンパイラはないため、実機コンパイルと補助C#チェックは未実行です。

## 保存と次の作業

動作確認後はPlay停止 → Ctrl+S。Summaryを`Save screen fades scene`として、Commit to feature/m08-screen-fades → Push origin。今回追加で保存するのは主にMainSceneです。

次は家具・アイテムのホバーと虫眼鏡カーソル、その後に質問の視覚効果・音響・本番素材・Windowsビルドを進めます。

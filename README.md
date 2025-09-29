# sugoroku

## Version について

| | version |
| ---- | ---- |
| Unity | Unity 6.0 (6000.0.58f1) |
| Unity Hub | Hub V3.12.1 |

## 事前準備(仮)

1. Unityをダウンロード

2. 2Dプロジェクトを作成

3. dangoro.zipをダウンロード

4. 2で作ったプロジェクトのフォルダに展開したdangoro.zipをコピー
    (Assets、Packages、ProjectSettingsの3つのフォルダをコピー)

5. Unityで動作確認

## スクリプトの概要

| スクリブト名 | 概要 |
| ---- | ---- |
| camraController.cs | ダンゴローの動きを制御するスクリプト |
| DangomushiController.cs | ダンゴローの動きを制御するスクリプト |
| GameDirector.cs | 主にゲーム状態を監視し, UIを制御するスクリプト |
| GameState.cs | enumでゲーム状態を定義したもの. intやstringなどでゲーム状態を管理すると分かりづらいのでこれを利用 |
| GenerateLine.cs | プレイヤーによる線の描画を制御するスクリプト |
| GoalDetection.cs | ゴール判定のスクリプト |

## GitHubの使い方

〇プルリクエストの動作確認
git fetch origin pull/{①プルリクエストのID}/head:{②作るブランチの名前}

①プルリクエストのID

    プルリクエストの場所に移動　例.feature/#68#80　左の例では80がID（薄く表示されている番号）
 
②作るブランチの名前

    適当に決める　例.test#68

具体例　git fetch origin pull/80/head:test#68

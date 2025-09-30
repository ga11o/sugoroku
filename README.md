# sugoroku

## Version について

| | version |
| ---- | ---- |
| Unity | Unity 6.0 (6000.0.58f1) |
| Unity Hub | Hub V3.1.4.2|

## 事前準備(仮)

1. Unityをダウンロード

2. 2Dプロジェクトを作成

3. sugoroku.zipをダウンロード

4. sugoroku.zipを展開して、2で作ったプロジェクトのフォルダにコピー
    (Assets、Packages、ProjectSettingsの3つのフォルダをコピー)

5. Unityで動作確認

## スクリプトの概要

| スクリブト名 | 概要 |
| ---- | ---- |
| 修正中 | 修正中 |

## GitHubの使い方

本章では開発作業の流れについて説明します。

### Issue を確認する

Issue を確認します。

このとき、 Project のステータスが`ToDo`になっている Issue が未着手の Issue です。
ただし、他の開発者が assign されている場合はその人が処理する予定であると考えられるため、本人に確認しましょう。

### Issue のステータスを変更する

処理する Issue が決まったら、そのステータスを`In Progress`にします。
Projects のカンバン画面で Issue をドラッグ & ドロップするか、 Issue 画面でステータスをクリックして変更してください。

また、 Issue を自分自身に assign してください。

### 作業する

`feature`ブランチを切り、作業を行います。

`feature`ブランチは`develop`ブランチから派生させます。
派生させる時には必ず`develop`ブランチを最新の状態に更新してください。
また、`feature`ブランチの名前は`feature/[Issue 番号]`としてください。

```
$ git switch develop
$ git pull
$ git switch -c feature/#999
```

### Pull Request を発行する

作業が完了したら、`feature`ブランチを push します。

```
$ git push -u origin feature/#999
```

`feature`ブランチを push した後、`develop`ブランチへ Pull Request を発行します。

Pull Request を発行する際は [Closing Keyword](https://docs.github.com/ja/issues/tracking-your-work-with-issues/linking-a-pull-request-to-an-issue) で Issue を関連付けてください。
例：`close #999`

また、 review request も行い、指摘事項があれば対応してください。

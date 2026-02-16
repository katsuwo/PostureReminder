# PostureReminder

車椅子ユーザーや長時間座位で作業する方など、**褥瘡（じょくそう）リスクの高い方**が同じ姿勢を取り続けることを防ぐためのWindowsタスクトレイ常駐アプリです。

設定した間隔で通知を出し、除圧やプッシュアップ、姿勢変更を促します。PC操作中に離席を検出すると自動でタイマーを一時停止するため、実際の着座時間に基づいたリマインドが可能です。

## 機能

- **定期リマインド** — 設定した間隔（デフォルト30分）でToast通知・通知音・ダイアログで姿勢変更を促す
- **トレイアイコン** — 残り分数を背景色付きで常時表示
- **離席検出** — マウスカーソルが動かなくなると自動で一時停止、復帰時にタイマーリセット
- **設定画面** — リマインド間隔、離席判定時間、通知方法、カスタム通知音、メッセージをGUIで設定
- **自動起動** — Windows起動時に自動起動（レジストリRun key）
- **多重起動防止** — 名前付きMutexで単一インスタンスを保証

## スクリーンショット

トレイアイコンに残り分数を表示:
- 動作中: 緑背景に白文字で残り分数
- 一時停止中: オレンジ背景に `||` マーク

## 動作要件

- Windows 10 (1809) 以降
- 自己完結型ビルドの場合は .NET ランタイム不要

## ビルド

```bash
dotnet build src/PostureReminder/PostureReminder.csproj
```

## 実行

```bash
dotnet run --project src/PostureReminder/PostureReminder.csproj
```

## パブリッシュ（自己完結型・単一ファイル）

```bash
dotnet publish src/PostureReminder/PostureReminder.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

`publish/PostureReminder.exe` を任意の場所にコピーして実行。

## 技術スタック

- C# / WPF (.NET 8)
- `System.Windows.Forms.NotifyIcon` でタスクトレイ常駐
- `Microsoft.Toolkit.Uwp.Notifications` でToast通知
- `System.Text.Json` で設定永続化（`%APPDATA%\PostureReminder\settings.json`）
- `GetCursorPos` (user32.dll) でカーソル位置ベースの離席検出

## 設定ファイル

`%APPDATA%\PostureReminder\settings.json` に保存。手動編集も可能。

```json
{
  "IntervalMinutes": 30,
  "IdleThresholdMinutes": 5,
  "ShowToast": true,
  "PlaySound": true,
  "ShowDialog": true,
  "CustomSoundPath": null,
  "AutoStart": false,
  "ReminderMessage": "姿勢を変えましょう！立ち上がってストレッチしてください。"
}
```

## ライセンス

MIT

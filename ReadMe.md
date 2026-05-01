# FileArchiver

FileArchiver は、指定フォルダ配下の古いファイルを条件に応じて **リネーム / ZIP圧縮 / 削除** するバッチ向けツールです。  
`.NET 10` 対応、クロスプラットフォーム対応（Windows / Linux / macOS）です。

## 主な機能

- ZIP 圧縮（元ファイル削除を含む）
- リネーム（タイムスタンプ付与）
- 削除
- Dry-run（実際の変更なしで対象確認）
- 設定チェック（`--check`）
- ログローテーション
- Windows EventLog 出力（Windows）
- 非Windowsイベントログ相当出力（ファイル / syslog）
- 実行サマリー JSON 出力
- 明示的な終了コード

## 必要環境

- .NET SDK 10.0 以上（開発・テスト時）
- 実行物は self-contained publish によりランタイム同梱可能

## 使い方

```bash
# ヘルプ
FileArchiver --help

# 設定テンプレート作成
FileArchiver --init-config

# 設定チェック
FileArchiver --check

# Dry-run
FileArchiver --dry-run

# 本番実行
FileArchiver
```

## 終了コード

- `0`: 正常終了
- `2`: 設定エラー（`--check` 失敗など）
- `3`: 実行時エラー（処理失敗件数あり）

## config.toml 主要設定

```toml
LogFilePath = "log.txt"
LogLevel = "info"                 # debug / info / warn / error
MaxLogSizeBytes = 1048576
ZipFileNameFormat = "archive_{0:yyyyMMddHHmmss}.zip"

EnableEventLog = false
EventLogLevel = "warn"            # debug / info / warn / error
NonWindowsEventLogPath = "eventlog.txt"
NonWindowsEventLogTarget = "both" # file / syslog / both

ActionOrder = "rename,compress,delete"
SummaryOutputPath = "summary.json"

[[FolderSettings]]
Directory = "./data"
DaysOld = 30
IncludePattern = "\\.log$"
ExcludePattern = "^temp"
Recursive = true

EnableRename = true
RenameDaysOld = 30
RenameOnInUse = "warn"            # warn / error

EnableDelete = true
DeleteDaysOld = 60
DeleteOnInUse = "warn"            # warn / error

EnableZipCompression = true
CreateEmptyAfterRename = false
DateComparisonToleranceMinutes = 5
```

## ActionOrder について

- `ActionOrder` は `rename,compress,delete` のみを許可する検証対象です。
- 不正な値は `--check` でエラーになります。

## 実行サマリー

`SummaryOutputPath` に以下カウンタを JSON 出力します。

- `Scanned`
- `Renamed`
- `Compressed`
- `Deleted`
- `Skipped`
- `Failed`

## テスト

- テストコード: `FileArchiver.Tests/ArchiverBehaviorTests.cs`
- 詳細テスト仕様: `test/TESTCASE.md`

```bash
dotnet test FileArchiver.sln -v minimal
```

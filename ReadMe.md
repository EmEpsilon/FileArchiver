# FileArchiver

FileArchiver は、指定フォルダ配下のファイルを条件に応じて **リネーム / ZIP圧縮 / 削除** する CLI ツールです。  
定期実行（タスクスケジューラ / cron）を前提とした運用を想定しています。

## 対応環境

- .NET 10（開発・ビルド時）
- 実行環境: Windows / Linux / macOS
- self-contained publish によりランタイム同梱配布可能

---

## クイックスタート

```bash
# 1) テンプレート設定を作成
FileArchiver --init-config

# 2) 設定内容を確認
FileArchiver --check

# 3) 影響確認（実変更なし）
FileArchiver --dry-run

# 4) 本番実行
FileArchiver
```

---

## コマンドラインオプション詳細

| オプション | 説明 | 主な用途 |
|---|---|---|
| `--help` | ヘルプを表示 | 使い方の確認 |
| `--version` | バージョン情報を表示 | 運用時のバージョン確認 |
| `--init-config [path]` | 設定テンプレートを作成。`path` 省略時は `./config.toml` | 初期導入 |
| `--check` | `config.toml` の整合性チェックのみを実施 | 本番前検証 / CI |
| `--dry-run` | 実ファイル操作なしで対象と予定操作をログ出力 | 影響範囲確認 |

### `--init-config` の注意

- `--init-config` の直後が別オプション（`--xxx`）なら、パス指定なし扱いになります。
- 例: `FileArchiver --init-config myconfig.toml`

---

## 終了コード

| コード | 意味 |
|---|---|
| `0` | 正常終了 |
| `2` | 設定エラー（`--check` 失敗など） |
| `3` | 実行時エラー（処理失敗件数あり） |

---

## config.toml 詳細

### グローバル設定

| キー | 型 | 説明 |
|---|---|---|
| `LogFilePath` | string | 通常ログの出力先 |
| `LogLevel` | string | `debug` / `info` / `warn` / `error` |
| `MaxLogSizeBytes` | int | ログローテーション閾値（超過で ZIP 化） |
| `ZipFileNameFormat` | string | ZIP ファイル名フォーマット（`{0}` は日時） |
| `EnableEventLog` | bool | イベントログ出力の有効化 |
| `EventLogLevel` | string | イベントログ出力レベル (`debug/info/warn/error`) |
| `NonWindowsEventLogPath` | string | 非Windows時のイベントログ相当ファイル |
| `NonWindowsEventLogTarget` | string | `file` / `syslog` / `both` |
| `SummaryOutputPath` | string | 実行サマリー JSON 出力先 |

### `[[FolderSettings]]`（複数可）

| キー | 型 | 説明 |
|---|---|---|
| `Directory` | string | 対象ディレクトリ |
| `Recursive` | bool | サブディレクトリを含めるか |
| `IncludePattern` | string | 対象に含める正規表現 |
| `ExcludePattern` | string | 対象から除外する正規表現 |
| `DaysOld` | int | 圧縮対象の更新日経過日数 |
| `EnableZipCompression` | bool | 圧縮機能ON/OFF |
| `EnableRename` | bool | リネーム機能ON/OFF |
| `RenameDaysOld` | int | リネーム対象の作成日経過日数 |
| `RenameOnInUse` | string | 使用中ファイル時の挙動 (`warn`/`error`) |
| `CreateEmptyAfterRename` | bool | リネーム後に元名の空ファイル作成 |
| `EnableDelete` | bool | 削除機能ON/OFF |
| `DeleteDaysOld` | int | 削除対象の作成日経過日数 |
| `DeleteOnInUse` | string | 使用中ファイル時の挙動 (`warn`/`error`) |
| `DateComparisonToleranceMinutes` | int | 日付比較の許容分 |

#### `[[FolderSettings]]` を複数設定する書き方

`[[FolderSettings]]` ブロックを **必要な数だけ繰り返し** 記述します。  
各ブロックが 1 つの対象ディレクトリ設定になります。

```toml
[[FolderSettings]]
Directory = "./app1-logs"
DaysOld = 7
IncludePattern = "\\.log$"
ExcludePattern = "^temp"
Recursive = true
EnableRename = true
RenameDaysOld = 1
EnableDelete = true
DeleteDaysOld = 30
EnableZipCompression = true

[[FolderSettings]]
Directory = "./app2-logs"
DaysOld = 3
IncludePattern = "\\.txt$"
ExcludePattern = ""
Recursive = false
EnableRename = false
EnableDelete = false
EnableZipCompression = true
```

---

## ファイルローテーションの具体例

### 要件

- 対象ディレクトリ: `./XX`
- 対象拡張子: `.log`
- 作成から **1日** 経過でリネーム
- 更新から **5日** 経過で圧縮
- 作成から **10日** 経過で削除

### 設定例（そのまま使える形）

```toml
LogFilePath = "log.txt"
LogLevel = "info"
MaxLogSizeBytes = 1048576
ZipFileNameFormat = "archive_{0:yyyyMMddHHmmss}.zip"

EnableEventLog = false
EventLogLevel = "warn"
NonWindowsEventLogPath = "eventlog.txt"
NonWindowsEventLogTarget = "file"

SummaryOutputPath = "summary.json"

[[FolderSettings]]
Directory = "./XX"
Recursive = true
IncludePattern = "\\.log$"
ExcludePattern = ""

EnableRename = true
RenameDaysOld = 1
RenameOnInUse = "warn"
CreateEmptyAfterRename = false

EnableZipCompression = true
DaysOld = 5

EnableDelete = true
DeleteDaysOld = 10
DeleteOnInUse = "warn"

DateComparisonToleranceMinutes = 5
```

### この設定で起こるローテーション

1. `./XX` 配下（再帰）で `.log` ファイルを対象にします。
2. 作成日時が 1 日以上前のファイルは `_yyyyMMddHHmmss` 付きにリネームされます。
3. 最終更新日時が 5 日以上前のファイルは ZIP 圧縮され、元ファイルは削除されます。
4. 作成日時が 10 日以上前のファイルは削除されます。

## 使用例

### 例1: 導入直後の安全確認

```bash
FileArchiver --init-config
FileArchiver --check
FileArchiver --dry-run
```

### 例2: Linux サーバーで syslog にも出したい

```toml
EnableEventLog = true
EventLogLevel = "info"
NonWindowsEventLogTarget = "both"
NonWindowsEventLogPath = "/var/log/filearchiver-event.log"
```

### 例3: 日次運用（cron）

```cron
0 2 * * * /opt/filearchiver/FileArchiver >> /var/log/filearchiver-cron.log 2>&1
```

### 例4: CI で設定ファイルだけ検証

```bash
FileArchiver --check
echo $?  # 0:OK / 2:設定不正
```

---

## 実行サマリー

`SummaryOutputPath` に JSON を出力します。

```json
{
  "Scanned": 120,
  "Renamed": 10,
  "Compressed": 30,
  "Deleted": 5,
  "Skipped": 72,
  "Failed": 3
}
```

---

## テスト

- テストコード: `FileArchiver.Tests/ArchiverBehaviorTests.cs`
- テスト設計書: `test/TESTCASE.md`

```bash
dotnet test FileArchiver.sln -v minimal
```

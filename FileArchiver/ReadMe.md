# FileZipArchiver

このアプリケーションは、指定したディレクトリ内のファイルを自動的に Zip 圧縮し、元ファイルを削除するツールです。
一定日数以上経過したファイルだけを対象とすることで、不要な古いファイルをアーカイブし、ディスク領域を効率的に利用できます。

## 主な機能

1. **Zip 圧縮**

   - 指定した日数 (DaysOld) よりもファイルの最終更新日が古い場合、ファイルを Zip 圧縮して元ファイルを削除。
   - 圧縮先の Zip ファイル名にはタイムスタンプ (yyyyMMddHHmmss) が付加され、上書き衝突を防ぎます。

2. **ドライラン (Dry-run)**

   - `--dry-run` オプションを指定すると、実際のファイル操作は行わず、どのファイルが対象なのかのみログ出力。

3. **ログ管理とローテーション**

   - 指定サイズ (`MaxLogSizeBytes`) を超えると、ログファイルが自動で Zip 圧縮されローテーション。

4. **設定ファイル (config.toml)**

   - TOML形式で設定を行い、圧縮対象の日数やフォルダ設定、ログ出力先などを制御可能。

## 使い方

1. **ヘルプ表示**

   ```powershell
   FileZipArchiver.exe --help
   ```

   主要なオプションと簡易説明を表示します。

2. **config.toml の準備**
   同じフォルダに `config.toml` を用意し、以下のような設定を書きます。または独自に編集してください。

   ```toml
   LogFilePath = "log.txt"       # ログファイル出力先
   LogLevel = "info"             # ログ出力詳細度 (debug/info/warn/error)
   MaxLogSizeBytes = 1048576      # 1MB でログローテーション
   ZipFileNameFormat = "archive_{0:yyyyMMddHHmmss}.zip"  # 圧縮ファイルの名前フォーマット

   [[FolderSettings]]
   Directory = "C:/data"        # 処理対象フォルダ
   DaysOld = 30                  # 30日以上前に更新されたファイルを圧縮
   IncludePattern = "\\.log$"   # .logファイルを対象
   ExcludePattern = "^temp"     # tempで始まるファイルは除外
   Recursive = true              # サブフォルダも含めるか
   ```

3. **整合性チェック**

   ```powershell
   FileZipArchiver.exe --check
   ```

   `config.toml` の内容と整合性をチェックし、エラーや警告があれば表示します。

4. **Dry-run**

   ```powershell
   FileZipArchiver.exe --dry-run
   ```

   ファイル操作は行わず、処理予定ファイルのみをログ出力。安全にテストできます。

5. **本番実行**

   ```powershell
   FileZipArchiver.exe
   ```

   設定ファイルにしたがって、ZIP圧縮や削除を実施。実行内容はコンソールと `log.txt` に出力されます。

## オプション一覧

| オプション           | 説明                                                                        |
| --------------- | ------------------------------------------------------------------------- |
| `--help`        | このヘルプを表示                                                                  |
| `--check`       | `config.toml` の内容と整合性をチェック                                                |
| `--dry-run`     | 実際の操作を行わず、どのファイルが対象になるかログ出力のみ                                             |
| `--version`     | バージョン情報を表示                                                                |
| `--init-config` | テンプレート `config.toml` を作成。引数で出力ファイル名を指定可能 (例: `--init-config myconf.toml`) |

## config.toml 詳細設定

| 項目                     | 説明                                           |
| ---------------------- | -------------------------------------------- |
| **LogFilePath**        | ログファイルの保存先パス                                 |
| **LogLevel**           | ログ出力の詳細度（debug / info / warn / error）        |
| **MaxLogSizeBytes**    | ログファイルサイズがこれを超えると自動で ZIP 圧縮                  |
| **ZipFileNameFormat**  | ZIP ファイル名のフォーマット (`{0}` に日時が挿入される)           |
| **EnableEventLog**     | Windows イベントログへ出力するか (true / false)          |
| **EventLogLevel**      | イベントログへの出力最低レベル（debug / info / warn / error） |
| **[[FolderSettings]]** | 複数指定可。各フォルダごとに細かい設定が可能。以下は主なパラメータを列挙。        |

### FolderSettings 内の主なパラメータ

| パラメータ                      | 意味                                          |
| -------------------------- | ------------------------------------------- |
| **Directory**              | 対象フォルダのパス（必須）                               |
| **DaysOld**                | ファイルの更新日がこれ以上古い場合にZIP圧縮対象                   |
| **IncludePattern**         | ファイル名を正規表現で指定し、対象に含める                       |
| **ExcludePattern**         | ファイル名を正規表現で指定し、対象から除外する                     |
| **Recursive**              | サブフォルダも含めるか (true/false)                    |
| **EnableRename**           | リネーム機能を使うか (true/false)                     |
| **RenameDaysOld**          | ファイル作成日がこれ以上古い場合リネーム対象                      |
| **RenameOnInUse**          | リネーム時、ファイルロックに遭遇した場合の挙動 (`warn` or `error`) |
| **EnableDelete**           | 削除機能を使うか (true/false)                       |
| **DeleteDaysOld**          | ファイル作成日がこれ以上古い場合削除対象                        |
| **DeleteOnInUse**          | 削除時ファイルロックに遭遇した場合の挙動 (`warn` or `error`)    |
| **EnableZipCompression**   | Zip圧縮を行うか (true/false)                      |
| **CreateEmptyAfterRename** | リネーム後、同名の空ファイルを作成するか (true/false)           |

## ログレベルについて

- `debug` : 全ログ (デバッグ情報を含む)
- `info`  : debug を除くログ
- `warn`  : 警告 (warn) とエラー (error) のみ
- `error` : エラーのみ

## Windowsイベントログへの出力

`EnableEventLog = true` に設定し、`EventLogLevel` を適切に指定すると、Windowsの Application イベントログにも出力します。ただし、以下の設定が必要です：

```powershell
# PowerShellなどで実行（管理者権限）
# "FileArchiver" というソースを "Application" ログに登録
New-EventLog -LogName Application -Source "FileArchiver"
```

管理者権限が必要となる場合があるため、権限設定やポリシーに注意してください。

## その他

- `.NET Framework 4.6.2` 以降で動作します。
- 圧縮先は個別ファイル (\*.zip) となり、衝突を避けるために `_yyyyMMddHHmmss` を付加して命名します。


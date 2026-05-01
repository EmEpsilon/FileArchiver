# FileArchiver テストケース一覧

このドキュメントは `FileArchiver.Tests/ArchiverBehaviorTests.cs` のテスト意図・前提・確認観点を整理したものです。

## テスト方針

- 実行方式: `dotnet run --project ...` による E2E 寄り統合テスト
- 各ケースで一時ディレクトリを作成し、`config.toml` を都度生成
- ファイルの存在/非存在、ZIP の中身、出力内容を検証

## テストケース

| ID | テスト名 | 目的 | 主な検証ポイント |
|---|---|---|---|
| TC-001 | 初期設定: --init-config でテンプレートファイルを作成する | 初期設定テンプレート生成確認 | 指定ファイルが作成される |
| TC-002 | Dry-run: 圧縮対象でもファイルは圧縮されない | Dry-run安全性確認（圧縮） | 元ファイル残存 / ZIP未生成 |
| TC-003 | 通常実行: 期限超過ファイルを圧縮し元ファイルを削除する | 圧縮メインフロー確認 | 元ファイル削除 / ZIP作成 / ZIP内エントリ |
| TC-004 | 非Windows: EnableEventLog=true でフォールバックログファイルへ出力する | クロスプラットフォームログ確認 | 非Windowsでフォールバックログ生成 |
| TC-005 | リネーム: EnableRename=true で期限超過ファイルをリネームする | リネーム機能確認 | リネーム後ファイル生成 / 元名消失 |
| TC-006 | 削除: EnableDelete=true で期限超過ファイルを削除する | 削除機能確認 | 対象ファイルが削除される |
| TC-007 | 設定チェック: 不正設定ではエラーが出力される | バリデーション確認 | `--check` 出力に `[error]` |
| TC-008 | リネーム: CreateEmptyAfterRename=true で空ファイルを再作成する | 追加仕様確認 | 元名の空ファイル作成 / リネーム済みファイル存在 |
| TC-009 | フィルタ: Include/Exclude の条件どおりに処理対象を選別する | 正規表現フィルタ確認 | includeのみ処理 / exclude残存 |
| TC-010 | Dry-run: リネーム/削除/圧縮を実行しない | Dry-run安全性確認（全操作） | rename/delete/compress 未実行 |

## 実行手順

```bash
dotnet test FileArchiver.sln -v minimal
```

Visual Studio の場合は Test Explorer から `ArchiverBehaviorTests` を実行できます。

## 追加推奨ケース（今後）

- `SummaryOutputPath` の JSON 内容検証（件数一致）
- 終了コード（0/2/3）の明示検証
- `ActionOrder` の実行順制御が実装された場合の順序テスト
- 大量ファイルでの性能・メモリ回帰テスト

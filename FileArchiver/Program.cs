// 完全実装されたファイルアーカイブユーティリティ（最終整形済）
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Nett;
using System.Diagnostics;

namespace FileArchiver
{
    public class FolderConfig
    {
        public string Directory { get; set; }
        public int DaysOld { get; set; } = 0;
        public string IncludePattern { get; set; } = ".*";
        public string ExcludePattern { get; set; } = "";
        public bool Recursive { get; set; } = false;

        public bool EnableRename { get; set; } = false;
        public int RenameDaysOld { get; set; } = 30;
        public string RenameOnInUse { get; set; } = "warn";

        public bool EnableDelete { get; set; } = false;
        public int DeleteDaysOld { get; set; } = 60;
        public string DeleteOnInUse { get; set; } = "warn";
        public bool EnableZipCompression { get; set; } = true;
        public bool CreateEmptyAfterRename { get; set; } = false;
        public int DateComparisonToleranceMinutes { get; set; } = 5; 
    }

    public class Config
    {
        public bool EnableEventLog { get; set; } = false; // Windowsイベントログ出力を有効にするか
        public string EventLogLevel { get; set; } = "warn"; // debug, info, warn, error から選択
                                                            // 修正: C# 7.3 では target-typed オブジェクトの作成がサポートされていないため、明示的な型指定を追加します。
        public List<FolderConfig> FolderSettings { get; set; } = new List<FolderConfig>();
        public string LogFilePath { get; set; } = "log.txt";
        public string ZipFileNameFormat { get; set; } = "archive_{0:yyyyMMddHHmmss}.zip";
        public string LogLevel { get; set; } = "info";
        public int MaxLogSizeBytes { get; set; } = 1024 * 1024;
    }

    class FileArchiverApp
    {
        static Config config;
        static bool isDryRun = false;

        static void Main(string[] args)
        {
            const string Version = "1.0.1";

            if (args.Contains("--version"))
            {
                Console.WriteLine($"FileArchiver バージョン: {Version}");
                return;
            }

            // 引数指定があればそのパスに、なければ "config.toml" に出力
            if (args.Contains("--init-config"))
            {
                string targetPath = "config.toml";
                int initIndex = Array.IndexOf(args, "--init-config");
                if (initIndex >= 0 && initIndex < args.Length - 1)
                {
                    // 次の引数が存在し、--init-config の直後であればそれをパスとして扱う
                    targetPath = args[initIndex + 1].StartsWith("--") ? "config.toml" : args[initIndex + 1];
                }
                File.WriteAllText(targetPath, 
                    "LogFilePath = \"log.txt\"" + Environment.NewLine +
                    "LogLevel = \"info\"" + Environment.NewLine +
                    "MaxLogSizeBytes = 1048576" + Environment.NewLine +
                    "ZipFileNameFormat = \"archive_{0:yyyyMMddHHmmss}.zip\"" + Environment.NewLine +
                    "EnableEventLog = false" + Environment.NewLine +
                    "EventLogLevel = \"warn\"" + Environment.NewLine +
                    "[[FolderSettings]]" + Environment.NewLine +
                    "Directory = \"C:/data\"" + Environment.NewLine +
                    "DaysOld = 30" + Environment.NewLine +
                    "IncludePattern = \"\\.log$\"" + Environment.NewLine +
                    "ExcludePattern = \"^temp\"" + Environment.NewLine +
                    "Recursive = true" + Environment.NewLine +
                    "EnableRename = true" + Environment.NewLine +
                    "RenameDaysOld = 30" + Environment.NewLine +
                    "RenameOnInUse = \"warn\"" + Environment.NewLine +
                    "EnableDelete = true" + Environment.NewLine +
                    "DeleteDaysOld = 60" + Environment.NewLine +
                    "DeleteOnInUse = \"warn\"" + Environment.NewLine +
                    "EnableZipCompression = true" + Environment.NewLine +
                    "CreateEmptyAfterRename = false" + Environment.NewLine +
                    "DateComparisonToleranceMinutes = 5" + Environment.NewLine
                );
                Console.WriteLine($"テンプレート {targetPath} を作成しました。");
                return;
            }

            if (args.Contains("--help"))
            {
                ShowHelp();
                return;
            }

            string configPath = "config.toml";
            if (!File.Exists(configPath))
            {
                Console.WriteLine($"設定ファイルが見つかりません: {configPath}");
                return;
            }

            try { config = Toml.ReadFile<Config>(configPath); }
            catch (Exception ex)
            {
                Log("error", $"設定ファイル読み込みエラー: {ex.Message}", ConsoleColor.Red);
                return;
            }

            if (args.Contains("--check"))
            {
                CheckConfig();
                return;
            }

            isDryRun = args.Contains("--dry-run");
            if (!Directory.Exists(Path.GetDirectoryName(config.LogFilePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(config.LogFilePath));

            // show config
            Log("info", "=== FileArchiver 設定内容 ===", ConsoleColor.Magenta);
            Log("info", $"LogFilePath: {config.LogFilePath}", ConsoleColor.Magenta);
            Log("info", $"LogLevel: {config.LogLevel}", ConsoleColor.Magenta);
            Log("info", $"MaxLogSizeBytes: {config.MaxLogSizeBytes}", ConsoleColor.Magenta);
            Log("info", $"EnableEventLog: {config.EnableEventLog}", ConsoleColor.Magenta);
            Log("info", $"EventLogLevel: {config.EventLogLevel}", ConsoleColor.Magenta);
            Log("info", $"ZipFileNameFormat: {config.ZipFileNameFormat}", ConsoleColor.Magenta);
            Log("info", "=== FileArchiver 処理開始 ===", ConsoleColor.Magenta);
            Log("info", $"[dry-run] {isDryRun}", ConsoleColor.Magenta);
            Log("info", $"[version] {Version}", ConsoleColor.Magenta);
            Log("info", $"[date] {DateTime.Now:yyyy-MM-dd HH:mm:ss}", ConsoleColor.Magenta);
            Log("info", $"[os] {Environment.OSVersion}", ConsoleColor.Magenta);
            Log("info", $"[user] {Environment.UserName}", ConsoleColor.Magenta);
            Log("info", $"[machine] {Environment.MachineName}", ConsoleColor.Magenta);
            Log("info", $"[working-directory] {Environment.CurrentDirectory}", ConsoleColor.Magenta);
            Log("info", $"[process-id] {Process.GetCurrentProcess().Id}", ConsoleColor.Magenta);
            Log("info", $"[process-name] {Process.GetCurrentProcess().ProcessName}", ConsoleColor.Magenta);
            Log("info", $"[process-path] {Process.GetCurrentProcess().MainModule.FileName}", ConsoleColor.Magenta);
            Log("info", $"[process-args] {string.Join(" ", args)}", ConsoleColor.Magenta);
            
            foreach (var folder in config.FolderSettings)
            {
                // show folder config
                Log("info", "=== FileArchiver フォルダ処理 ===", ConsoleColor.DarkMagenta);
                if (string.IsNullOrEmpty(folder.Directory))
                {
                    Log("error", "Directory が指定されていません", ConsoleColor.Red);
                    continue;
                }
                if (folder.DaysOld < 0)
                {
                    Log("error", "DaysOld が不正です", ConsoleColor.Red);
                    continue;
                }
                if (folder.RenameDaysOld < 0)
                {
                    Log("error", "RenameDaysOld が不正です", ConsoleColor.Red);
                    continue;
                }
                if (folder.DeleteDaysOld < 0)
                {
                    Log("error", "DeleteDaysOld が不正です", ConsoleColor.Red);
                    continue;
                }
                if (folder.EnableRename && folder.RenameDaysOld <= 0)
                {
                    Log("error", "EnableRename が true の場合、RenameDaysOld は 0 より大きくなければなりません", ConsoleColor.Red);
                    continue;
                }
                if (folder.EnableDelete && folder.DeleteDaysOld <= 0)
                {
                    Log("error", "EnableDelete が true の場合、DeleteDaysOld は 0 より大きくなければなりません", ConsoleColor.Red);
                    continue;
                }
                if (folder.RenameOnInUse != "warn" && folder.RenameOnInUse != "error")
                {
                    Log("error", "RenameOnInUse が不正です", ConsoleColor.Red);
                    continue;
                }
                if (folder.DeleteOnInUse != "warn" && folder.DeleteOnInUse != "error")
                {
                    Log("error", "DeleteOnInUse が不正です", ConsoleColor.Red);
                    continue;
                }
                if (folder.IncludePattern == null)
                {
                    Log("error", "IncludePattern が不正です", ConsoleColor.Red);
                    continue;
                }
                if (folder.ExcludePattern == null)
                {
                    Log("error", "ExcludePattern が不正です", ConsoleColor.Red);
                    continue;
                }
                if (folder.EnableZipCompression && folder.DaysOld <= 0)
                {
                    Log("error", "EnableZipCompression が true の場合、DaysOld は 0 より大きくなければなりません", ConsoleColor.Red);
                    continue;
                }
                if (folder.DateComparisonToleranceMinutes < 0)
                {
                    Log("error", "DateComparisonToleranceMinutes は 0 以上でなければなりません", ConsoleColor.Red);
                    continue;
                }
                if (folder.EnableZipCompression && folder.DaysOld > 0)
                {
                    Log("info", "ZIP圧縮を有効にしています", ConsoleColor.DarkMagenta);
                }
                else
                {
                    Log("info", "ZIP圧縮を無効にしています", ConsoleColor.DarkMagenta);
                }
                Log("info", $"処理対象: {folder.Directory}", ConsoleColor.DarkMagenta);
                if (!Directory.Exists(folder.Directory))
                {
                    Log("warn", $"ディレクトリが存在しません: {folder.Directory}", ConsoleColor.Yellow);
                    continue;
                }

                if (folder.Recursive)
                    Log("info", $"再帰的に処理: {folder.Directory}", ConsoleColor.DarkMagenta);
                else
                    Log("info", $"非再帰的に処理: {folder.Directory}", ConsoleColor.DarkMagenta);
                if (!string.IsNullOrEmpty(folder.IncludePattern))
                    Log("info", $"対象ファイルの正規表現: {folder.IncludePattern}", ConsoleColor.DarkMagenta);
                if (!string.IsNullOrEmpty(folder.ExcludePattern))
                    Log("info", $"除外ファイルの正規表現: {folder.ExcludePattern}", ConsoleColor.DarkMagenta);
                if (folder.EnableRename)
                    Log("info", $"リネーム対象: {folder.RenameDaysOld} 日以上前のファイル", ConsoleColor.DarkMagenta);
                if (folder.EnableDelete)
                    Log("info", $"削除対象: {folder.DeleteDaysOld} 日以上前のファイル", ConsoleColor.DarkMagenta);
                if (folder.EnableRename && folder.RenameOnInUse == "error")
                    Log("info", $"リネーム失敗時の挙動: エラーをスロー", ConsoleColor.DarkMagenta);
                if (folder.EnableRename && folder.RenameOnInUse == "warn")
                    Log("info", $"リネーム失敗時の挙動: 警告を表示", ConsoleColor.DarkMagenta);
                if (folder.EnableDelete && folder.DeleteOnInUse == "error")
                    Log("info", $"削除失敗時の挙動: エラーをスロー", ConsoleColor.DarkMagenta);
                if (folder.EnableDelete && folder.DeleteOnInUse == "warn")
                    Log("info", $"削除失敗時の挙動: 警告を表示", ConsoleColor.DarkMagenta);
                if (folder.EnableRename)
                    Log("info", $"リネーム有効: {folder.EnableRename}", ConsoleColor.DarkMagenta);
                if (folder.EnableRename && folder.CreateEmptyAfterRename)
                    Log("info", $"リネーム後に空ファイルを作成: {folder.CreateEmptyAfterRename}", ConsoleColor.DarkMagenta);
                if (folder.EnableDelete)
                    Log("info", $"削除有効: {folder.EnableDelete}", ConsoleColor.DarkMagenta);
                if (folder.EnableZipCompression)
                    Log("info", $"ZIP圧縮有効: {folder.EnableZipCompression}", ConsoleColor.DarkMagenta);
                if (folder.DaysOld > 0)
                    Log("info", $"圧縮対象: {folder.DaysOld} 日以上前のファイル", ConsoleColor.DarkMagenta);


                var option = folder.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var files = Directory.GetFiles(folder.Directory, "*", option)
                    .Where(f => File.Exists(f))
                    .Where(f => string.IsNullOrEmpty(folder.IncludePattern) || Regex.IsMatch(f, folder.IncludePattern))
                    .Where(f => string.IsNullOrEmpty(folder.ExcludePattern) || !Regex.IsMatch(f, folder.ExcludePattern));

                foreach (var file in files)
                {
                    Log("debug", $"処理対象(ファイル): {file}", ConsoleColor.Cyan);

                    bool isZip = Path.GetExtension(file).Equals(".zip", StringComparison.OrdinalIgnoreCase);
                    var created = File.GetCreationTime(file);
                    var now = DateTime.Now;

                    Log("debug", $"対象ファイルはZIPファイルか: {isZip}", ConsoleColor.Cyan);
                    Log("debug", $"ファイル作成日: {created:yyyy-MM-dd HH:mm:ss}", ConsoleColor.Cyan);
                    Log("debug", $"ファイル更新日: {File.GetLastWriteTime(file):yyyy-MM-dd HH:mm:ss}", ConsoleColor.Cyan);

                    // Rename
                    if (!isZip && folder.EnableRename && created <= now.AddDays(-folder.RenameDaysOld).AddMinutes(folder.DateComparisonToleranceMinutes) && !Regex.IsMatch(Path.GetFileNameWithoutExtension(file), "_\\d{8,14}$"))
                    {
                        string newName = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + "_" + now.ToString("yyyyMMddHHmmss") + Path.GetExtension(file));
                        try
                        {
                            if (isDryRun)
                                Log("info", $"[dry-run] リネーム予定: {file} → {newName}", ConsoleColor.Cyan);
                            else
                            {
                                File.Move(file, newName);
                                Log("info", $"リネーム完了: {file} → {newName}", ConsoleColor.Blue);
                                if (folder.CreateEmptyAfterRename)
                                {
                                    try
                                    {
                                        using (var fs = File.Create(file)) 
                                        {
                                            // 空ファイルを作成するための処理
                                            fs.Close();
                                        }
                                        // 作成日時と更新日時を現在時刻に設定(念のため)
                                        File.SetCreationTime(file, DateTime.Now);
                                        File.SetLastWriteTime(file, DateTime.Now);
                                        Log("info", $"空ファイル作成: {file}", ConsoleColor.Blue);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log("warn", $"空ファイル作成に失敗: {file} ({ex.Message})", ConsoleColor.Yellow);
                                    }
                                }
                            }
                            continue;
                        }
                        catch (IOException ex)
                        {
                            string level = folder.RenameOnInUse == "error" ? "error" : "warn";
                            Log(level, $"リネーム失敗（使用中）: {file} ({ex.Message})", level == "error" ? ConsoleColor.Red : ConsoleColor.Yellow);
                            continue;
                        }
                    }
                    else
                    {
                        Log("debug", $"リネームスキップ: {file}", ConsoleColor.Cyan);
                    }

                    // Compress
                    if (folder.EnableZipCompression && !isZip && File.GetLastWriteTime(file) <= now.AddDays(-folder.DaysOld).AddMinutes(folder.DateComparisonToleranceMinutes))
                    {
                        string zipPath = Path.Combine(Path.GetDirectoryName(file), Path.GetFileName(file) + "_" + now.ToString("yyyyMMddHHmmss") + ".zip");
                        try
                        {
                            if (isDryRun)
                            {
                                Log("info", $"[dry-run] 圧縮予定: {file} → {zipPath}", ConsoleColor.Cyan);
                            }
                            else
                            {
                                using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                                using (var input = File.OpenRead(file))
                                {
                                    var entry = zip.CreateEntry(Path.GetFileName(file), CompressionLevel.Optimal);
                                    using (var output = entry.Open())
                                    {
                                        input.CopyTo(output);
                                    }
                                }
                                File.Delete(file);
                                Log("info", $"圧縮削除完了: {file} → {zipPath}", ConsoleColor.Green);
                            }
                            continue;
                        }
                        catch (Exception ex)
                        {
                            Log("warn", $"圧縮失敗: {file} ({ex.Message})", ConsoleColor.Yellow);
                            continue;
                        }
                    }
                    else
                    {
                        Log("debug", $"圧縮スキップ: {file}", ConsoleColor.Cyan);
                    }

                    // Delete
                    if (folder.EnableDelete && created <= now.AddDays(-folder.DeleteDaysOld).AddMinutes(folder.DateComparisonToleranceMinutes))
                    {
                        try
                        {
                            if (isDryRun)
                                Log("info", $"[dry-run] 削除予定: {file}", ConsoleColor.Cyan);
                            else
                            {
                                File.Delete(file);
                                Log("info", $"削除完了: {file}", ConsoleColor.DarkRed);
                            }
                        }
                        catch (IOException ex)
                        {
                            string level = folder.DeleteOnInUse == "error" ? "error" : "warn";
                            Log(level, $"削除失敗（使用中）: {file} ({ex.Message})", level == "error" ? ConsoleColor.Red : ConsoleColor.Yellow);
                        }
                    }
                    else
                    {
                        Log("debug", $"削除スキップ: {file}", ConsoleColor.Cyan);
                    }
                }
            }
            Log("info", "=== FileArchiver 処理完了 ===", ConsoleColor.Magenta);
        }

        static void ShowHelp()
        {
            Console.WriteLine("=== FileArchiver 使用方法（詳細） ===");
            Console.WriteLine("このツールは、指定したディレクトリ内のファイルに対して、以下の順序で自動処理を行います：");
            Console.WriteLine("  1. リネーム：作成日が指定日数を超えたファイルに日時サフィックスを追加");
            Console.WriteLine("  2. ZIP圧縮 ：更新日が指定日数を超えたファイルをZIP圧縮し、元ファイルを削除");
            Console.WriteLine("  3. 削除    ：作成日が指定日数を超えたファイルを削除");

            Console.WriteLine("【コマンドラインオプション】");
            Console.WriteLine("  --help         : この詳細ヘルプを表示");
            Console.WriteLine("  --check        : 設定ファイル(config.toml)の整合性と内容の検証を行います");
            Console.WriteLine("  --dry-run      : 実際にファイル操作をせず、予定される処理内容をログ・画面に出力します");
            Console.WriteLine("  --version      : ツールのバージョン情報を表示します");
            Console.WriteLine("  --init-config  : 初期設定テンプレート(config.toml)をカレントディレクトリに出力します");

            Console.WriteLine("【設定ファイル (config.toml) の主な項目】");
            Console.WriteLine("  LogFilePath         : ログファイル出力先のパス");
            Console.WriteLine("  LogLevel            : 出力ログレベル（debug/info/warn/error）");
            Console.WriteLine("  MaxLogSizeBytes     : ログローテーションの最大サイズ（バイト）");
            Console.WriteLine("  EnableEventLog      : Windowsイベントログ出力を有効化するか");
            Console.WriteLine("  EventLogLevel       : イベントログに出力する最小レベル（warn 以上が一般的）");

            Console.WriteLine("[[FolderSettings]]  : 複数ディレクトリを対象に設定可能");
            Console.WriteLine("    Directory         : 対象ディレクトリ（必須）");
            Console.WriteLine("    Recursive         : サブフォルダも含めるか（true/false）");
            Console.WriteLine("    IncludePattern    : 対象ファイルの正規表現（例: \\.log$）");
            Console.WriteLine("    ExcludePattern    : 除外ファイルの正規表現（例: ^temp）");
            Console.WriteLine("    DaysOld           : 圧縮対象とする更新日からの経過日数");
            Console.WriteLine("    EnableRename      : リネームを行うか（true/false）");
            Console.WriteLine("    RenameDaysOld     : リネーム対象とする作成日からの経過日数");
            Console.WriteLine("    RenameOnInUse     : 使用中ファイルがあったときの挙動（warn/error）");
            Console.WriteLine("    EnableDelete      : 削除を行うか（true/false）");
            Console.WriteLine("    DeleteDaysOld     : 削除対象とする作成日からの経過日数");
            Console.WriteLine("    DeleteOnInUse     : 使用中ファイルがあったときの挙動（warn/error）");
            Console.WriteLine("    EnableZipCompression : ZIP圧縮を有効にするか（true/false）");
            Console.WriteLine("    CreateEmptyAfterRename : リネーム後、元ファイル名の空ファイルを作るか（true/false）\n");
            Console.WriteLine("    DateComparisonToleranceMinutes : 日付比較の際に許容する分数を指定");

            Console.WriteLine("【使用例】");
            Console.WriteLine("  FileArchiver.exe");
            Console.WriteLine("    → 設定ファイルの内容にしたがって、処理を行う。");
            Console.WriteLine("  FileArchiver.exe --dry-run");
            Console.WriteLine("    → 実際には操作せず、現在の設定でどのファイルが対象になるか確認できます。");
        }

        static void CheckConfig()
        {
            Console.WriteLine("[CHECK] 設定ファイルの整合性チェックを開始します");

            bool hasError = false;

            if (string.IsNullOrEmpty(config.LogFilePath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[error] LogFilePath が指定されていません");
                Console.ResetColor();
                hasError = true;
            }

            if (!new[] { "debug", "info", "warn", "error" }.Contains(config.LogLevel))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[error] LogLevel の値が不正です: {config.LogLevel}");
                Console.ResetColor();
                hasError = true;
            }

            if (config.MaxLogSizeBytes <= 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[error] MaxLogSizeBytes は 0 より大きくある必要があります");
                Console.ResetColor();
                hasError = true;
            }

            foreach (var folder in config.FolderSettings)
            {
                Console.WriteLine($"- フォルダ設定: {folder.Directory}");

                if (!Directory.Exists(folder.Directory))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("  [warn] 指定されたディレクトリが存在しません");
                    Console.ResetColor();
                }

                if (folder.DaysOld < 0 || folder.RenameDaysOld < 0 || folder.DeleteDaysOld < 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("  [error] 各 DaysOld の値は 0 以上である必要があります");
                    Console.ResetColor();
                    hasError = true;
                }

                try { _ = new Regex(folder.IncludePattern); }
                catch
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("  [error] IncludePattern が不正な正規表現です");
                    Console.ResetColor();
                    hasError = true;
                }

                try { _ = new Regex(folder.ExcludePattern); }
                catch
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("  [error] ExcludePattern が不正な正規表現です");
                    Console.ResetColor();
                    hasError = true;
                }

                if (!new[] { "warn", "error" }.Contains(folder.RenameOnInUse))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  [error] RenameOnInUse の値が不正です: {folder.RenameOnInUse}");
                    Console.ResetColor();
                    hasError = true;
                }

                if (!new[] { "warn", "error" }.Contains(folder.DeleteOnInUse))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  [error] DeleteOnInUse の値が不正です: {folder.DeleteOnInUse}");
                    Console.ResetColor();
                    hasError = true;
                }

                if (folder.EnableRename && folder.RenameDaysOld <= 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("  [error] EnableRename が true の場合、RenameDaysOld は 0 より大きくなければなりません");
                    Console.ResetColor();
                    hasError = true;
                }

                if (folder.EnableDelete && folder.DeleteDaysOld <= 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("  [error] EnableDelete が true の場合、DeleteDaysOld は 0 より大きくなければなりません");
                    Console.ResetColor();
                    hasError = true;
                }

                if (folder.EnableZipCompression && folder.DaysOld <= 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("  [error] EnableZipCompression が true の場合、DaysOld は 0 より大きくなければなりません");
                    Console.ResetColor();
                    hasError = true;
                }

                if (!folder.EnableRename && folder.CreateEmptyAfterRename)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("  [error] CreateEmptyAfterRename は EnableRename が true の場合のみ有効です");
                    Console.ResetColor();
                    hasError = true;
                }

                if (folder.DateComparisonToleranceMinutes < 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("  [error] DateComparisonToleranceMinutes は 0 以上でなければなりません");
                    Console.ResetColor();
                    hasError = true;
                }

                Console.WriteLine();
            }

            if (!hasError)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("すべての設定が正しく構成されています。");
                Console.ResetColor();
            }
        }

        static void Log(string type, string message, ConsoleColor color)
        {
            bool show = false;
            switch (config.LogLevel)
            {
                case "debug":
                    show = true;
                    break;
                case "info":
                    show = type != "debug";
                    break;
                case "warn":
                    show = type == "warn" || type == "error";
                    break;
                case "error":
                    show = type == "error";
                    break;
                default:
                    show = true;
                    break;
            }
            if (!show) return;

            var msg = $"[{type}] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {(isDryRun ? "[dry-run] " : "")}{message}";
            Console.ForegroundColor = color;
            Console.WriteLine(msg);
            Console.ResetColor();

            RotateLogIfNeeded();
            File.AppendAllText(config.LogFilePath, msg + Environment.NewLine);

            // Windows イベントログ出力
            if (config.EnableEventLog &&
                (config.EventLogLevel == "debug" ||
                 (config.EventLogLevel == "info" && type != "debug") ||
                 (config.EventLogLevel == "warn" && (type == "warn" || type == "error")) ||
                 (config.EventLogLevel == "error" && type == "error")))
            {
                try
                {

                    try
                    {
                        using (var eventLog = new System.Diagnostics.EventLog("Application"))
                        {
                            eventLog.Source = "FileArchiver";
                            EventLogEntryType logType;
                            switch (type)
                            {
                                case "error":
                                    logType = EventLogEntryType.Error;
                                    break;
                                case "warn":
                                    logType = EventLogEntryType.Warning;
                                    break;
                                default:
                                    logType = EventLogEntryType.Information;
                                    break;
                            }
                            eventLog.WriteEntry(message, logType);
                        }
                    }
                    catch (Exception ex)
                    {
                        // イベントログへの書き込みに失敗しても処理は継続
                        Console.WriteLine($"[warn] イベントログへの出力に失敗: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    // イベントログへの書き込みに失敗しても処理は継続
                    Console.WriteLine($"[warn] イベントログへの出力に失敗: {ex.Message}");
                }
            }
        }

        static void RotateLogIfNeeded()
        {
            if (!File.Exists(config.LogFilePath)) return;
            var info = new FileInfo(config.LogFilePath);
            if (info.Length < config.MaxLogSizeBytes) return;
            var zipName = Path.Combine(Path.GetDirectoryName(config.LogFilePath),
                Path.GetFileNameWithoutExtension(config.LogFilePath) + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".zip");
            using (var zip = ZipFile.Open(zipName, ZipArchiveMode.Create))
            {
                zip.CreateEntryFromFile(config.LogFilePath, Path.GetFileName(config.LogFilePath));
            }
            File.Delete(config.LogFilePath);
        }
    }
}

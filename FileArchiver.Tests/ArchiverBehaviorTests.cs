using System.Diagnostics;
using System.IO.Compression;
using FluentAssertions;

public class ArchiverBehaviorTests
{
    [Fact]
    public void InitConfig_CreatesTemplateFile()
    {
        using var wd = new TempDir();
        var r = RunApp(wd.Path, "--init-config", "custom.toml");
        r.ExitCode.Should().Be(0);
        File.Exists(System.IO.Path.Combine(wd.Path, "custom.toml")).Should().BeTrue();
    }

    [Fact]
    public void DryRun_DoesNotCompressTargetFile()
    {
        using var wd = new TempDir();
        var dataDir = System.IO.Path.Combine(wd.Path, "data");
        Directory.CreateDirectory(dataDir);
        var target = System.IO.Path.Combine(dataDir, "a.log");
        File.WriteAllText(target, "hello");
        File.SetLastWriteTime(target, DateTime.Now.AddDays(-40));

        File.WriteAllText(System.IO.Path.Combine(wd.Path, "config.toml"), $$"""
LogFilePath = "{{System.IO.Path.Combine(wd.Path, "log.txt").Replace("\\", "/")}}"
LogLevel = "debug"
MaxLogSizeBytes = 1048576
ZipFileNameFormat = "archive_{0:yyyyMMddHHmmss}.zip"
EnableEventLog = false
EventLogLevel = "warn"

[[FolderSettings]]
Directory = "{{dataDir.Replace("\\", "/")}}"
DaysOld = 1
IncludePattern = "\\.log$"
ExcludePattern = ""
Recursive = false
EnableRename = false
EnableDelete = false
EnableZipCompression = true
""");

        var r = RunApp(wd.Path, "--dry-run");
        r.ExitCode.Should().Be(0);
        File.Exists(target).Should().BeTrue();
        Directory.GetFiles(dataDir, "*.zip").Should().BeEmpty();
    }

    [Fact]
    public void Execution_CompressesAndDeletesOriginal()
    {
        using var wd = new TempDir();
        var dataDir = System.IO.Path.Combine(wd.Path, "data");
        Directory.CreateDirectory(dataDir);
        var target = System.IO.Path.Combine(dataDir, "a.log");
        File.WriteAllText(target, "payload");
        File.SetLastWriteTime(target, DateTime.Now.AddDays(-40));

        File.WriteAllText(System.IO.Path.Combine(wd.Path, "config.toml"), $$"""
LogFilePath = "{{System.IO.Path.Combine(wd.Path, "log.txt").Replace("\\", "/")}}"
LogLevel = "debug"
MaxLogSizeBytes = 1048576
EnableEventLog = false
EventLogLevel = "warn"

[[FolderSettings]]
Directory = "{{dataDir.Replace("\\", "/")}}"
DaysOld = 1
IncludePattern = "\\.log$"
ExcludePattern = ""
Recursive = false
EnableRename = false
EnableDelete = false
EnableZipCompression = true
""");

        var r = RunApp(wd.Path);
        r.ExitCode.Should().Be(0);
        File.Exists(target).Should().BeFalse();
        var zip = Directory.GetFiles(dataDir, "*.zip").Single();
        using var archive = ZipFile.OpenRead(zip);
        archive.Entries.Select(e => e.Name).Should().Contain("a.log");
    }

    [Fact]
    public void NonWindows_WhenEventLogEnabled_WritesFallbackEventLogFile()
    {
        if (OperatingSystem.IsWindows()) return;

        using var wd = new TempDir();
        var fallbackLog = System.IO.Path.Combine(wd.Path, "eventlog.txt");
        File.WriteAllText(System.IO.Path.Combine(wd.Path, "config.toml"), $$"""
LogFilePath = "{{System.IO.Path.Combine(wd.Path, "log.txt").Replace("\\", "/")}}"
LogLevel = "info"
MaxLogSizeBytes = 1048576
EnableEventLog = true
EventLogLevel = "info"
NonWindowsEventLogPath = "{{fallbackLog.Replace("\\", "/")}}"
NonWindowsEventLogTarget = "file"
FolderSettings = []
""");

        var r = RunApp(wd.Path);
        r.ExitCode.Should().Be(0);
        File.Exists(fallbackLog).Should().BeTrue();
        File.ReadAllText(fallbackLog).Should().Contain("FileArchiver 設定内容");
    }

    [Fact]
    public void Execution_RenamesOldFile_WhenRenameEnabled()
    {
        using var wd = new TempDir();
        var dataDir = System.IO.Path.Combine(wd.Path, "data");
        Directory.CreateDirectory(dataDir);
        var target = System.IO.Path.Combine(dataDir, "rename.log");
        File.WriteAllText(target, "payload");
        File.SetCreationTime(target, DateTime.Now.AddDays(-40));

        File.WriteAllText(System.IO.Path.Combine(wd.Path, "config.toml"), $$"""
LogFilePath = "{{System.IO.Path.Combine(wd.Path, "log.txt").Replace("\\", "/")}}"
LogLevel = "debug"
MaxLogSizeBytes = 1048576
EnableEventLog = false
EventLogLevel = "warn"

[[FolderSettings]]
Directory = "{{dataDir.Replace("\\", "/")}}"
DaysOld = 999
IncludePattern = "\\.log$"
ExcludePattern = ""
Recursive = false
EnableRename = true
RenameDaysOld = 1
RenameOnInUse = "warn"
EnableDelete = false
EnableZipCompression = false
CreateEmptyAfterRename = false
""");

        var r = RunApp(wd.Path);
        r.ExitCode.Should().Be(0);
        File.Exists(target).Should().BeFalse();
        Directory.GetFiles(dataDir, "rename_*.log").Should().HaveCount(1);
    }

    [Fact]
    public void Execution_DeletesOldFile_WhenDeleteEnabled()
    {
        using var wd = new TempDir();
        var dataDir = System.IO.Path.Combine(wd.Path, "data");
        Directory.CreateDirectory(dataDir);
        var target = System.IO.Path.Combine(dataDir, "delete.log");
        File.WriteAllText(target, "payload");
        File.SetCreationTime(target, DateTime.Now.AddDays(-40));

        File.WriteAllText(System.IO.Path.Combine(wd.Path, "config.toml"), $$"""
LogFilePath = "{{System.IO.Path.Combine(wd.Path, "log.txt").Replace("\\", "/")}}"
LogLevel = "debug"
MaxLogSizeBytes = 1048576
EnableEventLog = false
EventLogLevel = "warn"

[[FolderSettings]]
Directory = "{{dataDir.Replace("\\", "/")}}"
DaysOld = 999
IncludePattern = "\\.log$"
ExcludePattern = ""
Recursive = false
EnableRename = false
EnableDelete = true
DeleteDaysOld = 1
DeleteOnInUse = "warn"
EnableZipCompression = false
""");

        var r = RunApp(wd.Path);
        r.ExitCode.Should().Be(0);
        File.Exists(target).Should().BeFalse();
    }

    [Fact]
    public void CheckConfig_ReturnsErrors_ForInvalidConfiguration()
    {
        using var wd = new TempDir();
        File.WriteAllText(System.IO.Path.Combine(wd.Path, "config.toml"), """
LogFilePath = ""
LogLevel = "invalid"
MaxLogSizeBytes = 0

[[FolderSettings]]
Directory = ""
DaysOld = -1
IncludePattern = "["
ExcludePattern = "["
Recursive = false
EnableRename = true
RenameDaysOld = 0
RenameOnInUse = "invalid"
EnableDelete = true
DeleteDaysOld = 0
DeleteOnInUse = "invalid"
EnableZipCompression = true
DateComparisonToleranceMinutes = -1
""");

        var r = RunApp(wd.Path, "--check");
        r.Output.Should().Contain("[error]");
    }

    private static (int ExitCode, string Output) RunApp(string workingDir, params string[] args)
    {
        var projectPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "../../../../FileArchiver/FileArchiver.csproj"));
        var psi = new ProcessStartInfo("dotnet", $"run --project \"{projectPath}\" -- {string.Join(" ", args)}")
        {
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        var p = Process.Start(psi)!;
        var output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, output);
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"fa_test_{Guid.NewGuid():N}");
        public TempDir() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, true);
    }
}

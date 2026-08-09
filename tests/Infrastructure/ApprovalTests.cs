using System.Runtime.CompilerServices;
using Xunit;

namespace Serilog.Sinks.GrafanaLoki.Tests.Infrastructure;

internal static class ApprovalTests
{
    public static void Verify(
        string content,
        Func<string, string> scrubber,
        [CallerFilePath] string sourceFile = "",
        [CallerMemberName] string testMethod = "")
    {
        var dir = Path.GetDirectoryName(sourceFile)!;
        var testClass = Path.GetFileNameWithoutExtension(sourceFile);
        var approvedFile = Path.Combine(dir, $"{testClass}.{testMethod}.approved.txt");

        Assert.True(File.Exists(approvedFile), $"Approved file not found: {approvedFile}");

        var expected = File.ReadAllText(approvedFile).TrimEnd();
        var scrubbed = scrubber(content).TrimEnd();

        Assert.Equal(expected, scrubbed);
    }
}

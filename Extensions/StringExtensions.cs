using System.Runtime.CompilerServices;
using System.Text;

namespace osuCollectionDownloader.Extensions;

public static class StringExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReplaceInvalidPathChars(this string source)
    {
        return new StringBuilder(source)
            .Replace(":", "_")
            .Replace("?", string.Empty)
            .Replace("/", string.Empty)
            .Replace("\"", string.Empty)
            .Replace("<", string.Empty)
            .Replace(">", string.Empty)
            .Replace("|", string.Empty)
            .Replace("*", string.Empty)
            .Replace(".", string.Empty)
            .ToString();
    }
}

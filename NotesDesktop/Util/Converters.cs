using System.Linq;
using Avalonia.Data.Converters;

namespace skroy.NotesDesktop.Util;

public static class Converters
{
    public static readonly FuncMultiValueConverter<string, string> JoinLines =
        new(input => string.Join("\n", input.Where(p => !string.IsNullOrEmpty(p))));
}

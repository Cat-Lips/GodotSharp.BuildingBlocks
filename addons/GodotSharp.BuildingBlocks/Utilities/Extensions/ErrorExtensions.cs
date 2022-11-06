using Godot;
using Humanizer;

namespace GodotSharp.BuildingBlocks
{
    public static class ErrorExtensions
    {
        public static void ThrowOnError(this Error err, string title, object msg = null)
        {
            if (err is Error.Ok) return;
            var message = $"{title} ({err.Humanize(LetterCasing.Title)}): {msg}";
            GD.PrintErr(message);
            throw new Exception(message);
        }
    }
}

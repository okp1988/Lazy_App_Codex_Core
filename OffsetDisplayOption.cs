namespace Lazy_App_Codex_Core
{
    internal sealed record OffsetDisplayOption(string Value, string Label)
    {
        public override string ToString() => Label;

        public static readonly OffsetDisplayOption[] All =
        {
            new("0", "No offset"),
            new("-6:y", "Y: Up 6 steps"),
            new("-5:y", "Y: Up 5 steps"),
            new("-4:y", "Y: Up 4 steps"),
            new("-3:y", "Y: Up 3 steps"),
            new("-2:y", "Y: Up 2 steps"),
            new("-1:y", "Y: Up 1 step"),
            new("1:y", "Y: Down 1 step"),
            new("2:y", "Y: Down 2 steps"),
            new("3:y", "Y: Down 3 steps"),
            new("4:y", "Y: Down 4 steps"),
            new("5:y", "Y: Down 5 steps"),
            new("6:y", "Y: Down 6 steps"),
            new("-3:x", "X: Left 3 steps"),
            new("-2:x", "X: Left 2 steps"),
            new("-1:x", "X: Left 1 step"),
            new("1:x", "X: Right 1 step"),
            new("2:x", "X: Right 2 steps"),
            new("3:x", "X: Right 3 steps")
        };

        public static OffsetDisplayOption FromValue(string value)
        {
            return All.FirstOrDefault(option => option.Value.Equals(value, StringComparison.OrdinalIgnoreCase)) ?? All[0];
        }

        public static string ReadValue(object? selectedItem)
        {
            return selectedItem is OffsetDisplayOption option ? option.Value : selectedItem?.ToString() ?? "0";
        }
    }
}

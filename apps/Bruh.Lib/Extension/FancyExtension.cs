namespace Bruh.Lib.Extension;

public static class FancyExtension
{
    public static string ToFancyString(this string str)
    {
        return $"Fancy {str}";
    }
}
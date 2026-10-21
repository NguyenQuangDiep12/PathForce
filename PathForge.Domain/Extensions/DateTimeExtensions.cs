namespace PathForge.Domain.Extensions;

public static class DateTimeExtensions
{
    public static DateTime ToLocalTimeFromUtc(this DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();
        return utc.ToLocalTime();
    }
}

using System;
using System.Globalization;
using GameLocalization;

namespace Presenters.Shop
{
    /// <summary>Number, duration and key formatting shared by the shop cards.</summary>
    internal static class ShopText
    {
        public static string FormatNumber(int value)
        {
            // "1,000" reads wrong outside English; most other locales group with a space ("1 000").
            var format = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            if(!string.Equals(L10n.CurrentCode, "en", StringComparison.OrdinalIgnoreCase))
            {
                format.NumberGroupSeparator = " ";
            }

            return value.ToString("N0", format);
        }

        public static string FormatDuration(int minutes)
        {
            if(minutes >= 60 && minutes % 60 == 0)
            {
                return string.Format(L10n.Get("shop_duration_hours", "{0}h"), minutes / 60);
            }

            return string.Format(L10n.Get("shop_duration_minutes", "{0}m"), minutes);
        }

        public static string ToSnakeCase(string value)
        {
            var sb = new System.Text.StringBuilder(value.Length + 8);
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if(char.IsUpper(c) && i > 0)
                {
                    sb.Append('_');
                }

                sb.Append(char.ToLowerInvariant(c));
            }

            return sb.ToString();
        }
    }
}

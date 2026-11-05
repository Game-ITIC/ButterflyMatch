using System;
using System.Text;

public static class ConsoleGrep
{
    // Console entries containing `needle`; first line only unless fullFirst, max `max` entries.
    public static string Get(string needle, int max = 80, int lines = 1)
    {
        var sb = new StringBuilder();
        var logEntries = Type.GetType("UnityEditor.LogEntries, UnityEditor");
        var entryType = Type.GetType("UnityEditor.LogEntry, UnityEditor");
        int count = (int)logEntries.GetMethod("StartGettingEntries").Invoke(null, null);
        var entry = Activator.CreateInstance(entryType);
        var getEntry = logEntries.GetMethod("GetEntryInternal");
        var msgField = entryType.GetField("message");
        int shown = 0;
        try
        {
            for (int i = 0; i < count && shown < max; i++)
            {
                getEntry.Invoke(null, new object[] { i, entry });
                var m = (string)msgField.GetValue(entry);
                if (!m.Contains(needle))
                    continue;

                var parts = m.Split('\n');
                for (int l = 0; l < Math.Min(lines, parts.Length); l++)
                    sb.Append(l == 0 ? "" : "    ").Append(parts[l].Trim()).Append('\n');
                shown++;
            }
        }
        finally
        {
            logEntries.GetMethod("EndGettingEntries").Invoke(null, null);
        }

        return sb.ToString();
    }
}

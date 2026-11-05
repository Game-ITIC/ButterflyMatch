using System;
using System.Text;

public static class CompileErrors
{
    // Compiler errors (and optionally warnings for a path fragment) currently in the Editor console.
    public static string Get(string warningsFor = null)
    {
        var sb = new StringBuilder();
        sb.Append("compiling=").Append(UnityEditor.EditorApplication.isCompiling)
          .Append(" failed=").Append(UnityEditor.EditorUtility.scriptCompilationFailed).Append('\n');

        var logEntries = Type.GetType("UnityEditor.LogEntries, UnityEditor");
        var entryType = Type.GetType("UnityEditor.LogEntry, UnityEditor");
        int count = (int)logEntries.GetMethod("StartGettingEntries").Invoke(null, null);
        var entry = Activator.CreateInstance(entryType);
        var getEntry = logEntries.GetMethod("GetEntryInternal");
        var msgField = entryType.GetField("message");
        int shown = 0;
        try
        {
            for (int i = 0; i < count && shown < 60; i++)
            {
                getEntry.Invoke(null, new object[] { i, entry });
                var m = (string)msgField.GetValue(entry);
                bool err = m.Contains("error CS");
                bool warn = warningsFor != null && m.Contains("warning CS") && m.Contains(warningsFor);
                if (err || warn)
                {
                    sb.Append(m.Split('\n')[0]).Append('\n');
                    shown++;
                }
            }
        }
        finally
        {
            logEntries.GetMethod("EndGettingEntries").Invoke(null, null);
        }

        return sb.ToString();
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Immutable.Audience
{
    internal static class Json
    {
        internal static string Serialize(Dictionary<string, object> data)
        {
            var sb = new StringBuilder();
            WriteObject(sb, data, indent: 0, depth: 0);
            return sb.ToString();
        }

        // Pretty-prints `data` with `indent` spaces per nesting level.
        // Round-trips through Deserialize identically. Indent ≤ 0 returns
        // the compact form. Use for human-readable output; wire payloads
        // use the compact overload.
        internal static string Serialize(Dictionary<string, object> data, int indent)
        {
            if (indent <= 0) return Serialize(data);
            var sb = new StringBuilder();
            WriteObject(sb, data, indent, depth: 0);
            return sb.ToString();
        }

        private static void WriteValue(StringBuilder sb, object value, int indent, int depth)
        {
            if (value == null)
            {
                sb.Append("null");
            }
            else if (value is string s)
            {
                WriteString(sb, s);
            }
            else if (value is bool b)
            {
                sb.Append(b ? "true" : "false");
            }
            else if (value is int i)
            {
                sb.Append(i);
            }
            else if (value is long l)
            {
                sb.Append(l);
            }
            else if (value is float f)
            {
                if (float.IsNaN(f) || float.IsInfinity(f))
                    sb.Append("null");
                else
                    sb.Append(f.ToString("R", CultureInfo.InvariantCulture));
            }
            else if (value is double d)
            {
                if (double.IsNaN(d) || double.IsInfinity(d))
                    sb.Append("null");
                else
                    sb.Append(d.ToString("R", CultureInfo.InvariantCulture));
            }
            else if (value is decimal dec)
            {
                sb.Append(dec.ToString(CultureInfo.InvariantCulture));
            }
            else if (value is Dictionary<string, object> dict)
            {
                WriteObject(sb, dict, indent, depth);
            }
            else if (value is IList list)
            {
                WriteArray(sb, list, indent, depth);
            }
            else
            {
                WriteString(sb, value.ToString());
            }
        }

        private static void WriteObject(StringBuilder sb, Dictionary<string, object> dict, int indent, int depth)
        {
            sb.Append('{');
            if (dict.Count == 0) { sb.Append('}'); return; }

            var pretty = indent > 0;
            var first = true;
            foreach (var kvp in dict)
            {
                if (!first) sb.Append(',');
                first = false;
                if (pretty) AppendNewline(sb, indent, depth + 1);
                WriteString(sb, kvp.Key);
                sb.Append(pretty ? ": " : ":");
                WriteValue(sb, kvp.Value, indent, depth + 1);
            }
            if (pretty) AppendNewline(sb, indent, depth);
            sb.Append('}');
        }

        private static void WriteArray(StringBuilder sb, IList list, int indent, int depth)
        {
            sb.Append('[');
            if (list.Count == 0) { sb.Append(']'); return; }

            var pretty = indent > 0;
            for (var i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(',');
                if (pretty) AppendNewline(sb, indent, depth + 1);
                WriteValue(sb, list[i], indent, depth + 1);
            }
            if (pretty) AppendNewline(sb, indent, depth);
            sb.Append(']');
        }

        private static void AppendNewline(StringBuilder sb, int indent, int depth)
        {
            sb.Append('\n');
            sb.Append(' ', indent * depth);
        }

        private static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (var c in s)
            {
                switch (c)
                {
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        if (c < 0x20)
                            sb.Append("\\u").Append(((int)c).ToString("X4"));
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}

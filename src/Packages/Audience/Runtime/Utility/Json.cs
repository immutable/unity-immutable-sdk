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
            WriteObject(sb, data);
            return sb.ToString();
        }

        private static void WriteValue(StringBuilder sb, object value)
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
                var result = f.ToString("G", CultureInfo.InvariantCulture);
                if (result.IndexOf('E') >= 0 || result.IndexOf('e') >= 0)
                    result = f.ToString("F6", CultureInfo.InvariantCulture);
                sb.Append(result);
            }
            else if (value is double d)
            {
                var result = d.ToString("G", CultureInfo.InvariantCulture);
                if (result.IndexOf('E') >= 0 || result.IndexOf('e') >= 0)
                    result = d.ToString("F6", CultureInfo.InvariantCulture);
                sb.Append(result);
            }
            else if (value is decimal dec)
            {
                sb.Append(dec.ToString(CultureInfo.InvariantCulture));
            }
            else if (value is Dictionary<string, object> dict)
            {
                WriteObject(sb, dict);
            }
            else if (value is IList list)
            {
                WriteArray(sb, list);
            }
            else
            {
                WriteString(sb, value.ToString());
            }
        }

        private static void WriteObject(StringBuilder sb, Dictionary<string, object> dict)
        {
            sb.Append('{');
            var first = true;
            foreach (var kvp in dict)
            {
                if (!first)
                    sb.Append(',');
                first = false;
                WriteString(sb, kvp.Key);
                sb.Append(':');
                WriteValue(sb, kvp.Value);
            }
            sb.Append('}');
        }

        private static void WriteArray(StringBuilder sb, IList list)
        {
            sb.Append('[');
            for (var i = 0; i < list.Count; i++)
            {
                if (i > 0)
                    sb.Append(',');
                WriteValue(sb, list[i]);
            }
            sb.Append(']');
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
                            sb.AppendFormat("\\u{0:X4}", (int)c);
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}

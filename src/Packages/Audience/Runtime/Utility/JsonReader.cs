using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Immutable.Audience
{
    // Minimal JSON reader. Handles the subset produced by Json.Serialize:
    // objects, strings, numbers, booleans, null, arrays. Reflection-free so
    // IL2CPP-safe. Throws FormatException on malformed input.
    internal static class JsonReader
    {
        internal static Dictionary<string, object> DeserializeObject(string json)
        {
            var p = new Parser(json);
            p.SkipWhitespace();
            var result = p.ReadObject();
            p.SkipWhitespace();
            if (p.Pos != json.Length)
                throw new FormatException($"Trailing content at position {p.Pos}");
            return result;
        }

        private struct Parser
        {
            private readonly string _s;
            internal int Pos;

            internal Parser(string s) { _s = s; Pos = 0; }

            internal void SkipWhitespace()
            {
                while (Pos < _s.Length)
                {
                    var c = _s[Pos];
                    if (c == ' ' || c == '\t' || c == '\r' || c == '\n') Pos++;
                    else break;
                }
            }

            internal Dictionary<string, object> ReadObject()
            {
                Expect('{');
                var obj = new Dictionary<string, object>();
                SkipWhitespace();
                if (Peek() == '}') { Pos++; return obj; }

                while (true)
                {
                    SkipWhitespace();
                    var key = ReadString();
                    SkipWhitespace();
                    Expect(':');
                    SkipWhitespace();
                    obj[key] = ReadValue();
                    SkipWhitespace();
                    var next = Read();
                    if (next == ',') continue;
                    if (next == '}') return obj;
                    throw new FormatException($"Expected ',' or '}}' at position {Pos - 1}");
                }
            }

            private List<object> ReadArray()
            {
                Expect('[');
                var arr = new List<object>();
                SkipWhitespace();
                if (Peek() == ']') { Pos++; return arr; }

                while (true)
                {
                    SkipWhitespace();
                    arr.Add(ReadValue());
                    SkipWhitespace();
                    var next = Read();
                    if (next == ',') continue;
                    if (next == ']') return arr;
                    throw new FormatException($"Expected ',' or ']' at position {Pos - 1}");
                }
            }

            private object ReadValue()
            {
                SkipWhitespace();
                var c = Peek();
                if (c == '"') return ReadString();
                if (c == '{') return ReadObject();
                if (c == '[') return ReadArray();
                if (c == 't' || c == 'f') return ReadBool();
                if (c == 'n') { ReadLiteral("null"); return null; }
                return ReadNumber();
            }

            private string ReadString()
            {
                Expect('"');
                var sb = new StringBuilder();
                while (Pos < _s.Length)
                {
                    var c = _s[Pos++];
                    if (c == '"') return sb.ToString();
                    if (c == '\\')
                    {
                        if (Pos >= _s.Length) throw new FormatException("Unterminated escape");
                        var esc = _s[Pos++];
                        switch (esc)
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/': sb.Append('/'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'u':
                                if (Pos + 4 > _s.Length) throw new FormatException("Truncated \\u escape");
                                var hex = _s.Substring(Pos, 4);
                                Pos += 4;
                                sb.Append((char)int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                                break;
                            default: throw new FormatException($"Invalid escape \\{esc}");
                        }
                    }
                    else sb.Append(c);
                }
                throw new FormatException("Unterminated string");
            }

            private object ReadNumber()
            {
                var start = Pos;
                if (Peek() == '-') Pos++;
                while (Pos < _s.Length)
                {
                    var c = _s[Pos];
                    if ((c >= '0' && c <= '9') || c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-') Pos++;
                    else break;
                }
                var token = _s.Substring(start, Pos - start);
                if (token.IndexOfAny(new[] { '.', 'e', 'E' }) < 0
                    && long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
                {
                    if (l >= int.MinValue && l <= int.MaxValue) return (int)l;
                    return l;
                }
                if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                    return d;
                throw new FormatException($"Invalid number '{token}'");
            }

            private bool ReadBool()
            {
                if (Peek() == 't') { ReadLiteral("true"); return true; }
                ReadLiteral("false");
                return false;
            }

            private void ReadLiteral(string literal)
            {
                if (Pos + literal.Length > _s.Length || _s.Substring(Pos, literal.Length) != literal)
                    throw new FormatException($"Expected '{literal}' at position {Pos}");
                Pos += literal.Length;
            }

            private char Peek() =>
                Pos < _s.Length ? _s[Pos] : throw new FormatException("Unexpected end of input");

            private char Read() =>
                Pos < _s.Length ? _s[Pos++] : throw new FormatException("Unexpected end of input");

            private void Expect(char c)
            {
                if (Pos >= _s.Length || _s[Pos] != c)
                    throw new FormatException($"Expected '{c}' at position {Pos}");
                Pos++;
            }
        }
    }
}

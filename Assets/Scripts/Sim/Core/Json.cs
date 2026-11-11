using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NewGen.Sim.Core
{
    /// <summary>
    /// Tiny dependency-free JSON reader/writer so content files and saves work both inside
    /// Unity and in the plain .NET test harness. Values: JsonObject, List&lt;object&gt;, string,
    /// double, bool, null.
    /// </summary>
    public sealed class JsonObject : Dictionary<string, object>
    {
        public JsonObject() : base(StringComparer.Ordinal) { }

        public bool Has(string key) => ContainsKey(key);
        public string Str(string key, string def = null) => TryGetValue(key, out var v) && v is string s ? s : def;
        public int Int(string key, int def = 0) => TryGetValue(key, out var v) && v is double d ? (int)Math.Round(d) : def;
        public long Long(string key, long def = 0) => TryGetValue(key, out var v) && v is double d ? (long)Math.Round(d) : def;
        public double Num(string key, double def = 0) => TryGetValue(key, out var v) && v is double d ? d : def;
        public bool Bool(string key, bool def = false) => TryGetValue(key, out var v) && v is bool b ? b : def;
        public JsonObject Obj(string key) => TryGetValue(key, out var v) ? v as JsonObject : null;
        public List<object> Arr(string key) => TryGetValue(key, out var v) ? v as List<object> : null;

        public List<string> StrList(string key)
        {
            var r = new List<string>();
            var a = Arr(key); if (a == null) return r;
            foreach (var o in a) if (o is string s) r.Add(s);
            return r;
        }
        public List<JsonObject> ObjList(string key)
        {
            var r = new List<JsonObject>();
            var a = Arr(key); if (a == null) return r;
            foreach (var o in a) if (o is JsonObject j) r.Add(j);
            return r;
        }
    }

    public static class Json
    {
        public static object Parse(string text)
        {
            var p = new Parser(text);
            p.SkipWs();
            object v = p.ReadValue();
            p.SkipWs();
            if (p.pos != text.Length) throw p.Error("trailing characters");
            return v;
        }

        public static JsonObject ParseObject(string text)
        {
            return Parse(text) as JsonObject ?? throw new FormatException("JSON root is not an object");
        }

        public static string Write(object value, bool pretty = true)
        {
            var sb = new StringBuilder();
            WriteValue(sb, value, pretty, 0);
            return sb.ToString();
        }

        static void WriteValue(StringBuilder sb, object v, bool pretty, int depth)
        {
            switch (v)
            {
                case null: sb.Append("null"); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case string s: WriteString(sb, s); break;
                case double d: sb.Append(d.ToString("R", CultureInfo.InvariantCulture)); break;
                case float f: sb.Append(((double)f).ToString("R", CultureInfo.InvariantCulture)); break;
                case int i: sb.Append(i.ToString(CultureInfo.InvariantCulture)); break;
                case long l: sb.Append(l.ToString(CultureInfo.InvariantCulture)); break;
                case uint u: sb.Append(u.ToString(CultureInfo.InvariantCulture)); break;
                case byte by: sb.Append(by.ToString(CultureInfo.InvariantCulture)); break;
                case JsonObject o:
                    {
                        sb.Append('{');
                        bool first = true;
                        foreach (var kv in o)
                        {
                            if (!first) sb.Append(',');
                            first = false;
                            NewLine(sb, pretty, depth + 1);
                            WriteString(sb, kv.Key); sb.Append(pretty ? ": " : ":");
                            WriteValue(sb, kv.Value, pretty, depth + 1);
                        }
                        if (!first) NewLine(sb, pretty, depth);
                        sb.Append('}');
                        break;
                    }
                case System.Collections.IEnumerable list:
                    {
                        sb.Append('[');
                        bool first = true;
                        foreach (var item in list)
                        {
                            if (!first) sb.Append(',');
                            first = false;
                            NewLine(sb, pretty, depth + 1);
                            WriteValue(sb, item, pretty, depth + 1);
                        }
                        if (!first) NewLine(sb, pretty, depth);
                        sb.Append(']');
                        break;
                    }
                default: WriteString(sb, v.ToString()); break;
            }
        }

        static void NewLine(StringBuilder sb, bool pretty, int depth)
        {
            if (!pretty) return;
            sb.Append('\n'); sb.Append(' ', depth * 2);
        }

        static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        sealed class Parser
        {
            readonly string t; public int pos;
            public Parser(string text) { t = text; }

            public FormatException Error(string msg) => new FormatException($"JSON error at {pos}: {msg}");

            public void SkipWs()
            {
                while (pos < t.Length)
                {
                    char c = t[pos];
                    if (c == ' ' || c == '\t' || c == '\n' || c == '\r') pos++;
                    else if (c == '/' && pos + 1 < t.Length && t[pos + 1] == '/') { while (pos < t.Length && t[pos] != '\n') pos++; } // allow // comments in content files
                    else break;
                }
            }

            public object ReadValue()
            {
                if (pos >= t.Length) throw Error("unexpected end");
                char c = t[pos];
                if (c == '{') return ReadObject();
                if (c == '[') return ReadArray();
                if (c == '"') return ReadString();
                if (c == 't') { Expect("true"); return true; }
                if (c == 'f') { Expect("false"); return false; }
                if (c == 'n') { Expect("null"); return null; }
                return ReadNumber();
            }

            void Expect(string word)
            {
                if (string.CompareOrdinal(t, pos, word, 0, word.Length) != 0) throw Error("expected " + word);
                pos += word.Length;
            }

            JsonObject ReadObject()
            {
                var o = new JsonObject();
                pos++; SkipWs();
                if (pos < t.Length && t[pos] == '}') { pos++; return o; }
                while (true)
                {
                    SkipWs();
                    if (pos >= t.Length || t[pos] != '"') throw Error("expected key");
                    string key = ReadString();
                    SkipWs();
                    if (pos >= t.Length || t[pos] != ':') throw Error("expected ':'");
                    pos++; SkipWs();
                    o[key] = ReadValue();
                    SkipWs();
                    if (pos >= t.Length) throw Error("unterminated object");
                    if (t[pos] == ',') { pos++; continue; }
                    if (t[pos] == '}') { pos++; return o; }
                    throw Error("expected ',' or '}'");
                }
            }

            List<object> ReadArray()
            {
                var a = new List<object>();
                pos++; SkipWs();
                if (pos < t.Length && t[pos] == ']') { pos++; return a; }
                while (true)
                {
                    SkipWs();
                    a.Add(ReadValue());
                    SkipWs();
                    if (pos >= t.Length) throw Error("unterminated array");
                    if (t[pos] == ',') { pos++; continue; }
                    if (t[pos] == ']') { pos++; return a; }
                    throw Error("expected ',' or ']'");
                }
            }

            string ReadString()
            {
                pos++; // opening quote
                var sb = new StringBuilder();
                while (pos < t.Length)
                {
                    char c = t[pos++];
                    if (c == '"') return sb.ToString();
                    if (c != '\\') { sb.Append(c); continue; }
                    if (pos >= t.Length) break;
                    char e = t[pos++];
                    switch (e)
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
                            if (pos + 4 > t.Length) throw Error("bad \\u escape");
                            sb.Append((char)Convert.ToInt32(t.Substring(pos, 4), 16)); pos += 4; break;
                        default: throw Error("bad escape");
                    }
                }
                throw Error("unterminated string");
            }

            object ReadNumber()
            {
                int start = pos;
                while (pos < t.Length)
                {
                    char c = t[pos];
                    if ((c >= '0' && c <= '9') || c == '-' || c == '+' || c == '.' || c == 'e' || c == 'E') pos++;
                    else break;
                }
                if (start == pos) throw Error("unexpected character '" + t[pos] + "'");
                if (!double.TryParse(t.Substring(start, pos - start), NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                    throw Error("bad number");
                return d;
            }
        }
    }
}

using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Runtime.Serialization.Json;
using System.Text;

// A read-only, streaming projection of two UI fields. Unrelated global state is
// skipped; it is never deserialized into a settings dictionary or logged.
internal sealed class PetPlacement {
    public double X, Y;
    public RectangleF Display;
    public static PetPlacement Read(Stream stream) {
        if (stream.Length > 8388608) return null;
        // Microsoft's JSON XML reader tolerates a missing final delimiter. Check
        // syntax first without retaining any field names or values.
        if (!JsonSyntax.Valid(stream)) return null;
        stream.Position = 0;
        var quotas = new XmlDictionaryReaderQuotas { MaxDepth = 64, MaxStringContentLength = 8388608, MaxArrayLength = 8388608 };
        bool opened = false, seenOpen = false, seenBounds = false;
        PetPlacement placement = null;
        using (var reader = JsonReaderWriterFactory.CreateJsonReader(stream, quotas)) {
            reader.MoveToContent();
            if (reader.LocalName != "root" || reader.GetAttribute("type") != "object") return null;
            reader.Read();
            while (reader.NodeType != XmlNodeType.EndElement && !reader.EOF) {
                if (reader.NodeType != XmlNodeType.Element || reader.Depth != 1) return null;
                if (reader.LocalName == "electron-avatar-overlay-open") {
                    if (seenOpen) return null;
                    seenOpen = true;
                    var field = (XElement)XNode.ReadFrom(reader);
                    opened = (string)field.Attribute("type") == "boolean" && field.Value == "true";
                } else if (reader.LocalName == "electron-avatar-overlay-bounds") {
                    if (seenBounds) return null;
                    seenBounds = true;
                    var field = (XElement)XNode.ReadFrom(reader);
                    if ((string)field.Attribute("type") != "object") return null;
                    var display = field.Element("displayBounds");
                    if (display == null || (string)display.Attribute("type") != "object") return null;
                    double x = Number(field, "x"), y = Number(field, "y");
                    double dx = Number(display, "x"), dy = Number(display, "y"), w = Number(display, "width"), h = Number(display, "height");
                    if (w < 320 || h < 200 || w > 16384 || h > 16384 || Math.Abs(dx) > 65536 || Math.Abs(dy) > 65536 || x < dx || y < dy || x > dx + w || y > dy + h) return null;
                    placement = new PetPlacement { X = x, Y = y, Display = new RectangleF((float)dx, (float)dy, (float)w, (float)h) };
                } else reader.Skip();
            }
            reader.Read();
            if (!reader.EOF) return null;
        }
        return opened ? placement : null;
    }
    static double Number(XElement element, string name) {
        var field = element.Element(name);
        double value;
        if (element.Elements(name).Count() != 1 || field == null || (string)field.Attribute("type") != "number" || !Double.TryParse(field.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || Double.IsInfinity(value) || Double.IsNaN(value)) throw new FormatException("Invalid pet placement");
        return value;
    }
}

internal sealed class JsonSyntax {
    readonly TextReader input;
    readonly char[] buffer = new char[8192];
    int at, count;
    JsonSyntax(TextReader reader) { input = reader; }
    int Peek() { if (at == count) { count = input.Read(buffer, 0, buffer.Length); at = 0; } return count == 0 ? -1 : buffer[at]; }
    int Read() { int c = Peek(); if (c >= 0) at++; return c; }
    public static bool Valid(Stream stream) {
        using (var reader = new StreamReader(stream, new UTF8Encoding(false, true), true, 1024, true)) {
            var parser = new JsonSyntax(reader);
            return parser.Value(0) && parser.Space() == -1;
        }
    }
    int Space() { int c; while ((c = Peek()) == 32 || c == 9 || c == 10 || c == 13) Read(); return c; }
    bool Value(int depth) {
        if (depth > 64) return false;
        int c = Space();
        if (c == '"') return String();
        if (c == '{' || c == '[') {
            Read(); bool obj = c == '{'; int end = obj ? '}' : ']';
            if (Space() == end) { Read(); return true; }
            while (true) {
                if (obj && (!String() || Space() != ':' || Read() != ':')) return false;
                if (!Value(depth + 1)) return false;
                c = Space(); Read();
                if (c == end) return true;
                if (c != ',') return false;
                Space();
            }
        }
        if (c == 't') return Literal("true");
        if (c == 'f') return Literal("false");
        if (c == 'n') return Literal("null");
        if (c == '-') { Read(); c = Peek(); }
        if (c == '0') Read();
        else if (c >= '1' && c <= '9') { do { Read(); c = Peek(); } while (c >= '0' && c <= '9'); }
        else return false;
        if (Peek() == '.') { Read(); if (!Digits()) return false; }
        if (Peek() == 'e' || Peek() == 'E') { Read(); if (Peek() == '+' || Peek() == '-') Read(); if (!Digits()) return false; }
        return true;
    }
    bool Digits() { int count = 0, c; while ((c = Peek()) >= '0' && c <= '9') { Read(); count++; } return count > 0; }
    bool Literal(string token) { foreach (char c in token) if (Read() != c) return false; return true; }
    bool String() {
        if (Read() != '"') return false;
        int c;
        while ((c = Read()) >= 32) {
            if (c == '"') return true;
            if (c != '\\') continue;
            c = Read();
            if (c == 'u') { for (int n = 0; n < 4; n++) { c = Read(); if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))) return false; } }
            else if (c != '"' && c != '\\' && c != '/' && c != 'b' && c != 'f' && c != 'n' && c != 'r' && c != 't') return false;
        }
        return false;
    }
}

internal static class PetGeometry {
    // Codex 26.930 saves a 112x121 logical anchor; its 80x87 native drawing
    // is centered inside it, with a +16,+17 DIP offset. The adapter
    // fails closed if its UI schema or window identity changes.
    public static Rectangle Map(PetPlacement p, Rectangle monitor, double scale) {
        if (scale < 0.75 || scale > 4 || Math.Abs(monitor.Width / scale - p.Display.Width) > 3 || Math.Abs(monitor.Height / scale - p.Display.Height) > 3) return Rectangle.Empty;
        return new Rectangle(monitor.Left + (int)Math.Round((p.X - p.Display.Left + 16) * scale), monitor.Top + (int)Math.Round((p.Y - p.Display.Top + 17) * scale), (int)Math.Ceiling(80 * scale), (int)Math.Ceiling(87 * scale));
    }
    public static Rectangle Card(Rectangle pet, Rectangle work, double scale) {
        int w = (int)Math.Round(48 * scale), h = w, gap = (int)Math.Round(8 * scale);
        int x = Math.Max(work.Left, Math.Min(work.Right - w, pet.Left + (pet.Width - w) / 2));
        int y = pet.Top - h - gap;
        if (y < work.Top) y = pet.Bottom + gap;
        y = Math.Max(work.Top, Math.Min(work.Bottom - h, y));
        var result = new Rectangle(x, y, w, h);
        return result.IntersectsWith(pet) ? Rectangle.Empty : result;
    }
    public static bool Hold(Point cursor, Rectangle pet, Rectangle card, bool visible, long now, ref long lastHover) {
        if (pet.Contains(cursor) || (visible && card.Contains(cursor))) { lastHover = now; return true; }
        return visible && now - lastHover <= 650;
    }
    public static bool TrustedPath(string path) {
        if (String.IsNullOrEmpty(path)) return false;
        string prefix = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps") + Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
        string relative = path.Substring(prefix.Length);
        var pieces = relative.Split(Path.DirectorySeparatorChar);
        return pieces.Length == 3 && pieces[0].StartsWith("OpenAI.Codex_26.930.", StringComparison.Ordinal) && pieces[0].EndsWith("_x64__2p2nqsd0c76g0", StringComparison.Ordinal) && pieces[1] == "app" && pieces[2] == "ChatGPT.exe";
    }
}

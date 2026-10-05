using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
internal sealed class PetAnchor {internal Rectangle Pet,Work;internal double Scale;}
internal sealed class PetAdapter {
 readonly string statePath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".codex",".codex-global-state.json");
 readonly Dictionary<int,bool> identities=new Dictionary<int,bool>();
 PetPlacement placement;DateTime observedWrite;internal string status="unavailable";
 internal PetAnchor Find(){try{Rectangle work;double scale;var pet=FindPet(out work,out scale);return pet.IsEmpty?null:new PetAnchor{Pet=pet,Work=work,Scale=scale};}catch{status="unavailable";return null;}}
    void ReadPlacement() {
        try {
            var file = new FileInfo(statePath);
            if (!file.Exists) { placement = null; return; }
            if (file.LastWriteTimeUtc == observedWrite) return;
            observedWrite = file.LastWriteTimeUtc;
            using (var stream = new FileStream(statePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) placement = PetPlacement.Read(stream);
        } catch { placement = null; observedWrite = DateTime.MinValue; }
    }
    Rectangle FindPet(out Rectangle work, out double scale) {
        work = Rectangle.Empty; scale = 1;
        ReadPlacement();
        if (placement == null) { status = "pet-closed-or-placement-unavailable"; return Rectangle.Empty; }
        var candidates = new List<IntPtr>();
        identities.Clear();
        PetNative.EnumWindows((window, ignored) => {
            if (!PetNative.IsWindowVisible(window) || PetNative.IsIconic(window)) return true;
            long style = PetNative.GetWindowLongPtr(window, -16).ToInt64(), extended = PetNative.GetWindowLongPtr(window, -20).ToInt64();
            if ((style & 0x00C00000) != 0 || (extended & 8) == 0) return true; // no caption, topmost
            var name = new StringBuilder(128); PetNative.GetClassName(window, name, name.Capacity);
            if (name.ToString() != "Chrome_WidgetWin_1") return true;
            int pid; PetNative.GetWindowThreadProcessId(window, out pid);
            bool trusted;
            if (!identities.TryGetValue(pid, out trusted)) {
                trusted = PetGeometry.TrustedPath(PetNative.ProcessPath(pid));
                identities[pid] = trusted;
            }
            if (trusted) candidates.Add(window);
            return true;
        }, IntPtr.Zero);
        // Ambiguity hides the widget instead of choosing a different Codex surface.
        if (candidates.Count != 1) { status = candidates.Count == 0 ? "pet-window-unavailable" : "ambiguous-pet-window"; return Rectangle.Empty; }
        IntPtr target = candidates[0];
        uint dpi = PetNative.GetDpiForWindow(target); scale = dpi / 96.0;
        var monitor = PetNative.MonitorFromWindow(target, 2);
        var info = new PetNative.MonitorInfo { size = Marshal.SizeOf(typeof(PetNative.MonitorInfo)) };
        if (!PetNative.GetMonitorInfo(monitor, ref info)) { status = "monitor-unavailable"; return Rectangle.Empty; }
        var pet = PetGeometry.Map(placement, info.monitor.Rectangle, scale);
        PetNative.Rect windowBounds;
        if (pet.IsEmpty || !PetNative.GetWindowRect(target, out windowBounds) || !windowBounds.Rectangle.Contains(pet)) { status = "placement-does-not-match-window"; return Rectangle.Empty; }
        work = info.work.Rectangle; status = "attached"; return pet;
    }
}
internal static class PetNative {
    internal static string ProcessPath(int pid) {
        var process = OpenProcess(0x1000, false, pid); // limited read-only query
        if (process == IntPtr.Zero) return null;
        try { var value = new StringBuilder(1024); int length = value.Capacity; return QueryFullProcessImageName(process, 0, value, ref length) ? value.ToString() : null; }
        finally { CloseHandle(process); }
    }
    [DllImport("kernel32.dll")] static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern bool QueryFullProcessImageName(IntPtr process, int flags, StringBuilder value, ref int length);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
    internal delegate bool WindowProc(IntPtr window, IntPtr parameter);
    [StructLayout(LayoutKind.Sequential)] internal struct Rect { public int left, top, right, bottom; public Rectangle Rectangle { get { return Rectangle.FromLTRB(left, top, right, bottom); } } }
    [StructLayout(LayoutKind.Sequential)] internal struct MonitorInfo { public int size; public Rect monitor, work; public uint flags; }
    [DllImport("user32.dll")] internal static extern bool SetProcessDpiAwarenessContext(IntPtr value);
    [DllImport("user32.dll")] internal static extern bool EnumWindows(WindowProc callback, IntPtr parameter);
    [DllImport("user32.dll")] internal static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] internal static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] internal static extern IntPtr GetWindowLongPtr(IntPtr window, int index);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern int GetClassName(IntPtr window, StringBuilder text, int length);
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr window, out int pid);
    [DllImport("user32.dll")] internal static extern uint GetDpiForWindow(IntPtr window);
    [DllImport("user32.dll")] internal static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] internal static extern bool GetWindowRect(IntPtr window, out Rect bounds);
    [DllImport("user32.dll")] internal static extern bool ShowWindow(IntPtr window, int command);
    [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] internal static extern IntPtr WindowFromPoint(Point point);
}

internal sealed class AttachedLayout {
    internal Rectangle Bounds;
    internal Rectangle[] Buttons = new Rectangle[7]; // screen coordinates
    internal double Scale;
    internal static AttachedLayout Create(Rectangle pet, Rectangle work, double scale) {
        var result = new AttachedLayout { Scale = scale };
        if (pet.IsEmpty || work.IsEmpty || scale < .75 || scale > 4) return result;
        int size = (int)Math.Round(36 * scale), side = (int)Math.Round(32 * scale);
        int gap = (int)Math.Round(6 * scale), step = size + gap;
        int vertical = (int)Math.Round(4 * scale), pad = (int)Math.Ceiling(6 * scale);
        int rowWidth = size * 3 + gap * 2, corner = size + gap;
        int columnHeight = side * 3 + vertical * 2;
        int width = corner + rowWidth, height = size + vertical + columnHeight;
        if (width + 2 * pad > work.Width || height + 2 * pad > work.Height) return result;
        int preferred = pet.Left + pet.Width / 2 - rowWidth / 2 - corner;
        int left = Math.Max(work.Left + pad, Math.Min(work.Right - pad - width, preferred));
        bool right = left + side + pad > pet.Left;
        int column = right ? left + width - (size + side) / 2 : left + (size - side) / 2;
        bool below = pet.Top - size - vertical - pad < work.Top;
        // Center three project targets beside the existing pet.
        int columnTop = pet.Top + (pet.Height - columnHeight) / 2;
        columnTop = Math.Max(work.Top + pad + (below ? 0 : size + vertical), Math.Min(work.Bottom - pad - columnHeight - (below ? size + vertical : 0), columnTop));
        int row = below ? columnTop + columnHeight + vertical : columnTop - size - vertical;
        int top = below ? columnTop : row;
        // Claude fills the elbow. Only Parley × Promtly has a cross.
        int rowStart = right ? left : left + corner;
        result.Buttons[0] = new Rectangle(rowStart, row, size, size);
        result.Buttons[1] = new Rectangle(rowStart + step, row, size, size);
        result.Buttons[2] = new Rectangle(rowStart + 2 * step, row, size, size);
        for (int n = 0; n < 3; n++) result.Buttons[n + 4] = new Rectangle(column, columnTop + n * (side + vertical), side, side);
        result.Buttons[3] = new Rectangle(right ? left + width - size : left, row, size, size);
        foreach (var button in result.Buttons) if (!work.Contains(button) || button.IntersectsWith(pet)) return new AttachedLayout { Scale = scale };
        result.Bounds = new Rectangle(left - pad, top - pad, width + pad * 2, height + pad * 2);
        return result;
    }
    internal int Hit(Point screen) {
        if (Bounds.IsEmpty) return -1;
        for (int n = 0; n < Buttons.Length; n++) {
            var r = Buttons[n]; if (!r.Contains(screen)) continue;
            // Rounded corners and transparent padding never trigger a launch.
            using (var shape = DockRenderer.Round(r, (float)(10 * Scale))) if (shape.IsVisible(screen)) return n;
        }
        return -1;
    }
}


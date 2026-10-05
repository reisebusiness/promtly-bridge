using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

internal static class DockRenderer {
    internal static GraphicsPath Round(RectangleF r,float radius){var p=new GraphicsPath();float d=Math.Min(radius*2,Math.Min(r.Width,r.Height));p.AddArc(r.Left,r.Top,d,d,180,90);p.AddArc(r.Right-d,r.Top,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.Left,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
}
internal static class PublicNative {
    [DllImport("user32.dll")]internal static extern bool ShowWindow(IntPtr window,int cmd);
    [DllImport("user32.dll")]internal static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")]static extern bool SetProcessDpiAwarenessContext(IntPtr context);
    [DllImport("user32.dll")]static extern IntPtr MonitorFromPoint(Point point,uint flags);
    [DllImport("shcore.dll")]static extern int GetDpiForMonitor(IntPtr monitor,int type,out uint x,out uint y);
    internal static void EnableDpi(){try{SetProcessDpiAwarenessContext(new IntPtr(-4));}catch(EntryPointNotFoundException){}}
    internal static double Scale(Point point){try{uint x,y;if(GetDpiForMonitor(MonitorFromPoint(point,2),0,out x,out y)==0)return x/96.0;}catch(DllNotFoundException){}return 1;}
}

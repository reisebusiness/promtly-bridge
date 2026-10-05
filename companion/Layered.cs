using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
internal sealed class DockLabel : System.Windows.Forms.Form {
    string shownText,shownTitle,shownDetail;Rectangle shownButton,shownPet,shownDock;double shownScale;bool shownSide,hot;
    internal int Target=-1;int pressed=-1;
    internal event Action<int> LaunchRequested;
    internal bool IsShown {get{return IsHandleCreated&&PublicNative.IsWindowVisible(Handle);}}
    internal DockLabel() {
        FormBorderStyle=System.Windows.Forms.FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;AutoScaleMode=System.Windows.Forms.AutoScaleMode.None;
        Cursor=System.Windows.Forms.Cursors.Hand;AccessibleRole=System.Windows.Forms.AccessibleRole.PushButton;
        MouseEnter+=(s,e)=>{hot=true;RefreshArt();};MouseLeave+=(s,e)=>{hot=false;RefreshArt();};
        MouseDown+=(s,e)=>{if(e.Button==System.Windows.Forms.MouseButtons.Left){PointerDown(PointToScreen(e.Location));Capture=pressed>=0;RefreshArt();}};
        MouseUp+=(s,e)=>{if(e.Button==System.Windows.Forms.MouseButtons.Left){PointerUp(PointToScreen(e.Location));Capture=false;RefreshArt();}};
        MouseCaptureChanged+=(s,e)=>{if(!Capture){pressed=-1;RefreshArt();}};
    }
    protected override bool ShowWithoutActivation {get{return true;}}
    protected override System.Windows.Forms.CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle|=0x08080080;return p;}}
    protected override void WndProc(ref System.Windows.Forms.Message message) {if(message.Msg==0x21){message.Result=new IntPtr(3);return;}base.WndProc(ref message);}
    protected override void SetVisibleCore(bool visible) {base.SetVisibleCore(false);if(!IsHandleCreated)CreateHandle();}
    internal void Conceal() {shownText=null;Target=pressed=-1;hot=false;Capture=false;if(IsHandleCreated)PublicNative.ShowWindow(Handle,0);}
    internal bool Hit(Point screen) {
        if(!IsShown||!Bounds.Contains(screen))return false;
        using(var shape=DockRenderer.Round(new RectangleF(1,1,Width-2,Height-2),(float)(10*shownScale)))return shape.IsVisible(screen.X-Left,screen.Y-Top);
    }
    internal void PointerDown(Point screen) {pressed=Hit(screen)?Target:-1;}
    internal void PointerUp(Point screen) {int target=pressed;pressed=-1;if(target>=0&&target==Target&&Hit(screen)&&LaunchRequested!=null)LaunchRequested(target);}
    void RefreshArt() {if(shownText==null)return;string title=shownTitle,detail=shownDetail;shownText=null;Present(title,detail,shownButton,shownScale,shownSide,Target,shownPet,shownDock);}
    protected override System.Windows.Forms.AccessibleObject CreateAccessibilityInstance() {return new LabelAccessible(this);}
    sealed class LabelAccessible : System.Windows.Forms.Control.ControlAccessibleObject {
        readonly DockLabel owner;internal LabelAccessible(DockLabel value):base(value){owner=value;}
        public override string DefaultAction {get{return "Open";}}
        public override void DoDefaultAction() {if(owner.IsShown&&owner.Target>=0&&owner.LaunchRequested!=null)owner.LaunchRequested(owner.Target);}
    }
    internal static Rectangle Place(Rectangle button,Rectangle work,int width,int height,bool side) {
        int x=side?button.Left-width-8:button.Left+(button.Width-width)/2;
        int y=side?button.Top+(button.Height-height)/2:button.Top-height-8;
        if(x<work.Left+4)x=button.Right+8;
        if(y<work.Top+4)y=button.Bottom+8;
        return new Rectangle(Math.Max(work.Left+4,Math.Min(work.Right-width-4,x)),Math.Max(work.Top+4,Math.Min(work.Bottom-height-4,y)),width,height);
    }
    internal static Rectangle PlaceSafe(Rectangle button,Rectangle work,int width,int height,bool side,Rectangle pet,Rectangle dock) {
        var preferred=Place(button,work,width,height,side);
        var candidates=new[]{preferred,new Rectangle(dock.Left-width-8,button.Top+(button.Height-height)/2,width,height),new Rectangle(dock.Right+8,button.Top+(button.Height-height)/2,width,height),new Rectangle(button.Left+(button.Width-width)/2,dock.Top-height-8,width,height),new Rectangle(button.Left+(button.Width-width)/2,dock.Bottom+8,width,height)};
        foreach(var raw in candidates) {
            var r=new Rectangle(Math.Max(work.Left+4,Math.Min(work.Right-width-4,raw.X)),Math.Max(work.Top+4,Math.Min(work.Bottom-height-4,raw.Y)),width,height);
            if(work.Contains(r)&&!r.IntersectsWith(pet)&&!r.IntersectsWith(dock))return r;
        }
        return Rectangle.Empty;
    }
    internal void Present(string title,string detail,Rectangle button,double scale,bool side,int target=-1,Rectangle pet=default(Rectangle),Rectangle dock=default(Rectangle)) {
        string text=title+"\n"+detail;
        if(shownText==text&&shownButton==button&&shownScale==scale&&Target==target&&shownPet==pet&&shownDock==dock)return;
        if(Target!=target)pressed=-1;
        shownText=text;shownTitle=title;shownDetail=detail;shownButton=button;shownScale=scale;shownSide=side;shownPet=pet;shownDock=dock;Target=target;AccessibleName="Open "+title;
        using(var font=new Font("Segoe UI",(float)(9*scale),FontStyle.Bold,GraphicsUnit.Point))
        using(var small=new Font("Segoe UI",(float)(7.5*scale),FontStyle.Regular,GraphicsUnit.Point)) {
            int width=(int)Math.Ceiling(Math.Max(132*scale,Math.Max(System.Windows.Forms.TextRenderer.MeasureText(title,font).Width,System.Windows.Forms.TextRenderer.MeasureText(detail,small).Width)+24*scale));
            int height=(int)Math.Ceiling(48*scale);
            var bounds=PlaceSafe(button,System.Windows.Forms.Screen.FromRectangle(button).WorkingArea,width,height,side,pet,dock);
            if(bounds.IsEmpty){Conceal();return;}
            using(var bitmap=new Bitmap(width,height,PixelFormat.Format32bppPArgb))
            using(var g=Graphics.FromImage(bitmap)) {
                g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;
                g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                using(var p=DockRenderer.Round(new RectangleF(1,1,width-2,height-2),(float)(10*scale)))
                using(var fill=new SolidBrush(pressed>=0?Color.FromArgb(250,22,45,43):hot?Color.FromArgb(250,27,36,43):Color.FromArgb(244,17,21,29)))
                using(var border=new Pen(hot?Color.FromArgb(190,126,224,209):Color.FromArgb(85,152,167,190))) {g.FillPath(fill,p);g.DrawPath(border,p);}
                using(var ink=new SolidBrush(Color.FromArgb(246,247,251)))g.DrawString(title,font,ink,(float)(11*scale),(float)(6*scale));
                using(var ink=new SolidBrush(Color.FromArgb(170,185,204)))g.DrawString(detail,small,ink,(float)(11*scale),(float)(24*scale));
                Bounds=bounds;LayeredDock.Update(Handle,bounds,bitmap);PublicNative.ShowWindow(Handle,4);
            }
        }
    }
}

internal static class LayeredDock {
    [StructLayout(LayoutKind.Sequential)] struct PointNative { public int x,y; public PointNative(int a,int b) {x=a;y=b;} }
    [StructLayout(LayoutKind.Sequential)] struct SizeNative { public int width,height; }
    [StructLayout(LayoutKind.Sequential, Pack=1)] struct Blend { public byte operation,flags,alpha,format; }
    [DllImport("user32.dll", SetLastError=true)] static extern bool UpdateLayeredWindow(IntPtr hwnd,IntPtr dc,ref PointNative position,ref SizeNative size,IntPtr source,ref PointNative origin,uint color,ref Blend blend,uint flags);
    [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hwnd,IntPtr dc);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc,IntPtr image);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr image);
    [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
    internal static void Update(IntPtr handle, Rectangle bounds, Bitmap bitmap) {
        IntPtr screen=GetDC(IntPtr.Zero), memory=CreateCompatibleDC(screen), image=IntPtr.Zero, prior=IntPtr.Zero;
        try {
            image=bitmap.GetHbitmap(Color.FromArgb(0)); prior=SelectObject(memory,image);
            var position=new PointNative(bounds.Left,bounds.Top); var origin=new PointNative(0,0);
            var size=new SizeNative {width=bitmap.Width,height=bitmap.Height}; var blend=new Blend {alpha=255,format=1};
            if(!UpdateLayeredWindow(handle,screen,ref position,ref size,memory,ref origin,0,ref blend,2)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        } finally { if(prior!=IntPtr.Zero)SelectObject(memory,prior); if(image!=IntPtr.Zero)DeleteObject(image); if(memory!=IntPtr.Zero)DeleteDC(memory); if(screen!=IntPtr.Zero)ReleaseDC(IntPtr.Zero,screen); }
    }
}

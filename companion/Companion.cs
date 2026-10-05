using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

internal sealed class Slot {
    public string name, kind, target, icon;
}
internal sealed class Preferences {
    public int schema=1, x=int.MinValue, y=int.MinValue;
    public string petMode="auto";
    public bool motion=true;
    public Slot[] slots;
    internal static readonly string Home=Environment.GetEnvironmentVariable("PROMTLY_DOCK_HOME")??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"PromtlyDock");
    internal static readonly string PathName=Path.Combine(Home,"settings.json");
    internal static Preferences Defaults() {
        return new Preferences {slots=new[]{
            new Slot{name="Hugin",kind="https",target="https://hugin.studio",icon="hugin.png"},
            new Slot{name="Parley",kind="parley",target="https://hugin.studio/parley",icon="parley.png"},
            new Slot{name="Promtly",kind="promtly",target="https://promtly.dev",icon="promtly.png"},
            new Slot{name="Claude",kind="claude",target="https://claude.ai",icon="assistant.png"},
            new Slot{name="AEVUM",kind="https",target="https://aevumresearch.com/play",icon="aevum.png"},
            new Slot{name="Your project",kind="empty",target="",icon=""},
            new Slot{name="Folders",kind="folders",target="",icon="explorer.png"}}};
    }
    internal static Preferences Hayden(){var value=Defaults();value.slots[5]=new Slot{name="Cashflo",kind="https",target="https://cashflo.org/",icon="cashflow.png"};return value;}
    internal static void Validate(Preferences value) {
        if(value==null||value.schema!=1||value.slots==null||value.slots.Length!=7)throw new InvalidDataException("Settings must contain seven slots");
        if(value.petMode!="auto"&&value.petMode!="grove")throw new InvalidDataException("Choose Automatic or Grove pet mode");
        foreach(var s in value.slots) {
            if(s==null||String.IsNullOrWhiteSpace(s.name)||s.name.Length>36||Regex.IsMatch(s.name,@"[\x00-\x1f\x7f]"))throw new InvalidDataException("Use a short slot name");
            if(s.target==null||s.target.Length>2048||Regex.IsMatch(s.target,@"[\x00-\x1f\x7f]"))throw new InvalidDataException("Invalid target");
            if(!new List<string>{"empty","https","shortcut","folder","folders","parley","promtly","claude"}.Contains(s.kind))throw new InvalidDataException("Unsupported target kind");
            if(s.kind=="https"||s.kind=="parley"||s.kind=="promtly"||s.kind=="claude") {
                Uri url;if(!Uri.TryCreate(s.target,UriKind.Absolute,out url)||url.Scheme!="https"||String.IsNullOrEmpty(url.Host)||!String.IsNullOrEmpty(url.UserInfo)||!String.IsNullOrEmpty(url.Fragment))throw new InvalidDataException("Use an HTTPS address without credentials or fragments");
            }
            if(s.kind=="shortcut"||s.kind=="folder") {
                if(s.target.IndexOfAny(System.IO.Path.GetInvalidPathChars())>=0||!Regex.IsMatch(s.target,@"^[A-Za-z]:[\\/]")||s.target.StartsWith(@"\\"))throw new InvalidDataException("Choose an absolute local path");
                if(s.kind=="shortcut"&&!new List<string>{".exe",".lnk"}.Contains(System.IO.Path.GetExtension(s.target).ToLowerInvariant()))throw new InvalidDataException("Choose an executable or Windows shortcut");
            }
            if(!String.IsNullOrEmpty(s.icon)&&(!Regex.IsMatch(s.icon,@"\A[A-Za-z0-9_-]+\.png\z")||s.icon.Length>80))throw new InvalidDataException("Icons must be PNG files from the assets folder");
        }
    }
    internal static Preferences Load() {
        if(!File.Exists(PathName))return Defaults();
        var file=new FileInfo(PathName);if(file.Length>16384)throw new InvalidDataException("Settings exceed 16 KiB");
        var value=new JavaScriptSerializer{MaxJsonLength=16384}.Deserialize<Preferences>(File.ReadAllText(PathName));Validate(value);return value;
    }
    internal void Save() {
        Validate(this);Directory.CreateDirectory(Home);string temp=PathName+"."+Guid.NewGuid().ToString("N")+".tmp";
        File.WriteAllText(temp,new JavaScriptSerializer().Serialize(this),new UTF8Encoding(false));
        if(File.Exists(PathName))File.Replace(temp,PathName,PathName+".previous");else File.Move(temp,PathName);
    }
}

internal sealed class Geometry {
    internal Rectangle Bounds, Pet;
    internal Rectangle[] Buttons=new Rectangle[7];
    internal double Scale;
    internal static Geometry Around(PetAnchor anchor){var dock=AttachedLayout.Create(anchor.Pet,anchor.Work,anchor.Scale);return new Geometry{Bounds=dock.Bounds,Buttons=dock.Buttons,Pet=anchor.Pet,Scale=anchor.Scale};}
    internal static Geometry At(Point petOrigin,Rectangle work,double scale) {
        var result=new Geometry{Scale=scale};
        if(scale<.75||scale>4||work.Width<240*scale||work.Height<240*scale)return result;
        int size=(int)Math.Round(36*scale), side=(int)Math.Round(32*scale),gap=(int)Math.Round(6*scale),v=(int)Math.Round(4*scale),pad=(int)Math.Ceiling(6*scale);
        int pw=(int)Math.Round(90*scale),ph=(int)Math.Round(116*scale),w=side+gap+3*size+2*gap,h=size+v+ph;
        int left=Math.Max(work.Left+pad,Math.Min(work.Right-w-pad,petOrigin.X-(w-pw)/2));
        int top=Math.Max(work.Top+pad,Math.Min(work.Bottom-h-pad,petOrigin.Y-size-v));
        bool mirror=petOrigin.X<work.Left+work.Width/2;
        int column=mirror?left+w-side:left,rowStart=mirror?left:left+side+gap;
        result.Buttons[3]=new Rectangle(column,top,size,size);
        if(mirror)result.Buttons[3].X=left+w-size;
        for(int i=0;i<3;i++)result.Buttons[i]=new Rectangle(rowStart+i*(size+gap),top,size,size);
        int sideTop=top+size+v+(ph-3*side-2*v)/2;
        for(int i=0;i<3;i++)result.Buttons[i+4]=new Rectangle(column,sideTop+i*(side+v),side,side);
        result.Pet=new Rectangle(mirror?left+14:left+w-pw-14,top+size+v,pw,ph);
        result.Bounds=Rectangle.Inflate(new Rectangle(left,top,w,h),pad,pad);
        foreach(var b in result.Buttons)if(!work.Contains(b)||b.IntersectsWith(result.Pet))throw new InvalidOperationException("Dock overlaps pet");
        return result;
    }
    internal int Hit(Point point) {
        for(int i=0;i<7;i++)if(Buttons[i].Contains(point))using(var p=DockRenderer.Round(Buttons[i],(float)(9*Scale)))if(p.IsVisible(point))return i;
        return -1;
    }
}

internal sealed class Motion {
    internal int Hover=-1,Pressed=-1,Gaze=4;
    internal double[] Levels=new double[7];
    internal static int Direction(Point pointer,Rectangle pet) {
        double x=(pointer.X-(pet.Left+pet.Width*.5))/Math.Max(1,pet.Width*.55);
        double y=(pointer.Y-(pet.Top+pet.Height*.19))/Math.Max(1,pet.Height*.4);
        int col=x<-.4?0:x>.4?2:1,row=y<-.4?0:y>.4?2:1;
        return row*3+col;
    }
    internal bool Step(bool animate) {
        bool moving=false;for(int i=0;i<7;i++){double wanted=i==Hover?1:0,delta=wanted-Levels[i];Levels[i]=!animate||Math.Abs(delta)<.01?wanted:Levels[i]+delta*.3;moving|=Levels[i]!=wanted;}return moving;
    }
    internal void Down(int target){Pressed=target>=0&&target<7?target:-1;}
    internal int Up(int target){int result=target==Pressed?Pressed:-1;Pressed=-1;return result;}
}

internal sealed class Artwork : IDisposable {
    internal Bitmap[] Frames=new Bitmap[9],Icons=new Bitmap[7];
    readonly string root; int scaleWidth; Bitmap[] cached=new Bitmap[9],smallIcons=new Bitmap[7];
    internal Artwork(string directory,Preferences settings) {
        root=Path.Combine(directory,"assets");
        try {
            using(var source=ReadImage("grove-atlas.png")) {
                for(int i=0;i<9;i++) {
                    // Visual review found the two upward diagonals exchanged in
                    // the generated sheet. Bind actual gaze, not prompt order.
                    int cellIndex=i==0?2:i==2?0:i;
                    int x=(int)Math.Round((cellIndex%3)*source.Width/3.0),y=(int)Math.Round((cellIndex/3)*source.Height/3.0);
                    int right=(int)Math.Round((cellIndex%3+1)*source.Width/3.0),bottom=(int)Math.Round((cellIndex/3+1)*source.Height/3.0);
                    var cell=source.Clone(new Rectangle(x,y,right-x,bottom-y),PixelFormat.Format32bppPArgb);
                    Frames[i]=cell;
                }
            }
            SetIcons(settings);
        }catch{Dispose();throw;}
    }
    Bitmap ReadImage(string name){string file=Path.Combine(root,name);if(new FileInfo(file).Length>12*1024*1024)throw new InvalidDataException("Oversized image");using(var image=Image.FromFile(file)){if(image.Width>4096||image.Height>4096)throw new InvalidDataException("Oversized image dimensions");return new Bitmap(image);}}
    internal void SetIcons(Preferences settings){for(int i=0;i<7;i++){if(Icons[i]!=null)Icons[i].Dispose();if(smallIcons[i]!=null)smallIcons[i].Dispose();smallIcons[i]=null;Icons[i]=String.IsNullOrEmpty(settings.slots[i].icon)?null:ReadImage(settings.slots[i].icon);}}
    internal Bitmap Draw(Geometry layout,Motion motion,bool expanded,int busy,double phase,bool drawPet=true) {
        if(layout.Bounds.IsEmpty)throw new InvalidOperationException("No layout");
        var output=new Bitmap(layout.Bounds.Width,layout.Bounds.Height,PixelFormat.Format32bppPArgb);
        using(var g=Graphics.FromImage(output)) {
            g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
            var pet=layout.Pet;pet.Offset(-layout.Bounds.Left,-layout.Bounds.Top);
            if(scaleWidth!=pet.Width){foreach(var image in cached)if(image!=null)image.Dispose();for(int i=0;i<9;i++){cached[i]=new Bitmap(pet.Width,pet.Height,PixelFormat.Format32bppPArgb);using(var pg=Graphics.FromImage(cached[i])){pg.InterpolationMode=InterpolationMode.HighQualityBicubic;pg.PixelOffsetMode=PixelOffsetMode.HighQuality;pg.DrawImage(Frames[i],new Rectangle(0,0,pet.Width,pet.Height));}}scaleWidth=pet.Width;}
            if(drawPet)g.DrawImageUnscaled(cached[motion.Gaze],pet.Location);
            if(expanded) {
                for(int i=0;i<7;i++) {
                    var r=layout.Buttons[i];r.Offset(-layout.Bounds.Left,-layout.Bounds.Top);double level=motion.Levels[i];
                    Color accent=i==2?Color.FromArgb(177,128,255):i==3?Color.FromArgb(247,184,79):Color.FromArgb(121,231,212);
                    using(var shape=DockRenderer.Round(r,(float)(9*layout.Scale)))using(var fill=new SolidBrush(Color.FromArgb(1+(int)(28*level),accent)))g.FillPath(fill,shape);
                    if(level>.01)using(var shape=DockRenderer.Round(RectangleF.Inflate(r,-1,-1),(float)(8*layout.Scale)))using(var pen=new Pen(Color.FromArgb((int)(110*level),accent),(float)Math.Max(1,layout.Scale)))g.DrawPath(pen,shape);
                    float factor=i==motion.Pressed?.88f:(float)(.94+.06*level);var art=new RectangleF(r.Left+r.Width*(1-factor)/2,r.Top+r.Height*(1-factor)/2-(float)(level*layout.Scale),r.Width*factor,r.Height*factor);
                    if(Icons[i]!=null){if(smallIcons[i]==null||smallIcons[i].Width!=r.Width){if(smallIcons[i]!=null)smallIcons[i].Dispose();smallIcons[i]=new Bitmap(r.Width,r.Height,PixelFormat.Format32bppPArgb);using(var ig=Graphics.FromImage(smallIcons[i])){ig.InterpolationMode=InterpolationMode.HighQualityBicubic;ig.PixelOffsetMode=PixelOffsetMode.HighQuality;ig.DrawImage(Icons[i],new Rectangle(0,0,r.Width,r.Height));}}g.DrawImage(smallIcons[i],art);}
                    else {
                        using(var pen=new Pen(Color.FromArgb(140+(int)(90*level),175,197,198),(float)(1.25*layout.Scale))){pen.DashStyle=DashStyle.Dot;g.DrawEllipse(pen,RectangleF.Inflate(art,-7*(float)layout.Scale,-7*(float)layout.Scale));pen.DashStyle=DashStyle.Solid;float cx=r.Left+r.Width/2f,cy=r.Top+r.Height/2f,d=(float)(4*layout.Scale);g.DrawLine(pen,cx-d,cy,cx+d,cy);g.DrawLine(pen,cx,cy-d,cx,cy+d);}
                    }
                    if(i==busy)using(var pen=new Pen(Color.FromArgb(230,accent),(float)(1.5*layout.Scale)))g.DrawArc(pen,RectangleF.Inflate(r,-2,-2),(float)(phase*360),90);
                }
                var a=layout.Buttons[1];var b=layout.Buttons[2];float x=(a.Right+b.Left)/2f-layout.Bounds.Left,y=a.Top+a.Height/2f-layout.Bounds.Top,cross=(float)(2*layout.Scale);
                using(var pen=new Pen(Color.FromArgb(170,159,171,181),(float)Math.Max(1,layout.Scale))){g.DrawLine(pen,x-cross,y-cross,x+cross,y+cross);g.DrawLine(pen,x-cross,y+cross,x+cross,y-cross);}
            }
        }
        return output;
    }
    public void Dispose(){foreach(var b in Frames)if(b!=null)b.Dispose();foreach(var b in Icons)if(b!=null)b.Dispose();foreach(var b in cached)if(b!=null)b.Dispose();foreach(var b in smallIcons)if(b!=null)b.Dispose();}
}

internal static class Launchers {
    internal static void Open(Slot slot,string directory) {
        Preferences.Validate(new Preferences{slots=new[]{slot,slot,slot,slot,slot,slot,slot}});
        if(slot.kind=="empty")return;
        if(slot.kind=="promtly") {
            if(OpenPromtly())return;
            string node=FindNode();
            if(node!=null&&File.Exists(Path.Combine(directory,"promtly-bridge.mjs"))) {
                var info=new ProcessStartInfo(node,"\""+Path.Combine(directory,"promtly-bridge.mjs")+"\""){WorkingDirectory=directory,UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden};
                info.EnvironmentVariables["PROMTLY_LOCAL_DIR"]=Path.Combine(Preferences.Home,"promtly-local");
                Process.Start(info);
                for(int i=0;i<20;i++){Thread.Sleep(250);if(OpenPromtly())return;}
            }
        }
        if(slot.kind=="parley"||slot.kind=="claude") {
            string shortcut=FindShortcut(slot.kind=="claude"?"Claude":"Parley");
            if(shortcut!=null){Process.Start(new ProcessStartInfo(shortcut){UseShellExecute=true});return;}
            // Desktop installers can resolve MSIX applications into a local shortcut.
            string resolved=Path.Combine(Preferences.Home,slot.kind+".lnk");
            if(File.Exists(resolved)){Process.Start(new ProcessStartInfo(resolved){UseShellExecute=true});return;}
        }
        if(slot.kind=="shortcut"&&!File.Exists(slot.target))throw new IOException("App or shortcut is missing. Choose it in dock settings.");
        if(slot.kind=="folder"&&!Directory.Exists(slot.target))throw new IOException("Project folder is missing. Choose it in dock settings.");
        Process.Start(new ProcessStartInfo(slot.target){UseShellExecute=true});
    }
    internal static string FindShortcut(string name) {
        foreach(var folder in new[]{Environment.GetFolderPath(Environment.SpecialFolder.Programs),Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms)}){
            foreach(var candidate in new[]{Path.Combine(folder,name+".lnk"),Path.Combine(folder,name,name+".lnk")})if(File.Exists(candidate))return candidate;
        }return null;
    }
    internal static string FindNode(){foreach(var folder in (Environment.GetEnvironmentVariable("PATH")??"").Split(';')){try{var path=Path.Combine(folder.Trim('"'),"node.exe");if(Path.IsPathRooted(path)&&File.Exists(path))return path;}catch(ArgumentException){}}return null;}
    static bool OpenPromtly(){try{var request=(HttpWebRequest)WebRequest.Create("http://127.0.0.1:37222/launcher");request.Proxy=null;request.Method="POST";request.Timeout=800;request.ReadWriteTimeout=800;request.Headers["Origin"]="http://127.0.0.1:37222";request.ContentType="application/json";byte[] body=Encoding.UTF8.GetBytes("{\"minimize\":false}");request.ContentLength=body.Length;using(var output=request.GetRequestStream())output.Write(body,0,body.Length);using(var response=request.GetResponse())using(var input=new StreamReader(response.GetResponseStream())){char[] buffer=new char[1024];return Regex.IsMatch(new string(buffer,0,input.ReadBlock(buffer,0,buffer.Length)),"\\\"ok\\\"\\s*:\\s*true");}}catch(WebException){return false;}}
}

internal sealed class DockSettings : Form {
    readonly Preferences original; readonly TextBox[] names=new TextBox[7],targets=new TextBox[7]; readonly ComboBox[] kinds=new ComboBox[7];readonly CheckBox motion=new CheckBox();
    internal Preferences Result;
    internal DockSettings(Preferences value) {
        original=value;Text="Make your dock yours";Size=new Size(730,525);MinimumSize=new Size(650,525);StartPosition=FormStartPosition.CenterScreen;BackColor=Color.FromArgb(244,247,245);Font=new Font("Segoe UI",10);FormBorderStyle=FormBorderStyle.Sizable;MaximizeBox=false;
        var intro=new Label{Text="Seven slots. Add HTTPS links, local app shortcuts or project folders.",Location=new Point(18,18),AutoSize=true};Controls.Add(intro);
        for(int i=0;i<7;i++) {
            int y=55+i*46;names[i]=new TextBox{Text=value.slots[i].name,Location=new Point(18,y),Width=135,MaxLength=36};
            kinds[i]=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Location=new Point(162,y),Width=115};kinds[i].Items.AddRange(new object[]{"empty","https","shortcut","folder","folders","parley","promtly","claude"});kinds[i].SelectedItem=value.slots[i].kind;
            targets[i]=new TextBox{Text=value.slots[i].target,Location=new Point(287,y),Width=320,Anchor=AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Right,MaxLength=2048};
            int index=i;var browse=new Button{Text="…",AccessibleName="Choose target for slot "+(i+1),Location=new Point(617,y-1),Size=new Size(55,32),Anchor=AnchorStyles.Right|AnchorStyles.Top};browse.Click+=(s,e)=>Browse(index);
            Controls.Add(names[i]);Controls.Add(kinds[i]);Controls.Add(targets[i]);Controls.Add(browse);
        }
        motion.Text="Cursor tracking and hover animation";motion.Checked=value.motion;motion.Location=new Point(18,388);motion.AutoSize=true;Controls.Add(motion);
        var save=new Button{Text="Save dock",Location=new Point(530,428),Size=new Size(142,36),Anchor=AnchorStyles.Right|AnchorStyles.Bottom};save.Click+=(s,e)=>Save();Controls.Add(save);AcceptButton=save;
        var cancel=new Button{Text="Cancel",DialogResult=DialogResult.Cancel,Location=new Point(380,428),Size=new Size(140,36),Anchor=AnchorStyles.Right|AnchorStyles.Bottom};Controls.Add(cancel);CancelButton=cancel;
    }
    void Browse(int index){if((string)kinds[index].SelectedItem=="folder"){using(var d=new FolderBrowserDialog())if(d.ShowDialog(this)==DialogResult.OK)targets[index].Text=d.SelectedPath;}else using(var d=new OpenFileDialog{Filter="Apps and shortcuts|*.exe;*.lnk",CheckFileExists=true})if(d.ShowDialog(this)==DialogResult.OK){targets[index].Text=d.FileName;kinds[index].SelectedItem="shortcut";}}
    void Save(){try{var value=new Preferences{x=original.x,y=original.y,petMode=original.petMode,motion=motion.Checked,slots=new Slot[7]};for(int i=0;i<7;i++)value.slots[i]=new Slot{name=names[i].Text.Trim(),kind=(string)kinds[i].SelectedItem,target=targets[i].Text.Trim(),icon=original.slots[i].icon};Preferences.Validate(value);value.Save();Result=value;DialogResult=DialogResult.OK;Close();}catch(Exception error){MessageBox.Show(this,error.Message,"Check dock settings",MessageBoxButtons.OK,MessageBoxIcon.Information);}}
}

internal sealed class Companion : Form {
    readonly Func<PetAnchor> petProbe;
    internal bool UsingExistingPet{get;private set;}
    internal Geometry CurrentLayout{get{return layout;}}
    long nextPetProbe;
    readonly string directory; Preferences settings; Artwork art; Geometry layout; readonly Motion motion=new Motion();
    readonly System.Windows.Forms.Timer probe=new System.Windows.Forms.Timer{Interval=100},animation=new System.Windows.Forms.Timer{Interval=33};
    readonly DockLabel label=new DockLabel();readonly Stopwatch clock=Stopwatch.StartNew();readonly NotifyIcon tray;
    bool expanded,dragging,launching,paused;Point dragStart,dragOrigin,lastPointer;long hoverAt,lastInside,lastGaze;int busy=-1;string feedback;
    internal Companion(string root,Func<PetAnchor> existingPet=null) {
        var adapter=new PetAdapter();petProbe=existingPet??new Func<PetAnchor>(adapter.Find);
        directory=root;settings=Preferences.Load();art=new Artwork(root,settings);
        Text="Promtly companion";FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;AutoScaleMode=AutoScaleMode.None;AccessibleName="Grove project companion";AccessibleRole=AccessibleRole.ToolBar;
        var screen=Screen.PrimaryScreen;double scale=PublicNative.Scale(screen.Bounds.Location);var area=screen.WorkingArea;
        var initial=settings.x==int.MinValue?new Point(area.Right-(int)(105*scale),area.Bottom-(int)(124*scale)):new Point(settings.x,settings.y);
        MoveTo(initial);label.LaunchRequested+=Open;
        var menu=new ContextMenuStrip();menu.Items.Add("Dock settings…",null,(s,e)=>Edit());
        var automatic=new ToolStripMenuItem("Pet: Automatic",null,(s,e)=>SetPetMode("auto")){Checked=settings.petMode=="auto"};
        var grove=new ToolStripMenuItem("Pet: Use Grove",null,(s,e)=>SetPetMode("grove")){Checked=settings.petMode=="grove"};menu.Items.Add(automatic);menu.Items.Add(grove);menu.Opening+=(s,e)=>{automatic.Checked=settings.petMode=="auto";grove.Checked=settings.petMode=="grove";};
        menu.Items.Add("Center Grove on this screen",null,(s,e)=>{SetPetMode("grove");MoveTo(new Point(area.Left+area.Width/2,area.Bottom-(int)(124*scale)));});menu.Items.Add("Pause / resume motion",null,(s,e)=>{paused=!paused;motion.Gaze=4;Render();});menu.Items.Add("Exit companion",null,(s,e)=>Close());ContextMenuStrip=menu;
        tray=new NotifyIcon{Icon=SystemIcons.Application,Text="Promtly · Grove companion",ContextMenuStrip=menu,Visible=true};tray.DoubleClick+=(s,e)=>{Show();Edit();};
        MouseDown+=(s,e)=>{if(e.Button!=MouseButtons.Left||launching)return;int target=layout.Hit(Cursor.Position);if(expanded&&target>=0){motion.Down(target);Capture=true;Render();}else if(!UsingExistingPet&&layout.Pet.Contains(Cursor.Position)){dragging=true;dragStart=Cursor.Position;dragOrigin=layout.Pet.Location;Capture=true;}};
        MouseMove+=(s,e)=>{Cursor=expanded&&layout.Hit(Cursor.Position)>=0?Cursors.Hand:Cursors.SizeAll;if(dragging)MoveTo(new Point(dragOrigin.X+Cursor.Position.X-dragStart.X,dragOrigin.Y+Cursor.Position.Y-dragStart.Y));};
        MouseUp+=(s,e)=>{if(e.Button!=MouseButtons.Left)return;if(dragging){dragging=false;settings.x=layout.Pet.X;settings.y=layout.Pet.Y;settings.Save();}else{int target=motion.Up(layout.Hit(Cursor.Position));Render();Open(target);}Capture=false;};
        MouseCaptureChanged+=(s,e)=>{if(!Capture){dragging=false;motion.Pressed=-1;Render();}};
        probe.Tick+=(s,e)=>Tick();animation.Tick+=(s,e)=>{bool moving=motion.Step(EnabledMotion);Render();if(!moving&&!launching)animation.Stop();};probe.Start();
    }
    bool EnabledMotion{get{return settings.motion&&!paused&&SystemInformation.UIEffectsEnabled&&SystemInformation.IsMenuAnimationEnabled;}}
    protected override AccessibleObject CreateAccessibilityInstance(){return new CompanionAccessible(this);}
    sealed class CompanionAccessible : ControlAccessibleObject {
        readonly Companion owner;internal CompanionAccessible(Companion value):base(value){owner=value;}
        public override int GetChildCount(){return 7;}
        public override AccessibleObject GetChild(int index){return index>=0&&index<7?new SlotAccessible(owner,index):null;}
    }
    sealed class SlotAccessible : AccessibleObject {
        readonly Companion owner;readonly int index;internal SlotAccessible(Companion value,int i){owner=value;index=i;}
        public override string Name{get{return owner.settings.slots[index].name;}set{}}
        public override string DefaultAction{get{return "Open";}}
        public override AccessibleRole Role{get{return AccessibleRole.PushButton;}}
        public override Rectangle Bounds{get{return owner.layout.Buttons[index];}}
        public override AccessibleStates State{get{return owner.expanded?AccessibleStates.None:AccessibleStates.Invisible;}}
        public override void DoDefaultAction(){owner.Open(index);}
    }
    protected override bool ShowWithoutActivation{get{return true;}}
    protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x08080080;return p;}}
    protected override void WndProc(ref Message m){if(m.Msg==0x21){m.Result=new IntPtr(3);return;}base.WndProc(ref m);}
    protected override void OnShown(EventArgs e){ApplyAnchor(settings.petMode=="auto"?petProbe():null);base.OnShown(e);Render();}
    void MoveTo(Point pet){var screen=Screen.FromPoint(pet);layout=Geometry.At(pet,screen.WorkingArea,PublicNative.Scale(screen.Bounds.Location));if(layout.Bounds.IsEmpty)throw new InvalidOperationException("Screen is too small for this dock");Bounds=layout.Bounds;label.Conceal();Render();}
    internal void ApplyAnchor(PetAnchor anchor){
        if(anchor!=null){var next=Geometry.Around(anchor);if(!next.Bounds.IsEmpty){if(!UsingExistingPet||layout.Bounds!=next.Bounds||layout.Pet!=next.Pet){UsingExistingPet=true;layout=next;Bounds=next.Bounds;expanded=false;motion.Hover=motion.Pressed=-1;label.Conceal();Render();}return;}}
        if(UsingExistingPet){UsingExistingPet=false;expanded=false;motion.Hover=motion.Pressed=-1;var area=Screen.PrimaryScreen.WorkingArea;MoveTo(settings.x==int.MinValue?new Point(area.Right-105,area.Bottom-124):new Point(settings.x,settings.y));}
    }
    void SetPetMode(string value){settings.petMode=value;settings.Save();ApplyAnchor(value=="auto"?petProbe():null);}
    void Tick() {
        if(clock.ElapsedMilliseconds>=nextPetProbe&&!dragging&&!Capture&&!launching&&!QuickFolders.IsOpen&&!ContextMenuStrip.Visible){nextPetProbe=clock.ElapsedMilliseconds+500;ApplyAnchor(settings.petMode=="auto"?petProbe():null);}
        var point=Cursor.Position;long now=clock.ElapsedMilliseconds;int target=expanded?layout.Hit(point):-1;bool inside=layout.Pet.Contains(point)||target>=0||label.Hit(point)||dragging||QuickFolders.IsOpen||ContextMenuStrip.Visible;
        if(inside){lastInside=now;if(!expanded){expanded=true;Render();}}else if(expanded&&now-lastInside>900&&!launching){expanded=false;motion.Hover=-1;label.Conceal();animation.Start();}
        if(target>=0&&motion.Hover!=target){motion.Hover=target;hoverAt=now;feedback=null;label.Conceal();QuickFolders.Conceal();animation.Start();}
        if(expanded&&motion.Hover>=0&&now-hoverAt>=220&&!label.IsShown){int i=motion.Hover;if(settings.slots[i].kind=="folders"){if(target==i&&!QuickFolders.IsOpen)QuickFolders.Show(this,layout.Buttons[i],layout.Scale,layout.Pet,layout.Bounds);}else label.Present(settings.slots[i].name,feedback??(settings.slots[i].kind=="empty"?"Add a project":"Click to open"),layout.Buttons[i],layout.Scale,i>=4,i,layout.Pet,layout.Bounds);}
        if(!EnabledMotion||UsingExistingPet){if(motion.Gaze!=4){motion.Gaze=4;Render();}}
        else if(now-lastGaze>=140){int gaze=Motion.Direction(point,layout.Pet);if(gaze!=motion.Gaze){motion.Gaze=gaze;Render();}lastGaze=now;}
        lastPointer=point;
    }
    void Render(){if(layout==null||layout.Bounds.IsEmpty||IsDisposed)return;using(var bitmap=art.Draw(layout,motion,expanded,busy,(clock.ElapsedMilliseconds%900)/900.0,!UsingExistingPet))LayeredDock.Update(Handle,layout.Bounds,bitmap);}
    void Edit(){label.Conceal();using(var dialog=new DockSettings(settings)){if(dialog.ShowDialog()==DialogResult.OK){settings=dialog.Result;art.SetIcons(settings);Render();}}}
    void Open(int target) {
        if(target<0||target>=7||launching)return;if(settings.slots[target].kind=="empty"){Edit();return;}if(settings.slots[target].kind=="folders"){label.Conceal();QuickFolders.Show(this,layout.Buttons[target],layout.Scale,layout.Pet,layout.Bounds);return;}
        launching=true;busy=target;animation.Start();var slot=settings.slots[target];
        ThreadPool.QueueUserWorkItem(state=>{string result="Opened";try{Launchers.Open(slot,directory);}catch(Exception){result="Choose target in dock settings";}if(IsDisposed)return;try{BeginInvoke(new Action(()=>{launching=false;busy=-1;feedback=result;motion.Hover=target;hoverAt=clock.ElapsedMilliseconds-220;lastInside=clock.ElapsedMilliseconds;label.Conceal();Render();}));}catch(InvalidOperationException){}});
    }
    protected override void Dispose(bool disposing){if(disposing){probe.Stop();probe.Dispose();animation.Stop();animation.Dispose();tray.Visible=false;tray.Dispose();label.Dispose();QuickFolders.DisposeFor(this);art.Dispose();if(ContextMenuStrip!=null)ContextMenuStrip.Dispose();}base.Dispose(disposing);}
}

internal static class Entry {
    [STAThread] static int Main(){
        bool created;using(var mutex=new Mutex(true,"Local\\PromtlyGroveCompanion-"+Environment.UserName,out created)){
            if(!created)return 0;
            PublicNative.EnableDpi();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            try{Application.Run(new Companion(AppDomain.CurrentDomain.BaseDirectory));return 0;}
            catch(Exception error){MessageBox.Show(error.Message+"\nYour settings were preserved. Inspect settings.json under LocalAppData\\PromtlyDock.","Companion could not start",MessageBoxButtons.OK,MessageBoxIcon.Information);return 1;}
        }
    }
}

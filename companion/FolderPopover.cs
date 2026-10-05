using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// One reusable hover surface. No ContextMenuStrip lifecycle or modal management.
internal sealed class FolderPopover : Form {
    readonly Timer timer=new Timer{Interval=100};
    readonly Stopwatch clock=Stopwatch.StartNew();
    readonly Panel rows=new Panel{AutoScroll=true};
    readonly Label title=new Label(),message=new Label();
    readonly Button add=new Button(),edit=new Button(),save=new Button(),cancel=new Button();
    readonly TextBox path=new TextBox(),name=new TextBox();
    readonly Action<string> openFolder;
    FolderCollection folders; Rectangle anchor,pet,dock; double scale=1;
    long lastInside; bool managing,editing,protectedFile; int editingIndex=-1;
    internal string Status{get{return message.Text;}}
    internal bool Editing{get{return editing;}}
    internal int SavedCount{get{return folders.items.Count;}}
    internal void ProtectSavedFile(){protectedFile=true;message.Text="Saved folders could not be read · file kept";add.Enabled=edit.Enabled=false;}
    internal FolderPopover(FolderCollection value,Action<string> opener=null){
        QuickFolders.Validate(value);folders=value;openFolder=opener??QuickFolders.Open;
        FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;
        AutoScaleMode=AutoScaleMode.None;BackColor=Color.FromArgb(18,24,28);
        ForeColor=Color.FromArgb(235,245,241);Font=new Font("Segoe UI",9);
        AccessibleName="Quick access folders";AccessibleRole=AccessibleRole.MenuPopup;
        title.Text="Folders";title.ForeColor=ForeColor;title.Font=new Font("Segoe UI",10,FontStyle.Bold);
        message.ForeColor=Color.FromArgb(160,191,181);message.AutoEllipsis=true;
        foreach(var button in new[]{add,edit,save,cancel}){
            button.FlatStyle=FlatStyle.Flat;button.FlatAppearance.BorderSize=0;
            button.BackColor=Color.FromArgb(31,48,46);button.ForeColor=ForeColor;
            button.Cursor=Cursors.Hand;Controls.Add(button);
        }
        add.Text="+ Add folder";edit.Text="Edit";save.Text="Save";cancel.Text="Cancel";
        add.Click+=(s,e)=>StartEditor(-1);edit.Click+=(s,e)=>{managing=!managing;editing=false;Rebuild();};
        save.Click+=(s,e)=>SaveEditor(name.Text,path.Text);cancel.Click+=(s,e)=>{editing=false;Rebuild();};
        path.MaxLength=1024;path.AccessibleName="Folder path";name.MaxLength=48;name.AccessibleName="Shortcut name";
        foreach(var field in new[]{path,name}){field.BackColor=Color.FromArgb(31,40,44);field.ForeColor=ForeColor;field.BorderStyle=BorderStyle.FixedSingle;Controls.Add(field);}
        Controls.Add(title);Controls.Add(message);Controls.Add(rows);
        timer.Tick+=(s,e)=>Poll(Cursor.Position,clock.ElapsedMilliseconds);
        KeyPreview=true;KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Escape){if(editing){editing=false;Rebuild();}else Conceal();e.Handled=true;}};
        Rebuild();
    }
    protected override bool ShowWithoutActivation{get{return true;}}
    protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x80;return p;}}
    protected override void WndProc(ref Message m){if(m.Msg==0x21&&!editing){m.Result=new IntPtr(3);return;}base.WndProc(ref m);}
    int D(double value){return Math.Max(1,(int)Math.Round(value*scale));}
    internal static Rectangle Place(Size size,Rectangle button,Rectangle pet,Rectangle dock,Rectangle work){
        int margin=8,gap=10;
        int y=Math.Max(work.Top+margin,Math.Min(work.Bottom-size.Height-margin,button.Bottom-size.Height));
        foreach(int x in new[]{dock.Left-size.Width-gap,dock.Right+gap,button.Left-size.Width-gap,button.Right+gap}){
            var candidate=new Rectangle(x,y,size.Width,size.Height);
            if(work.Contains(candidate)&&!candidate.IntersectsWith(pet)&&!candidate.IntersectsWith(dock))return candidate;
        }
        // Tiny displays: keep the complete surface visible; the pet is preferred clear.
        var fallback=new Rectangle(Math.Max(work.Left+margin,Math.Min(work.Right-size.Width-margin,button.Left-size.Width-gap)),y,size.Width,size.Height);
        if(fallback.IntersectsWith(pet)){int above=pet.Top-size.Height-gap;if(above>=work.Top+margin)fallback.Y=above;}
        return fallback;
    }
    internal void Present(Control owner,Rectangle button,double dpi,Rectangle petBounds,Rectangle dockBounds){
        bool moved=anchor!=button||scale!=dpi||pet!=petBounds||dock!=dockBounds;
        anchor=button;scale=dpi;pet=petBounds;dock=dockBounds;
        if(moved)Rebuild();lastInside=clock.ElapsedMilliseconds;
        if(!Visible){if(owner!=null&&owner.IsHandleCreated)Show(owner);else Show();}
        timer.Start();
    }
    internal void Poll(Point pointer,long now){
        if(!Visible)return;
        if(anchor.Contains(pointer)||Bounds.Contains(pointer)||editing||Capture)lastInside=now;
        else if(now-lastInside>800)Conceal();
    }
    internal void Conceal(){timer.Stop();if(!IsDisposed)Hide();}
    internal bool Hit(Point pointer){return Visible&&Bounds.Contains(pointer);}
    void Rebuild(){
        foreach(Control child in new List<Control>(ControlsOf(rows)))child.Dispose();rows.Controls.Clear();
        var work=Screen.FromRectangle(anchor.IsEmpty?new Rectangle(Cursor.Position,new Size(1,1)):anchor).WorkingArea;
        int width=Math.Min(D(276),work.Width-D(16)),rowHeight=D(42),count=folders.items.Count+3;
        int rowsHeight=Math.Min(count,6)*rowHeight;
        ClientSize=new Size(width,D(50)+rowsHeight+D(68)+(editing?D(108):0));
        if(ClientSize.Height>work.Height-D(16)){rowsHeight=Math.Max(D(42),work.Height-D(134)-(editing?D(108):0));ClientSize=new Size(width,D(118)+rowsHeight+(editing?D(108):0));}
        title.SetBounds(D(14),D(12),width-D(28),D(23));
        rows.SetBounds(D(9),D(46),width-D(18),rowsHeight);
        int y=0;
        for(int i=0;i<folders.items.Count;i++){int index=i;var item=folders.items[i];AddRow(item.name,item.path,y,rowHeight,()=>{if(managing)StartEditor(index);else OpenRow(index);},managing?new Action(()=>Remove(index)):null);y+=rowHeight;}
        foreach(var item in Defaults()){string destination=item.path;AddRow(item.name,destination,y,rowHeight,()=>OpenPath(destination),null);y+=rowHeight;}
        rows.AutoScrollMinSize=new Size(0,y);
        int footer=rows.Bottom+D(7);add.SetBounds(D(12),footer,D(166),D(30));edit.SetBounds(width-D(85),footer,D(73),D(30));edit.Text=managing?"Done":"Edit";
        path.Visible=name.Visible=save.Visible=cancel.Visible=editing;
        int textTop=footer+D(37);
        path.SetBounds(D(12),textTop,width-D(24),D(25));name.SetBounds(D(12),textTop+D(29),width-D(24),D(25));
        save.SetBounds(D(12),textTop+D(58),D(121),D(29));cancel.SetBounds(D(143),textTop+D(58),D(121),D(29));
        message.SetBounds(D(13),ClientSize.Height-D(23),width-D(26),D(19));
        if(String.IsNullOrEmpty(message.Text))message.Text=editing?"Paste a folder path · name optional":managing?"Click a saved row to rename · × removes shortcut":"Hover to browse · click a folder to open";
        Bounds=Place(ClientSize,anchor,pet,dock,work);
        using(var curve=Rounded(new Rectangle(0,0,Width,Height),D(13))){var previous=Region;Region=new Region(curve);if(previous!=null)previous.Dispose();}
        Invalidate();
    }
    static IEnumerable<Control> ControlsOf(Control owner){foreach(Control item in owner.Controls)yield return item;}
    static IEnumerable<FolderBookmark> Defaults(){yield return new FolderBookmark{name="Documents",path=Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)};yield return new FolderBookmark{name="Downloads",path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads")};yield return new FolderBookmark{name="Desktop",path=Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)};}
    void AddRow(string label,string destination,int y,int height,Action clicked,Action removed){
        var row=new FolderRow(label,Directory.Exists(destination)?destination:"Unavailable · shortcut retained"){Location=new Point(0,y),Size=new Size(rows.Width-D(4)-(removed!=null?D(30):0),height-D(3)),Font=Font,Cursor=Cursors.Hand};
        row.Click+=(s,e)=>clicked();rows.Controls.Add(row);
        if(removed!=null){var remove=new Button{Text="×",AccessibleName="Remove shortcut "+label,Location=new Point(rows.Width-D(34),y),Size=new Size(D(28),height-D(3)),FlatStyle=FlatStyle.Flat,BackColor=BackColor,ForeColor=Color.FromArgb(184,197,193),Cursor=Cursors.Hand};remove.FlatAppearance.BorderSize=0;remove.Click+=(s,e)=>removed();rows.Controls.Add(remove);}
    }
    internal void OpenRow(int index){if(index<0||index>=folders.items.Count)return;OpenPath(folders.items[index].path);}
    void OpenPath(string destination){try{openFolder(destination);message.Text="Opened";Conceal();}catch(Exception error){message.Text=error.Message;Invalidate();}}
    internal void StartEditor(int index){if(protectedFile)return;editingIndex=index;editing=true;message.Text="Paste a folder path · name optional";path.Text=index>=0?folders.items[index].path:"";name.Text=index>=0?folders.items[index].name:"";Rebuild();SendMessage(path.Handle,0x1501,new IntPtr(1),"Folder path · e.g. C:\\Projects\\Cashflo");SendMessage(name.Handle,0x1501,new IntPtr(1),"Shortcut name (optional)");Activate();path.Focus();}
    internal bool SaveEditor(string label,string destination){
        if(protectedFile)return false;
        try{
            destination=destination.Trim();if(!System.Text.RegularExpressions.Regex.IsMatch(destination,@"\A[A-Za-z]:[\\/]"))throw new IOException("Paste a local drive folder path.");if(!Directory.Exists(destination))throw new IOException("Choose an existing local folder path.");
            string normalized=Path.GetFullPath(destination);label=label.Trim();if(label.Length==0){label=new DirectoryInfo(normalized).Name;if(label.Length==0)label=normalized;}
            var next=Copy();var item=new FolderBookmark{name=label,path=normalized};if(editingIndex>=0)next.items[editingIndex]=item;else next.items.Add(item);
            QuickFolders.Save(next);folders=next;editing=false;message.Text="Saved · ready for quick access";Rebuild();return true;
        }catch(Exception error){message.Text=error.Message;Invalidate();return false;}
    }
    internal bool Remove(int index){
        if(protectedFile)return false;
        if(index<0||index>=folders.items.Count)return false;
        try{var next=Copy();next.items.RemoveAt(index);QuickFolders.Save(next);folders=next;message.Text="Shortcut removed · folder kept";Rebuild();return true;}
        catch(Exception error){message.Text=error.Message;Invalidate();return false;}
    }
    FolderCollection Copy(){var value=new FolderCollection();foreach(var item in folders.items)value.items.Add(new FolderBookmark{name=item.name,path=item.path});return value;}
    static GraphicsPath Rounded(Rectangle box,int radius){int r=radius*2;var path=new GraphicsPath();path.AddArc(box.Left,box.Top,r,r,180,90);path.AddArc(box.Right-r-1,box.Top,r,r,270,90);path.AddArc(box.Right-r-1,box.Bottom-r-1,r,r,0,90);path.AddArc(box.Left,box.Bottom-r-1,r,r,90,90);path.CloseFigure();return path;}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var curve=Rounded(new Rectangle(0,0,Width,Height),D(13)))using(var pen=new Pen(Color.FromArgb(65,88,83))){e.Graphics.DrawPath(pen,curve);}}
    protected override void Dispose(bool disposing){if(disposing){timer.Stop();timer.Dispose();}base.Dispose(disposing);}
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr SendMessage(IntPtr window,int message,IntPtr parameter,string value);
    sealed class FolderRow : Button {
        readonly string detail;bool over,pressed;
        internal FolderRow(string title,string path){Text=title;detail=path;AccessibleName=title;FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);MouseEnter+=(s,e)=>{over=true;Invalidate();};MouseLeave+=(s,e)=>{over=false;Invalidate();};}
        protected override void OnPaint(PaintEventArgs e){
            e.Graphics.Clear(pressed?Color.FromArgb(44,71,62):over?Color.FromArgb(32,54,49):Color.FromArgb(18,24,28));
            if(over)using(var ink=new SolidBrush(Color.FromArgb(132,216,182)))e.Graphics.FillRectangle(ink,0,5,2,Height-10);
            int inset=Math.Max(8,Height/4);var text=new Rectangle(inset,3,Width-inset*2,Height/2);
            TextRenderer.DrawText(e.Graphics,Text,Font,text,Color.FromArgb(236,247,241),TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
            using(var small=new Font(Font.FontFamily,Math.Max(7,Font.Size-1)))TextRenderer.DrawText(e.Graphics,detail,small,new Rectangle(inset,Height/2,Width-inset*2,Height/2-2),Color.FromArgb(148,173,164),TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
        }
        protected override void OnMouseDown(MouseEventArgs e){pressed=e.Button==MouseButtons.Left;base.OnMouseDown(e);Invalidate();}
        protected override void OnMouseUp(MouseEventArgs e){pressed=false;base.OnMouseUp(e);Invalidate();}
        protected override void OnMouseCaptureChanged(EventArgs e){pressed=false;base.OnMouseCaptureChanged(e);Invalidate();}
    }
}

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

internal static class CompanionTests {
    static int count;
    static void Check(bool condition,string message){count++;if(!condition)throw new Exception(message);}
    static void Reject(Action action,string message){bool refused=false;try{action();}catch(InvalidDataException){refused=true;}Check(refused,message);}
    [STAThread] static int Main(string[] args){
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);PublicNative.EnableDpi();Application.EnableVisualStyles();
        try{
            string root=args[0];var settings=Preferences.Defaults();Preferences.Validate(settings);Check(settings.slots.Length==7,"seven slots");
            Check(settings.slots[0].name=="Hugin"&&settings.slots[1].name=="Parley"&&settings.slots[2].name=="Promtly"&&settings.slots[3].name=="Claude","top plus elbow order");
            Check(settings.slots[5].kind=="empty"&&settings.slots[5].icon==""&&settings.slots[6].kind=="folders","generic project plus editable folder utility slots");
            var hayden=Preferences.Hayden();Preferences.Validate(hayden);Check(hayden.slots[5].target=="https://cashflo.org/"&&hayden.slots[5].icon=="cashflow.png","Hayden preset uses approved CF and real website");
            foreach(string value in new[]{"http://example.org","file:///C:/Windows","javascript:alert(1)","https://name:secret@example.org","https://example.org/#secret","\nhttps://example.org"}){var p=Preferences.Defaults();p.slots[0].target=value;Reject(()=>Preferences.Validate(p),"refused URL: "+value);}
            foreach(string value in new[]{"relative.exe",@"\\host\share\app.exe",@"C:\run.cmd",@"C:\run.ps1","C:\\run.exe\" /arg"}){var p=Preferences.Defaults();p.slots[5].kind="shortcut";p.slots[5].target=value;Reject(()=>Preferences.Validate(p),"refused executable target");}
            foreach(string value in new[]{"../outside.png","sub/icon.png","icon.jpg","x.png\n"}){var p=Preferences.Defaults();p.slots[0].icon=value;Reject(()=>Preferences.Validate(p),"refused icon escape");}
            var custom=Preferences.Defaults();custom.slots[5].kind="https";custom.slots[5].target="https://example.org/project";custom.slots[5].name="My game";Preferences.Validate(custom);Check(custom.slots[5].target=="https://example.org/project","custom project link");
            var m=new Motion();m.Down(2);Check(m.Up(1)==-1,"cross-target press cancels");m.Down(2);Check(m.Up(2)==2,"same-target click");m.Down(-1);Check(m.Up(0)==-1,"transparent gap cancels");m.Hover=1;for(int i=0;i<25;i++)m.Step(true);Check(m.Levels[1]>.99,"hover eases in");m.Hover=-1;for(int i=0;i<25;i++)m.Step(true);Check(m.Levels[1]==0,"hover settles to idle");m.Hover=3;m.Step(false);Check(m.Levels[3]==1,"reduced motion snaps");
            foreach(double scale in new[]{1,1.25,1.5,2,3,4})foreach(var work in new[]{new Rectangle(0,0,3840,2160),new Rectangle(-3840,0,3840,2160),new Rectangle(0,-2160,3840,2160)})foreach(var origin in new[]{new Point(work.Left,work.Top),new Point(work.Right-1,work.Top),new Point(work.Left,work.Bottom-1),new Point(work.Right-1,work.Bottom-1),new Point(work.Left+work.Width/2,work.Top+work.Height/2)}){
                var g=Geometry.At(origin,work,scale);Check(!g.Bounds.IsEmpty&&work.Contains(g.Bounds),"edge/DPI bounds");
                for(int i=0;i<7;i++){Check(!g.Buttons[i].IntersectsWith(g.Pet),"pet clear");Check(g.Hit(new Point(g.Buttons[i].Left+g.Buttons[i].Width/2,g.Buttons[i].Top+g.Buttons[i].Height/2))==i,"target hit");for(int j=i+1;j<7;j++)Check(!g.Buttons[i].IntersectsWith(g.Buttons[j]),"targets separate");}
                Check(g.Hit(new Point(g.Pet.Left+g.Pet.Width/2,g.Pet.Top+g.Pet.Height/2))==-1,"pet isn't app launch target");
                var a=g.Buttons[1];var b=g.Buttons[2];Check(g.Hit(new Point((a.Right+b.Left)/2,a.Top+a.Height/2))==-1,"cross isn't clickable");
            }
            var pet=new Rectangle(200,200,90,116);for(int row=0;row<3;row++)for(int col=0;col<3;col++){var pointer=new Point((int)(pet.Left+pet.Width*.5+(col-1)*pet.Width),(int)(pet.Top+pet.Height*.19+(row-1)*pet.Height));Check(Motion.Direction(pointer,pet)==row*3+col,"nine gaze directions");}
            using(var art=new Artwork(root,settings)){
                Check(art.Frames.Length==9,"nine frames");for(int i=0;i<9;i++){int alpha=0;for(int y=0;y<art.Frames[i].Height;y+=6)for(int x=0;x<art.Frames[i].Width;x+=6)if(art.Frames[i].GetPixel(x,y).A>40)alpha++;Check(alpha>250,"nonempty character cell");Check(art.Frames[i].GetPixel(0,0).A==0,"transparent atlas cell corner");}
                var geometry=Geometry.At(new Point(1500,850),new Rectangle(0,0,1920,1080),1);var motion=new Motion{Hover=2,Gaze=0};motion.Step(false);
                var watch=Stopwatch.StartNew();for(int i=0;i<120;i++)using(var frame=art.Draw(geometry,motion,true,-1,0))Check(frame.Width==geometry.Bounds.Width,"cached draw");watch.Stop();Console.WriteLine("120 native draws: "+watch.ElapsedMilliseconds+"ms");
                using(var sprite=art.Draw(geometry,motion,false,-1,0)){var button=geometry.Buttons[0];Check(sprite.GetPixel(button.Left+button.Width/2-geometry.Bounds.Left,button.Top+button.Height/2-geometry.Bounds.Top).A==0,"collapsed targets transparent");}
                using(var frame=art.Draw(geometry,motion,true,-1,0))using(var preview=new Bitmap(700,360))using(var pg=Graphics.FromImage(preview)){
                    pg.Clear(Color.FromArgb(23,24,26));pg.DrawImageUnscaled(frame,370,55);using(var f=new Font("Segoe UI",12))using(var ink=new SolidBrush(Color.FromArgb(233,242,238))){pg.DrawString("Grove · Hugin · Parley × Promtly",f,ink,20,20);pg.DrawString("Claude in the corner. AEVUM + your projects.",f,ink,20,310);}preview.Save(Path.Combine(root,"preview.png"),ImageFormat.Png);
                }
            }
            using(var label=new DockLabel()){
                var button=new Rectangle(500,300,36,36);int launches=0;label.LaunchRequested+=i=>{Check(i==3,"label selected target");launches++;};label.Present("Claude","Click to open",button,1,false,3,new Rectangle(510,390,90,116),new Rectangle(450,300,172,168));
                Check(label.IsShown,"label visible");Check(label.Width>=132&&label.Height>=48,"large label target");var center=new Point(label.Left+label.Width/2,label.Top+label.Height/2);Check(label.Hit(center),"label target");label.PointerDown(center);label.PointerUp(center);Check(launches==1,"label click launches once");label.PointerDown(center);label.PointerUp(new Point(-100,-100));Check(launches==1,"label release outside cancels");
                long style=GetWindowLong(label.Handle,-20).ToInt64();Check((style&0x08000000)!=0&&(style&0x20)==0,"label noactivate and accepts input");label.Conceal();Check(!label.IsShown&&!label.Hit(center),"hidden label never launches");
            }
            using(var companion=new Companion(root,()=>null)){Check(companion.AccessibilityObject.GetChildCount()==7,"seven accessible targets");for(int i=0;i<7;i++){var target=companion.AccessibilityObject.GetChild(i);Check(target.Name==settings.slots[i].name&&target.Role==AccessibleRole.PushButton,"accessible target identity");Check(target.State==AccessibleStates.Invisible,"collapsed target invisible to accessibility");}
                var before=companion.CurrentLayout.Pet;var anchor=new PetAnchor{Pet=new Rectangle(1823,927,80,87),Work=new Rectangle(0,0,1920,1040),Scale=1};companion.ApplyAnchor(anchor);Check(companion.UsingExistingPet&&companion.CurrentLayout.Pet==anchor.Pet,"automatic mode attaches existing pet");companion.ApplyAnchor(null);Check(!companion.UsingExistingPet&&companion.CurrentLayout.Pet==before,"pet disappearance restores Grove");companion.ApplyAnchor(anchor);Check(companion.UsingExistingPet,"existing pet reappears without duplicate mascot");
            }
            foreach(double scale in new[]{1,1.25,1.5,2})foreach(var spot in new[]{new Rectangle(1823,927,80,87),new Rectangle(16,17,80,87),new Rectangle(16,927,80,87),new Rectangle(1823,17,80,87)}){
                var anchor=new PetAnchor{Pet=new Rectangle((int)(spot.X*scale),(int)(spot.Y*scale),(int)(80*scale),(int)(87*scale)),Work=new Rectangle(0,0,(int)(1920*scale),(int)(1040*scale)),Scale=scale};var attached=Geometry.Around(anchor);Check(!attached.Bounds.IsEmpty&&anchor.Work.Contains(attached.Bounds),"attached corner/DPI bounds");for(int i=0;i<7;i++){Check(!attached.Buttons[i].IntersectsWith(anchor.Pet),"attached targets stay off existing pet");for(int j=i+1;j<7;j++)Check(!attached.Buttons[i].IntersectsWith(attached.Buttons[j]),"attached targets separate");}
                using(var art=new Artwork(root,settings))using(var frame=art.Draw(attached,new Motion(),true,-1,0,false)){var intersection=Rectangle.Intersect(anchor.Pet,attached.Bounds);bool empty=true;for(int y=intersection.Top;y<intersection.Bottom;y++)for(int x=intersection.Left;x<intersection.Right;x++)if(frame.GetPixel(x-attached.Bounds.Left,y-attached.Bounds.Top).A!=0)empty=false;Check(empty,"existing pet receives no duplicate pixels or hit plate");}
            }
            string state="{\"electron-avatar-overlay-open\":true,\"electron-avatar-overlay-bounds\":{\"x\":1807,\"y\":910,\"displayBounds\":{\"x\":0,\"y\":0,\"width\":1920,\"height\":1080}},\"unrelated\":{\"credential\":\"fixture-not-retained\"}}";
            using(var stream=new MemoryStream(System.Text.Encoding.UTF8.GetBytes(state)))Check(PetPlacement.Read(stream)!=null,"bounded UI projection ignores unrelated fields");
            using(var stream=new MemoryStream(System.Text.Encoding.UTF8.GetBytes(state.Replace("true","false"))))Check(PetPlacement.Read(stream)==null,"closed pet rejected");
            using(var stream=new MemoryStream(System.Text.Encoding.UTF8.GetBytes(state+"{}")))Check(PetPlacement.Read(stream)==null,"malformed pet state rejected");
            string trusted=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),@"WindowsApps\OpenAI.Codex_26.930.3930.0_x64__2p2nqsd0c76g0\app\ChatGPT.exe");Check(PetGeometry.TrustedPath(trusted)&&!PetGeometry.TrustedPath(trusted.Replace("26.930","26.931")),"unsupported pet version rejected");
            Check(Preferences.Defaults().petMode=="auto","automatic pet default");var invalidMode=Preferences.Defaults();invalidMode.petMode="copy-account-avatar";Reject(()=>Preferences.Validate(invalidMode),"unsupported pet mode refused");
            if(args.Length>1&&args[1]=="--live-pet-probe"){var probe=new PetAdapter();var live=probe.Find();Console.WriteLine("Live supported pet: "+(live!=null)+"; status="+probe.status);}
            var save=Preferences.Defaults();save.slots[5].name="Persisted game";save.slots[5].kind="https";save.slots[5].target="https://example.org/game";save.Save();Check(Preferences.Load().slots[5].name=="Persisted game","durable custom settings");save.motion=false;save.Save();Check(File.Exists(Preferences.PathName+".previous")&&!Preferences.Load().motion,"recoverable settings backup");
            var folders=new FolderCollection();folders.items.Add(new FolderBookmark{name="My project",path=Path.GetFullPath(root)});QuickFolders.Save(folders);Check(QuickFolders.Load().items.Count==1,"folder persists");folders.items[0].name="Renamed project";QuickFolders.Save(folders);Check(QuickFolders.Load().items[0].name=="Renamed project"&&File.Exists(QuickFolders.FileName+".previous"),"folder rename has backup");
            var duplicate=new FolderCollection();duplicate.items.Add(new FolderBookmark{name="First",path=@"C:\demo"});duplicate.items.Add(new FolderBookmark{name="Duplicate",path=@"c:\DEMO\"});Reject(()=>QuickFolders.Validate(duplicate),"duplicate folder refused");
            foreach(string path in new[]{"relative",@"\\server\share","C:\\folder\" & calc","C:\\bad\nfolder"}){var bad=new FolderCollection();bad.items.Add(new FolderBookmark{name="Bad",path=path});Reject(()=>QuickFolders.Validate(bad),"bad folder path refused");}
            var many=new FolderCollection();for(int i=0;i<25;i++)many.items.Add(new FolderBookmark{name="Project "+i,path=@"C:\demo"+i});Reject(()=>QuickFolders.Validate(many),"folder count bounded");
            using(var owner=new Form())using(var menu=new FolderPopover(folders,p=>{Check(p==Path.GetFullPath(root),"folder row launches stored path");})){
                var button=new Rectangle(1500,930,32,32);var dock=new Rectangle(1500,770,180,200);var petArea=new Rectangle(1560,835,80,87);var foreground=GetForegroundWindow();
                menu.Present(owner,button,1,petArea,dock);Check(menu.Visible&&menu.SavedCount==1,"hover folder panel displays saved items");Check(GetForegroundWindow()==foreground,"hover panel does not activate");Check(!menu.Bounds.IntersectsWith(petArea)&&!menu.Bounds.IntersectsWith(dock),"folder panel remains outside dock and pet");
                using(var preview=new Bitmap(menu.Width,menu.Height)){menu.DrawToBitmap(preview,new Rectangle(Point.Empty,preview.Size));preview.Save(Path.Combine(root,"folder-popover.png"),ImageFormat.Png);}
                menu.Poll(new Point(menu.Left+15,menu.Top+60),100);Check(menu.Visible,"panel stays while browsing");menu.Poll(new Point(-100,-100),200);Check(menu.Visible,"crossing gap has grace");menu.Poll(new Point(-100,-100),2000);Check(!menu.Visible,"panel closes after leaving");
                for(int i=0;i<20;i++){menu.Present(owner,button,1,petArea,dock);menu.Conceal();}menu.Present(owner,button,1,petArea,dock);Check(menu.Visible&&!menu.IsDisposed,"repeat hide and reopen never disposes menu");
                menu.OpenRow(0);Check(!menu.Visible,"folder click opens and closes surface");menu.StartEditor(0);Check(menu.SaveEditor("Inline rename",root)&&QuickFolders.Load().items[0].name=="Inline rename","inline rename persists without a dialog");
                Check(!menu.SaveEditor("Missing",Path.Combine(root,"does-not-exist"))&&menu.SavedCount==1,"invalid edit keeps prior bookmark and reports inline");
                Check(menu.Remove(0)&&Directory.Exists(root)&&QuickFolders.Load().items.Count==0,"remove shortcut preserves real folder");
                menu.ProtectSavedFile();Check(!menu.SaveEditor("Protected",root),"unreadable source is protected from overwrite");
            }
            using(var failed=new FolderPopover(folders,p=>{throw new IOException("Fixture unavailable");})){failed.OpenRow(0);Check(failed.Status=="Fixture unavailable","open failures stay inline without modal popups");}
            foreach(double scale in new[]{1,1.25,1.5,2})foreach(var corner in new[]{new Point(18,18),new Point(1740,18),new Point(18,835),new Point(1740,835)}){
                var work=new Rectangle(0,0,(int)(1920*scale),(int)(1040*scale));var dock=new Rectangle((int)(corner.X*scale),(int)(corner.Y*scale),(int)(162*scale),(int)(185*scale));var petArea=new Rectangle(dock.Left+(int)(65*scale),dock.Top+(int)(65*scale),(int)(80*scale),(int)(87*scale));var button=new Rectangle(dock.Left,dock.Bottom-(int)(32*scale),(int)(32*scale),(int)(32*scale));var popup=FolderPopover.Place(new Size((int)(276*scale),(int)(244*scale)),button,petArea,dock,work);
                Check(work.Contains(popup)&&!popup.IntersectsWith(dock)&&!popup.IntersectsWith(petArea),"folder corners and DPI stay visible and clear");
            }
            Console.WriteLine("PASS "+count+" companion assertions");return 0;
        }catch(Exception error){Console.Error.WriteLine(error);return 1;}
    }
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")]static extern IntPtr GetWindowLong(IntPtr handle,int index);
    [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows.Forms;

internal sealed class FolderBookmark {public string name,path;}
internal sealed class FolderCollection {public int schema=1;public List<FolderBookmark> items=new List<FolderBookmark>();}
internal static class QuickFolders {
    internal static readonly string Home=Environment.GetEnvironmentVariable("PROMTLY_DOCK_HOME")??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"PromtlyDock");
    internal static readonly string FileName=Path.Combine(Home,"folders.json");
    internal static void Validate(FolderCollection value){if(value==null||value.schema!=1||value.items==null||value.items.Count>24)throw new InvalidDataException("Use at most 24 saved folders");var paths=new HashSet<string>(StringComparer.OrdinalIgnoreCase);foreach(var item in value.items){if(item==null||String.IsNullOrWhiteSpace(item.name)||item.name.Length>48||Regex.IsMatch(item.name,@"[\x00-\x1f\x7f]")||String.IsNullOrEmpty(item.path)||item.path.Length>1024||item.path.IndexOfAny(Path.GetInvalidPathChars())>=0||!Regex.IsMatch(item.path,@"\A[A-Za-z]:[\\/]")||item.path.StartsWith(@"\\")||!paths.Add(Path.GetFullPath(item.path).TrimEnd('\\','/')))throw new InvalidDataException("Choose distinct local folder paths and short names");}}
    internal static FolderCollection Load(){if(!File.Exists(FileName))return new FolderCollection();if(new FileInfo(FileName).Length>32768)throw new InvalidDataException("Saved folders exceed 32 KiB");var value=new JavaScriptSerializer{MaxJsonLength=32768}.Deserialize<FolderCollection>(File.ReadAllText(FileName));Validate(value);return value;}
    internal static void Save(FolderCollection value){Validate(value);Directory.CreateDirectory(Home);string temp=FileName+"."+Guid.NewGuid().ToString("N")+".tmp";File.WriteAllText(temp,new JavaScriptSerializer().Serialize(value),new UTF8Encoding(false));if(File.Exists(FileName))File.Replace(temp,FileName,FileName+".previous");else File.Move(temp,FileName);}
    internal static void Open(string path){if(!Directory.Exists(path))throw new IOException("This folder is unavailable. Its saved shortcut was kept.");Process.Start(new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"explorer.exe"),"\""+Path.GetFullPath(path)+"\""){UseShellExecute=false});}
    static FolderPopover panel; static Control panelOwner;
    internal static bool IsOpen {get{return panel!=null&&!panel.IsDisposed&&panel.Visible;}}
    internal static void Show(Control owner,Rectangle anchor,double scale,Rectangle pet,Rectangle dock){
        if(panel==null||panel.IsDisposed||panelOwner!=owner){if(panel!=null)panel.Dispose();panelOwner=owner;FolderCollection value;bool unreadable=false;try{value=Load();}catch{value=new FolderCollection();unreadable=true;}panel=new FolderPopover(value);if(unreadable)panel.ProtectSavedFile();}
        panel.Present(owner,anchor,scale,pet,dock);
    }
    internal static void Conceal(){if(panel!=null&&!panel.IsDisposed)panel.Conceal();}
    internal static void DisposeFor(Control owner){if(panelOwner==owner&&panel!=null){panel.Dispose();panel=null;panelOwner=null;}}

}
internal static class DesktopLinks {
    internal static void OpenClaude(){
        foreach(string folder in new[]{AppDomain.CurrentDomain.BaseDirectory,Environment.GetFolderPath(Environment.SpecialFolder.Programs),Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),QuickFolders.Home})foreach(string name in new[]{"Claude.lnk",Path.Combine("Claude","Claude.lnk"),"claude.lnk"}){string path=Path.Combine(folder,name);if(File.Exists(path)){Process.Start(new ProcessStartInfo(path){UseShellExecute=true});return;}}
        Process.Start(new ProcessStartInfo("https://claude.ai"){UseShellExecute=true});
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
namespace SanaQuickItems {
 [Serializable] public class BuildEntry { public string Item; public int Count; public BuildEntry() {} public BuildEntry(string item,int count) { Item=item; Count=count; } }
 [Serializable] public class SavedBuild {
  public string Name="Мой билд";
  public List<BuildEntry> Items=new List<BuildEntry>();
  public void Validate() {
   if (String.IsNullOrWhiteSpace(Name) || Name.Length>80) throw new InvalidDataException("Название должно содержать от 1 до 80 символов.");
   if (Items==null || Items.Count>2000) throw new InvalidDataException("Некорректный список предметов.");
   var seen=new HashSet<string>(StringComparer.Ordinal);
   foreach(var row in Items) if(row==null || String.IsNullOrEmpty(row.Item) || row.Item.Length>200 || row.Count<1 || row.Count>1000000 || !seen.Add(row.Item)) throw new InvalidDataException("Некорректный предмет или количество (1–1000000).");
   if(Items.Sum(x=>(long)x.Count)>10000000) throw new InvalidDataException("В билде слишком много предметов.");
  }
 }
 public static class BuildStorage {
  public static SavedBuild Read(string file) {
   if(new FileInfo(file).Length>1048576) throw new InvalidDataException("Файл билда слишком большой.");
   var settings=new XmlReaderSettings { DtdProcessing=DtdProcessing.Prohibit, XmlResolver=null };
   using(var reader=XmlReader.Create(file,settings)) { var build=(SavedBuild)new XmlSerializer(typeof(SavedBuild)).Deserialize(reader);build.Validate();return build; }
  }
  public static string SaveNew(string directory,SavedBuild build) {
   build.Validate();Directory.CreateDirectory(directory);
   string file=Path.Combine(directory,Guid.NewGuid().ToString("N")+".xml");
   string temp=file+".tmp";
   try { using(var writer=XmlWriter.Create(temp,new XmlWriterSettings {Indent=true})) new XmlSerializer(typeof(SavedBuild)).Serialize(writer,build);File.Move(temp,file); }
   finally { if(File.Exists(temp)) File.Delete(temp); }
   return file;
  }
 }
}

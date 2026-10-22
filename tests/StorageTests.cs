using System;
using System.IO;
using System.Collections.Generic;
using SanaQuickItems;
class StorageTests {
 static int passed;
 static void Check(bool ok,string name) {if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);passed++;}
 static void Reject(Action a,string name) {bool rejected=false;try{a();}catch{rejected=true;}Check(rejected,name);}
 static void Main() {
  string folder=Path.Combine(Path.GetTempPath(),"QuickItemsTests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
  var b=new SavedBuild {Name="Крит & скорость",Items=new List<BuildEntry>{new BuildEntry("CritGlasses",10),new BuildEntry("Syringe",20)}};
  string first=BuildStorage.SaveNew(folder,b);var loaded=BuildStorage.Read(first);Check(loaded.Name==b.Name && loaded.Items.Count==2 && loaded.Items[1].Count==20,"XML round trip / Russian name");
  string second=BuildStorage.SaveNew(folder,b);Check(first!=second && File.Exists(first),"save never overwrites an existing build");
  b.Name="../../escaped";string safe=BuildStorage.SaveNew(folder,b);Check(Path.GetDirectoryName(safe)==folder,"display name cannot control file path");
  b.Items[0].Count=1000000;string large=BuildStorage.SaveNew(folder,b);Check(BuildStorage.Read(large).Items[0].Count==1000000,"million stack round trip");b.Items[0].Count=0;Reject(()=>b.Validate(),"zero quantity rejected");b.Items[0].Count=1000001;Reject(()=>b.Validate(),"excess quantity rejected");b.Items[0].Count=10;
  b.Items.Add(new BuildEntry("CritGlasses",1));Reject(()=>b.Validate(),"duplicate items rejected");b.Items.RemoveAt(2);
  b.Name=" ";Reject(()=>b.Validate(),"blank name rejected");b.Name="Build";
  string hostile=Path.Combine(folder,"hostile.xml");File.WriteAllText(hostile,"<!DOCTYPE SavedBuild [<!ENTITY x SYSTEM 'file:///not-read'>]><SavedBuild><Name>&x;</Name></SavedBuild>");Reject(()=>BuildStorage.Read(hostile),"DTD / external entities rejected");
  string malformed=Path.Combine(folder,"bad.xml");File.WriteAllText(malformed,"<SavedBuild>");Reject(()=>BuildStorage.Read(malformed),"malformed XML rejected");
  var empty=new SavedBuild();string emptyFile=BuildStorage.SaveNew(folder,empty);Check(BuildStorage.Read(emptyFile).Items.Count==0,"empty draft can be saved");
  Check(Directory.GetFiles(folder,"*.tmp").Length==0,"no partial files after successful saves");
  Console.WriteLine("TOTAL "+passed+" passed; test files: "+folder);
 }
}

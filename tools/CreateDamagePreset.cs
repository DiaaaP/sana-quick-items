using System;
using System.IO;
using SanaQuickItems;
class CreateDamagePreset {
 static void Main() {
  var b=new SavedBuild {Name="УРОН — разнос"};
  string[] items={"CritGlasses","CritDamage","Crowbar","BossDamageBonus","NearbyDamageBonus","Missile","IceRing","FireRing","Syringe","BleedOnHit","ArmorReductionOnHit","PermanentDebuffOnHit"};
  int[] counts={10,1000,1000,1000,1000,100,100,100,30,10,1,10};
  for(int i=0;i<items.Length;i++)b.Items.Add(new BuildEntry(items[i],counts[i]));
  string folder=@"C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2\BepInEx\config\QuickItemsBuilds";
  string file=BuildStorage.SaveNew(folder,b);var verify=BuildStorage.Read(file);
  if(verify.Name!=b.Name || verify.Items.Count!=12)throw new Exception("Preset validation failed");
  File.Copy(file,@"C:\Users\D\Documents\RoR2-QuickItems\DamagePreset.xml",true);
  Console.WriteLine("Created and verified: "+file);
 }
}

using System;
using System.IO;
using SanaQuickItems;
class ExtremePreset {
 static void Main() {
  var b=new SavedBuild {Name="ЭКСТРИМ — миллионный урон"};
  string[] items={"CritGlasses","CritDamage","Crowbar","BossDamageBonus","NearbyDamageBonus","Missile","IceRing","FireRing","Syringe","BleedOnHit","ArmorReductionOnHit","PermanentDebuffOnHit"};
  int[] counts={10,1000000,1000000,1000000,1000000,1000,1000,1000,30,10,1,100};
  for(int i=0;i<items.Length;i++)b.Items.Add(new BuildEntry(items[i],counts[i]));
  string file=BuildStorage.SaveNew(@"C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2\BepInEx\config\QuickItemsBuilds",b);
  var verified=BuildStorage.Read(file);if(verified.Items[1].Count!=1000000)throw new Exception("Validation failed");
  File.Copy(file,@"C:\Users\D\Documents\RoR2-QuickItems\ExtremeDamagePreset.xml",true);
  Console.WriteLine("Verified preset: "+file);
 }
}

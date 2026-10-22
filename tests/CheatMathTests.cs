using System;
using SanaQuickItems;
class CheatMathTests {
 static int passed;
 static void Check(bool ok,string name) {if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);passed++;}
 static void Main() {
  Check(CheatMath.ScaleDamage(12,1000)==12000,"normal damage multiplier");
  Check(CheatMath.ScaleDamage(1e20f,1e20f)==CheatMath.DamageLimit,"huge damage capped before float overflow");
  Check(CheatMath.ScaleDamage(float.PositiveInfinity,1e20f)==CheatMath.DamageLimit,"infinite incoming value becomes finite");
  Check(CheatMath.ScaleDamage(float.NaN,1000)==0,"NaN cannot propagate");
  Check(CheatMath.ScaleDamage(0,1e20f)==0,"zero damage remains zero");
  Check(CheatMath.ScaleDamage(-10,1000)==0,"negative damage excluded");
  Check(CheatMath.ScaleDamage(12,float.NaN)==12,"invalid multiplier fallback");
  Check(CheatMath.ScaleSpeed(7,3)==21,"movement multiplier");
  Check(CheatMath.ScaleSpeed(100,10)==200,"movement ceiling");
  Check(CheatMath.ScaleSpeed(float.PositiveInfinity,3)==200,"infinite speed capped");
  Check(CheatMath.ScaleSpeed(float.NaN,3)==0,"invalid speed rejected");
  Check(CheatMath.ScaleDamage(1e18f,1e6f)>1e19f,"hyper damage exceeds previous screenshot scale");
  Console.WriteLine("TOTAL "+passed+" passed");
 }
}

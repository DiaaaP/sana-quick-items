using System;
namespace SanaQuickItems {
 public static class CheatMath {
  public const float DamageLimit=1e30f;
  public static float ScaleDamage(float damage,float multiplier) {
   if(float.IsNaN(damage)||damage<=0)return 0;
   if(float.IsNaN(multiplier)||multiplier<1)multiplier=1;
   double result=(double)damage*multiplier;
   return (float)Math.Min(DamageLimit,result);
  }
  public static float ScaleSpeed(float speed,float multiplier) {
   if(float.IsNaN(speed)||speed<0)return 0;
   return (float)Math.Min(200.0,(double)speed*Math.Max(1,multiplier));
  }
 }
}

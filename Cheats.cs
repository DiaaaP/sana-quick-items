using System;
using System.Reflection;
using HarmonyLib;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
namespace SanaQuickItems {
 public sealed partial class QuickItems {
  internal static QuickItems Instance;
  Harmony harmony;
  bool invincible,fastMove,instantSkills,instantEquipment,highDamage,infiniteJumps;
  float moveMultiplier=3f,damageMultiplier=1000000f,nextRecharge;
  CharacterBody trackedBody;
  GenericSkill[] trackedSkills=new GenericSkill[0];
  Run trackedRun;
  bool patchesReady;
  static readonly FieldInfo MoveSpeedField=AccessTools.Field(typeof(CharacterBody),"<moveSpeed>k__BackingField");
  void InitCheats() {
   Instance=this;harmony=new Harmony("local.sana.quickitems.cheats");
   try { if(MoveSpeedField==null)throw new MissingFieldException("CharacterBody.moveSpeed");harmony.PatchAll(typeof(QuickItems).Assembly);patchesReady=true;Logger.LogInfo("Quick Items cheats hooks installed."); }
   catch(Exception e) {harmony.UnpatchSelf();Logger.LogError(e);status="Читы недоступны: ошибка подключения к игре.";}
  }
  bool OwnBody(CharacterBody body) {return enabled && CanAct && body && User!=null && body==User.cachedBody;}
  void CheatsTick() {
   if(trackedRun!=Run.instance) {DisableCheats();trackedRun=Run.instance;trackedBody=null;}
   if(!patchesReady||!CanAct)return;
   CharacterBody body=User.cachedBody;
   if(body!=trackedBody) {trackedBody=body;trackedSkills=body?body.GetComponents<GenericSkill>():new GenericSkill[0];if(body)body.RecalculateStats();}
   if(!body)return;
   if(infiniteJumps && body.characterMotor)body.characterMotor.jumpCount=0;
   if(Time.unscaledTime<nextRecharge)return;nextRecharge=Time.unscaledTime+0.05f;
   if(instantSkills && body.skillLocator)foreach(GenericSkill skill in trackedSkills)if(skill && skill.stock<skill.maxStock)skill.stock=skill.maxStock;
   if(instantEquipment && Inv)Inv.RestockActiveEquipmentCharges(Inv.GetActiveEquipmentMaxCharges());
  }
  void DisableCheats() {
   invincible=fastMove=instantSkills=instantEquipment=highDamage=infiniteJumps=false;
   if(trackedBody)trackedBody.RecalculateStats();
  }
  void DisposeCheats() {DisableCheats();if(harmony!=null)harmony.UnpatchSelf();if(Instance==this)Instance=null;}
  [HarmonyPatch(typeof(CharacterBody),"RecalculateStats")]
  static class MovementHook {
   static void Postfix(CharacterBody __instance) {var p=Instance;if(p!=null && p.fastMove && p.OwnBody(__instance))MoveSpeedField.SetValue(__instance,CheatMath.ScaleSpeed(__instance.moveSpeed,p.moveMultiplier));}
  }
  [HarmonyPatch(typeof(HealthComponent),"TakeDamageProcess")]
  static class DamageHook {
   static bool Prefix(HealthComponent __instance,DamageInfo damageInfo) {
    var p=Instance;if(p==null||!p.enabled||!p.CanAct||damageInfo==null)return true;
    if(p.invincible && p.OwnBody(__instance.body)) {damageInfo.rejected=true;return false;}
    if(p.highDamage && damageInfo.attacker && p.User!=null && p.User.cachedBody) {
     if(damageInfo.attacker==p.User.cachedBody.gameObject || (p.Master && damageInfo.attacker==p.Master.gameObject))damageInfo.damage=CheatMath.ScaleDamage(damageInfo.damage,p.damageMultiplier);
    }
    return true;
   }
  }
  void CheatSwitch(string name,string description,ref bool value) {
   GUILayout.BeginVertical(GUI.skin.box);
   GUILayout.BeginHorizontal();GUILayout.Label(name,titleStyle);bool next=GUILayout.Toggle(value,value?"ВКЛЮЧЕНО":"ВЫКЛЮЧЕНО",GUI.skin.button,GUILayout.Width(145),GUILayout.Height(34));if(next!=value) {value=next;if(trackedBody)trackedBody.RecalculateStats();}
   GUILayout.EndHorizontal();GUILayout.Label(description,textStyle);GUILayout.EndVertical();
  }
  void DrawCheats() {
   GUILayout.Label("СПОСОБНОСТИ БЕЗ ПРЕДМЕТОВ",titleStyle);
   GUILayout.Label("Только твой персонаж · настройки сбрасываются при смене забега",textStyle);
   QuickCheatProfiles();
   GUI.enabled=CanAct && patchesReady;
   GUILayout.BeginHorizontal();GUILayout.BeginVertical(GUILayout.Width(490));
   CheatSwitch("Бессмертие","Блокирует входящий урон. Не отменяет завершение забега.",ref invincible);
   CheatSwitch("Быстрое перемещение","Ускоряет ходьбу и бег. При выключении скорость восстанавливается.",ref fastMove);
   GUILayout.BeginHorizontal();foreach(float f in new[]{2f,3f,5f,10f}) {float value=f;Button("×"+f,()=>{moveMultiplier=value;if(trackedBody)trackedBody.RecalculateStats();});}GUILayout.EndHorizontal();GUILayout.Label("Скорость ×"+moveMultiplier+" · предел 200 м/с",textStyle);
   CheatSwitch("Прыжки в воздухе","Повторно нажимай прыжок, чтобы подниматься выше.",ref infiniteJumps);
   GUILayout.EndVertical();GUILayout.BeginVertical();
   CheatSwitch("Без отката навыков","Возвращает заряды каждые 0,05 с; анимации и особые ресурсы сохраняются.",ref instantSkills);
   CheatSwitch("Без отката экипировки","Автоматически восстанавливает заряды активной экипировки.",ref instantEquipment);
   CheatSwitch("Гиперурон","Умножает урон твоих попаданий. Множитель применяется и к предметным эффектам с твоим авторством.",ref highDamage);
   GUILayout.BeginHorizontal();foreach(float f in new[]{1000f,1e6f,1e12f,1e20f}) {float value=f;Button("×"+f.ToString("0E+0"),()=>damageMultiplier=value);}GUILayout.EndHorizontal();GUILayout.Label("Множитель ×"+damageMultiplier.ToString("0E+0")+" · предел попадания 1E+30",textStyle);
   GUILayout.EndVertical();GUILayout.EndHorizontal();GUI.enabled=true;
   Button("ВЫКЛЮЧИТЬ ВСЕ ЧИТЫ",()=>{DisableCheats();status="Читы выключены. Предметы не изменены.";});
   GUILayout.Label("Гиперурон может превысить диапазон итоговой статистики: рекорд на экране не гарантирует точного измерения.",textStyle);
  }
 }
}


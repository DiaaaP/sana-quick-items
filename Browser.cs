using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using RoR2;
using UnityEngine;
namespace SanaQuickItems {
 public sealed partial class QuickItems {
  ConfigEntry<string> favoriteNames;
  HashSet<string> favorites=new HashSet<string>(StringComparer.Ordinal);
  bool favoritesOnly,dlcOnly;
  int sortMode;
  Vector2 descriptionScroll;
  readonly string[] sorts={"Название","Редкость","Больше в сумке","Дополнение"};
  void InitBrowser() {favoriteNames=Config.Bind("Browser","Favorites","","Favorite item internal names separated by semicolons.");favorites=new HashSet<string>(favoriteNames.Value.Split(new[]{';'},StringSplitOptions.RemoveEmptyEntries),StringComparer.Ordinal);}
  bool NewDlc(ItemDef item) {return item.requiredExpansion && item.requiredExpansion.name.IndexOf("DLC4",StringComparison.OrdinalIgnoreCase)>=0;}
  bool SpecialTier(ItemDef item) {string t=item.tier.ToString();return t=="Cursed"||t=="Hex"||t=="Page"||t=="FoodTier";}
  string TierName(ItemDef item) {
   switch(item.tier.ToString()) {case "Tier1":return "Белый";case "Tier2":return "Зелёный";case "Tier3":return "Красный";case "Lunar":return "Лунный";case "Boss":return "Босс";case "Hex":return "Хэкс";case "Cursed":return "Освящённый";case "Page":return "Страница";case "FoodTier":return "Еда";default:return item.tier.ToString().StartsWith("Void")?"Пустота":item.tier.ToString();}
  }
  List<ItemDef> BrowserItems() {
   IEnumerable<ItemDef> list=items.Where(d=> {
    int owned=Inv?Inv.GetItemCountPermanent(d.itemIndex):0;
    if(tab==1 && owned==0)return false;
    if(tab==0 && (d.isConsumed || d.tier==ItemTier.NoTier))return false;
    if(tab==0 && onlyAvailable && Run.instance && (!ExpansionOK(d)||(!SpecialTier(d)&&!Run.instance.availableItems.Contains(d.itemIndex))))return false;
    if(favoritesOnly&&!favorites.Contains(d.name))return false;
    if(dlcOnly&&!NewDlc(d))return false;
    if(!TierMatch(d))return false;
    return Matches(NameOf(d),d.name)||Matches(Plain(Language.GetString(d.descriptionToken)),"");
   });
   if(sortMode==1)list=list.OrderBy(d=>(int)d.tier).ThenBy(NameOf,StringComparer.CurrentCultureIgnoreCase);
   else if(sortMode==2)list=list.OrderByDescending(d=>Inv?Inv.GetItemCountPermanent(d.itemIndex):0).ThenBy(NameOf,StringComparer.CurrentCultureIgnoreCase);
   else if(sortMode==3)list=list.OrderBy(d=>d.requiredExpansion?d.requiredExpansion.name:"").ThenBy(NameOf,StringComparer.CurrentCultureIgnoreCase);
   else list=list.OrderBy(NameOf,StringComparer.CurrentCultureIgnoreCase);
   return list.ToList();
  }
  void BrowserFilters() {
   tier=GUILayout.SelectionGrid(tier,tiers,6,GUILayout.Height(56));
   GUILayout.BeginHorizontal();sortMode=GUILayout.Toolbar(sortMode,sorts,GUILayout.Height(27));favoritesOnly=GUILayout.Toggle(favoritesOnly,"★ Избранное",GUILayout.Width(130));dlcOnly=GUILayout.Toggle(dlcOnly,"Hallowed Concepts",GUILayout.Width(170));GUILayout.EndHorizontal();
  }
  void FavoriteButton(ItemDef d) {if(GUILayout.Button(favorites.Contains(d.name)?"★":"☆",GUILayout.Width(36),GUILayout.Height(43))) {if(!favorites.Add(d.name))favorites.Remove(d.name);favoriteNames.Value=String.Join(";",favorites.OrderBy(x=>x).ToArray());Config.Save();}}
  void AddDraft(ItemDef d,int count) {int old;draft.TryGetValue(d.name,out old);draft[d.name]=Math.Min(1000000,old+count);}
  void BrowserTools(List<ItemDef> visible) {
   GUILayout.BeginHorizontal();GUILayout.Label("Найдено: "+visible.Count,textStyle,GUILayout.Width(115));
   GUI.enabled=visible.Count>0;
   Button("Случайный в билд",()=>{var pool=visible.Where(d=>d.tier.ToString()!="Hex" || tier==7).ToList();if(pool.Count==0){status="Сначала выбери категорию хэксов.";return;}var item=pool[UnityEngine.Random.Range(0,pool.Count)];AddDraft(item,Quantity);status="В черновик: "+NameOf(item);});
   Button("По 1 из списка в билд",()=>{int n=0;foreach(var d in visible) {if(d.tier.ToString()=="Hex" && tier!=7)continue;AddDraft(d,1);n++;}status="В черновик добавлено типов: "+n+". Выдача — в конструкторе.";});GUI.enabled=true;GUILayout.EndHorizontal();
  }
  void DraftRow(KeyValuePair<string,int> row) {
   var d=ItemCatalog.GetItemDef(ItemCatalog.FindItemIndex(row.Key));GUILayout.BeginHorizontal();SmallIcon(d?IconFor(d):null);GUILayout.Label(d?NameOf(d):row.Key,textStyle,GUILayout.Width(230));
   string next=GUILayout.TextField(row.Value.ToString(),7,GUILayout.Width(90));int parsed;if(Int32.TryParse(next,out parsed))draft[row.Key]=Math.Max(1,Math.Min(1000000,parsed));
   if(GUILayout.Button("−",GUILayout.Width(35))) {if(row.Value>1)draft[row.Key]--;else draft.Remove(row.Key);}if(GUILayout.Button("+",GUILayout.Width(35)))draft[row.Key]=Math.Min(1000000,draft[row.Key]+1);if(GUILayout.Button("×",GUILayout.Width(35)))draft.Remove(row.Key);GUILayout.EndHorizontal();
  }
  void DlcPreset() {
   var b=new SavedBuild {Name="Hallowed Concepts — освящённая имба"};foreach(var d in items)if(d.tier.ToString()=="Cursed"&&!d.isConsumed)b.Items.Add(new BuildEntry(d.name,5));LoadDraft(b);status="Освящённые предметы по 5 загружены в редактор. Хэксы не добавлены.";
  }
  void QuickCheatProfiles() {
   GUILayout.BeginHorizontal();GUI.enabled=CanAct && patchesReady;
   Button("ИМБУЛЯ",()=>{invincible=fastMove=instantSkills=instantEquipment=highDamage=infiniteJumps=true;moveMultiplier=3;damageMultiplier=1e12f;if(trackedBody)trackedBody.RecalculateStats();status="Имбуля: защита, скорость, прыжки, откаты и гиперурон ×10^12.";});
   Button("Спидран",()=>{DisableCheats();fastMove=instantSkills=infiniteJumps=true;moveMultiplier=5;if(trackedBody)trackedBody.RecalculateStats();status="Спидран включён.";});
   Button("Гиперурон MAX",()=>{highDamage=true;damageMultiplier=1e20f;status="Гиперурон ×10^20 включён.";});GUI.enabled=true;GUILayout.EndHorizontal();
  }
  void LogCategories() {foreach(var group in items.GroupBy(d=>d.tier).OrderBy(g=>(int)g.Key))Logger.LogInfo("Item category "+group.Key+": "+group.Count());Logger.LogInfo("Hallowed Concepts browser items: "+items.Count(NewDlc));}
 }
}

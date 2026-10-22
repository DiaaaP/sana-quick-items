using System;
using System.Collections.Generic;
using System.IO;
using Path = System.IO.Path;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Networking;
namespace SanaQuickItems {
 [BepInPlugin("local.sana.quickitems", "Quick Items", "4.0.0")]
 public sealed partial class QuickItems : BaseUnityPlugin {
  ConfigEntry<KeyboardShortcut> shortcut;
  bool opened, onlyAvailable=true;
  MPEventSystem cursorOwner;
  bool cursorCaptured,previousCursorVisible;
  CursorLockMode previousCursorLock;
  Rect window=new Rect(25,25,1040,790);
  Vector2 listScroll,buildScroll,fileScroll,contentScroll;
  string query="",quantityText="1",status="Готово. NumPad 8 открывает и закрывает меню.",buildName="Мой билд";
  int tab=5,tier;
  ItemDef selected;
  EquipmentDef selectedEquipment;
  GUIStyle textStyle,titleStyle,rowStyle;
  List<ItemDef> items=new List<ItemDef>();
  List<EquipmentDef> equipment=new List<EquipmentDef>();
  Dictionary<string,int> draft=new Dictionary<string,int>();
  List<KeyValuePair<string,SavedBuild>> saved=new List<KeyValuePair<string,SavedBuild>>();
  SavedBuild undo;
  CharacterMaster undoMaster,pendingMaster;
  Action pending;
  string pendingLabel;
  readonly string[] tabs={"Предметы","Инвентарь","Конструктор билдов","Экипировка","Бонусы","Читы"};
  readonly string[] tiers={"Все","Белые","Зелёные","Красные","Жёлтые","Лунные","Пустота","Хэксы","Освящённые","Страницы","Еда","Другие"};
  string BuildFolder { get { return Path.Combine(Paths.ConfigPath,"QuickItemsBuilds"); } }
  int Quantity { get { int n; return Int32.TryParse(quantityText,out n)?Math.Max(1,Math.Min(1000000,n)):1; } }
  LocalUser User { get { return LocalUserManager.GetFirstLocalUser(); } }
  CharacterMaster Master { get { return User==null?null:User.cachedMaster; } }
  Inventory Inv { get { return Master?Master.inventory:null; } }
  bool CanAct { get { return Run.instance && NetworkServer.active && Inv; } }
  void Awake() { shortcut=Config.Bind("Controls","OpenMenu",new KeyboardShortcut(KeyCode.Keypad8),"Open or close item menu.");LoadFiles();InitBrowser();InitCheats();StartCoroutine(PreloadIcons());Logger.LogInfo("Quick Items 4.0 ready: inventory, builds, equipment, bonuses. NumPad 8."); }
  void Update() { CheatsTick();if(shortcut.Value.IsDown()) SetOpen(!opened);if(opened && Input.GetKeyDown(KeyCode.Escape)) SetOpen(false);if(opened) { UpdateCursor();if(pending!=null && (pendingMaster!=Master || !CanAct)) pending=null; } }
  void SetOpen(bool value) { opened=value;pending=null;if(value) { CaptureCursor();Catalog();LoadFiles();UpdateCursor(); } else ReleaseCursor(); }
  void UpdateCursor() { ForceCursor();MPEventSystem next=User==null?null:User.eventSystem;if(next==cursorOwner)return;ReleaseCursorOwner();cursorOwner=next;if(cursorOwner)cursorOwner.cursorOpenerCount++; }
  void CaptureCursor() { if(cursorCaptured)return;previousCursorVisible=Cursor.visible;previousCursorLock=Cursor.lockState;cursorCaptured=true; }
  void ForceCursor() {if(!opened)return;CaptureCursor();Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
  void LateUpdate() {if(opened)ForceCursor();}
  void ReleaseCursor() {ReleaseCursorOwner();if(cursorCaptured) {Cursor.lockState=previousCursorLock;Cursor.visible=previousCursorVisible;cursorCaptured=false;}}
  void ReleaseCursorOwner() { if(cursorOwner)cursorOwner.cursorOpenerCount=Math.Max(0,cursorOwner.cursorOpenerCount-1);cursorOwner=null; }
  void OnDisable() { opened=false;ReleaseCursor();DisableCheats(); }
  void OnDestroy() { ReleaseCursor();DisposeCheats();DisposeTheme();DisposeIcons(); }
  static string NameOf(ItemDef d) { return Language.GetString(d.nameToken); }
  static string NameOf(EquipmentDef d) { return Language.GetString(d.nameToken); }
  static string Plain(string s) { return Regex.Replace(s??"","<[^>]+>",""); }
  void Catalog() {
   if(!RoR2Application.loadFinished || items.Count>0 || ItemCatalog.itemCount==0)return;
   for(int i=0;i<ItemCatalog.itemCount;i++) { var d=ItemCatalog.GetItemDef((ItemIndex)i);if(d && !d.hidden)items.Add(d); }
   items.Sort((a,b)=>StringComparer.CurrentCultureIgnoreCase.Compare(NameOf(a),NameOf(b)));
   for(int i=0;i<EquipmentCatalog.equipmentCount;i++) { var d=EquipmentCatalog.GetEquipmentDef((EquipmentIndex)i);if(d && d.canDrop)equipment.Add(d); }
   equipment.Sort((a,b)=>StringComparer.CurrentCultureIgnoreCase.Compare(NameOf(a),NameOf(b)));
  }
  bool Matches(string label,string internalName) { return query.Length==0 || label.IndexOf(query,StringComparison.CurrentCultureIgnoreCase)>=0 || internalName.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0; }
  bool TierMatch(ItemDef d) { string t=d.tier.ToString();return tier==0 || (tier==1&&t=="Tier1") || (tier==2&&t=="Tier2") || (tier==3&&t=="Tier3") || (tier==4&&t=="Boss") || (tier==5&&t=="Lunar") || (tier==6&&t.StartsWith("Void")) || (tier==7&&t=="Hex") || (tier==8&&t=="Cursed") || (tier==9&&t=="Page") || (tier==10&&t=="FoodTier") || (tier==11&&t=="AssignedAtRuntime"); }
  bool ExpansionOK(ItemDef d) { return !d.requiredExpansion || !Run.instance || Run.instance.IsExpansionEnabled(d.requiredExpansion); }
  void Safe(Action action) { try { action(); } catch(Exception e) { status="Ошибка: "+e.Message;Logger.LogError(e); } }
  void Act(Action action) { if(!CanAct) { status="Начни одиночный забег или стань хостом.";return; }Safe(action); }
  void Confirm(string label,Action action) { if(!CanAct)return;pendingLabel=label;pendingMaster=Master;pending=action; }
  SavedBuild Capture() { var b=new SavedBuild {Name=buildName};foreach(var d in items) { int count=Inv.GetItemCountPermanent(d.itemIndex);if(count>0)b.Items.Add(new BuildEntry(d.name,count)); }return b; }
  void Snapshot() { undo=Capture();undoMaster=Master; }
  void ClearItems() { foreach(var d in items) if(Inv.GetItemCountPermanent(d.itemIndex)>0)Inv.ResetItemPermanent(d.itemIndex); }
  SavedBuild DraftBuild() { return new SavedBuild {Name=buildName.Trim(),Items=draft.OrderBy(x=>x.Key).Select(x=>new BuildEntry(x.Key,x.Value)).ToList()}; }
  List<KeyValuePair<ItemDef,int>> Resolve(SavedBuild build) {
   build.Validate();var result=new List<KeyValuePair<ItemDef,int>>();
   foreach(var row in build.Items) { var d=ItemCatalog.GetItemDef(ItemCatalog.FindItemIndex(row.Item));if(!d || d.hidden || !ExpansionOK(d))throw new InvalidOperationException("Предмет недоступен: "+row.Item);result.Add(new KeyValuePair<ItemDef,int>(d,row.Count)); }return result;
  }
  void Apply(SavedBuild build,bool replace) {
   Act(()=>{var rows=Resolve(build);foreach(var row in rows) {if(!replace && (long)Inv.GetItemCountPermanent(row.Key.itemIndex)+row.Value>Int32.MaxValue)throw new InvalidOperationException("Слишком большой суммарный стак: "+row.Key.name);}Snapshot();if(replace)ClearItems();foreach(var row in rows)Inv.GiveItemPermanent(row.Key.itemIndex,row.Value);status=(replace?"Инвентарь заменён: ":"Добавлен билд: ")+build.Name;Logger.LogInfo(status+"; entries="+rows.Count);});
  }
  void LoadFiles() { saved.Clear();try { Directory.CreateDirectory(BuildFolder);foreach(string f in Directory.GetFiles(BuildFolder,"*.xml").OrderBy(x=>x)) { try { saved.Add(new KeyValuePair<string,SavedBuild>(f,BuildStorage.Read(f))); }catch(Exception e) { Logger.LogWarning("Skipped build: "+Path.GetFileName(f)+": "+e.Message); } } }catch(Exception e) {status="Не удалось прочитать билды: "+e.Message;} }
  void LoadDraft(SavedBuild b) { draft=b.Items.ToDictionary(x=>x.Item,x=>x.Count);buildName=b.Name;status="Билд загружен в редактор. Для выдачи нажми «Добавить» или «Заменить»."; }
  void Preset(string name,string[] ids,int[] counts) { var b=new SavedBuild {Name=name};for(int i=0;i<ids.Length;i++) {var d=ItemCatalog.GetItemDef(ItemCatalog.FindItemIndex(ids[i]));if(d && !d.hidden)b.Items.Add(new BuildEntry(ids[i],counts[i]));}LoadDraft(b); }
  void OnGUI() {
   if(!opened)return;ForceCursor();Catalog();
   Theme();var oldSkin=GUI.skin;GUI.skin=menuSkin;
   float scale=Math.Min(1.3f,Math.Min(Screen.width/1100f,Screen.height/850f));var old=GUI.matrix;GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(scale,scale,1));
   window.x=Mathf.Clamp(window.x,0,Math.Max(0,Screen.width/scale-window.width));window.y=Mathf.Clamp(window.y,0,Math.Max(0,Screen.height/scale-window.height));
   try { window=GUI.Window(193761,window,Draw," "); } finally {GUI.matrix=old;GUI.skin=oldSkin;}
  }
  bool Button(string label,Action action) { if(GUILayout.Button(label,GUILayout.Height(30))) {Safe(action);return true;}return false; }
  void Draw(int id) {
   GUILayout.BeginHorizontal();GUILayout.Label("QUICK ITEMS 4  /  ПУЛЬТ УПРАВЛЕНИЯ",titleStyle);Button("Закрыть · NumPad 8",()=>SetOpen(false));GUILayout.EndHorizontal();
   int next=GUILayout.Toolbar(tab,tabs,GUILayout.Height(34));if(next!=tab) {tab=next;query="";listScroll=Vector2.zero;contentScroll=Vector2.zero;pending=null;}
   GUILayout.Label(CanAct?"В ЗАБЕГЕ  ·  Управление своим персонажем":"ГЛАВНОЕ МЕНЮ  ·  Начни забег для включения читов",textStyle);
   GUILayout.Space(8);
   if(pending!=null) {
    GUILayout.Label(pendingLabel,titleStyle);GUILayout.Label("Перед изменением сохранится снимок для кнопки «Отменить последнее изменение». Снимок действует в этом забеге.",textStyle);
    GUILayout.BeginHorizontal();Button("Да, выполнить",()=>{var action=pending;pending=null;Act(action);});Button("Отмена",()=>pending=null);GUILayout.EndHorizontal();GUILayout.Space(400);
   } else {
    contentScroll=GUILayout.BeginScrollView(contentScroll,GUILayout.Height(565));
    if(tab==0 || tab==1)DrawItems();else if(tab==2)DrawBuilds();else if(tab==3)DrawEquipment();else if(tab==4)DrawExtras();else DrawCheats();
    GUILayout.EndScrollView();
   }
   GUILayout.FlexibleSpace();GUILayout.Label(status,textStyle);
   if(!CanAct)GUILayout.Label("Редактировать билды можно в меню. Выдача — в одиночке или у хоста.",textStyle);
   GUI.DragWindow(new Rect(0,0,window.width-150,42));
  }
  void Search() { GUILayout.BeginHorizontal();GUILayout.Label("Поиск",GUILayout.Width(55));query=GUILayout.TextField(query,100,GUILayout.Height(28));Button("Сброс",()=>query="");GUILayout.EndHorizontal(); }
  void Quantities() {
   GUILayout.BeginHorizontal();GUILayout.Label("Количество",GUILayout.Width(95));quantityText=GUILayout.TextField(quantityText,7,GUILayout.Width(75),GUILayout.Height(28));foreach(int n in new[]{1,5,10,20,50,100}) {int count=n;Button(n.ToString(),()=>quantityText=count.ToString());}GUILayout.EndHorizontal();GUILayout.Label("Будет применено: "+Quantity+" · допустимо 1–1000000",textStyle);
  }
  void DrawItems() {
   Search();BrowserFilters();
   if(tab==0)onlyAvailable=GUILayout.Toggle(onlyAvailable," Только доступные в текущем забеге");else GUILayout.Label("Обычные постоянные стаки; временные эффекты и служебные предметы сохраняются.",textStyle);
   var visible=BrowserItems();BrowserTools(visible);
   listScroll=GUILayout.BeginScrollView(listScroll,GUILayout.Height(210));
   foreach(var d in visible) {
    int owned=Inv?Inv.GetItemCountPermanent(d.itemIndex):0;
    GUILayout.BeginHorizontal();FavoriteButton(d);
    var color=GUI.backgroundColor;if(selected==d)GUI.backgroundColor=new Color(.3f,.8f,.7f);
    if(IconButton((selected==d?"> ":"")+NameOf(d)+"   ["+TierName(d)+"]   ×"+owned,IconFor(d))) {selected=d;descriptionScroll=Vector2.zero;}GUI.backgroundColor=color;
    GUILayout.EndHorizontal();
   }GUILayout.EndScrollView();
   descriptionScroll=GUILayout.BeginScrollView(descriptionScroll,GUILayout.Height(65));GUILayout.Label(selected?NameOf(selected)+" — "+Plain(Language.GetString(selected.descriptionToken)):"Выбери предмет, чтобы прочитать эффект",textStyle);GUILayout.EndScrollView();
   if(selected && selected.tier.ToString()=="Hex")GUILayout.Label("ХЭКС — проклятие. Некоторые эффекты затрагивают союзников!",textStyle);
   Quantities();
   GUILayout.BeginHorizontal();GUI.enabled=CanAct && selected;
   Button("Выдать",()=>Act(()=>{if(!ExpansionOK(selected))throw new InvalidOperationException("Дополнение не включено.");if((long)Inv.GetItemCountPermanent(selected.itemIndex)+Quantity>Int32.MaxValue)throw new InvalidOperationException("Превышен предел количества предметов.");Snapshot();Inv.GiveItemPermanent(selected.itemIndex,Quantity);status="Выдано: "+NameOf(selected)+" ×"+Quantity;}));
   Button("Убрать количество",()=>Act(()=>{Snapshot();int n=Math.Min(Quantity,Inv.GetItemCountPermanent(selected.itemIndex));Inv.RemoveItemPermanent(selected.itemIndex,n);status="Убрано: "+n;}));
   Button("Убрать весь стак",()=>{var target=selected;Confirm("Убрать все стаки: "+NameOf(target)+"?",()=>{Snapshot();Inv.ResetItemPermanent(target.itemIndex);status="Стак удалён.";});});
   GUI.enabled=selected;Button("В конструктор +",()=>{int count;draft.TryGetValue(selected.name,out count);draft[selected.name]=Math.Min(1000000,count+Quantity);status="Добавлено в конструктор: "+NameOf(selected);});GUI.enabled=true;GUILayout.EndHorizontal();
   GUILayout.BeginHorizontal();GUI.enabled=CanAct;Button("УБРАТЬ ВСЕ ПРЕДМЕТЫ",()=>Confirm("Очистить постоянные предметы инвентаря?",()=>{Snapshot();ClearItems();status="Инвентарь очищен. Доступна отмена.";}));GUI.enabled=CanAct && undo!=null && undoMaster==Master;Button("Отменить последнее изменение",()=>Act(()=>{var previous=undo;var current=Capture();var resolved=new List<KeyValuePair<ItemDef,int>>();foreach(var row in previous.Items) {var d=ItemCatalog.GetItemDef(ItemCatalog.FindItemIndex(row.Item));if(!d)throw new InvalidOperationException("Предмет снимка не найден: "+row.Item);resolved.Add(new KeyValuePair<ItemDef,int>(d,row.Count));}ClearItems();foreach(var row in resolved)Inv.GiveItemPermanent(row.Key.itemIndex,row.Value);undo=current;undoMaster=Master;status="Предыдущее состояние предметов восстановлено.";}));GUI.enabled=true;GUILayout.EndHorizontal();
  }
  void DrawBuilds() {
   GUILayout.BeginHorizontal();GUILayout.Label("Название",GUILayout.Width(80));buildName=GUILayout.TextField(buildName,80,GUILayout.Height(28));Button("Сохранить копию",()=>{BuildStorage.SaveNew(BuildFolder,DraftBuild());LoadFiles();status="Билд сохранён на диск.";});GUILayout.EndHorizontal();
   GUILayout.BeginHorizontal();GUI.enabled=CanAct;Button("Считать текущий инвентарь",()=>LoadDraft(Capture()));GUI.enabled=true;Button("Очистить черновик",()=>{draft.Clear();status="Черновик очищен. Инвентарь не изменён.";});GUILayout.EndHorizontal();
   GUILayout.BeginHorizontal();
   GUILayout.BeginVertical(GUILayout.Width(535));GUILayout.Label("Состав: "+draft.Count+" типов / "+draft.Values.Sum(x=>(long)x)+" предметов",textStyle);
   buildScroll=GUILayout.BeginScrollView(buildScroll,GUILayout.Height(245));
   foreach(var row in draft.ToArray()) DraftRow(row);GUILayout.EndScrollView();GUILayout.EndVertical();
   GUILayout.BeginVertical();GUILayout.Label("Сохранённые билды",textStyle);fileScroll=GUILayout.BeginScrollView(fileScroll,GUILayout.Height(245));foreach(var row in saved) {var b=row.Value;if(GUILayout.Button(b.Name+" ("+b.Items.Count+")",rowStyle))LoadDraft(b);}GUILayout.EndScrollView();GUILayout.EndVertical();GUILayout.EndHorizontal();
   GUILayout.Label("Быстрые наборы — сначала загружаются в редактор:",textStyle);GUILayout.BeginHorizontal();
   Button("Крит + скорость",()=>Preset("Крит и скорость",new[]{"CritGlasses","Syringe","Hoof"},new[]{10,10,5}));
   Button("Мобильность",()=>Preset("Мобильность",new[]{"Hoof","Feather","SprintBonus"},new[]{5,3,3}));
   Button("Выживание",()=>Preset("Выживание",new[]{"ArmorPlate","Bear","ExtraLife"},new[]{5,10,1}));Button("DLC: освящённая имба",DlcPreset);GUILayout.EndHorizontal();
   GUILayout.Label("Добавляй предметы кнопкой «В конструктор +» на вкладке «Предметы». Билды содержат постоянные предметы; экипировка настраивается отдельно.",textStyle);
   GUILayout.BeginHorizontal();GUI.enabled=CanAct && draft.Count>0;Button("Добавить билд к инвентарю",()=>Apply(DraftBuild(),false));Button("Заменить инвентарь билдом",()=>{var b=DraftBuild();Resolve(b);Confirm("Заменить текущие предметы билдом «"+b.Name+"»?",()=>Apply(b,true));});GUI.enabled=true;GUILayout.EndHorizontal();
  }
  void DrawEquipment() {
   Search();listScroll=GUILayout.BeginScrollView(listScroll,GUILayout.Height(360));foreach(var d in equipment) {if(!Matches(NameOf(d),d.name))continue;if(d.requiredExpansion && Run.instance && !Run.instance.IsExpansionEnabled(d.requiredExpansion))continue;if(IconButton((selectedEquipment==d?"> ":"")+NameOf(d),IconFor(d)))selectedEquipment=d;}GUILayout.EndScrollView();
   GUILayout.Label(selectedEquipment?NameOf(selectedEquipment)+" — "+Plain(Language.GetString(selectedEquipment.pickupToken)):"Выбери экипировку",textStyle,GUILayout.Height(55));
   GUILayout.BeginHorizontal();GUI.enabled=CanAct && selectedEquipment;Button("Выдать в активный слот",()=>Act(()=>{if(selectedEquipment.requiredExpansion && !Run.instance.IsExpansionEnabled(selectedEquipment.requiredExpansion))throw new InvalidOperationException("Дополнение не включено.");var location=Inv.activeEquipmentLocation;Inv.SetEquipmentIndex(selectedEquipment.equipmentIndex,ref location);status="Выдано: "+NameOf(selectedEquipment);}));GUI.enabled=CanAct;Button("Убрать из активного слота",()=>Act(()=>{var location=Inv.activeEquipmentLocation;Inv.SetEquipmentIndex(EquipmentIndex.None,ref location);status="Активный слот очищен.";}));Button("Восстановить заряды",()=>Act(()=>{Inv.RestockActiveEquipmentCharges(Inv.GetActiveEquipmentMaxCharges());status="Заряды восстановлены.";}));GUI.enabled=true;GUILayout.EndHorizontal();
  }
  void DrawExtras() {
   GUILayout.Label("Бонусы для текущего забега",titleStyle);GUILayout.Label("Деньги получает только твой персонаж. Лунные монеты профиля не меняются.",textStyle);GUI.enabled=CanAct;
   Button("+1 000 золота",()=>Act(()=>{Master.GiveMoney(1000u,false);status="Добавлено 1000 золота.";}));Button("+10 000 золота",()=>Act(()=>{Master.GiveMoney(10000u,false);status="Добавлено 10000 золота.";}));
   Button("Восстановить здоровье",()=>Act(()=>{var body=User.cachedBody;if(!body || !body.healthComponent)throw new InvalidOperationException("Персонаж сейчас не активен.");body.healthComponent.HealFraction(1f,default(ProcChainMask));status="Здоровье восстановлено.";}));GUI.enabled=true;
   GUILayout.Space(12);GUI.enabled=CanAct;
   Button("Снять все хэксы",()=>Confirm("Удалить все постоянные хэксы у твоего персонажа?",()=>{Snapshot();int total=0;foreach(var d in items)if(d.tier.ToString()=="Hex") {total+=Inv.GetItemCountPermanent(d.itemIndex);Inv.ResetItemPermanent(d.itemIndex);}status="Удалено хэксов: "+total+". Отмена доступна в инвентаре.";}));
   Button("Полное лечение + восстановить щит",()=>Act(()=>{var body=User.cachedBody;if(!body || !body.healthComponent)throw new InvalidOperationException("Персонаж сейчас не активен.");body.healthComponent.HealFraction(1f,default(ProcChainMask));body.healthComponent.RechargeShieldFull();status="Здоровье и щит восстановлены.";}));GUI.enabled=true;
   GUILayout.Space(18);GUILayout.Label("Папка с билдами",textStyle);GUILayout.TextArea(BuildFolder);GUILayout.Label("Каждое сохранение создаёт отдельный XML-файл. Билды сохраняются между запусками игры. Меню: NumPad 8, закрытие: NumPad 8 / Esc.",textStyle);
  }
 }
}




using System;
using System.Collections;
using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
namespace SanaQuickItems {
 public sealed partial class QuickItems {
  readonly Dictionary<string,AsyncOperationHandle<Sprite>> iconLoads=new Dictionary<string,AsyncOperationHandle<Sprite>>();
  readonly HashSet<string> iconErrors=new HashSet<string>();
  Sprite IconFor(ItemDef def) {return def?LoadIcon(def.GetIcon(),def.GetIconReference()):null;}
  Sprite IconFor(EquipmentDef def) {return def?LoadIcon(def.GetIcon(),def.GetIconReference()):null;}
  Sprite LoadIcon(Sprite ready,AssetReferenceT<Sprite> reference) {
   if(ready)return ready;
   if(reference==null || !reference.RuntimeKeyIsValid())return null;
   string key=reference.RuntimeKey.ToString();AsyncOperationHandle<Sprite> handle;
   try {
    if(!iconLoads.TryGetValue(key,out handle)) {handle=Addressables.LoadAssetAsync<Sprite>(reference.RuntimeKey);iconLoads.Add(key,handle);}
    if(handle.IsDone && handle.Status==AsyncOperationStatus.Succeeded)return handle.Result;
    if(handle.IsDone && handle.Status==AsyncOperationStatus.Failed && iconErrors.Add(key))Logger.LogWarning("Icon load failed: "+key+" "+handle.OperationException);
   }catch(Exception e) {if(iconErrors.Add(key))Logger.LogWarning("Icon load error: "+key+" "+e.Message);}
   return null;
  }
  IEnumerator PreloadIcons() {
   while(!RoR2Application.loadFinished || ItemCatalog.itemCount==0 || EquipmentCatalog.equipmentCount==0)yield return null;
   Catalog();LogCategories();
   for(int i=0;i<items.Count;i++) {IconFor(items[i]);if(i%16==0)yield return null;}
   for(int i=0;i<equipment.Count;i++) {IconFor(equipment[i]);if(i%16==0)yield return null;}
   float deadline=Time.realtimeSinceStartup+45f;
   while(Time.realtimeSinceStartup<deadline) {bool pending=false;foreach(var h in iconLoads.Values)if(!h.IsDone)pending=true;if(!pending)break;yield return null;}
   int available=0;foreach(var item in items)if(IconFor(item))available++;
   int equips=0;foreach(var item in equipment)if(IconFor(item))equips++;
   Logger.LogInfo("Icon preload complete: items "+available+"/"+items.Count+", equipment "+equips+"/"+equipment.Count);
  }
  void DisposeIcons() {foreach(var h in iconLoads.Values)if(h.IsValid())Addressables.Release(h);iconLoads.Clear();}
 }
}


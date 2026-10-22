using UnityEngine;
namespace SanaQuickItems {
 public sealed partial class QuickItems {
  GUISkin menuSkin;
  Texture2D panelTexture,cardTexture,buttonTexture,hoverTexture,activeTexture;
  Texture2D Solid(Color color) {var t=new Texture2D(1,1);t.SetPixel(0,0,color);t.Apply();return t;}
  void Theme() {
   if(menuSkin)return;
   menuSkin=Instantiate(GUI.skin);
   panelTexture=Solid(new Color(.055f,.07f,.10f,.98f));cardTexture=Solid(new Color(.09f,.115f,.16f,1));buttonTexture=Solid(new Color(.13f,.17f,.23f,1));hoverTexture=Solid(new Color(.19f,.28f,.34f,1));activeTexture=Solid(new Color(.08f,.40f,.36f,1));
   menuSkin.window.normal.background=panelTexture;menuSkin.window.onNormal.background=panelTexture;menuSkin.window.padding=new RectOffset(18,18,15,15);
   menuSkin.box.normal.background=cardTexture;menuSkin.box.padding=new RectOffset(12,12,9,9);menuSkin.box.margin=new RectOffset(4,4,5,5);
   foreach(var s in new[]{menuSkin.button,menuSkin.textField,menuSkin.textArea}) {
    s.normal.background=buttonTexture;s.hover.background=hoverTexture;s.active.background=activeTexture;s.focused.background=hoverTexture;s.onNormal.background=activeTexture;s.onHover.background=activeTexture;
    s.fontSize=15;s.normal.textColor=new Color(.90f,.95f,1f);s.hover.textColor=Color.white;s.active.textColor=Color.white;s.onNormal.textColor=Color.white;s.padding=new RectOffset(10,10,5,5);
   }
   menuSkin.label.normal.textColor=new Color(.83f,.90f,.96f);menuSkin.label.fontSize=15;
   menuSkin.toggle.normal.textColor=menuSkin.label.normal.textColor;menuSkin.toggle.onNormal.textColor=Color.white;menuSkin.toggle.fontSize=14;
   textStyle=new GUIStyle(menuSkin.label){fontSize=15,wordWrap=true};
   titleStyle=new GUIStyle(textStyle){fontSize=20,fontStyle=FontStyle.Bold};titleStyle.normal.textColor=new Color(.30f,.94f,.78f);
   rowStyle=new GUIStyle(menuSkin.button){fontSize=15,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(10,10,5,5)};
  }

  void DrawSprite(Rect rect,Sprite sprite) {
   if(!sprite || !sprite.texture) {GUI.Label(rect,"?");return;}
   Rect region=sprite.textureRect;
   Rect uv=new Rect(region.x/sprite.texture.width,region.y/sprite.texture.height,region.width/sprite.texture.width,region.height/sprite.texture.height);
   float aspect=region.width/region.height;
   if(aspect>1) {float height=rect.width/aspect;rect.y+=(rect.height-height)/2;rect.height=height;}
   else {float width=rect.height*aspect;rect.x+=(rect.width-width)/2;rect.width=width;}
   GUI.DrawTextureWithTexCoords(rect,sprite.texture,uv,true);
  }
  bool IconButton(string label,Sprite sprite) {
   Rect rect=GUILayoutUtility.GetRect(new GUIContent(label),rowStyle,GUILayout.Height(43),GUILayout.ExpandWidth(true));
   bool clicked=GUI.Button(rect,GUIContent.none,rowStyle);
   DrawSprite(new Rect(rect.x+6,rect.y+4,35,35),sprite);
   GUI.Label(new Rect(rect.x+49,rect.y+7,rect.width-55,rect.height-10),label,textStyle);
   return clicked;
  }
  void SmallIcon(Sprite sprite) {Rect rect=GUILayoutUtility.GetRect(28,28,GUILayout.Width(28),GUILayout.Height(28));DrawSprite(rect,sprite);}
  void DisposeTheme() {foreach(var t in new[]{panelTexture,cardTexture,buttonTexture,hoverTexture,activeTexture})if(t)Destroy(t);if(menuSkin)Destroy(menuSkin);}
 }
}

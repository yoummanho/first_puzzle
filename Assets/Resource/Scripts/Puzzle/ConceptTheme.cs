using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstPuzzle
{
    /// <summary>Image-driven cream/ink skin. Existing gameplay, labels and button events remain the source of truth.</summary>
    public sealed class ConceptTheme : MonoBehaviour
    {
        public static readonly Color Cream = Color.white;
        public static readonly Color Background = new Color32(248, 249, 250, 255);
        public static readonly Color Ink = new Color32(30, 30, 36, 255);
        public static readonly Color Green = new Color32(56, 229, 77, 255);
        public static readonly Color Pink = new Color32(255, 51, 102, 255);
        public static readonly Color Cyan = new Color32(0, 210, 255, 255);
        public static readonly Color Amber = new Color32(255, 190, 11, 255);
        public static readonly Color Surface = new Color32(243, 244, 246, 255);
        public static readonly Color Locked = new Color32(209, 213, 219, 255);
        private static TMP_FontAsset displayFont;
        private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        private PuzzleGame game;
        private RectTransform undo, preview, fan;
        private TMP_Text undoCount;
        private UnityEngine.UI.Button undoAction;
        private PlayOrientation orientation = (PlayOrientation)(-1);

        public static Sprite Load(string key)
        {
            if (sprites.TryGetValue(key, out var cached)) return cached;
            var texture = Resources.Load<Texture2D>(AssetPath(key));
            if (texture == null) { sprites[key] = null; return null; }
            var border = key == "panel" || key == "pill" || key == "tile" ? new Vector4(48, 48, 48, 48) : Vector4.zero;
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f,.5f), 100, 0, SpriteMeshType.FullRect, border);
            sprites[key] = sprite;
            return sprite;
        }

        public static string AssetPath(string key)
        {
            string folder = key.StartsWith("icon_") ? "Buttons/Icons/" :
                key.StartsWith("label_") ? "Buttons/Labels/" :
                key == "background" ? "Backgrounds/" : key == "logo" ? "Brand/" : "Surfaces/";
            return "ConceptUI/" + folder + key;
        }

        public static void StyleText(TMP_Text label)
        {
            if (displayFont == null)
            {
                var latin = Resources.Load<Font>("ConceptUI/Fonts/LilitaOne-Regular");
                var korean = Resources.Load<Font>("ConceptUI/Fonts/Jua-Regular");
                if (latin != null)
                {
                    displayFont = TMP_FontAsset.CreateFontAsset(latin);
                    displayFont.name = "Concept Lilita One";
                    displayFont.fallbackFontAssetTable = new List<TMP_FontAsset>();
                    if (korean != null)
                        displayFont.fallbackFontAssetTable.Add(TMP_FontAsset.CreateFontAsset(korean));
                    var material = displayFont.material;
                    material.EnableKeyword(ShaderUtilities.Keyword_Outline);
                    material.SetColor(ShaderUtilities.ID_OutlineColor, Ink);
                    material.SetFloat(ShaderUtilities.ID_OutlineWidth, .4f);
                    material.SetFloat(ShaderUtilities.ID_FaceDilate, .1f);
                    material.EnableKeyword("UNDERLAY_ON");
                    material.SetColor("_UnderlayColor", Ink);
                    material.SetFloat("_UnderlayOffsetY", -.5f);
                    material.SetFloat("_UnderlaySoftness", 0f);
                }
            }
            if (displayFont != null) { label.font = displayFont; label.fontSharedMaterial = displayFont.material; }
            label.color = Color.white;
            label.fontStyle = FontStyles.Normal;
            label.extraPadding = true;
        }

        public static void Anchors(RectTransform rect, float x0, float y0, float x1, float y1)
        {
            rect.anchorMin = new Vector2(x0,y0); rect.anchorMax = new Vector2(x1,y1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        public static UnityEngine.UI.Image Art(string name, Transform parent, string sprite, float x0, float y0, float x1, float y1)
        {
            var node = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            node.transform.SetParent(parent,false);
            var img = node.GetComponent<UnityEngine.UI.Image>();
            Anchors(img.rectTransform,x0,y0,x1,y1);
            img.sprite = sprite == null ? null : Load(sprite);
            img.preserveAspect = true; img.raycastTarget = false;
            return img;
        }

        public static void Skin(UnityEngine.UI.Image image, Color color, string sprite = "panel")
        {
            if (image == null) return;
            image.sprite = Load(sprite); image.type = UnityEngine.UI.Image.Type.Sliced;
            image.preserveAspect = false; image.color = color;
            // Effects live outside the tinted fill, keeping the charcoal outline neutral.
            var shadow = image.GetComponent<UnityEngine.UI.Shadow>();
            if (shadow == null || shadow is UnityEngine.UI.Outline)
                shadow = image.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            shadow.effectColor = Ink;
            shadow.effectDistance = new Vector2(0, -6);
            shadow.useGraphicAlpha = true;
            var outline = image.GetComponent<UnityEngine.UI.Outline>();
            if (outline == null) outline = image.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = Ink;
            outline.effectDistance = new Vector2(3, -3);
            outline.useGraphicAlpha = true;
        }

        public static void Install(PuzzleGame owner)
        {
            var theme = owner.GetComponent<ConceptTheme>();
            if (theme != null) return;
            theme = owner.gameObject.AddComponent<ConceptTheme>();
            theme.game = owner;
            theme.Build();
        }

        private void Build()
        {
            // Full-screen backdrop is intentionally outside SafeArea; all controls stay inside it.
            var bg = game.transform.Find("Background");
            if (bg != null)
            {
                var raw = bg.GetComponent<RawImage>();
                if (raw != null) { raw.texture = Resources.Load<Texture2D>(AssetPath("background")); raw.color = Color.white; }
                var image = bg.GetComponent<UnityEngine.UI.Image>();
                if (image != null) { image.sprite = Load("background"); image.color = Color.white; }
            }
            foreach (var label in game.GetComponentsInChildren<TMP_Text>(true))
            {
                StyleText(label);
                if (label.GetComponent<ConceptImageLabel>() == null) label.gameObject.AddComponent<ConceptImageLabel>().Initialize(label);
            }
            foreach (var button in game.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                Skin(button.GetComponent<UnityEngine.UI.Image>(), button.name == "PlayButton" ? Green : Cream);
                var colors = button.colors;
                colors.normalColor = colors.highlightedColor = colors.selectedColor = Color.white;
                colors.pressedColor = new Color(.85f, .85f, .85f, 1);
                colors.disabledColor = new Color(.82f, .84f, .86f, 1);
                button.colors = colors;
            }
            Skin(game.pauseButton.GetComponent<UnityEngine.UI.Image>(), Cream, "pill");
            var hamburger = game.pauseButton.transform.Find("HamburgerIcon");
            if (hamburger != null) hamburger.gameObject.SetActive(false);
            Art("PauseIcon",game.pauseButton.transform,"icon_pause",.12f,.12f,.88f,.88f);
            var curtain = game.curtain.GetComponent<UnityEngine.UI.Image>();
            if (curtain != null) curtain.color = Background;
            Plate(game.heading, Cream); Plate(game.stats, Amber);
            game.heading.margin = new Vector4(20,10,20,10);
            game.stats.margin = new Vector4(35,8,25,8);
            var content = game.ResponsiveLayout.ContentRect;
            var instruction = content.Find("Instructions")?.GetComponent<TMP_Text>();
            if(instruction != null) { instruction.text="Slide a tile into the empty space."; Plate(instruction,Ink); }
            var footer=content.Find("Footer"); if(footer != null)footer.gameObject.SetActive(false);
            var frame=game.board.transform.parent.Find("ImageFrame")?.GetComponent<UnityEngine.UI.Image>();
            Skin(frame,Cream);
            undo=Control("HudUndo",content,Green,"icon_undo");
            undoAction=undo.gameObject.AddComponent<UnityEngine.UI.Button>(); undoAction.targetGraphic=undo.GetComponentInChildren<UnityEngine.UI.Image>();
            undoAction.onClick.AddListener(game.Undo);
            var undoColors=undoAction.colors;
            undoColors.disabledColor=new Color(.82f,.84f,.86f,1);
            undoColors.pressedColor=new Color(.85f,.85f,.85f,1);
            undoAction.colors=undoColors;
            var badge=Art("Remaining",undo,"pill",.65f,.70f,1.05f,1.10f); Skin(badge,Amber,"pill");
            undoCount=new GameObject("Count",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            undoCount.transform.SetParent(badge.transform,false); Anchors(undoCount.rectTransform,0,0,1,1);
            undoCount.font=TMP_Settings.defaultFontAsset; undoCount.fontSize=36; undoCount.alignment=TextAlignmentOptions.Center;
            StyleText(undoCount); undoCount.raycastTarget=false;
            preview=Control("HudPreview",content,Pink,"icon_preview");
            preview.gameObject.AddComponent<HoldPreview>().game=game;
            // Match reference title using the project's own artwork rather than baking UI into a screen-sized image.
            var title=game.Lobby.transform.Find("TitlePanel/TitleCard");
            if(title != null)
            {
                var main=title.Find("MainTitle"); main.GetComponent<TMP_Text>().enabled=false;
                var old=main.Find("ImageLettering"); if(old != null) old.gameObject.SetActive(false);
                Art("TitleLogo",main,"logo",0,0,1,1);
                var sub=title.Find("SubTitle")?.GetComponent<TMP_Text>(); if(sub != null) Plate(sub,Ink);
                Skin(title.Find("StartButton").GetComponent<UnityEngine.UI.Image>(),Green,"pill");
                fan=new GameObject("IllustrationCards",typeof(RectTransform)).GetComponent<RectTransform>(); fan.SetParent(title,false);
                var composition = new GameObject("CardComposition", typeof(RectTransform), typeof(AspectRatioFitter)).GetComponent<RectTransform>();
                composition.SetParent(fan, false);
                var compositionFit = composition.GetComponent<AspectRatioFitter>();
                compositionFit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                compositionFit.aspectRatio = 1.2f;
                for(int i=0;i<Mathf.Min(3, game.stages.Length);i++)
                {
                    var card=Art("CollectibleCard"+i,composition,"panel",.18f+i*.09f,.08f,.66f+i*.09f,.92f);
                    Skin(card,Cream); card.rectTransform.localEulerAngles=new Vector3(0,0,i==0?18:i==1?-18:0);
                    var raw=new GameObject("Artwork",typeof(RectTransform),typeof(RawImage)).GetComponent<RawImage>(); raw.transform.SetParent(card.transform,false);
                    Anchors(raw.rectTransform,.09f,.07f,.91f,.93f); raw.texture=game.stages[i].Illustration; raw.raycastTarget=false;
                    raw.uvRect=new Rect(0,0,1,1);
                    var fit=raw.gameObject.AddComponent<AspectRatioFitter>();
                    fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;
                    fit.aspectRatio=(float)raw.texture.width/raw.texture.height;
                }
            }
            foreach(var img in game.Lobby.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                if(img.name=="ThumbFrame") Skin(img,Surface);
                if(img.name=="GalleryButton")Skin(img,Cyan);
            }
            Skin(game.nextButton?.GetComponent<UnityEngine.UI.Image>(), Green, "pill");
            IconButton(game.Lobby.transform,"PrevPage","icon_prev"); IconButton(game.Lobby.transform,"NextPage","icon_next");
            IconButton(game.Lobby.transform,"CloseViewer","icon_close");
            ApplyLayout();
        }
        private static void Plate(TMP_Text label,Color color)
        {
            var plate=Art("Plate",label.transform,"pill",0,0,1,1); Skin(plate,color,"pill"); plate.transform.SetAsFirstSibling();
            // Child images draw above their parent's text, so place plate behind the text as a sibling.
            plate.transform.SetParent(label.transform.parent,false);
            plate.rectTransform.anchorMin=label.rectTransform.anchorMin; plate.rectTransform.anchorMax=label.rectTransform.anchorMax;
            plate.rectTransform.offsetMin=label.rectTransform.offsetMin; plate.rectTransform.offsetMax=label.rectTransform.offsetMax;
            plate.transform.SetSiblingIndex(label.transform.GetSiblingIndex());
            var follow=plate.gameObject.AddComponent<ConceptPlate>(); follow.target=label.rectTransform;
        }
        private static RectTransform Control(string name,Transform parent,Color color,string icon)
        {
            var root=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); root.SetParent(parent,false);
            var image=Art("Face",root,"circle",0,0,1,1); Skin(image,color,"circle"); image.preserveAspect=true; image.type=UnityEngine.UI.Image.Type.Simple; image.raycastTarget=true;
            var aspect=image.gameObject.AddComponent<AspectRatioFitter>();aspect.aspectMode=AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio=1;
            Art("Icon",image.transform,icon,.17f,.17f,.83f,.83f);return root;
        }
        private static void IconButton(Transform root,string name,string icon)
        {
            foreach(var button in root.GetComponentsInChildren<UnityEngine.UI.Button>(true)) if(button.name==name)
            {
                foreach(var label in button.GetComponentsInChildren<TMP_Text>(true))label.gameObject.SetActive(false);
                Skin(button.GetComponent<UnityEngine.UI.Image>(),Cream,"pill");
                Art("Icon",button.transform,icon,.20f,.12f,.80f,.88f);
            }
        }
        private void LateUpdate()
        {
            if(game==null)return;
            if(orientation!=game.Settings.Orientation)ApplyLayout();
            undoCount.text=game.board.UndoRemaining.ToString();
            undoAction.interactable=game.board.CanUndo || game.board.CanRequestUndoReward;
        }
        public void ApplyLayout()
        {
            if(game==null || undo==null || preview==null)return;
            orientation=game.Settings.Orientation; bool wide=orientation==PlayOrientation.Landscape;
            Anchors(undo,wide?.67f:.02f,wide?.20f:.115f,wide?.80f:.27f,wide?.40f:.235f);
            Anchors(preview,wide?.84f:.73f,wide?.20f:.115f,wide?.97f:.98f,wide?.40f:.235f);
            var title=game.Lobby.transform.Find("TitlePanel/TitleCard");
            if(title!=null)
            {
                Anchors((RectTransform)title.Find("MainTitle"),wide?.06f:.08f,wide?.58f:.72f,wide?.52f:.92f,wide?.90f:.93f);
                Anchors((RectTransform)title.Find("SubTitle"),wide?.06f:.10f,wide?.43f:.62f,wide?.52f:.90f,wide?.55f:.70f);
                Anchors((RectTransform)title.Find("StartButton"),wide?.12f:.18f,wide?.14f:.10f,wide?.46f:.82f,wide?.32f:.20f);
                Anchors(fan,wide?.54f:.08f,wide?.10f:.24f,wide?.94f:.92f,wide?.90f:.60f);
            }
        }
    }
}

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstPuzzle
{
    /// <summary>Image-driven cream/ink skin. Existing gameplay, labels and button events remain the source of truth.</summary>
    public sealed class ConceptTheme : MonoBehaviour
    {
        public static readonly Color Cream = new Color32(247, 243, 226, 255);
        public static readonly Color Ink = new Color32(18, 18, 16, 255);
        public static readonly Color Green = new Color32(111, 211, 103, 255);
        public static readonly Color Pink = new Color32(255, 114, 165, 255);
        private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        private PuzzleGame game;
        private RectTransform undo, preview, fan;
        private TMP_Text undoCount;
        private UnityEngine.UI.Button undoAction;
        private PlayOrientation orientation = (PlayOrientation)(-1);

        public static Sprite Load(string key)
        {
            if (sprites.TryGetValue(key, out var cached)) return cached;
            var texture = Resources.Load<Texture2D>("ConceptUI/" + key);
            if (texture == null) { sprites[key] = null; return null; }
            var border = key == "panel" || key == "pill" || key == "tile" ? new Vector4(48, 48, 48, 48) : Vector4.zero;
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f,.5f), 100, 0, SpriteMeshType.FullRect, border);
            sprites[key] = sprite;
            return sprite;
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
                if (raw != null) { raw.texture = Resources.Load<Texture2D>("ConceptUI/background"); raw.color = Color.white; }
                var image = bg.GetComponent<UnityEngine.UI.Image>();
                if (image != null) { image.sprite = Load("background"); image.color = Color.white; }
            }
            foreach (var label in game.GetComponentsInChildren<TMP_Text>(true))
            {
                label.color = Ink; label.fontStyle |= FontStyles.Bold;
                if (label.GetComponent<ConceptImageLabel>() == null) label.gameObject.AddComponent<ConceptImageLabel>().Initialize(label);
            }
            foreach (var button in game.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                Skin(button.GetComponent<UnityEngine.UI.Image>(), Cream);
            Skin(game.pauseButton.GetComponent<UnityEngine.UI.Image>(), Cream, "pill");
            var hamburger = game.pauseButton.transform.Find("HamburgerIcon");
            if (hamburger != null) hamburger.gameObject.SetActive(false);
            Art("PauseIcon",game.pauseButton.transform,"icon_pause",.12f,.12f,.88f,.88f);
            var curtain = game.curtain.GetComponent<UnityEngine.UI.Image>();
            if (curtain != null) curtain.color = new Color32(250,250,243,255);
            Plate(game.heading, Cream); Plate(game.stats, new Color32(255,218,44,255));
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
            var badge=Art("Remaining",undo,"pill",.65f,.70f,1.05f,1.10f); badge.color=Cream;
            undoCount=new GameObject("Count",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            undoCount.transform.SetParent(badge.transform,false); Anchors(undoCount.rectTransform,0,0,1,1);
            undoCount.font=TMP_Settings.defaultFontAsset; undoCount.fontSize=36; undoCount.alignment=TextAlignmentOptions.Center;
            undoCount.fontStyle=FontStyles.Bold; undoCount.color=Ink; undoCount.raycastTarget=false;
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
                for(int i=0;i<3;i++)
                {
                    var card=Art("CollectibleCard"+i,fan,"panel",.18f+i*.09f,.08f,.66f+i*.09f,.92f);
                    Skin(card,Cream); card.rectTransform.localEulerAngles=new Vector3(0,0,i==0?18:i==1?-18:0);
                    var raw=new GameObject("Artwork",typeof(RectTransform),typeof(RawImage)).GetComponent<RawImage>(); raw.transform.SetParent(card.transform,false);
                    Anchors(raw.rectTransform,.09f,.07f,.91f,.93f); raw.texture=game.stages[i].Illustration; raw.raycastTarget=false;
                    raw.uvRect=game.stages[i].GetImageUV();
                }
            }
            foreach(var img in game.Lobby.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                if(img.name=="ThumbFrame") img.color=new Color32(233,233,228,255);
                if(img.name=="GalleryButton")Skin(img,new Color32(118,211,237,255));
            }
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
            var image=Art("Face",root,"circle",0,0,1,1); image.color=color; image.raycastTarget=true;
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
            Anchors(undo,wide?.67f:.02f,wide?.20f:.09f,wide?.80f:.27f,wide?.40f:.23f);
            Anchors(preview,wide?.84f:.73f,wide?.20f:.09f,wide?.97f:.98f,wide?.40f:.23f);
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

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using System.Linq;
using System.IO;

namespace ProteinWeb.Editor
{
    public static class WebSceneBuilder
    {
        const string Root="Assets/WebVersion/";
        static Font font;
        static Sprite ring;
        static Material Mat(string name,Color color)
        {
            string path=Root+"Materials/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m) {m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.color=color; return m;
        }
        static GameObject Shape(string name,PrimitiveType type,Transform parent,Vector3 pos,Vector3 scale,Material mat,bool collider=false)
        {
            var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);
            g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;
            if(!collider) Object.DestroyImmediate(g.GetComponent<Collider>());return g;
        }
        static RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;return r;
        }
        static Text Text(string name,Transform parent,string value,int size,Vector2 pos,Vector2 bounds,Color color)
        {
            var t=Rect(name,parent,pos,bounds).gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;
            t.text=value;t.color=color;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;
        }
        static Image Image(string name,Transform parent,Vector2 pos,Vector2 size,Color color)
        {
            var im=Rect(name,parent,pos,size).gameObject.AddComponent<Image>();im.color=color;im.raycastTarget=false;return im;
        }
        static Button Button(Transform parent,string title,Vector2 pos,UnityEngine.Events.UnityAction callback)
        {
            var im=Image(title,parent,pos,new Vector2(260,52),new Color(.12f,.55f,.35f));im.raycastTarget=true;
            var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;
            UnityEditor.Events.UnityEventTools.AddPersistentListener(b.onClick,callback);
            Text("Caption",im.transform,title,21,Vector2.zero,new Vector2(250,48),Color.white);return b;
        }
        static Canvas WorldCanvas(Transform parent,string name,Vector2 size,float scale)
        {
            var r=Rect(name,parent,Vector2.zero,size);r.localScale=Vector3.one*scale;
            var c=r.gameObject.AddComponent<Canvas>();c.renderMode=RenderMode.WorldSpace;return c;
        }
        public static void Build()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!=Root+"Scenes/网页版场景.unity") throw new System.Exception("Open the web copy first.");
            if(GameObject.Find("WebExperience")) throw new System.Exception("Already built; refusing duplicate setup.");
            Directory.CreateDirectory(Root+"Materials");Directory.CreateDirectory(Root+"Fonts");Directory.CreateDirectory(Root+"Textures");
            // Font source is copied only to the web feature folder; existing assets are not altered.
            string fontPath=Root+"Fonts/Chinese.ttf";
            if(!File.Exists(fontPath)) File.Copy("C:/Windows/Fonts/simhei.ttf",fontPath);
            AssetDatabase.Refresh();font=AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            var tex=new Texture2D(256,256,TextureFormat.RGBA32,false);
            for(int y=0;y<256;y++) for(int x=0;x<256;x++) {float d=Vector2.Distance(new Vector2(x,y),new Vector2(127.5f,127.5f));tex.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(Mathf.Min(119-d,d-99))));}
            tex.Apply();File.WriteAllBytes(Root+"Textures/Ring.png",tex.EncodeToPNG());Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(Root+"Textures/Ring.png");var ti=(TextureImporter)AssetImporter.GetAtPath(Root+"Textures/Ring.png");ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.alphaIsTransparency=true;ti.mipmapEnabled=false;ti.SaveAndReimport();ring=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Textures/Ring.png");
            foreach(var g in scene.GetRootGameObjects())
                if(g.name!="Office" && g.name!="GameObject" && !g.name.Contains("Lighting")) g.SetActive(false);
            // Disable original behaviours only in this scene copy, retain its environment geometry.
            foreach(var b in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)) b.enabled=false;
            foreach(var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) c.gameObject.SetActive(false);
            var root=new GameObject("WebExperience");
            var player=new GameObject("WebPlayer");player.transform.SetParent(root.transform);
            player.transform.position=new Vector3(9.8f,2.69f,7.4f);player.transform.rotation=Quaternion.Euler(0,180,0);
            var motor=player.AddComponent<CharacterController>();motor.height=1.7f;motor.center=new Vector3(0,.85f,0);motor.radius=.25f;motor.stepOffset=.25f;
            var cameraObj=new GameObject("WebCamera");cameraObj.transform.SetParent(player.transform,false);cameraObj.transform.localPosition=new Vector3(0,1.55f,0);
            var cam=cameraObj.AddComponent<Camera>();cam.tag="MainCamera";cam.nearClipPlane=.04f;cam.farClipPlane=80;cam.fieldOfView=65;cameraObj.AddComponent<AudioListener>();
            var app=player.AddComponent<ProteinExperience>();app.motor=motor;app.view=cam;
            var anchor=new GameObject("HeldProduct").transform;anchor.SetParent(cam.transform,false);anchor.localPosition=new Vector3(.27f,-.23f,.7f);app.holdAnchor=anchor;
            var events=new GameObject("WebEventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));events.transform.SetParent(root.transform);
            var canvasObj=new GameObject("WebHUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvasObj.transform.SetParent(root.transform);
            canvasObj.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvasObj.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1440,900);scaler.matchWidthOrHeight=.5f;
            var hud=canvasObj.transform;
            Text("Reticle",hud,"·",32,Vector2.zero,new Vector2(40,40),Color.white);
            app.prompt=Text("Interaction",hud,"",22,new Vector2(0,-100),new Vector2(720,50),Color.white);
            var help=Text("Controls",hud,"WASD: Move  /  Mouse: Look  /  Left Click: Pick Up / Put Back  /  Hold F: View Watch  /  Esc: Menu",18,new Vector2(0,32),new Vector2(1050,50),Color.white);
            help.rectTransform.anchorMin=help.rectTransform.anchorMax=new Vector2(.5f,0);
            var panel=Image("Menu",hud,Vector2.zero,new Vector2(1440,900),new Color(.035f,.055f,.07f,.96f));panel.raycastTarget=true;
            panel.rectTransform.anchorMin=Vector2.zero;panel.rectTransform.anchorMax=Vector2.one;panel.rectTransform.sizeDelta=Vector2.zero;
            app.menu=panel.gameObject;
            Text("Eyebrow",panel.transform,"PROTEIN  /  DAILY SELECTION",18,new Vector2(0,165),new Vector2(800,50),new Color(.4f,.9f,.65f));
            app.menuTitle=Text("Title",panel.transform,"Discover High-Protein Foods",42,new Vector2(0,95),new Vector2(850,75),Color.white);
            Text("Description",panel.transform,"Explore the shelves and pick up products to reach your daily 140g protein goal.\nEach item counts once. Putting it back does not reduce your total.",21,new Vector2(0,10),new Vector2(950,90),new Color(.8f,.85f,.86f));
            Button(panel.transform,"Start / Resume",new Vector2(0,-100),app.Begin);
            Button(panel.transform,"Restart",new Vector2(0,-168),app.ResetExperience);
            Text("Footer",panel.transform,"WASD: Move  ·  Mouse: Look  ·  Left Click: Pick Up / Put Back  ·  Hold F: View Watch  ·  Esc: Menu",17,new Vector2(0,-260),new Vector2(1000,50),Color.white);
            var wrist=new GameObject("VirtualWrist").transform;wrist.SetParent(cam.transform,false);wrist.localPosition=new Vector3(-.29f,-.64f,.65f);app.wrist=wrist;
            var skin=Mat("Skin",new Color(.66f,.42f,.29f));var black=Mat("WatchCase",new Color(.035f,.045f,.055f));
            Shape("Forearm",PrimitiveType.Cube,wrist,new Vector3(0,-.14f,.05f),new Vector3(.20f,.55f,.15f),skin);
            Shape("Band",PrimitiveType.Cube,wrist,Vector3.zero,new Vector3(.22f,.16f,.17f),black);
            Shape("Watch",PrimitiveType.Cube,wrist,new Vector3(0,0,-.10f),new Vector3(.22f,.24f,.05f),black);
            var face=WorldCanvas(wrist,"WatchFace",new Vector2(240,260),.0008f);face.transform.localPosition=new Vector3(0,0,-.13f);
            app.glow=Image("Glow",face.transform,Vector2.zero,new Vector2(225,225),new Color(.2f,1,.5f,.15f));app.glow.sprite=ring;
            var back=Image("RingBack",face.transform,new Vector2(0,12),new Vector2(185,185),new Color(.17f,.22f,.22f));back.sprite=ring;
            app.progress=Image("Progress",face.transform,new Vector2(0,12),new Vector2(185,185),new Color(.2f,1,.5f));app.progress.sprite=ring;app.progress.type=UnityEngine.UI.Image.Type.Filled;app.progress.fillMethod=UnityEngine.UI.Image.FillMethod.Radial360;app.progress.fillOrigin=2;app.progress.fillAmount=0;
            Text("Caption",face.transform,"Protein Selected",18,new Vector2(0,35),new Vector2(170,35),Color.white);
            app.totalText=Text("Total",face.transform,"0 / 140g",26,new Vector2(0,0),new Vector2(190,40),Color.white);
            app.gainText=Text("Gain",face.transform,"Hold F to View",18,new Vector2(0,-105),new Vector2(240,35),new Color(.3f,1,.6f));
            var red=Mat("ProteinRed",new Color(.68f,.06f,.10f));var cream=Mat("YogurtCream",new Color(.92f,.92f,.80f));
            var list=new System.Collections.Generic.List<ProteinProduct>();
            for(int i=0;i<6;i++)
            {
                bool bar=i<3;float x=bar?6.6f+i*.42f:12.65f+(i-3)*.42f;
                var g=Shape((bar?"ProteinBar_":"Yogurt_")+i,bar?PrimitiveType.Cube:PrimitiveType.Cylinder,root.transform,new Vector3(x,3.84f,4.04f),bar?new Vector3(.22f,.32f,.12f):new Vector3(.22f,.16f,.22f),bar?red:cream,true);
                var p=g.AddComponent<ProteinProduct>();p.displayName=bar?"Protein Bar":"High-Protein Yogurt";p.grams=bar?44:30;
                var packaging=WorldCanvas(g.transform,"Packaging",new Vector2(160,200),.004f);
                packaging.transform.localPosition=new Vector3(0,0,.51f);packaging.transform.localRotation=Quaternion.Euler(0,180,0);
                Text("Brand",packaging.transform,bar?"PROTEIN\nBAR\n44g":"PROTEIN\nYOGURT\n30g",28,Vector2.zero,new Vector2(150,180),bar?Color.white:new Color(.08f,.3f,.18f));
                var label=WorldCanvas(root.transform,"SpatialLabel_"+i,new Vector2(260,70),.0018f);
                Image("Background",label.transform,Vector2.zero,new Vector2(260,70),new Color(.025f,.10f,.075f,.95f));
                Text("Protein",label.transform,"+"+p.grams+"g Protein",27,Vector2.zero,new Vector2(250,65),new Color(.4f,1,.65f));p.label=label.transform;label.gameObject.SetActive(false);
                list.Add(p);
            }
            app.products=list.ToArray();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("WEB_SCENE_READY products="+list.Count);
        }
    }
}

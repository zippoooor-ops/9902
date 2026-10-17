using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Object=UnityEngine.Object;

namespace ProteinWeb.Editor
{
    public static class WebArtInstaller
    {
        const string A="Assets/WebVersion/Art/";
        static Material atlas, metal, white, green, wall, dark;
        static Font font;
        static Transform root;
        static Material Solid(string name,Color color,float smooth=.12f)
        {
            string path=A+"Materials/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.color=color;m.SetFloat("_Smoothness",smooth);return m;
        }
        static GameObject Cube(string name,Vector3 pos,Vector3 size,Material m,Transform parent=null)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;
            g.transform.SetParent(parent?parent:root,false);g.transform.localPosition=pos;g.transform.localScale=size;
            g.GetComponent<Renderer>().sharedMaterial=m;Object.DestroyImmediate(g.GetComponent<Collider>());return g;
        }
        static GameObject Model(string name,Transform parent,Vector3 pos)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(A+"Models/"+name+".fbx");
            if(!source)throw new System.Exception("Missing Blender model: "+name);
            var g=(GameObject)PrefabUtility.InstantiatePrefab(source);g.transform.SetParent(parent,false);g.transform.localPosition=pos;
            foreach(var r in g.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(_=>atlas).ToArray();
            return g;
        }
        static Bounds BoundsOf(GameObject g)
        {
            var rs=g.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;
        }
        static Canvas Label(Transform parent,string name,Vector3 pos,Vector2 size,float scale)
        {
            var g=new GameObject(name,typeof(RectTransform),typeof(Canvas));g.transform.SetParent(parent,false);g.transform.localPosition=pos;
            g.transform.localRotation=Quaternion.Euler(0,180,0);g.transform.localScale=Vector3.one*scale;
            g.GetComponent<RectTransform>().sizeDelta=size;var c=g.GetComponent<Canvas>();c.renderMode=RenderMode.WorldSpace;return c;
        }
        static Text Text(Transform parent,string name,string value,int size,Color color,Vector2 bounds)
        {
            var g=new GameObject(name,typeof(RectTransform),typeof(Text));g.transform.SetParent(parent,false);
            var t=g.GetComponent<Text>();t.font=font;t.fontSize=size;t.text=value;t.color=color;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.rectTransform.sizeDelta=bounds;return t;
        }
        static void ReplaceProduct(ProteinProduct p,string model)
        {
            foreach(var c in p.GetComponents<Collider>())Object.DestroyImmediate(c);
            var old=p.transform.Find("Packaging");if(old)Object.DestroyImmediate(old.gameObject);
            var r=p.GetComponent<Renderer>();if(r)Object.DestroyImmediate(r);
            var f=p.GetComponent<MeshFilter>();if(f)Object.DestroyImmediate(f);
            p.transform.localScale=Vector3.one;var v=Model(model,p.transform,Vector3.zero);var b=BoundsOf(v);
            var box=p.gameObject.AddComponent<BoxCollider>();box.center=p.transform.InverseTransformPoint(b.center);box.size=b.size;
            p.labelHeight=b.size.y+.09f;
            p.transform.position=new Vector3(p.transform.position.x,3.672f,p.transform.position.z);
            p.label.localScale=Vector3.one*.00145f;
        }
        static void Tiles(Bounds b)
        {
            var vs=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
            for(float x=b.min.x;x<b.max.x-1;x+=1)for(float z=b.min.z;z<b.max.z-1;z+=1)
            {
                int n=vs.Count;float y=b.max.y+.003f;
                vs.AddRange(new[]{new Vector3(x+.005f,y,z+.005f),new Vector3(x+.005f,y,z+.995f),new Vector3(x+.995f,y,z+.995f),new Vector3(x+.995f,y,z+.005f)});
                for(int i=0;i<4;i++)uv.Add(new Vector2(.018f,.018f)); // atlas cell 12: matte warm-grey floor
                tris.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
            }
            var mesh=new Mesh{name="FloorTilesMesh"};mesh.SetVertices(vs);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,A+"Models/FloorTiles.asset");var go=new GameObject("FloorTileSurface",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=atlas;
        }
        [MenuItem("Tools/Protein Web/Apply Blender and Painter Art")]
        public static void Apply()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!="Assets/WebVersion/Scenes/网页版场景.unity" || EditorApplication.isPlaying)throw new System.Exception("Open web copy outside Play mode.");
            if(GameObject.Find("WebArtRefresh"))throw new System.Exception("Art already installed. Edit existing objects instead of duplicating.");
            Directory.CreateDirectory(A+"Materials");font=AssetDatabase.LoadAssetAtPath<Font>("Assets/WebVersion/Fonts/Chinese.ttf");
            foreach(var name in new[]{"Shop_BaseColor","Shop_Roughness"})
            {
                var ti=(TextureImporter)AssetImporter.GetAtPath(A+"Textures/"+name+".png");ti.maxTextureSize=2048;ti.sRGBTexture=name=="Shop_BaseColor";ti.mipmapEnabled=true;ti.SaveAndReimport();
            }
            atlas=Solid("PainterAtlas",Color.white);atlas.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(A+"Textures/Shop_BaseColor.png");
            metal=Solid("ShelfMetal",new Color(.46f,.50f,.52f));white=Solid("PriceStrip",new Color(.9f,.9f,.85f));
            green=Solid("Sage",new Color(.42f,.51f,.37f));wall=Solid("WarmWall",new Color(.79f,.76f,.67f));dark=Solid("Trim",new Color(.22f,.25f,.24f));
            root=new GameObject("WebArtRefresh").transform;
            var office=GameObject.Find("Office");
            foreach(var r in office.GetComponentsInChildren<MeshRenderer>())
            {
                var b=r.bounds;
                if(r.name=="Floor"){r.sharedMaterial=Solid("Grout",new Color(.49f,.48f,.43f));Tiles(b);}
                else if(r.name.StartsWith("Wall"))
                {
                    r.sharedMaterial=wall;
                    bool front=b.size.z<b.size.x;float face=front?b.max.z+.012f:(b.center.x<8?b.max.x+.012f:b.min.x-.012f);
                    Cube("SageWallBand",front?new Vector3(b.center.x,3.20f,face):new Vector3(face,3.20f,b.center.z),front?new Vector3(b.size.x,1.08f,.025f):new Vector3(.025f,1.08f,b.size.z),green);
                    Cube("WallBaseTrim",front?new Vector3(b.center.x,2.73f,face+.006f):new Vector3(face,2.73f,b.center.z),front?new Vector3(b.size.x,.13f,.032f):new Vector3(.032f,.13f,b.size.z),dark);
                }
            }
            foreach(var r in GameObject.Find("GameObject").GetComponentsInChildren<MeshRenderer>())
            {
                var b=r.bounds;
                if(b.size.y<.15f&&b.size.x<.3f){r.gameObject.SetActive(false);continue;}
                r.sharedMaterial=metal;
                if(b.size.x>1 && b.size.y<.15f)
                {
                    Cube("ShelfPriceRail",new Vector3(b.center.x,b.center.y,b.max.z+.012f),new Vector3(b.size.x,.065f,.025f),white);
                    var tag=Label(root,"ShelfTag",new Vector3(b.center.x,b.center.y,b.max.z+.028f),new Vector2(500,60),.0015f);
                    Text(tag.transform,"Price","DAILY SELECTION     $6.90",25,new Color(.18f,.21f,.20f),new Vector2(500,60));
                }
            }
            // Existing six interactive targets retain their scripts, references and identifiers.
            var app=Object.FindFirstObjectByType<ProteinExperience>();
            foreach(var p in app.products)ReplaceProduct(p,p.name.StartsWith("ProteinBar")?"ProteinBar":"YoghurtCup");
            string[,] stock={{"CerealBox","OatsBox","CerealBox"},{"CheeseTub","JerkyPouch","CheeseTub"},{"ProteinMilk","TunaCan","ProteinMilk"}};
            float[] levels={3.172f,4.252f,4.812f};
            foreach(float x in new[]{7.05f,13.13f})for(int row=0;row<3;row++)for(int col=0;col<3;col++)
            {
                var g=Model(stock[row,col],root,new Vector3(x+(col-1)*.43f,levels[row],4.02f));g.name="Stock_"+stock[row,col];
            }
            Cube("SectionSign",new Vector3(9.8f,5.63f,-1.81f),new Vector3(3.9f,.74f,.06f),green);
            var sign=Label(root,"SectionTitle",new Vector3(9.8f,5.63f,-1.765f),new Vector2(1200,200),.003f);
            Text(sign.transform,"Title","HIGH-PROTEIN FOODS",88,new Color(.95f,.95f,.87f),new Vector2(1200,200));
            foreach(var n in new[]{"Forearm","Band","Watch"}){var t=app.wrist.Find(n);if(t)t.gameObject.SetActive(false);}
            Model("LeftHand",app.wrist,Vector3.zero);
            Model("Smartwatch",app.wrist,Vector3.zero);
            var watchFace=app.wrist.Find("WatchFace");watchFace.localPosition=new Vector3(0,0,-.113f);watchFace.localScale=Vector3.one*.00065f;
            // Soft transparent halo rather than lighting the entire watch face.
            var halo=new Texture2D(256,256,TextureFormat.RGBA32,false);
            for(int y=0;y<256;y++)for(int x=0;x<256;x++){float d=Vector2.Distance(new Vector2(x,y),new Vector2(127.5f,127.5f));float a=Mathf.Exp(-Mathf.Pow((d-104)/10,2));halo.SetPixel(x,y,new Color(1,1,1,a*.65f));}
            halo.Apply();File.WriteAllBytes(A+"Textures/SoftRing.png",halo.EncodeToPNG());Object.DestroyImmediate(halo);AssetDatabase.ImportAsset(A+"Textures/SoftRing.png");
            var imp=(TextureImporter)AssetImporter.GetAtPath(A+"Textures/SoftRing.png");imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.alphaIsTransparency=true;imp.mipmapEnabled=false;imp.SaveAndReimport();
            app.glow.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(A+"Textures/SoftRing.png");app.glow.rectTransform.anchoredPosition=new Vector2(0,12);app.glow.rectTransform.sizeDelta=new Vector2(204,204);
            app.glow.type=Image.Type.Filled;app.glow.fillMethod=Image.FillMethod.Radial360;app.glow.fillOrigin=2;app.glow.fillClockwise=true;
            app.wrist.localScale=Vector3.one*.8f;app.wrist.localRotation=Quaternion.Euler(0,0,-25);
            app.holdAnchor.localPosition=new Vector3(.29f,-.26f,.85f);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.72f,.74f,.76f);RenderSettings.ambientIntensity=1;
            var lights=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.type==LightType.Directional).ToArray();
            for(int i=0;i<lights.Length;i++){lights[i].enabled=i==0;if(i==0){lights[i].intensity=.9f;lights[i].color=new Color(1,.96f,.89f);lights[i].transform.rotation=Quaternion.Euler(45,170,0);}}
            app.wrist.localPosition=new Vector3(-.29f,-1.05f,.65f);
            app.view.backgroundColor=new Color(.7f,.75f,.78f);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("BLENDER_PAINTER_ART_INSTALLED");
        }
    }
}

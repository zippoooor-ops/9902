using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
namespace ProteinWeb.Editor
{
    public static class RoofLightingUpdate
    {
        static Material Material(string name,Color color,bool emissive=false)
        {
            var path="Assets/WebVersion/Art/Materials/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.color=color;m.SetFloat("_Smoothness",.15f);
            if(emissive){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(1,.95f,.85f)*1.2f);}
            return m;
        }
        static GameObject Box(Transform parent,string name,Vector3 pos,Vector3 size,Material material,bool collision=false)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);
            g.transform.position=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;
            if(!collision)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;
        }
        public static void Apply()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying||scene.path!="Assets/WebVersion/Scenes/网页版场景.unity")throw new System.Exception("Open web scene outside play mode.");
            if(GameObject.Find("RoofAndInteriorLighting"))throw new System.Exception("Roof already installed.");
            var walls=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.name.StartsWith("Wall_")).ToArray();
            Bounds b=walls[0].bounds;foreach(var w in walls)b.Encapsulate(w.bounds);
            var root=new GameObject("RoofAndInteriorLighting").transform;
            float underside=b.max.y-.08f;
            Box(root,"Ceiling",new Vector3(b.center.x,underside+.12f,b.center.z),new Vector3(b.size.x,.24f,b.size.z),Material("CeilingWarmWhite",new Color(.82f,.82f,.77f)),true);
            var frame=Material("CeilingLightFrame",new Color(.40f,.43f,.44f));
            var diffuser=Material("CeilingLightDiffuser",new Color(.95f,.94f,.87f),true);
            foreach(float x in new[]{5f,12.5f})foreach(float z in new[]{2.1f,9f})
            {
                Box(root,"CeilingPanelFrame",new Vector3(x,underside-.06f,z),new Vector3(2.2f,.12f,1.1f),frame);
                Box(root,"CeilingPanelDiffuser",new Vector3(x,underside-.13f,z),new Vector3(2.05f,.025f,.95f),diffuser);
                var g=new GameObject("InteriorCeilingLight");g.transform.SetParent(root,false);g.transform.position=new Vector3(x,underside-.25f,z);g.transform.rotation=Quaternion.Euler(90,0,0);
                var l=g.AddComponent<Light>();l.type=LightType.Spot;l.color=new Color(1,.96f,.89f);l.intensity=25f;l.range=19;l.spotAngle=110;l.innerSpotAngle=75;l.shadows=LightShadows.Hard;l.shadowStrength=.55f;l.shadowBias=.035f;l.shadowNormalBias=.2f;l.renderMode=LightRenderMode.ForcePixel;
            }
            foreach(var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(l.type==LightType.Directional)l.enabled=false;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.80f,.80f,.77f);
            Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
    }
}


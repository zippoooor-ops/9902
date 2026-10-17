using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace ProteinWeb.Editor
{
    public static class ClosedRoomUpdate
    {
        static void Box(Transform parent,string name,Vector3 position,Vector3 size,Material material,bool collision)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name=name;g.transform.SetParent(parent,false);
            g.transform.position=position;g.transform.localScale=size;
            g.GetComponent<Renderer>().sharedMaterial=material;
            if(!collision)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        }
        public static void Apply()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying || scene.path!="Assets/WebVersion/Scenes/网页版场景.unity")throw new System.Exception("Open web scene outside Play mode.");
            if(GameObject.Find("ClosedRoomAddition"))throw new System.Exception("Back wall already installed.");
            var left=GameObject.Find("Wall_Left").GetComponent<Renderer>().bounds;
            var right=GameObject.Find("Wall_Right").GetComponent<Renderer>().bounds;
            var front=GameObject.Find("Wall_Front").GetComponent<Renderer>();
            float xMin=Mathf.Min(left.min.x,right.min.x),xMax=Mathf.Max(left.max.x,right.max.x);
            float width=xMax-xMin,center=(xMin+xMax)*.5f;
            float backZ=Mathf.Min(left.max.z,right.max.z)-.08f;
            var root=new GameObject("ClosedRoomAddition").transform;
            var b=front.bounds;
            // Overlap the side walls at the corners so narrow gaps cannot show sky.
            var scale=front.transform.localScale;scale.x*=width/b.size.x;front.transform.localScale=scale;
            front.transform.position=new Vector3(center,front.transform.position.y,front.transform.position.z);
            string mats="Assets/WebVersion/Art/Materials/";
            Box(root,"Wall_Back",new Vector3(center,b.center.y,backZ),new Vector3(width,b.size.y,.20f),front.sharedMaterial,true);
            Box(root,"BackWallBand",new Vector3(center,3.20f,backZ-.115f),new Vector3(width,1.08f,.03f),AssetDatabase.LoadAssetAtPath<Material>(mats+"Sage.mat"),false);
            Box(root,"BackWallBaseTrim",new Vector3(center,2.73f,backZ-.135f),new Vector3(width,.13f,.035f),AssetDatabase.LoadAssetAtPath<Material>(mats+"Trim.mat"),false);
            Physics.SyncTransforms();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log("ROOM_FOUR_WALLS_COMPLETE");
        }
    }
}

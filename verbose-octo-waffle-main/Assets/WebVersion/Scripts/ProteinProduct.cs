using UnityEngine;
namespace ProteinWeb
{
    public class ProteinProduct : MonoBehaviour
    {
        public string displayName = "Protein Bar";
        public int grams = 44;
        public Transform label;
        public float labelHeight = .24f;
        public bool Counted { get; set; }
        Vector3 homePosition, homeScale;
        Quaternion homeRotation;
        Transform homeParent;
        Collider[] colliders;
        bool[] colliderStates;
        public void Initialize()
        {
            homePosition=transform.position; homeRotation=transform.rotation;
            homeScale=transform.localScale; homeParent=transform.parent;
            colliders=GetComponentsInChildren<Collider>();
            colliderStates=new bool[colliders.Length];
            for(int i=0;i<colliders.Length;i++) colliderStates[i]=colliders[i].enabled;
        }
        public void Hold(Transform anchor)
        {
            foreach(var c in colliders) c.enabled=false;
            transform.SetParent(anchor,true);
            transform.localPosition=Vector3.zero; transform.localRotation=Quaternion.Euler(0,180,0);
            if(label) label.gameObject.SetActive(false);
        }
        public void PutBack()
        {
            transform.SetParent(homeParent); transform.position=homePosition;
            transform.rotation=homeRotation; transform.localScale=homeScale;
            for(int i=0;i<colliders.Length;i++) colliders[i].enabled=colliderStates[i];
        }
    }
}

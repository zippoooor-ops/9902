using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProteinWeb
{
    public class ProteinExperience : MonoBehaviour
    {
        public Camera view;
        public CharacterController motor;
        public Transform holdAnchor, wrist;
        public ProteinProduct[] products;
        public GameObject menu;
        public Text prompt, totalText, gainText, menuTitle;
        public Image progress, glow;
        public float speed=3f, reach=2.8f;
        public int target=140;
        public int Total { get; private set; }
        public ProteinProduct Held { get; private set; }
        Vector3 spawn; Quaternion spawnRotation;
        float pitch, gravity, feedbackUntil, shownProgress;
        int inputAfterFrame;
        bool active;
        Vector3 wristDown=new Vector3(-.29f,-1.05f,.65f);
        Vector3 wristUp=new Vector3(-.24f,-.20f,.80f);

        void Start()
        {
            spawn=transform.position; spawnRotation=transform.rotation;
            foreach(var p in products) p.Initialize();
            Pause(); RefreshText();
        }
        public void Begin()
        {
            active=true; menu.SetActive(false); inputAfterFrame=Time.frameCount+2;
            Cursor.lockState=CursorLockMode.Locked; Cursor.visible=false;
        }
        public void Pause()
        {
            active=false; menu.SetActive(true); Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
            if(prompt) prompt.text="";
        }
        void OnApplicationFocus(bool focus) { if(!focus) Pause(); }
        void OnDisable() { Cursor.lockState=CursorLockMode.None; Cursor.visible=true; }
        public ProteinProduct GetAimedProduct()
        {
            if(Held) return null;
            var ray=view.ViewportPointToRay(new Vector3(.5f,.5f,0));
            if(Physics.Raycast(ray,out var hit,reach,~0,QueryTriggerInteraction.Ignore))
            {
                var product=hit.collider.GetComponentInParent<ProteinProduct>();
                if(product && System.Array.IndexOf(products,product)>=0) return product;
            }
            return null;
        }
        void Update()
        {
            var keyboard=Keyboard.current; var mouse=Mouse.current;
            if(active && (Cursor.lockState!=CursorLockMode.Locked || (keyboard!=null && keyboard.escapeKey.wasPressedThisFrame))) Pause();
            ProteinProduct candidate=null;
            if(active && keyboard!=null && mouse!=null)
            {
                Vector2 look=Time.frameCount>inputAfterFrame?mouse.delta.ReadValue()*.10f:Vector2.zero;
                transform.Rotate(0,look.x,0); pitch=Mathf.Clamp(pitch-look.y,-75,75);
                view.transform.localRotation=Quaternion.Euler(pitch,0,0);
                Vector3 move=new Vector3((keyboard.dKey.isPressed?1:0)-(keyboard.aKey.isPressed?1:0),0,(keyboard.wKey.isPressed?1:0)-(keyboard.sKey.isPressed?1:0));
                if(motor.isGrounded && gravity<0) gravity=-2;
                gravity-=18*Time.deltaTime;
                motor.Move((transform.TransformDirection(Vector3.ClampMagnitude(move,1))*speed+Vector3.up*gravity)*Time.deltaTime);
                if(transform.position.y<-10) ResetExperience();
                candidate=GetAimedProduct();
                prompt.text=Held?"Left Click to Put Back":candidate?"Left Click to Pick Up  ·  "+candidate.displayName+(candidate.Counted?"  Already Counted":""):"";
                if(Time.frameCount>inputAfterFrame && mouse.leftButton.wasPressedThisFrame && !(EventSystem.current && EventSystem.current.IsPointerOverGameObject()))
                {
                    if(Held) { Held.PutBack(); Held=null; }
                    else if(candidate) PickUp(candidate);
                }
            }
            foreach(var p in products)
            {
                if(!p.label) continue;
                Vector3 point=p.transform.position+Vector3.up*p.labelHeight;
                bool visible=active && !Held && p==candidate;
                p.label.gameObject.SetActive(visible);
                if(visible) { p.label.position=point; p.label.rotation=Quaternion.LookRotation(point-view.transform.position); }
            }
            bool feedback=Time.unscaledTime<feedbackUntil;
            bool show=active && (feedback || (keyboard!=null && keyboard.fKey.isPressed));
            wrist.localPosition=Vector3.Lerp(wrist.localPosition,show?wristUp:wristDown,1-Mathf.Exp(-12*Time.unscaledDeltaTime));
            shownProgress=Mathf.MoveTowards(shownProgress,Mathf.Clamp01((float)Total/target),Time.unscaledDeltaTime*.7f);
            progress.fillAmount=shownProgress;
            glow.fillAmount=shownProgress;
            glow.color=new Color(.2f,1,.5f,feedback?.25f+.15f*Mathf.Sin(Time.unscaledTime*10):.08f);
            if(!feedback) gainText.text=Total>=target?"Goal Reached":"Hold F to View";
        }
        public bool PickUp(ProteinProduct product)
        {
            if(product==null || Held!=null || System.Array.IndexOf(products,product)<0) return false;
            Held=product; product.Hold(holdAnchor);
            if(!product.Counted)
            {
                product.Counted=true; Total+=Mathf.Max(0,product.grams);
                feedbackUntil=Time.unscaledTime+2.8f; gainText.text="+"+product.grams+"g Protein"; RefreshText();
            }
            return true;
        }
        public void ResetExperience()
        {
            foreach(var p in products) {p.PutBack();p.Counted=false;}
            Held=null; Total=0; shownProgress=0; feedbackUntil=0; progress.fillAmount=0;
            motor.enabled=false; transform.SetPositionAndRotation(spawn,spawnRotation); motor.enabled=true;
            pitch=0;gravity=0;view.transform.localRotation=Quaternion.identity;
            wrist.localPosition=wristDown; RefreshText(); Pause();
        }
        void RefreshText() {totalText.text=Total+" / "+target+"g";}
    }
}

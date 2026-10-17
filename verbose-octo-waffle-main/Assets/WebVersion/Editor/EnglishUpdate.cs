using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
namespace ProteinWeb.Editor {
public static class EnglishUpdate {
 public static void Apply(){
 var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
 if(EditorApplication.isPlaying || scene.path!="Assets/WebVersion/Scenes/网页版场景.unity")throw new System.Exception("Open the web scene outside Play mode.");
 var map=new System.Collections.Generic.Dictionary<string,string>{ {"WASD 移动  /  鼠标左键 拾取与放回  /  按住 F 查看手表  /  Esc 菜单","WASD: Move  /  Mouse: Look  /  Left Click: Pick Up / Put Back  /  Hold F: View Watch  /  Esc: Menu"},
{"WASD 移动   ·   左键拾取 / 放回   ·   F 查看手表   ·   Esc 释放鼠标","WASD: Move  ·  Mouse: Look  ·  Left Click: Pick Up / Put Back  ·  Hold F: View Watch  ·  Esc: Menu"},
{"探索货架，拿起商品，抬腕查看每日 140g 目标进度。\n同一件商品仅计入一次，放回不扣减。","Explore the shelves and pick up products to reach your daily 140g protein goal.\nEach item counts once. Putting it back does not reduce your total."},
{"发现高蛋白商品","Discover High-Protein Foods"},
{"开始 / 继续体验","Start / Resume"},
{"重新开始","Restart"},
{"高蛋白食品区","HIGH-PROTEIN FOODS"},
{"高蛋白蛋白棒","Protein Bar"},
{"高蛋白酸奶","High-Protein Yogurt"},
{"鼠标左键放回商品","Left Click to Put Back"},
{"鼠标左键拾取  ·  ","Left Click to Pick Up  ·  "},
{"已计入","Already Counted"},
{"已选蛋白质","Protein Selected"},
{"按住 F 查看","Hold F to View"},
{"目标已达成","Goal Reached"},
{"蛋白质","Protein"},
{"蛋白棒","Protein Bar"},
{"高蛋白购物体验","High-Protein Shopping"} };
 var texts=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UnityEngine.UI.Text>(true));
 foreach(var t in texts){
  foreach(var pair in map)t.text=t.text.Replace(pair.Key,pair.Value);
  if(t.name=="Controls" || t.name=="Footer"){t.fontSize=16;t.rectTransform.sizeDelta=new Vector2(1250,55);}
  if(t.name=="Description")t.fontSize=19;
  if(t.name=="Protein")t.fontSize=24;
  if(t.transform.parent && t.transform.parent.name=="WatchFace"){
   if(t.name=="Caption")t.fontSize=15;
   if(t.name=="Gain")t.fontSize=16;
  }
  EditorUtility.SetDirty(t);
 }
 var app=UnityEngine.Object.FindFirstObjectByType<ProteinExperience>();
 foreach(var p in app.products){p.displayName=p.grams==44?"Protein Bar":"High-Protein Yogurt";p.label.gameObject.SetActive(false);EditorUtility.SetDirty(p);}
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
 Debug.Log("ENGLISH_UPDATE_COMPLETE");
 }
}}

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Soulbound
{
    public sealed class SceneNavigation : MonoBehaviour
    {
        public const string MenuScene="MainMenu", GameScene="ProceduralMazePrototype";
        public static bool IsLoading { get; private set; }
        public static void Menu()=>Load(MenuScene);
        public static void Play()=>Load(GameScene);
        public static void Retry(bool sameSeed)
        { if(IsLoading)return;if(sameSeed)FindFirstObjectByType<ProceduralMaze>()?.ReplayNextRun();Load(SceneManager.GetActiveScene().name); }
        private static void Load(string scene)
        {
            if(IsLoading)return;
            if(!Application.CanStreamedLevelBeLoaded(scene)) { Debug.LogError("Scene missing from Build Settings: "+scene);return; }
            var root=new GameObject("Scene transition");DontDestroyOnLoad(root);root.AddComponent<SceneNavigation>().StartCoroutine(root.GetComponent<SceneNavigation>().Transition(scene));
        }
        private IEnumerator Transition(string scene)
        {
            IsLoading=true;Time.timeScale=0;AudioListener.pause=false;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            var root=RuntimeUI.Canvas("Loading screen",500);root.transform.SetParent(transform,false);
            var panel=RuntimeUI.Panel(root.transform,"Loading",new Vector2(.5f,.5f),Vector2.zero,new Vector2(4000,4000));
            panel.GetComponent<Image>().color=new Color(.015f,.022f,.024f,1);
            var box=RuntimeUI.Rect(root.transform,"Loading content",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(500,170));
            var title=RuntimeUI.Label(box,"SOULBOUND",0,0,500,50,36);title.alignment=TextAnchor.MiddleCenter;title.color=RuntimeUI.Gold;
            var status=RuntimeUI.Label(box,scene==MenuScene?"Returning to sanctuary…":"Descending into the dungeon…",0,60,500,35,17);status.alignment=TextAnchor.MiddleCenter;
            var progress=RuntimeUI.Panel(box,"Progress",new Vector2(0,1),new Vector2(0,-120),new Vector2(500,3));
            yield return null;
            var operation=SceneManager.LoadSceneAsync(scene);
            while(!operation.isDone) { progress.sizeDelta=new Vector2(500*Mathf.Clamp01(operation.progress/.9f),3);yield return null; }
            Time.timeScale=1;IsLoading=false;Destroy(gameObject);
        }
        private void OnDestroy() { IsLoading=false;Time.timeScale=1; }
    }
    public static class MenuSettings
    {
        public static float Look=>PlayerPrefs.GetFloat("Soulbound.Look",1);
        public static float Volume=>PlayerPrefs.GetFloat("Soulbound.Volume",.8f);
        public static void Apply()=>AudioListener.volume=Volume;
        public static void ChangeVolume(float step) { PlayerPrefs.SetFloat("Soulbound.Volume",Mathf.Clamp(Volume+step,0,1));PlayerPrefs.Save();Apply(); }
        public static void ChangeLook(float step) { PlayerPrefs.SetFloat("Soulbound.Look",Mathf.Clamp(Look+step,.4f,2));PlayerPrefs.Save(); }
    }
}

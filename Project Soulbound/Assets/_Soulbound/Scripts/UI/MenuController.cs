using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Soulbound
{
    [DefaultExecutionOrder(-150)]
    public sealed class MenuController : MonoBehaviour
    {
        private bool mainMenu=true,paused;
        private FirstPersonController controller;
        private RunState run;
        private GameObject canvas,body;
        private readonly List<Button> buttons=new List<Button>();
        private int selection,page,openedFrame;
        private Text footer;
        private readonly List<EnemyAI> pausedEnemies=new List<EnemyAI>();
        public void Configure(Transform player,RunState state) { mainMenu=false;controller=player.GetComponent<FirstPersonController>();run=state; }
        private void Start()
        {
            MenuSettings.Apply();canvas=RuntimeUI.Canvas(mainMenu?"Main menu":"Pause menu",100);
            if(mainMenu) { Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;gameObject.AddComponent<MenuBackdrop>().Build();Show(0); }
            else canvas.SetActive(false);
        }
        private void Show(int screen)
        {
            page=screen;selection=0;buttons.Clear();openedFrame=Time.frameCount;
            if(body!=null) { body.SetActive(false);Destroy(body); }
            body=new GameObject("Menu content",typeof(RectTransform));body.transform.SetParent(canvas.transform,false);
            var bodyRect=(RectTransform)body.transform;bodyRect.anchorMin=Vector2.zero;bodyRect.anchorMax=Vector2.one;bodyRect.offsetMin=Vector2.zero;bodyRect.offsetMax=Vector2.zero;
            var shade=RuntimeUI.Panel(body.transform,"Shade",new Vector2(.5f,.5f),Vector2.zero,new Vector2(4000,4000));
            shade.GetComponent<Image>().color=new Color(.008f,.014f,.018f,mainMenu?.48f:.93f);
            var panel=RuntimeUI.Rect(body.transform,"Menu",new Vector2(mainMenu&&screen==0?.07f:.5f,.5f),new Vector2(mainMenu&&screen==0?0:.5f,.5f),Vector2.zero,new Vector2(530,560));
            RuntimeUI.Label(panel,mainMenu?"A MAZE SURVIVAL HORROR":"SOULBOUND",0,0,530,26,13).color=RuntimeUI.Gold;
            RuntimeUI.Label(panel,screen==1?"SETTINGS":screen==2?"HOW TO SURVIVE":mainMenu?"SOULBOUND":"PAUSED",0,35,530,72,screen==0&&mainMenu?54:36).color=new Color(.94f,.90f,.80f);
            var line=RuntimeUI.Panel(panel,"Divider",new Vector2(0,1),new Vector2(0,-120),new Vector2(460,2));line.GetComponent<Image>().color=RuntimeUI.Gold;
            float y=155;
            if(screen==0)
            {
                RuntimeUI.Label(panel,mainMenu?"Three keys. One exit.\nEvery soul you leave behind returns.":"Take a breath. The dungeon can wait.",0,128,490,64,16).color=new Color(.65f,.69f,.66f);y=220;
                if(mainMenu) Add(panel,"ENTER THE DUNGEON",ref y,SceneNavigation.Play);
                else Add(panel,"RESUME",ref y,()=>StartCoroutine(Resume()));
                Add(panel,"SETTINGS",ref y,()=>Show(1));Add(panel,"HOW TO PLAY",ref y,()=>Show(2));
                if(!mainMenu)Add(panel,"RETURN TO MAIN MENU",ref y,SceneNavigation.Menu);
                else Add(panel,"QUIT",ref y,Quit);
            }
            else if(screen==1)
            {
                RuntimeUI.Label(panel,"Audio and look controls are saved automatically.",0,130,510,30,15);
                y=180;Add(panel,"VOLUME −    "+Mathf.RoundToInt(MenuSettings.Volume*100)+"%",ref y,()=> { MenuSettings.ChangeVolume(-.1f);int old=selection;Show(1);selection=old; });
                Add(panel,"VOLUME +",ref y,()=> { MenuSettings.ChangeVolume(.1f);int old=selection;Show(1);selection=old; });
                Add(panel,"LOOK SENSITIVITY −    "+MenuSettings.Look.ToString("0.0")+"×",ref y,()=> { MenuSettings.ChangeLook(-.1f);int old=selection;Show(1);selection=old; });
                Add(panel,"LOOK SENSITIVITY +",ref y,()=> { MenuSettings.ChangeLook(.1f);int old=selection;Show(1);selection=old; });
                Add(panel,"BACK",ref y,()=>Show(0));
            }
            else
            {
                RuntimeUI.Label(panel,"WASD / left stick    Move\nMouse / right stick    Look\nQ / E · L3 / R3    Hold to peek\nClick / RT    Shoot    ·    R / Y    Reload\nF / A    Interact; hold 3s to capture a soul\nH / X    Heal    ·    J / View    Journal\nG / LB    Flashlight    ·    Esc / Start    Pause\n\nRead notes, find items and solve the three key journeys.\nCapture defeated enemies before their souls multiply.\nBring all three chest keys to the exit.",0,148,520,300,17);
                y=465;Add(panel,"BACK",ref y,()=>Show(0));
            }
            footer=RuntimeUI.Label(panel,"↑ ↓ / D-pad select  ·  Enter / A confirm  ·  Esc / B back",0,532,530,26,13);footer.color=new Color(.57f,.62f,.60f);
            Highlight();
        }
        private void Add(Transform parent,string label,ref float y,UnityEngine.Events.UnityAction action)
        {
            var button=RuntimeUI.Button(parent,label,0,y,460,46,action);y+=55;
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.2f,1.12f,.9f);colors.pressedColor=new Color(.7f,.65f,.5f);button.colors=colors;
            buttons.Add(button);
            int index=buttons.Count-1;var trigger=button.gameObject.AddComponent<EventTrigger>();
            var entry=new EventTrigger.Entry { eventID=EventTriggerType.PointerEnter };entry.callback.AddListener(_=> { selection=index;Highlight(); });trigger.triggers.Add(entry);
        }
        private void Highlight()
        { for(int i=0;i<buttons.Count;i++)buttons[i].GetComponent<Image>().color=i==selection?new Color(.30f,.26f,.17f,.95f):new Color(.045f,.065f,.07f,.86f); }
        private void Update()
        {
            if(canvas==null||SceneNavigation.IsLoading)return;
            bool pause=(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)||(Gamepad.current!=null&&Gamepad.current.startButton.wasPressedThisFrame);
            if(!mainMenu&&!paused)
            {
                if(run.HasEnded||controller.InputBlocked)return;
                if(pause)
                {
                    paused=true;controller.SetInteractionMode(true);Time.timeScale=0;AudioListener.pause=true;
                    // Time scale alone does not stop Update-based attack logic.
                    pausedEnemies.Clear();foreach(var enemy in FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))if(enemy.enabled){pausedEnemies.Add(enemy);enemy.enabled=false;}
                    canvas.SetActive(true);Show(0);
                }
                return;
            }
            if(Time.frameCount<=openedFrame+1)return;
            if(PlayerControls.CancelPressed||(!mainMenu&&pause))
            { if(page!=0)Show(0);else if(!mainMenu)StartCoroutine(Resume());return; }
            if(buttons.Count==0)return;
            selection=(selection+PlayerControls.MenuStep+buttons.Count)%buttons.Count;Highlight();
            if(PlayerControls.ConfirmPressed)buttons[selection].onClick.Invoke();
        }
        private IEnumerator Resume()
        {
            if(!paused)yield break;paused=false;canvas.SetActive(false);Time.timeScale=1;AudioListener.pause=false;
            foreach(var enemy in pausedEnemies)if(enemy!=null)enemy.enabled=true;pausedEnemies.Clear();
            yield return null;if(controller!=null && !run.HasEnded)controller.SetInteractionMode(false);
        }
        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
        private void OnDestroy() { Time.timeScale=1;AudioListener.pause=false;if(canvas!=null)Destroy(canvas); }
    }
}

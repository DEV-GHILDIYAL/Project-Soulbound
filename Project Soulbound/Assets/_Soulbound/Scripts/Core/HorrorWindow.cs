using UnityEngine;
namespace Soulbound
{
    public sealed class HorrorWindow : MonoBehaviour
    {
        private Transform player;
        private GameObject figure;
        private float phase, appearedAt;
        private bool window, triggered;
        private Quaternion rest;
        private AudioSource tap;
        private AudioClip clip;
        private bool sounded;
        public void Configure(Transform target,GameObject silhouette,double offset,bool pane)
        { player=target;figure=silhouette;phase=(float)offset;window=pane;rest=transform.localRotation;if(figure!=null)figure.SetActive(false);
            if(pane) { tap=gameObject.AddComponent<AudioSource>();tap.spatialBlend=1;tap.minDistance=1;tap.maxDistance=7;tap.volume=.3f;clip=HorrorAmbience.MakeClip("Glass tap",.15f,true); }
        }
        private void Update()
        {
            if(player==null)return;
            if(!window) { transform.localRotation=rest*Quaternion.Euler(0,0,Mathf.Sin(Time.time*.7f+phase)*2f);return; }
            if(!triggered && Vector3.Distance(player.position,transform.position)<4f)
            { triggered=true;appearedAt=Time.time+phase; }
            if(triggered)
            {
                float elapsed=Time.time-appearedAt;
                if(elapsed>=0 && !sounded) { sounded=true;tap.PlayOneShot(clip); }
                if(figure!=null) { figure.SetActive(elapsed>=0 && elapsed<2.8f);if(elapsed>=0)figure.transform.localPosition=new Vector3(Mathf.Clamp(elapsed-1.7f,0,1)*1.5f,0,-.055f); }
            }
        }
        private void OnDestroy() { if(clip!=null)Destroy(clip); }
    }
}

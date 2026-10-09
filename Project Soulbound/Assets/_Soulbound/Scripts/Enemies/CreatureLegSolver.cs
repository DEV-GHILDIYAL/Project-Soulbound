using UnityEngine;
namespace Soulbound
{
    public static class CreatureLegSolver
    {
        public static void Solve(Vector3 hip,Vector3 requestedFoot,Vector3 forward,out Vector3 ankle,out Vector3 knee)
        {
            // Keep the endpoint below the pelvis, including stale plants after a turn.
            ankle=requestedFoot;ankle.y=Mathf.Clamp(ankle.y,hip.y-.78f,hip.y-.48f);
            Vector3 horizontal=ankle-hip;horizontal.y=0;
            float vertical=hip.y-ankle.y;
            float maxHorizontal=Mathf.Sqrt(Mathf.Max(0,.84f*.84f-vertical*vertical));
            horizontal=Vector3.ClampMagnitude(horizontal,maxHorizontal);
            ankle.x=hip.x+horizontal.x;ankle.z=hip.z+horizontal.z;
            Vector3 delta=ankle-hip;float distance=delta.magnitude;
            Vector3 axis=delta/distance;
            Vector3 bend=Vector3.ProjectOnPlane(forward,axis);
            if(bend.sqrMagnitude<.0001f)bend=Vector3.ProjectOnPlane(Vector3.forward,axis);
            bend.Normalize();
            knee=hip+delta*.5f+bend*Mathf.Sqrt(Mathf.Max(0,.43f*.43f-distance*distance*.25f));
        }
    }
}

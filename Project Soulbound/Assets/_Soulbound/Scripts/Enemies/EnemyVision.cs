using UnityEngine;
namespace Soulbound
{
    public static class EnemyVision
    {
        public static bool InCone(Vector3 forward,Vector3 offset,float range,float angle)
        {
            if(offset.sqrMagnitude>range*range)return false;
            // Horizontal field of view; height remains part of the range check.
            forward.y=0;offset.y=0;
            if(offset.sqrMagnitude<.0001f)return true;
            return Vector3.Dot(forward.normalized,offset.normalized)>=Mathf.Cos(angle*.5f*Mathf.Deg2Rad);
        }
    }
}

using System;
using UnityEngine;
using Soulbound;
public static class EnemyVisionChecks
{
    public static void Main()
    {
        if(!EnemyVision.InCone(Vector3.forward,new Vector3(0,0,5),10,110))throw new Exception("Front target missed");
        if(EnemyVision.InCone(Vector3.forward,new Vector3(0,0,-1),10,110))throw new Exception("Rear target detected");
        if(EnemyVision.InCone(Vector3.forward,new Vector3(5,0,0),10,110))throw new Exception("Side target detected");
        if(EnemyVision.InCone(Vector3.forward,new Vector3(0,0,11),10,110))throw new Exception("Out-of-range target detected");
        for(int heading=0;heading<360;heading+=5)
        {
            float r=heading*MathF.PI/180;var forward=new Vector3(MathF.Sin(r),0,MathF.Cos(r));
            if(!EnemyVision.InCone(forward,forward*5,10,110)||EnemyVision.InCone(forward,-forward*5,10,110))throw new Exception("Turn cone incorrect");
        }
        Console.WriteLine("PASS: vision front/side/rear/range and 72 heading cases.");
    }
}

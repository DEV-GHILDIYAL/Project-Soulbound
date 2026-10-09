using System;
using UnityEngine;
using Soulbound;
public static class CreatureLegChecks
{
    public static void Main()
    {
        var rng=new System.Random(42);
        for(int i=0;i<10000;i++)
        {
            var hip=new Vector3(0,.82f,0);
            var foot=new Vector3((float)rng.NextDouble()*6-3,(float)rng.NextDouble()*6-2,(float)rng.NextDouble()*6-3);
            float angle=(float)rng.NextDouble()*MathF.PI*2;
            CreatureLegSolver.Solve(hip,foot,new Vector3(MathF.Sin(angle),0,MathF.Cos(angle)),out var ankle,out var knee);
            if(ankle.y>=hip.y || knee.y>=hip.y || Vector3.Distance(hip,knee)>.431f || Vector3.Distance(knee,ankle)>.431f || float.IsNaN(knee.y))
                throw new Exception("Unsafe leg pose: "+i);
        }
        Console.WriteLine("PASS: 10,000 stale/high/low foot targets and turn directions remain below the hip, finite, and within limb reach.");
    }
}

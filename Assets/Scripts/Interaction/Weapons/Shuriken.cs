using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shuriken : Weapon
{
    [Header("Primary Fire")]
    public bool enablePrimaryFire;
    public int primaryDamage;
    public float primarySpawnCameraOffset;
    public float primaryForwardForce;
    public float primaryUpForce;
    public float primaryFireCooldown;
    public float primaryFireVisualResetTime;
    bool canPrimaryFire;
    float primaryFireVisualResetTimer;
    bool primaryVisualResetting;

    private float GetAngleToThrow()
    {
        /*  Formula:
         *      angle = sin^-1( ( a * sqrt( c^2 + a^2 ) ) / ( c^2 + a^2 ) )
         */

        float a = primaryForwardForce;
        float c = GameManager.Instance.playerWalkSpeed;

        float r1 = Mathf.Pow(c, 2) + Mathf.Pow(a, 2);
        float r = a * Mathf.Sqrt(r1);
        r = r / r1;

        return r;
    }
}

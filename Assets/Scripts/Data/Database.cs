using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Database
{
    public static GameObject LoadCharacter(string name)
    {
        return Resources.Load<GameObject>($"Characters/{name}");
    }

    public static GameObject LoadProjectile(string directory, string name)
    {
        return Resources.Load<GameObject>($"Projectiles/{directory}/{name}");
    }

    public static GameObject LoadParticleSystem(string directory, string name)
    {
        return Resources.Load<GameObject>($"ParticleSystems/{directory}/{name}");
    }
}

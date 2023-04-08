using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Database
{
    public static GameObject LoadCharacter(string name)
    {
        return Resources.Load<GameObject>($"Characters/{name}");
    }
}

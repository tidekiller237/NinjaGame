using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Effect_KunaiExplosion : MonoBehaviour
{
    public float startRadius;
    public float endRadius;
    public float time;
    public float lingerTime;
    public GameObject effectObject;

    private void Start()
    {
        effectObject.transform.localScale = Vector3.one * startRadius;
        StartCoroutine(Expand());
    }

    private IEnumerator Expand()
    {
        float radius = startRadius;
        float t = 0;
        float speed = 1 / time;

        while(t <= 1)
        {
            radius = Mathf.Lerp(startRadius, endRadius, Mathf.Min(t, 1));
            effectObject.transform.localScale = Vector3.one * radius;

            t += Time.deltaTime * speed;
            yield return null;
        }

        Invoke(nameof(Cleanup), lingerTime);
    }

    private void Cleanup()
    {
        Destroy(gameObject);
    }
}

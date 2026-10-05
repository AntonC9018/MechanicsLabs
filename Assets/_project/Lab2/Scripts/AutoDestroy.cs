using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    public float delay;

    private void Awake()
    {
        Invoke("DestroyGameObject", delay);
    }

    private void DestroyGameObject()
    {
        Destroy(this);
    }

}

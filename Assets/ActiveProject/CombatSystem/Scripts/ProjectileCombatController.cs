
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ProjectileCombatController : UdonSharpBehaviour
{
    public string projectileName = "default_bullet";
    public float projectileDamage = 1.0f;
    public float projectileLifetime = 30.0f;
    public bool destroyOnEnter = true;
    public VRCPlayerApi owner;

    // Its weird, but this is used to confirm that we got this behaviour via GetProgramVariable()
    [HideInInspector]
    public bool isProjectileCombatController = true;

    private void Update()
    {
        if (projectileLifetime > 0.0f)
            projectileLifetime -= Time.deltaTime;
        else Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Bullet Hit: {other.name}");
        if (destroyOnEnter)
            Destroy(gameObject);
        else GetComponent<Rigidbody>().isKinematic = true;
    }
}

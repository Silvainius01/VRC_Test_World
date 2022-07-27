
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Components;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class ProjectileCombatController : UdonSharpBehaviour
{
    // Externally set
    public string projectileName;
    public float projectileDamage;
    public float projectileLifetime;
    public bool destroyOnEnter;
    public VRCPlayerApi owner;
    public RangedWeaponCombatController linkedWeapon;

    // ========== MONO BEHAVIOUR ==========

    private void OnEnable()
    {
        if (linkedWeapon == null)
        {
            Debug.LogError($"{projectileName} has no linked weapon!");
        }

        projectileName = linkedWeapon.weaponName + "_round";
        projectileDamage = linkedWeapon.projectileDamage;
        projectileLifetime = linkedWeapon.projectileLifespan;
        destroyOnEnter = linkedWeapon.destroyOnEnter;
        owner = linkedWeapon.pickupComponent.currentPlayer;

        if (owner != null && !owner.isLocal)
        {
            Debug.Log($"bullet owner invalid: {(owner == null ? "null" : $"{owner.displayName}[{owner.playerId}]")}");
            projectileDamage = 0.0f;
        }

        Debug.Log("bullet enabled");
    }

    private void Update()
    {
        if (projectileLifetime > 0.0f)
            projectileLifetime -= Time.deltaTime;
        else Destroy(this.gameObject);
    }

    private void OnTriggerEnter(Collider collider)
    {
        Debug.Log($"bullet hit {collider.gameObject.name}");

        //var behaviour = GetBehaviour(collider.gameObject);
        //string scriptType = (string)behaviour.GetProgramVariable("scriptType");
        //if (behaviour == null || scriptType == "PlayerCombatController")
        //    return;

        // Dont bother sending nerf events
        var playerController = collider.gameObject.GetComponentInParent<PlayerCombatController>();

        if (playerController == null || projectileDamage <= 0.0f)
        {
            Debug.Log($"did not hit damageable target: p={playerController != null} d={projectileDamage > 0.0f}");
            return;
        }

        if (playerController.linkedPlayer == null)
        {
            Debug.LogError($"{projectileName} hit null linked collider");
            return;
        }

        if (linkedWeapon == null)
            Debug.LogError("How null");
        Debug.Log("Attempting to damage player");
        linkedWeapon._hitPlayerId = playerController.linkedPlayer.playerId;
        linkedWeapon.DamagePlayerLocal();
        Debug.Log("Bullet damaged player");

        if (destroyOnEnter)
        {
            Debug.Log("Destroying bullet.");
            Destroy(this.gameObject);
        }
    }

    UdonBehaviour GetBehaviour(GameObject obj)
    {
        return (UdonBehaviour)obj.GetComponent(typeof(UdonBehaviour));
    }
}

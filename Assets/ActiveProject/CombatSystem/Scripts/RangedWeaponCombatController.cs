
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class RangedWeaponCombatController : UdonSharpBehaviour
{
    public string weaponName = "default_weapon";

    [Header("Damage Settings")]
    [Tooltip("If enabled, the weapon will fire as long as the trigger is held down.")]
    public bool isAutomatic;
    [Tooltip("The damage each round deals to a player")]
    public float projectileDamage = 1.0f;
    [Tooltip("The amount of rounds the weapon can fire before being forced to reload")]
    public int magazineSize;
    [Tooltip("The maximum rate of fire for the weapon. Values above 500rpm will make the weapon fire as fast as you can pull the trigger. Time between rounds in seconds = 60 / RPM")]
    public int roundsPerMin;

    [Header("Projectile Settings")]
    [Tooltip("If enabled, the fired projectile will be destroyed when it contacts another collider.")]
    public bool destroyOnEnter;
    [Tooltip("How long the projectile is allowed to exist after being spawned")]
    public float projectileLifespan = 10.0f;
    [Tooltip("The speed of the projectile after being spawned")]
    public float muzzleVelocity;
    [Tooltip("Location at which the projectile is spawned")]
    public Transform bulletSpawn;
    [Tooltip("The unique bullet prefab for this weapon. It will be updated on Start() if the stats dont match.")]
    public GameObject bulletPrefab;

    [Header("Reload Settings")]
    [Tooltip("If enabled, each shot must be rechambered manually. Think pump action shotties, or bolt action snipers. Turning this on will disable automatic fire.")]
    public bool rechamberEachShot;
    [Tooltip("Time it takes to rechamber the weapon. If the mag is fully depleted, this will be added onto the reload time.")]
    public float rechamberTime;
    [Tooltip("Time it takes to reload the weapon.")]
    public float reloadTime;

    [Header("Accuracy Settings")]
    [Tooltip("The max angle of deviation while at rest (Min Cone of Fire)")]
    public float minConeOfFireBloom = 0.0f;
    [Tooltip("The maximum angle of deviation while firing (Max Cone of Fire")]
    public float maxConeOfFireBloom = 1.0f;
    [Tooltip("Angle of deviation increase per round fired (Cone of Fire bloom")]
    public float bloomPerShot = 0.0f;
    [Tooltip("How quickly the bloom decays back to normal (angle per second)")]
    public float bloomDecayRate = 1.0f;
    [Tooltip("How long the user must wait before bloom begins to decay (in seconds)")]
    public float bloomDecayDelay = 0.0f;

    [Header("External References")]
    [Tooltip("If a bullet prefab for this weapon isnt defined, one will be made on Start() for this weapon.\n\nPlease note, that EACH INSTANCE of the weapon creates its own prefab. It is HIGHLY ENCOURAGED you create and link a copy of the bullet prefab.")]
    public VRC_Pickup pickupComponent;
    public GameObject defaultBulletPrefab;
    public Collider[] nonTriggerChildColliders;

    // Externally set
    [HideInInspector] public CombatController combatController;
    [HideInInspector] public PlayerCombatController localPlayerController;

    // Hidden public

    // Private vars
    [Header("Debug Values")]
    int roundsLeft = 0;
    bool isFiring = false;
    bool triggerDown = false;
    float currentBloom = 0.0f;
    VRCPlayerApi localPlayer;
    ProjectileCombatController bulletBehaviour;

    // Timers
    float timerRefire = 0.0f;
    float timerRechamber = 0.0f;
    float timerReload = 0.0f;
    float timerBloom = 0.0f;

    // ========== MONO BEHAVIOUR ==========

    private void Start()
    {
        if (bulletPrefab == null)
        {
            bulletPrefab = VRCInstantiate(defaultBulletPrefab);
            bulletPrefab.SetActive(false);
        }

        localPlayer = Networking.LocalPlayer;

        if(pickupComponent == null)
            pickupComponent = (VRC_Pickup)GetComponent(typeof(VRC_Pickup));

        bulletBehaviour = bulletPrefab.GetComponent<ProjectileCombatController>();
        //bulletBehaviour.projectileDamage = projectileDamage;
        //bulletBehaviour.projectileName = $"{weaponName}_round";
        //bulletBehaviour.projectileLifetime = projectileLifespan;
        //bulletBehaviour.destroyOnEnter = destroyOnEnter;
        bulletBehaviour.linkedWeapon = this;

        // Pump/Lever/Bolt/etc action weapons should not fire automatically.
        if (rechamberEachShot)
            isAutomatic = false;

        roundsLeft = magazineSize;
        currentBloom = minConeOfFireBloom;

        if (reloadTime <= 0.0f)
        {
            Debug.LogWarning("Reload time not specified!");
            reloadTime = 0.001f;
        }

        //if (combatController == null)
        //    Debug.LogError($"{gameObject.name} does not have combat controller set! Please add it to the combat controller.");
    }

    private void Update()
    {
#if UNITY_EDITOR
        triggerDown = Input.GetKey(KeyCode.Space);
#endif

        if (pickupComponent.IsHeld && pickupComponent.currentPlayer.isLocal)
        {
            if (CanFireWeapon() && triggerDown)
            {
                FireWeapon();
            }

            if (roundsLeft <= 0 || Input.GetKeyDown(KeyCode.R))
                ReloadWeapon();
        }

        // isFiring is set to false as soon as:
        //   - the trigger is released
        //   - the weapon is reloading
        isFiring &= triggerDown || roundsLeft <= 0;
        UpdateBloom();
    }

    // ========== U# BEHAVIOUR ==========

    public override void OnPickupUseDown()
    {
        triggerDown = true;
    }
    public override void OnPickupUseUp()
    {
        triggerDown = false;
    }

    public override void OnPickup()
    {
        bulletBehaviour.owner = pickupComponent.currentPlayer;
        foreach (var collider in nonTriggerChildColliders)
            collider.isTrigger = true;
    }
    public override void OnDrop()
    {
        bulletBehaviour.owner = null;
        foreach (var collider in nonTriggerChildColliders)
            collider.isTrigger = false;
        ReloadWeapon();
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        //int spreadLength = (spreadPattern == null) ? -1 : spreadPattern.Length;
        //Debug.Log($"Weapon {weaponName} for player {player.displayName}[{player.playerId}]:" +
        //    $"\nOwner: {player.isInstanceOwner} Spread: {spreadLength} Index: {spreadIndex}");
    }

    // ========== PUBLIC ==========

    [HideInInspector][UdonSynced] public int _hitPlayerId;

    public void DamagePlayerLocal()
    {
        Debug.Log("Damage - Local phase");
        if (Networking.GetOwner(gameObject).playerId != localPlayer.playerId)
        {
            Debug.Log($"{weaponName} cannot damage player, not owned by local player.");
            return;
        }

        RequestSerialization();
        this.SendCustomNetworkEvent(NetworkEventTarget.All, "DamagePlayerGlobal");
    }

    public void DamagePlayerGlobal()
    {
        Debug.Log("Damage - Global phase");
        VRCPlayerApi hitPlayer = VRCPlayerApi.GetPlayerById(_hitPlayerId);
        if (hitPlayer.isLocal)
        {
            localPlayerController._damage = projectileDamage;
            localPlayerController.DamagePlayer();
        }
    }

    public void SpawnBulletGlobal()
    {
        Vector3 angles = Random.insideUnitCircle * currentBloom;
        var adjustedDirection = Quaternion.Euler(angles) * bulletSpawn.rotation;
        var bullet = VRCInstantiate(bulletPrefab);

        bullet.SetActive(true);
        bullet.transform.position = bulletSpawn.position;
        bullet.transform.rotation = adjustedDirection;
        bullet.GetComponent<Rigidbody>().velocity = bullet.transform.forward * muzzleVelocity;
        Debug.Log("Bullet Spawned");
    }

    // ========== PRIVATE ==========

    void ReloadWeapon()
    {
        if (timerReload > 0.0f || roundsLeft >= magazineSize)
            return;

        Debug.Log("Reloading");

        if(roundsLeft > 0)
        {
            timerReload += reloadTime;
        }
        else
        {
            timerReload += reloadTime;
            timerRechamber += rechamberTime;
        }

        timerRefire = 0.0f;
    }

    bool CanFireWeapon()
    {
        // Cannot fire weapon if reloading.
        if(timerReload > 0.0f)
        {
            timerReload -= Time.deltaTime;
            if(timerReload < 0.0f)
            {
                timerReload = 0.0f;
                roundsLeft = magazineSize;
                Debug.Log("Reload Done");
            }
            else return false;
        }

        if (timerRefire > 0.0f)
            timerRefire -= Time.deltaTime;
        if (timerRechamber > 0.0f && roundsLeft > 0)
            timerRechamber -= Time.deltaTime;

        // Trigger is active if:
        //  - The weapon isnt firing
        //  - If it is automatic
        //  - It has any rounds left in the mag
        bool triggerActive = roundsLeft > 0;
        triggerActive &= !isFiring || (isFiring && isAutomatic);

        // Weapon can only fire if it has been rechambered, and the refire timer is at 0.
        return timerRefire <= 0.0f && timerRechamber <= 0.0f && triggerActive;
    }

    void FireWeapon()
    {
        isFiring = true;

        Debug.Log("Firing");
        SendCustomNetworkEvent(NetworkEventTarget.All, "SpawnBulletGlobal");

        // Only update these stats on the user's end. 
        // NOTE: Only updating bloom here means other people only see bullets go
        // in the direction the weapon is aiming, and wont see the bloom on your end.
        if (pickupComponent.IsHeld && pickupComponent.currentPlayer.isLocal)
        {
            --roundsLeft;
            currentBloom = Mathf.Min(currentBloom + bloomPerShot, maxConeOfFireBloom);
            timerBloom = bloomDecayDelay; // Reset the bloom delay when the weapon fires
            timerRefire += 60.0f / roundsPerMin; // Refire is += to ensure that we get as close as possible to the exact fire rate.
            timerRechamber = rechamberEachShot ? rechamberTime : 0.0f;
            Debug.Log("vars updated");
        }
        else Debug.Log("vars skipped");
    }

    void UpdateBloom()
    {
        // Dont update bloom if the weapon is firing
        if (isFiring)
            return;

        // Only update bloom after the delay completes
        if (timerBloom > 0.0f)
            timerBloom -= Time.deltaTime;
        else currentBloom = Mathf.Max(currentBloom - (bloomDecayRate * Time.deltaTime), minConeOfFireBloom);
    }

    UdonBehaviour GetBehaviour(GameObject obj)
    {
        return (UdonBehaviour)obj.GetComponent(typeof(UdonBehaviour));
    }
}

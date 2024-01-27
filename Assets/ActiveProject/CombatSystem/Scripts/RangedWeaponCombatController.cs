
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.SDK3.Components;
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
    [Tooltip("How much the bullet is affected by gravity. 1 = normal, 0 = none, -1 is inverted, etc.")]
    public float projectileGravity = 1;
    [Tooltip("How long the projectile is allowed to exist after being spawned")]
    public float projectileLifespan = 10.0f;
    [Tooltip("The speed of the projectile after being spawned")]
    public float muzzleVelocity;
    [Tooltip("Location at which the projectile is spawned")]
    public Transform bulletSpawn;

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
    public VRC_Pickup pickupComponent;
    public GameObject bulletPrefab;
    public AudioSource audioSource;
    public AudioClip[] muzzleSounds;
    public Collider[] nonTriggerChildColliders;
    public Canvas weaponCanvas;
    public Slider reloadSlider;
    public Text ammoText;

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
    //ProjectileCombatController bulletBehaviour;

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
            bulletPrefab = Instantiate(bulletPrefab);
            bulletPrefab.SetActive(false);
        }

        localPlayer = Networking.LocalPlayer;
        pickupComponent = (VRC_Pickup)GetComponent(typeof(VRC_Pickup));

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

        reloadSlider.value = 0;
        weaponCanvas.gameObject.SetActive(false);
        ammoText.text = $"{magazineSize} / {magazineSize}";

        //if (combatController == null)
        //    Debug.LogError($"{gameObject.name} does not have combat controller set! Please add it to the combat controller.");
    }

    private void Update()
    {
#if UNITY_EDITOR
        triggerDown = Input.GetMouseButton(0);
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
        foreach (var collider in nonTriggerChildColliders)
            collider.isTrigger = true;

        if (pickupComponent.currentPlayer.isLocal)
            weaponCanvas.gameObject.SetActive(true);
    }
    public override void OnDrop()
    {
        foreach (var collider in nonTriggerChildColliders)
            collider.isTrigger = false;
        ReloadWeapon();
        weaponCanvas.gameObject.SetActive(false);
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        //int spreadLength = (spreadPattern == null) ? -1 : spreadPattern.Length;
        //Debug.Log($"Weapon {weaponName} for player {player.displayName}[{player.playerId}]:" +
        //    $"\nOwner: {player.isMaster} Spread: {spreadLength} Index: {spreadIndex}");
    }

    // ========== PUBLIC ==========

    [HideInInspector] public int _hitPlayerId;
    [UdonSynced][FieldChangeCallback(nameof(SyncedHitPlayerId))] int _syncedHitPlayerId;

    // We use a callback to ensure we run damage as soon as a change occurs.
    int SyncedHitPlayerId
    {
        set
        {
            Debug.Log($"Received new hit player: {value}");
            _syncedHitPlayerId = value;
            DamagePlayerGlobal();
        }
        get => _syncedHitPlayerId;
    }

    public void DamagePlayerLocal()
    {
        Debug.Log("Damage - Local phase");

        if (!Networking.IsOwner(localPlayer, gameObject))
        {
            Debug.Log($"{weaponName} cannot damage player, not owned by local player.");
            return;
        }

        if (SyncedHitPlayerId != _hitPlayerId)
        {
            SyncedHitPlayerId = _hitPlayerId;
            RequestSerialization();
        }
        else this.SendCustomNetworkEvent(NetworkEventTarget.All, "DamagePlayerGlobal");
    }

    public void DamagePlayerGlobal()
    {
        Debug.Log($"Damage - Global phase");
        VRCPlayerApi hitPlayer = VRCPlayerApi.GetPlayerById(SyncedHitPlayerId);
        if (hitPlayer != null && hitPlayer.isLocal)
        {
            localPlayerController._damage = projectileDamage;
            localPlayerController.DamagePlayer();
        }
        else Debug.Log($"Did not damage player: null={hitPlayer == null} local={SyncedHitPlayerId == localPlayer.playerId}");
    }

    public void SpawnBulletGlobal()
    {
        Vector3 angles = Random.insideUnitCircle * currentBloom;
        var adjustedDirection = Quaternion.Euler(angles) * bulletSpawn.rotation;
        var bullet = Instantiate(bulletPrefab);

        bullet.SetActive(true);
        bullet.transform.position = bulletSpawn.position;
        bullet.transform.rotation = adjustedDirection;
        bullet.GetComponent<Rigidbody>().velocity = bullet.transform.forward * muzzleVelocity;

        if (muzzleSounds.Length > 1)
            audioSource.clip = muzzleSounds[Random.Range(0, muzzleSounds.Length)];
        audioSource.Play();

        var bulletBehaviour = bullet.GetComponent<ProjectileCombatController>();
        bulletBehaviour.linkedWeapon = this;
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
            reloadSlider.value = Mathf.Max(0, 1 - (timerReload / reloadTime));
            if(timerReload <= 0.0f)
            {
                timerReload = 0.0f;
                reloadSlider.value = 0;
                roundsLeft = magazineSize;
                ammoText.text = $"{roundsLeft} / {magazineSize}";
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
            ammoText.text = $"{roundsLeft} / {magazineSize}";
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

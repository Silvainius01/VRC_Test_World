
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class PlayerCombatController : UdonSharpBehaviour
{
    [Header("Health Settings")]
    public float maxHealth;
    public Slider healthSlider;

    [Header("Colliders")]
    public GameObject bodyColliderParent;
    public float bodyColliderHeight;

    [Header("External References")]
    public GameObject canvasParent;
    public Text debugText;

    // Externally Set
    [HideInInspector] public int localTeam = -1;
    [HideInInspector] public int playerTeam = -1;
    [HideInInspector] public bool allowFriendlyFire;
    [HideInInspector] public float ffDamageMult;
    [HideInInspector] public VRCPlayerApi linkedPlayer;
    [HideInInspector] public CombatController combatController;

    // Hidden
    [HideInInspector] public string scriptType = "PlayerCombatController";

    // Synced
    [UdonSynced] public float currentHealth;
    [UdonSynced] bool respawnOnSync = false;

    // Private
    bool inited = false;
    float respawnTime;
    VRCPlayerApi localPlayer;
    Collider[] bodyColliders;
    MeshRenderer[] bodyColliderMeshs;
    LobbyController lobby;
    bool isRespawning;
    float respawnTimer = 0.0f;

    private void Start()
    {
        if (!inited)
        {
            lobby = combatController.gameLobby;
            localPlayer = Networking.LocalPlayer;
            respawnTime = combatController.respawnTime;

            bodyColliders = bodyColliderParent.GetComponentsInChildren<Collider>();
            bodyColliderMeshs = bodyColliderParent.GetComponentsInChildren<MeshRenderer>();

            // Ensure we dont have any collision. Bad things happen otherwise.
            foreach (var collider in bodyColliders)
                collider.isTrigger = true;

            // Disable the controller until we need it.
            debugText.text += $"\ninit {gameObject.name}";
            inited = true;
        }
    }

    private void Update()
    {
        if (linkedPlayer != null)
        {
            // Update the body colliders to be under the player
            //if (linkedPlayer.IsUserInVR())
            //{
            //    var originTrackingData = linkedPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);
            //    bodyColliderParent.transform.position = originTrackingData.position;
            //}
            //else 
            
            if(linkedPlayer.isLocal)
            {
                bodyColliderParent.transform.position = linkedPlayer.GetPosition();
                // Update the HUD to track player head
                var headTrackingData = linkedPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                canvasParent.transform.position = headTrackingData.position;
                canvasParent.transform.rotation = headTrackingData.rotation;

                if (isRespawning && respawnTimer <= 0.0f)
                    RespawnPlayerLocal();
                else respawnTimer -= Time.deltaTime;
            }
        }
    }

    private void OnTriggerEnter(Collider collider)
    {
        Debug.Log($"AH! {collider.gameObject.name}");
        return;

        var behaviour = GetBehaviour(collider.gameObject);
        if (behaviour == null || behaviour.GetProgramVariable("isProjectileCombatController") == null)
            return;

        VRCPlayerApi owner = (VRCPlayerApi)behaviour.GetProgramVariable("owner");

        lobby._player = owner;
        lobby.GetPlayerTeam();
        int ownerTeam = lobby._team;

        // Damage player if friendly fire is on, or if the bullet is from an enemy
        if (allowFriendlyFire || ownerTeam != playerTeam)
        {
            float damage = (float)behaviour.GetProgramVariable("projectileDamage");
            damage *= (ownerTeam == playerTeam) ? ffDamageMult : 1.0f;

            if (damage > 0.0f && !linkedPlayer.isLocal)
            {

            }

            //currentHealth -= damage;
            //UpdateHealthSlider();
        }

        if (currentHealth < 0.0f)
        {
            linkedPlayer.Respawn();
            currentHealth = maxHealth;
        }
    }

    //========== U# BEHAVIOUR ==========

    public override void OnDeserialization()
    {
    }

    // ========== PUBLIC ==========

    public void InitController()
    {
        if (linkedPlayer == null)
        {
            debugText.text += "Tried linking null player!";
            return;
        }
        else if (!inited)
            Start();

        string linkedPlayerString = $"{linkedPlayer.displayName}[{linkedPlayer.playerId}]";
        string localPlayerString = $"{localPlayer.displayName}[{localPlayer.playerId}]";

        debugText.text += $" {linkedPlayerString}: T:{playerTeam} L:{localTeam}";

        Material teamMat = localTeam == playerTeam
            ? combatController.playerAllyMaterial
            : combatController.playerEnemyMaterial;
        foreach (var renderer in bodyColliderMeshs)
            renderer.material = teamMat;

        // Dont want to see our own colliders
        if (linkedPlayer.isLocal)
        {
            currentHealth = maxHealth;
            canvasParent.SetActive(true);
            SetCollidersVisible(false);
            debugText.text += " off";
            UpdateHealthSlider();
        }
        else
        {
            SetCollidersVisible(combatController.gameLobby.uiSettings.showColliders);
            canvasParent.SetActive(false);
            debugText.text += " on";
        }

        this.enabled = true;

        if (!linkedPlayer.isLocal)
        {
            Debug.Log($"Giving ownership to {linkedPlayerString} from {localPlayerString}");
            Networking.SetOwner(linkedPlayer, this.gameObject);

            // Set all children to be owned by this player.
            int numChildren = this.gameObject.transform.childCount;
            for (int i = 0; i < numChildren; ++i)
                Networking.SetOwner(linkedPlayer, this.gameObject.transform.GetChild(i).gameObject);

            RequestSerialization();
        }
    }

    [HideInInspector] public float _damage;

    public void DamagePlayer()
    {
        Debug.Log("Damage - player phase");
        if (linkedPlayer.isLocal)
        {
            debugText.text += $"\n{linkedPlayer.displayName}[{linkedPlayer.playerId}] {currentHealth}->{currentHealth - _damage}";
            currentHealth -= _damage;

            UpdateHealthSlider();
            if (currentHealth <= 0)
            {
                PlayerDeathLocal();
            }
        }
    }

    public void PlayerDeathLocal()
    {
        var pickupLeft = localPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Left);
        var pickupRight = localPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Right);

        debugText.text += "\nYou Died, repsawning.";
        isRespawning = true;
        respawnTimer = respawnTime;
        localPlayer.Immobilize(true);

        // Drop weapons and prevent pickup
        if (pickupLeft != null)
            pickupLeft.Drop();
        if (pickupRight != null)
            pickupRight.Drop();
        localPlayer.EnablePickups(false);

        this.SendCustomNetworkEvent(NetworkEventTarget.All, "PlayerDeathGlobal");
    }
    public void PlayerDeathGlobal()
    {
        bodyColliderParent.SetActive(false);
    }

    public void RespawnPlayerLocal()
    {
        debugText.text += "\nRespawned!";

        // Renable collisions, weapons, movement
        localPlayer.Immobilize(false);
        localPlayer.EnablePickups(true);

        // tp to a random spawn
        var t = GetRandomTeamSpawn();
        localPlayer.TeleportTo(t.position, t.rotation);

        // Reset hp
        isRespawning = false;
        currentHealth = maxHealth;
        UpdateHealthSlider();

        this.SendCustomNetworkEvent(NetworkEventTarget.All, "RespawnPlayerGlobal");
    }
    public void RespawnPlayerGlobal()
    {
        bodyColliderParent.SetActive(true);
    }

    // ========== PRIVATE ==========

    void UpdateHealthSlider()
    {
        healthSlider.value = currentHealth / maxHealth;
    }

    Transform GetRandomTeamSpawn()
    {
        var teamSpawns = combatController.allTeamSpawns[playerTeam];
        int rIndex = Random.Range(0, teamSpawns.Length);
        return teamSpawns[rIndex].transform;
    }

    UdonBehaviour GetBehaviour(GameObject obj)
    {
        return (UdonBehaviour)obj.GetComponent(typeof(UdonBehaviour));
    }

    void SetCollidersVisible(bool value)
    {
        foreach (var meshRenderer in bodyColliderMeshs)
            meshRenderer.enabled = value;
    }
}

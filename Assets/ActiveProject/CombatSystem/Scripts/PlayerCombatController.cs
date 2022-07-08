
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class PlayerCombatController : UdonSharpBehaviour
{
    [Header("Health Settings")]
    public float maxHealth;
    [UdonSynced] public float currentHealth;
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

    // Private
    bool inited = false;
    VRCPlayerApi localPlayer;
    Collider[] bodyColliders;
    MeshRenderer[] bodyColliderMeshs;
    LobbyController lobby;

    private void Start()
    {
        if (!inited)
        {
            lobby = combatController.gameLobby;
            localPlayer = Networking.LocalPlayer;

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

        debugText.text += $"{linkedPlayer.displayName}[{linkedPlayer.playerId}]: T:{playerTeam} L:{localTeam}";

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
            foreach (var renderer in bodyColliderMeshs)
                renderer.enabled = false;
            debugText.text += " off";
        }
        else
        {
            foreach (var renderer in bodyColliderMeshs)
                renderer.enabled = true;
            canvasParent.SetActive(false);
            debugText.text += " on";
        }

        this.enabled = true;

        if (linkedPlayer.isLocal)
        {
            Networking.SetOwner(linkedPlayer, this.gameObject);
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
        }
    }

    // ========== PRIVATE ==========

    void UpdateHealthSlider()
    {
        healthSlider.value = currentHealth / maxHealth;
    }

    UdonBehaviour GetBehaviour(GameObject obj)
    {
        return (UdonBehaviour)obj.GetComponent(typeof(UdonBehaviour));
    }
}

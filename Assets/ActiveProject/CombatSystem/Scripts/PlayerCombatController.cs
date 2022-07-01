
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class PlayerCombatController : UdonSharpBehaviour
{
    [Header("Health Settings")]
    public float maxHealth;
    public float currentHealth;
    public Slider healthSlider;

    [Header("Colliders")]
    public GameObject bodyColliderParent;
    public float bodyColliderHeight;

    [Header("External References")]
    public GameObject canvasParent;
    public CombatController combatController;

    // Externally Set
    [HideInInspector] public int playerTeam = -1;
    [HideInInspector] public bool allowFriendlyFire;

    // Private
    VRCPlayerApi linkedPlayer;
    Collider[] bodyColliders;
    MeshRenderer[] bodyColliderMeshs;

    public void Start()
    {
        linkedPlayer = Networking.LocalPlayer;

        bodyColliders = bodyColliderParent.GetComponentsInChildren<Collider>();
        bodyColliderMeshs = bodyColliderParent.GetComponentsInChildren<MeshRenderer>();

        // Ensure we dont have any collision. Bad things happen otherwise.
        foreach (var collider in bodyColliders)
            collider.isTrigger = true;

        // Dont want to see our own colliders
        if (linkedPlayer.isLocal)
        {
            foreach (var renderer in bodyColliderMeshs)
                renderer.enabled = false;
        }
        else
        {
            canvasParent.SetActive(false);
        }
    }

    public void Update()
    {
        if (linkedPlayer.isLocal)
        {
            // Update the body colliders to be under the player
            var originTrackingData = linkedPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);
            bodyColliderParent.transform.position = originTrackingData.position + new Vector3(0, 1, 0);

            // Update the HUD to track player head
            var headTrackingData = linkedPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            canvasParent.transform.position = headTrackingData.position;
            canvasParent.transform.rotation = headTrackingData.rotation;
        }
    }

    public void OnTriggerEnter(Collider collider)
    {
        // Ignore collisions on other players
        if (!linkedPlayer.isLocal)
            return;

        Debug.Log($"AH! {collider.gameObject.name}");

        var behaviour = GetBehaviour(collider.gameObject);
        if (behaviour == null || behaviour.GetProgramVariable("isProjectileCombatController") == null)
            return;

        VRCPlayerApi owner = (VRCPlayerApi)behaviour.GetProgramVariable("owner");

        combatController._player = owner;
        combatController.GetPlayerTeam();
        int ownerTeam = combatController._team;

        // Damage player if friendly fire is on, or if the bullet is from an enemy
        if (allowFriendlyFire || ownerTeam != playerTeam)
        {
            float damage = (float)behaviour.GetProgramVariable("projectileDamage");
            currentHealth -= damage;
            UpdateHealthSlider();
        }

        if (currentHealth < 0.0f)
        {
            linkedPlayer.Respawn();
            currentHealth = maxHealth;
        }
    }

    void UpdateHealthSlider()
    {
        healthSlider.value = currentHealth / maxHealth;
    }

    UdonBehaviour GetBehaviour(GameObject obj)
    {
        return (UdonBehaviour)obj.GetComponent(typeof(UdonBehaviour));
    }
}


using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

/*
 * TODO:
 *      When Udon/U# is more mature, a centrallized system will likely be possible. For now, this class is irrelevant.
 */
public class CombatController : UdonSharpBehaviour
{
    [Header("Player Colliders")]
    public Material playerAllyMaterial;
    public Material playerEnemyMaterial;
    public Material playerNeutralMaterial;

    [Header("Player Combat Controller")]
    public GameObject playerCombatPrefab;

    // Private Vars
    GameObject[] allControllers;

    void AddToStart()
    {
        allControllers = new GameObject[maxPlayers];

        for (int i = 0; i < maxPlayers; ++i)
        {
            allControllers[i] = VRCInstantiate(playerCombatPrefab);

            var playerBehaviour = GetBehaviour(allControllers[i]);
            playerBehaviour.SetProgramVariable("combatController", this);
        }
    }

    [Header("Lobby Settings")]
    public int maxPlayers = 12;
    public bool enableTeamLimits = false;
    public int maxTeamSize = 6;

    // Private vars 
    int currentPlayers;
    int[] playerSlots;
    int[] playerTeams;
    VRCPlayerApi[] allPlayers;

    // ========== MONO BEHAVIOUR ==========

    protected void Start()
    {
        currentPlayers = 0;
        playerSlots = new int[maxPlayers];
        playerTeams = new int[maxPlayers];
        allPlayers = new VRCPlayerApi[maxPlayers];
        allControllers = new GameObject[maxPlayers];

        for (int i = 0; i < maxPlayers; ++i)
        {
            playerSlots[i] = -1;
            playerTeams[i] = -1;
            allControllers[i] = VRCInstantiate(playerCombatPrefab);

            var playerBehaviour = GetBehaviour(allControllers[i]);
            playerBehaviour.SetProgramVariable("combatController", this);
        }
    }

    // ========== U# BEHAVIOUR ==========

    public override void OnPlayerJoined(VRCPlayerApi player) { }
    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        //RemovePlayerFromLobby(player);
    }

    // ========== PUBLIC ==========
    // These vars are so we can set arguments before calling the event
    [HideInInspector] public int _team;
    [HideInInspector] public VRCPlayerApi _player;

    public void AddPlayerToLobby() => AddPlayerToLobbyInternal(_player, _team);
    private void AddPlayerToLobbyInternal(VRCPlayerApi player, int team)
    {
        if (currentPlayers >= maxPlayers)
        {
            Debug.LogWarning("Cannot add player: lobby full");
            return;
        }
        if (IsTeamFull(team))
        {
            Debug.LogWarning($"Cannot add player: team {_team} full");
            return;
        }

        for (int i = 0; i < maxPlayers; ++i)
            if (playerSlots[i] < 0)
            {
                ++currentPlayers;
                playerSlots[i] = _player.playerId;
                playerTeams[i] = _team;
                allPlayers[i] = _player;
                Debug.Log($"Added player to slot {i}");
            }
    }

    public void RemovePlayerFromLobby() => RemovePlayerFromLobbyInternal(_player);
    private void RemovePlayerFromLobbyInternal(VRCPlayerApi player)
    {
        if (currentPlayers <= 0)
        {
            Debug.LogWarning("Cannot remove player: lobby empty");
            return;
        }

        for (int i = 0; i < maxPlayers; ++i)
            if (playerSlots[i] == _player.playerId)
            {
                --currentPlayers;
                playerSlots[i] = -1;
                playerTeams[i] = -1;
                allPlayers[i] = null;
                Debug.Log($"Removed player from slot {i}");
            }
    }

    public void GetPlayerTeam() => _team = GetPlayerTeamInternal(_player);
    private int GetPlayerTeamInternal(VRCPlayerApi player)
    {
        for (int i = 0; i < maxPlayers; ++i)
            if (playerSlots[i] == player.playerId)
                return playerTeams[i];
        return -1;
    }

    // ========== PRIVATE ==========

    private void UpdateLobby()
    {

    }

    // Will always return false for teamId < 0
    private bool IsTeamFull(int team)
    {
        if (enableTeamLimits && team >= 0)
        {
            int count = 0;
            foreach (int teamId in playerTeams)
                if (teamId == team)
                    ++count;
            return count > maxTeamSize;
        }
        return false;
    }

    UdonBehaviour GetBehaviour(GameObject obj)
    {
        return (UdonBehaviour)obj.GetComponent(typeof(UdonBehaviour));
    }
}

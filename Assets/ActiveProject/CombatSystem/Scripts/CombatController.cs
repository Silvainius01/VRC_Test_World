
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Components;

/*
 *  The combat controller is meant to ONLY handle:
 *      - Combat specific settings
 *      - Managment of other combat controllers
 */
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class CombatController : UdonSharpBehaviour
{
    [Header("Scenario Settings")]
    [Tooltip("How long the game lasts (in seconds)")]
    public float gameLength = 5 * 60;
    [Tooltip("The parent objects for all team spawn locations. If the object has no children, the parent will be used instead.")]
    public GameObject[] teamSpawns;

    [Header("Combat Settings")]
    [Tooltip("If enabled, players on the same team can damage each other.")]
    public bool allowFriendlyFire;
    [Tooltip("Damage modifier against friendly targets")]
    public float friendlyFireDamageMult = 1.0f;

    [Header("External References")]
    public Material playerAllyMaterial;
    public Material playerEnemyMaterial;
    public Material playerNeutralMaterial;
    public LobbyController gameLobby;
    public GameObject playerCombatPrefab;
    public VRCObjectPool playerCombatPool;
    public Text debugText;
    public GameObject[] rangedWeaponObjects;

    // Externally set
    [HideInInspector] public int[] playerSlots = null;
    [HideInInspector] public int[] playerTeams = null;
    [HideInInspector] public VRCPlayerApi[] allPlayers = null;

    // Private vars
    int numTeams;
    int maxPlayers;
    bool gameStarted;
    VRCPlayerApi localPlayer;
    GameObject[] allControllers;
    GameObject[][] allTeamSpawns;

    // Timers
    float timerGameLength;

    // ========== MONO BEHAVIOUR ==========

    void Start()
    {
        numTeams = gameLobby.numTeams;
        localPlayer = Networking.LocalPlayer;
        maxPlayers = gameLobby.maxPlayers;

        allTeamSpawns = new GameObject[numTeams][];
        allControllers = new GameObject[maxPlayers];

        foreach (var controller in playerCombatPool.Pool)
        {
            var playerBehaviour = GetBehaviour(controller);
            playerBehaviour.SetProgramVariable("combatController", this);
            playerBehaviour.SetProgramVariable("allowFriendlyFire", allowFriendlyFire);
            playerBehaviour.SetProgramVariable("ffDamageMult", friendlyFireDamageMult);
            playerBehaviour.SetProgramVariable("debugText", debugText);
        }

        for(int i = 0; i < numTeams; ++i)
        {
            int numSpawns = teamSpawns[i].transform.childCount;

            // If there are no child spawns, use the parent.
            if (numSpawns <= 0)
                allTeamSpawns[i] = new GameObject[1] { teamSpawns[i] };
            else // Fill the spawns with the children
            {
                allTeamSpawns[i] = new GameObject[numSpawns];
                for (int j = 0; j < numSpawns; ++j)
                    allTeamSpawns[i][j] = teamSpawns[i].transform.GetChild(j).gameObject;
            }
        }

        foreach(var rangedControllerObject in rangedWeaponObjects)
        {
            var behaviour = rangedControllerObject.GetComponent<RangedWeaponCombatController>();

            if (behaviour == null)
            {
                Debug.LogError($"{rangedControllerObject.name} is not a ranged weapon.");
                continue;
            }

            behaviour.combatController = this;
        }
    }

    private void Update()
    {
        if (gameStarted && timerGameLength > 0.0f)
        {
            timerGameLength -= Time.deltaTime;

            // End the game
            if(timerGameLength <= 0.0f)
            {
                EndGame();
            }
        }
    }

    // ========== PUBLIC ==========

    [HideInInspector] public int _localIndex;
    [HideInInspector] public int _playerId;
    // [HideInInspector] public UdonBehaviour _playerControllerRetval;

    public void StartGame()
    {
        debugText.text += "\nCombat controller started.";

        if (playerSlots == null)
            debugText.text += "\nplayerSlots[] null!";
        if (playerTeams == null)
            debugText.text += "\nplayerTeams[] null!";
        if (allPlayers == null)
            debugText.text += "\allPlayers[] null!";

        debugText.text += $"\ns: {playerSlots.Length} t: {playerTeams.Length} p: {allPlayers.Length}";

        for (int i = 0; i < maxPlayers; ++i)
        {
            debugText.text += $"\nP{i} ";
            if (playerSlots[i] >= 0)
            {
                int playerTeam = playerTeams[i];
                GameObject controller = playerCombatPool.Pool[i];

                controller.SetActive(true);
                allControllers[i] = controller;

                var playerCombatCont = GetBehaviour(controller);
                playerCombatCont.enabled = allPlayers[i].isLocal; // Only enable the local controller.
                playerCombatCont.SetProgramVariable("localTeam", playerTeams[_localIndex]);
                playerCombatCont.SetProgramVariable("playerTeam", playerTeams[i]);
                playerCombatCont.SetProgramVariable("linkedPlayer", allPlayers[i]);
                playerCombatCont.SendCustomEvent("InitController");

                if(localPlayer.playerId == playerSlots[i])
                    foreach(var weaponObject in rangedWeaponObjects)
                    {
                        var weaponBehavior = GetBehaviour(weaponObject);
                        weaponBehavior.SetProgramVariable("localPlayerController", playerCombatCont);
                    }
            }
        }


        gameStarted = true;
        timerGameLength = gameLength;
    }

    public void EndGame()
    {
        for (int i = 0; i < maxPlayers; ++i)
        {
            allControllers[i].SetActive(false);
            if (playerSlots[i] >= 0)
            {
                allPlayers[i].Respawn();
            }
        }

        allPlayers = null;
        playerSlots = null;
        playerTeams = null;
        gameStarted = false;
        timerGameLength = 0.0f;
    }

    //public void GetPlayerCombatController()
    //{
    //    for(int i = 0; i < maxPlayers; ++i)
    //    {
    //        if(playerSlots[i] == _playerId)
    //        {
    //            _playerControllerRetval = GetBehaviour(allControllers[i]);
    //        }
    //    }
    //}

    // ========== PRIVATE ==========

    UdonBehaviour GetBehaviour(GameObject obj)
    {
        return (UdonBehaviour)obj.GetComponent(typeof(UdonBehaviour));
    }
}

﻿
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;


/*
 * TODO:
 *  - Add "cancel game" button
 *  - Add "master start only" toggle [synced]
 *  - Add "auto balance" toggle [synced]
 *  - Add "show colliders" button [local]
 *  - Add "avatar-sized colliders" button [synced]
 */

// This is not really intended to be used as is.
// Instead, it is instead to house the code for a functional lobby
// so that it can be copy+pasted else where. Hopefully U# gets inheritance eventually. 
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class LobbyController : UdonSharpBehaviour
{
    [Header("Lobby Settings")]
    public int maxPlayers = 12;
    public int minPlayers = 2;

    [Header("Team Settings")]
    [Tooltip("If enabled, players must form teams manually.\n\nIf disabled, the lobbyTrigger adds people to the neutral team, and are distributed randomly.")]
    public bool joinByTeam = false;
    [Tooltip("The number of teams in this game mode. Set to one if FFA, PvE, co-op, etc.\n\nNOTE: at least 3 teams are needed for proper shuffling. One neutral, two opposed.")]
    public int numTeams = 1;
    [Tooltip("If this is a valid team, members will be randomly placed among other teams.\n\nIf joinByTeam is off, then this will be team 0 by default.")]
    public int neutralTeam = 0;
    [Tooltip("If enabled, the lobby will never allow more X players on a team.")]
    public bool enableTeamLimits = false;
    [Tooltip("The maximum amount of players on a team if team size limits are on.")]
    public int maxTeamSize = 6;

    [Header("Synced Player Modifiables")]
    public LobbySettingsController uiSettings;

    [Header("External References")]
    public Canvas lobbyCanvas;
    public CombatController combatController;
    public Button startButton;
    public GameObject[] teamUiObjects;


    [Header("Debug")]
    public Text debugText;

    // Private vars
    int currentPlayers;
    VRCPlayerApi localPlayer;
    VRCPlayerApi[] allPlayers;
    Text[] teamJoinTexts;
    Text[] teamPlayerTexts;
    Button[] teamJoinButtons;
    Text startButtonText;
    LobbyPlayerTriggerController lobbyAreaController;

    // Private synced
    [UdonSynced] bool lobbyStartedSynced;
    [UdonSynced] int[] playerSlots;
    [UdonSynced] int[] playerTeams;
    [UdonSynced] int[] teamPlayerCounts;

    #region ========== MONO BEHAVIOUR ==========

    private void Start()
    {
        // Init static references
        debugText.text += "\nInitializing lobby: ";

        localPlayer = Networking.LocalPlayer;
        startButtonText = startButton.GetComponentInChildren<Text>();
        lobbyAreaController = GetComponentInChildren<LobbyPlayerTriggerController>();

        InitPlayerSlots();
        InitTeamSlots();
    }

    #endregion

    #region ========== U# BEHAVIOUR ==========

    public override void OnDeserialization()
    {
        debugText.text += "\nLobby received data: ";
        var localPlayer = Networking.LocalPlayer;
        if (localPlayer.isMaster && !localPlayer.IsOwner(this.gameObject))
        {
            Networking.SetOwner(localPlayer, this.gameObject);
            debugText.text += " master -> owner.";
        }
        UpdateLobbyLocal();

        debugText.text += $" start={lobbyStartedSynced}";

        //if (lobbyStartedSynced)
        //{
        //    StartGameLobbyPostSync();
        //    lobbyStartedSynced = false;
        //}
    }
    public override void OnPostSerialization(SerializationResult result)
    {
        if (!localPlayer.isMaster)
            return;

        Debug.Log("Post serialize!!");
        debugText.text += " Post serialize! ";
        if (lobbyStartedSynced)
        {
            // Fire the start lobby for everyone else.
            SendCustomNetworkEvent(NetworkEventTarget.All, "StartGameLobbyGlobal");
        }
    }

    public override void OnPlayerJoined(VRCPlayerApi player) 
    {
        // sync the lobby to new players
        SyncBehaviour();
    }
    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        RemovePlayerFromLobbyInternal(player);
    }

    public void StartGameButton()
    {
        if (currentPlayers >= minPlayers)
            if (localPlayer.isMaster)
            {
                StartGameLobbyLocal();
                return;
            }
            else if(!uiSettings.masterStartOnly)
            {
                this.SendCustomNetworkEvent(NetworkEventTarget.Owner, "StartGameLobbyLocal");
                return;
            }
        debugText.text = $"Attempted start:\nm:{localPlayer.isMaster || !uiSettings.masterStartOnly} p:{currentPlayers >= minPlayers}";
    }

    public void EndGameButton()
    {
        if (localPlayer.isMaster)
        {
            // End game for everyone locally
            debugText.text = "EndGame Master:";
            combatController.SendCustomNetworkEvent(NetworkEventTarget.All, "EndGame");
            this.SendCustomNetworkEvent(NetworkEventTarget.All, "ResetGameLobby");
            return;
        }
        else if (!uiSettings.masterStartOnly)
        {
            this.SendCustomNetworkEvent(NetworkEventTarget.Owner, "EndGameButton");
            return;
        }
    }

    #endregion

    #region ========== PUBLIC ==========
    // ITs unclear how to consitently have references to this class directly.
    // As such, in the event we must call exposed methods through custom events
    //   these arguments are public so we can set them before firing the event.
    [HideInInspector] public int _team;
    [HideInInspector] public VRCPlayerApi _player;

    public void OnPlayerLobbyInteract() => OnPlayerLobbyInteractInternal(_player, _team);
    private void OnPlayerLobbyInteractInternal(VRCPlayerApi player, int team)
    {
        int playerIndex = GetPlayerIdIndex(player.playerId);
        debugText.text += $"\nLobby got event: p{player.playerId} i{playerIndex} ";

        if(playerIndex >= 0)
        {
            if (team != playerTeams[playerIndex])
            {
                playerTeams[playerIndex] = team;
                debugText.text += "Swapped teams.";
                UpdateLobbyGlobal();
            }
            else
            {
                debugText.text += "Left lobby.";
                RemovePlayerFromLobbyInternal(player);
            }
        }
        else
        {
            debugText.text += "Joined lobby.";
            AddPlayerToLobbyInternal(player, team);
        }
    }

    public void RemovePlayerFromLobby() => RemovePlayerFromLobbyInternal(_player);

    // outputs to _team. Super scuffed.
    public void GetPlayerTeam() => _team = GetPlayerTeamInternal(_player);
    private int GetPlayerTeamInternal(VRCPlayerApi player)
    {
        for (int i = 0; i < maxPlayers; ++i)
            if (playerSlots[i] == player.playerId)
                return playerTeams[i];
        return -1;
    }

    public void StartGameLobbyLocal()
    {
        if (!localPlayer.isMaster)
        {
            Debug.LogError("Cannot start lobby: not master");
            debugText.text = "Cannot start lobby: not master ";
            return;
        }

        lobbyStartedSynced = true;
        debugText.text = "Starting lobby: ";
        ShuffleNeutralPlayers();

        debugText.text += "\nEmitting lobby data";

        // Update the lobby for all players.
        UpdateLobbyGlobal();
    }
    public void StartGameLobbyGlobal()
    {
        if (lobbyStartedSynced)
        {
            lobbyStartedSynced = false;
            StartGameLobbyPostSync();
        }
        else
        {
            Debug.LogError("Lobby start failed: start sync false");
            debugText.text = "Lobby start failed: start sync false";
        }
    }

    public void ResetGameLobby()
    {
        debugText.text += $"\nResetting lobby";

        startButton.interactable = true;
        startButtonText.text = "Start Game";

        for(int i = 0; i < numTeams; ++i)
        {
            bool isActiveTeam = joinByTeam 
                ? i != neutralTeam
                : i == neutralTeam;

            teamPlayerTexts[i].text = string.Empty;
            teamJoinButtons[i].interactable = joinByTeam && isActiveTeam;
            teamUiObjects[i].SetActive(isActiveTeam);
        }

        if(!joinByTeam)
            lobbyAreaController.triggerArea.enabled = true;

        for (int i = 0; i < maxPlayers; ++i)
        {
            playerSlots[i] = -1;
            playerTeams[i] = -1;
        }

        if(localPlayer.IsOwner(gameObject))
            UpdateLobbyGlobal();
    }
#endregion

#region ========== PRIVATE ==========

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

    private bool IsPlayerInLobby(VRCPlayerApi player) => IsPlayerIdInLobby(player.playerId);
    private bool IsPlayerIdInLobby(int playerId)
    {
        foreach (int id in playerSlots)
            if (id == playerId)
                return true;
        return false;
    }

    private int GetPlayerIndex(VRCPlayerApi player) => GetPlayerIdIndex(player.playerId);
    private int GetPlayerIdIndex(int playerId)
    {
        for (int i = 0; i < maxPlayers; ++i)
            if (playerSlots[i] == playerId)
                return i;
        return -1;
    }

    private void SyncBehaviour()
    {
        if (!localPlayer.IsOwner(this.gameObject))
            return;
        debugText.text += " Sync.";
        RequestSerialization();
    }

    private void UpdateLobbyGlobal()
    {
        debugText.text += "\nUpdating lobby:";
        SyncBehaviour();
        UpdateLobbyLocal();
    }
    private void UpdateLobbyLocal()
    {
        currentPlayers = 0;

        // Clear Team Texts
        for (int i = 0; i < numTeams; ++i)
        {
            teamJoinTexts[i].text = "Join";
            teamPlayerTexts[i].text = string.Empty;
            teamPlayerCounts[i] = 0;
        }

        for (int i = 0; i < maxPlayers; ++i)
            if (playerSlots[i] > -1)
            {
                ++currentPlayers;
                allPlayers[i] = VRCPlayerApi.GetPlayerById(playerSlots[i]);

                int team = playerTeams[i];
                if (team > -1)
                {
                    teamPlayerTexts[team].text += $"\n{allPlayers[i].displayName}";
                    if (playerSlots[i] == Networking.LocalPlayer.playerId)
                        teamJoinTexts[team].text = "Leave";
                    ++teamPlayerCounts[team];
                }
            }
        debugText.text += " Done.";
    }

    private UdonBehaviour GetBehaviour(GameObject obj)
    {
        return (UdonBehaviour)obj.GetComponent(typeof(UdonBehaviour));
    }

    private void InitPlayerSlots()
    {
        debugText.text += "\nIniting player slot data.";

        currentPlayers = 0;
        playerSlots = new int[maxPlayers];
        playerTeams = new int[maxPlayers];
        allPlayers = new VRCPlayerApi[maxPlayers];

        for (int i = 0; i < maxPlayers; ++i)
        {
            playerSlots[i] = -1;
            playerTeams[i] = -1;
        }
    }

    private void InitTeamSlots()
    {
        if (teamUiObjects.Length < numTeams)
        {
            debugText.text = "\nNOT ENOUGH TEAM OBJECTS!!";
            Debug.LogError("Not enough team UI objects!");
            return;
        }

        debugText.text += "\nIniting team slot data.";
        teamJoinTexts = new Text[numTeams];
        teamPlayerTexts = new Text[numTeams];
        teamJoinButtons = new Button[numTeams];
        teamPlayerCounts = new int[numTeams];

        // Neutral Team is invalid if:
        //      - It is less than zero OR
        //      - It is greater than the max team index.
        // If join by team is off, set neutral team to be team0, otherwise there is no neutral team.
        if (neutralTeam < 0 || neutralTeam >= numTeams)
            neutralTeam = joinByTeam ? -1 : 0;

        // Disable neutral join area if we are joining teams directly.
        if (joinByTeam)
            lobbyAreaController.triggerArea.enabled = false;

        for (int i = 0; i < numTeams; ++i)
        {
            var teamUiObject = teamUiObjects[i];
            var teamJoinObject = teamUiObject.transform.Find("JoinTeam").gameObject;
            var teamJoinButton = GetBehaviour(teamJoinObject);

            teamJoinButtons[i] = teamJoinObject.GetComponent<Button>();
            teamJoinButton.SetProgramVariable("team", i);
            teamJoinButton.SetProgramVariable("lobby", this);
            teamJoinButton.SendCustomEvent("Init");

            teamJoinTexts[i] = teamUiObject.transform.Find("JoinTeam/JoinText").GetComponent<Text>();

            teamPlayerTexts[i] = teamUiObject.transform.Find("Players/PlayerText").GetComponent<Text>();
            teamPlayerTexts[i].text = string.Empty;

            if (!joinByTeam)
            {
                // Only the neutral team is visible
                teamUiObject.SetActive(i == neutralTeam);
                teamJoinButtons[i].interactable = false;
            }
        }
    }

    private void AddPlayerToLobbyInternal(VRCPlayerApi player, int team)
    {
        if (currentPlayers >= maxPlayers)
        {
            Debug.LogWarning("Cannot add player: lobby full");
            return;
        }
        if (IsTeamFull(team))
        {
            Debug.LogWarning($"Cannot add player: team {team} full");
            return;
        }

        for (int i = 0; i < maxPlayers; ++i)
            if (playerSlots[i] < 0)
            {
                playerSlots[i] = player.playerId;
                playerTeams[i] = team;
                allPlayers[i] = player;
                break;
            }

        UpdateLobbyGlobal();
    }

    private void RemovePlayerFromLobbyInternal(VRCPlayerApi player)
    {
        if (currentPlayers <= 0)
        {
            Debug.LogWarning("Cannot remove player: lobby empty");
            return;
        }

        for (int i = 0; i < maxPlayers; ++i)
            if (playerSlots[i] == player.playerId)
            {
                playerSlots[i] = -1;
                playerTeams[i] = -1;
                allPlayers[i] = null;
                Debug.Log($"Removed player from slot {i}");
            }

        UpdateLobbyGlobal();
    }

    private void ShuffleNeutralPlayers()
    {
        // Cannot shuffle if not the owner, or there is there are less than 2 teams.
        // (at least 3 are needed: one neutral, two opposed)
        bool canShuffle = localPlayer.isMaster && numTeams > 2;
        Debug.Log($"Can Shuffle: {localPlayer.isMaster} && {numTeams > 2}");

        // Cant shuffle if there is no neutral team, or if there is no on the neutral team.
        bool neutralTeamActive = neutralTeam >= 0 && teamPlayerCounts[neutralTeam] > 0;
        Debug.Log($"neutralTeamActive: {neutralTeam >= 0} && {teamPlayerCounts[neutralTeam] > 0}");

        if (!canShuffle || !neutralTeamActive)
            return;

        debugText.text += "\nShuffling players";

        int smallestTeam = neutralTeam;
        for(int i = 0; i < maxPlayers; ++i)
        {
            if(playerSlots[i] >= 0 && playerTeams[i] == neutralTeam)
            {
                smallestTeam = GetRandomSmallestTeam();
                if (smallestTeam == neutralTeam)
                    smallestTeam = GetRandomTeam();
                SetPlayerTeamLocal(i, smallestTeam);
            }
        }
    }

    // Returns a random team from the set of those with the least players
    private int GetRandomSmallestTeam()
    {
        // McGuiver a List<int>
        int count = 0; // List.Count
        int smallestTeam = neutralTeam; // List[0]
        int[] smallTeams = new int[numTeams]; // List.Capacity
        string msg = string.Empty;

        for (int i = 0; i < numTeams; ++i)
        {
            msg += $"\nTeam {i} count: {teamPlayerCounts[i]}";
            if (i == neutralTeam)
                continue;

            // New minimum is found if:
            //  - It is less than the prev smallest team
            //  - It is the first non-neutral team
            if (teamPlayerCounts[i] < teamPlayerCounts[smallestTeam] || smallestTeam == neutralTeam)
            {
                // Clear the list and set this team as the first entry
                count = 1;
                smallestTeam = i;
                smallTeams[0] = i;
            }
            else if (teamPlayerCounts[i] == teamPlayerCounts[smallestTeam])
            {
                // If a team is the same size, add it to the list.
                smallTeams[count] = i;
                ++count;
            };
        }
        debugText.text += msg;
        // Return a random smallest team, or the neutral team if none found somehow.
        return count > 0 ? smallTeams[Random.Range(0, count)] : neutralTeam;
    }

    // Return a random non-neutral team.
    private int GetRandomTeam()
    {
        if (numTeams == 1)
        {
            Debug.LogError("Cannot get random team: only one exists!");
            return 0;
        }
        else if(numTeams == 2 && (neutralTeam == 0 || neutralTeam == 1))
        {
            Debug.LogError("Cannot get random team: only one non-neutral one exists!");
            return (neutralTeam + 1) % 2; // 0 + 1 = 1 // 1 + 1 = 0
        }
        
        int r = neutralTeam;
        while (r == neutralTeam)
            r = Random.Range(0, numTeams);
        return r;
    }

    /// <summary>
    /// Only ran by the master when shuffling teams.
    /// </summary>
    private void SetPlayerTeamLocal(int player, int newTeam)
    {
        int currentTeam = playerTeams[player];
        --teamPlayerCounts[currentTeam];
        ++teamPlayerCounts[newTeam];
        playerTeams[player] = newTeam;
        debugText.text += $"\nSet P{player} to team {newTeam}";
    }

    private void StartGameLobbyPostSync()
    {
        if(!localPlayer.isMaster)
            debugText.text = $"Lobby starting: Received.";
        debugText.text += $"\nYOU: {localPlayer.displayName}[{localPlayer.playerId}]";

        // Disable lobby interaction
        startButton.interactable = false;
        startButtonText.text = "Game Started!";
        lobbyAreaController.triggerArea.enabled = false;
        
        for (int i = 0; i < numTeams; ++i)
        {
            teamUiObjects[i].SetActive(i != neutralTeam);
            teamJoinButtons[i].interactable = false;
        }

        int localIndex = -1;

        for (int i = 0; i < maxPlayers; ++i)
        {
            if (playerSlots[i] >= 0 && allPlayers[i].isLocal)
            {
                localIndex = i;
                break;
            }
        }

        debugText.text += $"\nStarting CC. localIndex: {localIndex}";
        combatController._localIndex = localIndex;
        combatController.playerSlots = playerSlots;
        combatController.playerTeams = playerTeams;
        combatController.allPlayers = allPlayers;
        combatController.StartGame();
    }
#endregion
}

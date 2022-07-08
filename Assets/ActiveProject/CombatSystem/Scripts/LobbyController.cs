
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

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
    [Tooltip("If enabled, players must form teams manually.\n\nIf disabled, team 0 will be marked as the neutral team, and all other UIs will be disabled.")]
    public bool joinByTeam = false;
    [Tooltip("The number of teams in this game mode. Set to one if FFA, PvE, co-op, etc.")]
    public int numTeams = 1;
    [Tooltip("If this is a valid team, members will be randomly placed among other teams.")]
    public int neutralTeam = 0;
    [Tooltip("If enabled, the lobby will never allow more X players on a team.")]
    public bool enableTeamLimits = false;
    [Tooltip("The maximum amount of players on a team if team size limits are on.")]
    public int maxTeamSize = 6;

    [Header("External References")]
    public Canvas lobbyCanvas;
    public CombatController combatController;
    public Button startButton;
    public GameObject[] teamUiObjects;

    [Header("Synced Player Modifiables")]
    [Tooltip("If enabled, will attempt to make teams as even as possible. Otherwise, teams are random. Ignored if teams are formed manually.")]
    public bool autoBalanceTeams = true;
    [Tooltip("If enabled, only the master of the world can start the game.")]
    public bool masterStartOnly = true;

    [Header("Local Player Modifiables")]
    [Tooltip("If enabled, player colliders will be rendered in game.")]
    public bool showColliders = true;

    [Header("Debug")]
    public Text debugText;

    // Hidden
    [HideInInspector] public string scriptType = "LobbyController";

    // Private vars
    int currentPlayers;
    [UdonSynced] int[] playerSlots;
    [UdonSynced] int[] playerTeams;
    VRCPlayerApi localPlayer;
    VRCPlayerApi[] allPlayers;
    Text[] teamJoinTexts;
    Text[] teamPlayerTexts;
    Button[] teamJoinButtons;
    Text startButtonText;

    #region ========== MONO BEHAVIOUR ==========

    private void Start()
    {
        // Init static references
        debugText.text += "\nInitializing lobby: ";

        localPlayer = Networking.LocalPlayer;
        startButtonText = startButton.GetComponentInChildren<Text>();

        InitPlayerSlots();
        InitTeamSlots();
    }

    #endregion

    #region ========== U# BEHAVIOUR ==========

    public override void OnDeserialization()
    {
        debugText.text += "\nLobby received data: ";
        var localPlayer = Networking.LocalPlayer;
        if (!localPlayer.IsOwner(this.gameObject) && localPlayer.isMaster)
        {
            Networking.SetOwner(localPlayer, this.gameObject);
            debugText.text += " master -> owner.";
        }
        UpdateLobby();
    }

    public override void OnPlayerJoined(VRCPlayerApi player) 
    {
        // sync the lobby to new players
        if (player.isMaster)
            RequestSerialization();
    }
    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        RemovePlayerFromLobbyInternal(player);
    }

    // This is for starting the game.
    public override void Interact()
    {
        if (currentPlayers >= minPlayers)
            if (localPlayer.isMaster || !masterStartOnly)
            {
                this.SendCustomNetworkEvent(NetworkEventTarget.All, "StartGameLobby");
                return;
            }
        debugText.text = $"Attempted start:\nm:{localPlayer.isMaster || !masterStartOnly} p:{currentPlayers >= minPlayers}";
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
                UpdateLobby();
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

    // outputs to _team. Super scuffed.
    public void GetPlayerTeam() => _team = GetPlayerTeamInternal(_player);
    private int GetPlayerTeamInternal(VRCPlayerApi player)
    {
        for (int i = 0; i < maxPlayers; ++i)
            if (playerSlots[i] == player.playerId)
                return playerTeams[i];
        return -1;
    }

    public void StartGameLobby()
    {
        debugText.text = $"Lobby starting: {(localPlayer.isMaster ? "Emitted" : "Received")}";

        // Disable lobby buttons
        startButton.interactable = false;
        startButtonText.text = "Game Started!";

        foreach(var button in teamJoinButtons)
        {
            button.interactable = false;
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
        var localPlayer = Networking.LocalPlayer;

        if (!localPlayer.IsOwner(this.gameObject))
            return; // Networking.SetOwner(localPlayer, this.gameObject);

        RequestSerialization();
    }

    private void UpdateLobby()
    {
        debugText.text += "\nUpdating lobby:";
        if (Networking.LocalPlayer.IsOwner(this.gameObject))
        {
            debugText.text += " Sync.";
            SyncBehaviour();
        }

        currentPlayers = 0;

        // Clear Team Texts
        foreach (var text in teamPlayerTexts)
            text.text = string.Empty;
        foreach (var text in teamJoinTexts)
            text.text = "Join";

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

        // Neutral Team is invalid if:
        //      - It is less than zero OR
        //      - It is greater than the max team index.
        // If join by team is off, set neutral team to be team0, otherwise there is no neutral team.
        if (neutralTeam < 0 || neutralTeam >= numTeams)
            neutralTeam = joinByTeam ? -1 : 0;

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

            // Disable all other UIs
            if (!joinByTeam && i != neutralTeam)
                teamUiObject.SetActive(false);
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
            Debug.LogWarning($"Cannot add player: team {_team} full");
            return;
        }

        for (int i = 0; i < maxPlayers; ++i)
            if (playerSlots[i] < 0)
            {
                playerSlots[i] = _player.playerId;
                playerTeams[i] = _team;
                allPlayers[i] = _player;
                break;
            }

        UpdateLobby();
    }

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
                playerSlots[i] = -1;
                playerTeams[i] = -1;
                allPlayers[i] = null;
                Debug.Log($"Removed player from slot {i}");
            }

        UpdateLobby();
    }

    #endregion
}

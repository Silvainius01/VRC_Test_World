
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

// This is not really intended to be used as is.
// Instead, it is instead to house the code for a functional lobby
// so that it can be copy+pasted else where. Hopefully U# gets inheritance eventually. 
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class LobbyController : UdonSharpBehaviour
{
    [Header("Lobby Settings")]
    public int maxPlayers = 12;

    [Header("Team Settings")]
    [Tooltip("If enabled, players must form teams manually. Otherwise, the lobby will auto populate them.")]
    public bool joinByTeam = false;
    [Tooltip("The number of teams in this game mode. Set to one if FFA, PvE, co-op, etc.")]
    public int numTeams = 1;
    [Tooltip("If enabled, the lobby will never allow more X players on a team.")]
    public bool enableTeamLimits = false;
    [Tooltip("The maximum amount of players on a team if team size limits are on.")]
    public int maxTeamSize = 6;

    [Header("External References")]
    public Canvas lobbyCanvas;
    public GameObject[] teamUiObjects;
    public Text debugText;

    [Header("Synced Player Modifiables")]
    [Tooltip("If enabled, will attempt to make teams as even as possible. Otherwise, teams are random. Ignored if teams are formed manually.")]
    [UdonSynced] public bool autoBalanceTeams = true;

    [Header("Local Player Modifiables")]
    public bool showColliders = true;

    // Private vars
    int currentPlayers;
    [UdonSynced] int[] playerSlots;
    [UdonSynced] int[] playerTeams;
    VRCPlayerApi[] allPlayers;
    Text[] teamJoinTexts;
    Text[] teamPlayerTexts;

    #region ========== MONO BEHAVIOUR ==========

    protected void Start()
    {
        // Initialize player slot data
        currentPlayers = 0;
        playerSlots = new int[maxPlayers];
        playerTeams = new int[maxPlayers];
        allPlayers = new VRCPlayerApi[maxPlayers];

        for (int i = 0; i < maxPlayers; ++i)
        {
            playerSlots[i] = -1;
            playerTeams[i] = -1;
        }

        // Initialize team lobbies
        if(teamUiObjects.Length < numTeams)
        {
            Debug.LogError("Not enough team UI objects!");
            return;
        }

        teamJoinTexts = new Text[numTeams];
        teamPlayerTexts = new Text[numTeams];

        for(int i = 0; i < numTeams; ++i)
        {
            var teamUiObject = teamUiObjects[i];
            var teamJoinButton = GetBehaviour(teamUiObject.transform.Find("JoinTeam").gameObject);

            teamJoinButton.SetProgramVariable("team", i);
            teamJoinButton.SetProgramVariable("lobby", this);
            teamJoinButton.SendCustomEvent("Init");

            teamJoinTexts[i] = teamUiObject.transform.Find("JoinTeam/JoinText").GetComponent<Text>();

            teamPlayerTexts[i] = teamUiObject.transform.Find("Players/PlayerText").GetComponent<Text>();
            teamPlayerTexts[i].text = string.Empty;
        }
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

    public override void OnPlayerJoined(VRCPlayerApi player) { }
    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        RemovePlayerFromLobbyInternal(player);
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
                playerSlots[i] = _player.playerId;
                playerTeams[i] = _team;
                allPlayers[i] = _player;
                break;
            }

        UpdateLobby();
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
                playerSlots[i] = -1;
                playerTeams[i] = -1;
                allPlayers[i] = null;
                Debug.Log($"Removed player from slot {i}");
            }

        UpdateLobby();
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

    #endregion
}

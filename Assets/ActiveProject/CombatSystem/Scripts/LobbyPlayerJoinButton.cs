
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class LobbyPlayerJoinButton : UdonSharpBehaviour
{
    // Externally Set
    [HideInInspector] public int team;
    [HideInInspector] public LobbyController lobby;
    [HideInInspector] public Text debugText;

    // Hidden
    [HideInInspector] public string scriptType = "LobbyPlayerJoinButton";

    // Private vars
    VRCPlayerApi localPlayer;
    [UdonSynced] int localPlayerId;

    #region ========== MONO BEHAVIOUR ==========
    private void Start()
    {
        localPlayer = Networking.LocalPlayer;
    }
    #endregion

    #region ========== U# BEHAVIOUR ==========

    public override void Interact()
    {
        // Set local player, then sync if not the owner.
        localPlayerId = localPlayer.playerId;

        debugText.text = $"{localPlayerId} press T{team}: ";

        if (localPlayer.isMaster)
        {
            debugText.text += "Sending event.";
            SendLobbyInteractionEvent();
        }
        else
        {
            debugText.text += " Sync,";
            SyncBehaviour();
        }
    }

    public override void OnDeserialization()
    {
        // When the master recieves new data, update the lobby.
        debugText.text += $"\nT{team} Got data: ";
        SendLobbyInteractionEvent();
    }

    #endregion

    #region ========== PUBLIC ==========

    // Send the joining player data to the lobby. Only usable by the master of the world.
    public void SendLobbyInteractionEvent()
    {
        if (localPlayer.isMaster)
        {
            debugText.text += " sending...";
            lobby._team = team;
            lobby._player = VRCPlayerApi.GetPlayerById(localPlayerId);
            lobby.OnPlayerLobbyInteract();
        }
        else debugText.text += " not master.";
    }

    #endregion

    #region ========== PRIVATE ==========

    private void SyncBehaviour()
    {
        if (!localPlayer.IsOwner(this.gameObject))
        {
            Networking.SetOwner(localPlayer, this.gameObject);
            debugText.text += $" {localPlayer.playerId}->owner.";
        }
        else debugText.text += $" local owner.";
        RequestSerialization();
    }

    #endregion
}

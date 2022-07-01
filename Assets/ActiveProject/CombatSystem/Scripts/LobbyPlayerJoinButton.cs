
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class LobbyPlayerJoinButton : UdonSharpBehaviour
{
    public int team;
    public LobbyController lobby;

    VRCPlayerApi localPlayer;

    #region ========== MONO BEHAVIOUR ==========

    #endregion

    #region ========== U# BEHAVIOUR ==========

    public override void Interact()
    {
        lobby._team = team;
        lobby._player = Networking.LocalPlayer;
        lobby.OnPlayerLobbyInteract();
    }

    #endregion

    #region ========== PUBLIC ==========

    #endregion

    #region ========== PRIVATE ==========

    #endregion
}

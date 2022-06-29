
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class CombatPlayerController : UdonSharpBehaviour
{
    public UdonBehaviour combatController;

    public int numMeters;
    public float[] maxMeterValues;
    public float[] currMeterValues;
    public float[] meterDecayRates;
    public float[] meterTempDecayRates;
    public float[] meterTempDecayTimes;
    public Color[] meterColors;
    // For some reason serializing it works.
    [SerializeField] public string [] meterNames;

    Canvas meterCanvas;
    Slider[] sliders;
    VRCPlayerApi localPlayer;

    public void Start()
    {
        localPlayer = Networking.LocalPlayer;
    }

    // System string not exposed in udon??
    public void OnCollisionEnter(Collision collision)
    {
        var behaviour = GetBehaviour(collision.collider.gameObject);
        string[] splitTags = "".Split('_');

        behaviour.GetProgramVariable<string>("damageTag");

        // Tag not constructed to be used in the system, skip.
        if (splitTags.Length < 2)
        {
            return;
        }

        switch (splitTags[0])
        {
            case "damage":
                break;
            case "decay":
                break;
            default:
                Debug.LogWarning($"Unrecognized damage tag: {splitTags[0]}");
                break;
        }

        if (splitTags.Length > 1)
        {
            bool meterMatch = false;
            for (int i = 0; i < numMeters; ++i)
            {
                //if (meterNames[i] == splitTags[1])
                //    ;
            }
        }
    }

    UdonBehaviour GetBehaviour(GameObject obj)
    {
        return (UdonBehaviour)obj.GetComponent(typeof(UdonBehaviour));
    }
}

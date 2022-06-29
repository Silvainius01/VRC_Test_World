
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ObjectCombatController : UdonSharpBehaviour
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
    [SerializeField] public string[] meterNames;


    void Start()
    {
        
    }
}

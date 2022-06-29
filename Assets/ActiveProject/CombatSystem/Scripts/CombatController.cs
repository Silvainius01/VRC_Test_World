
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class CombatController : UdonSharpBehaviour
{
    [Header("Meter Set Up")]
    [SerializeField] readonly int numMeters;
    [SerializeField] string[] meterNames;
    [SerializeField] float[] maxMeterValues;
    [SerializeField] float[] meterDecayRates;

    [Header("Hitbox Setup")]
    [SerializeField] string[] hitboxTags;
    [SerializeField] float[] tagMultiplier;
    

    public void Start()
    {
#if UNITY_EDITOR
        DebugMeters();
#endif
    }
    void DebugMeters()
    {
        if (numMeters <= 0)
        {
            Debug.LogError("The meter const is set to <= 0. This will break any meter logic within the combat system.");
        }
        else
        {
            if (meterNames.Length != numMeters)
                Debug.LogError($"meterNames has a length of {meterNames.Length}. Add/remove elements to match the value of numMeters: {numMeters}");
            if (meterNames.Length != numMeters)
                Debug.LogError($"meterNames has a length of {meterNames.Length}. Add/remove elements to match the value of numMeters: {numMeters}");
            if (maxMeterValues.Length != numMeters)
                Debug.LogError($"maxMeterValues has a length of {meterNames.Length}. Add/remove elements to match the value of numMeters: {numMeters}");
            if (meterDecayRates.Length != numMeters)
                Debug.LogError($"meterDecayRates has a length of {meterNames.Length}. Add/remove elements to match the value of numMeters: {numMeters}");
        }
    }
}

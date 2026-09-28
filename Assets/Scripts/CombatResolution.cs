using UnityEngine;

public class CombatResolution
{
    public static bool endFight(float attackerPower, float defenderPower)
    {
        return Random.value < (attackerPower / (attackerPower + defenderPower));
    }

    public static float powerDifferential(float thisPower, float otherPower)
    {
        if (otherPower <= 0f)
            return 0f;

        return Mathf.Clamp01(1f - (thisPower /  otherPower));
    }
}

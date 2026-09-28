using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager instance {  get; private set; }
    private HashSet<(Hero, Villain)> activeCombatants = new HashSet<(Hero, Villain)> ();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void LateUpdate()
    {
        activeCombatants.Clear();
    }

    public void InitiateCombat(Hero hero, Villain villain)
    {
        if (activeCombatants.Contains((hero, villain))) return;
        activeCombatants.Add((hero, villain));

        bool heroWins = CombatResolution.endFight(hero.powerLvl, villain.powerLvl);
        Debug.Log($"Combat: {hero.name} (power:{hero.powerLvl:F1}) vs " +
                  $"{villain.name} (power:{villain.powerLvl:F1}) — " +
                  $"{(heroWins ? "Hero wins" : "Villain wins")}");

        if (heroWins)
            onVillainDefeat(villain, hero);
        else
            onHeroDefeat(hero, villain);
    }

    public void InitiateCombat(Vigilante vigilante, Villain villain)
    {
        if (activeCombatants.Contains((null, villain))) return;

        bool vigilanteWins = CombatResolution.endFight(vigilante.powerLevel, villain.powerLvl);
        Debug.Log($"Combat: Vigilante (power:{vigilante.powerLevel:F1}) vs " +
                  $"{villain.name} (power:{villain.powerLvl:F1}) — " +
                  $"{(vigilanteWins ? "Vigilante wins" : "Villain wins")}");

        if (vigilanteWins)
        {
            MoraleManager.Instance.OnVillainDefeated();
            villain.die();
        }
        else
        {
            MoraleManager.Instance.onCivilianOutcome(CivilianOutcome.Killed);
            vigilante.die();
        }
    }

    private void onVillainDefeat(Villain villain, Hero hero)
    {
        if (villain.isSupervillain)
            MoraleManager.Instance.onSupervillainDefeated();
        else
            MoraleManager.Instance.OnVillainDefeated();

        Debug.Log($"{villain.name} defeated and removed");
        CrimeReport.instance?.LogEvent($"{hero.name} defeated {villain.name}!");
        villain.die();
    }

    private void onHeroDefeat(Hero hero, Villain villain)
    {
        float moralePenalty = hero.powerLvl * 2f;
        MoraleManager.Instance.OnHeroDefeated(hero.powerLvl);

        int despawnCivilians = Mathf.RoundToInt(hero.powerLvl / 2f);

        if (despawnCivilians > 0)
        {
            Vector2 posi = new Vector2(hero.transform.position.x, hero.transform.position.z);
            List<Entity> groundZero = QTManager.instance.QueryRadius(posi, 20f);

            int gone = 0;
            foreach (Entity person in groundZero)
            {
                if (gone >= despawnCivilians)
                    break;
                if (person == null)
                    continue;
                if (person is not Civilian civ)
                    continue;
                if (civ.State == CivilianState.Dead)
                    continue;

                civ.doubtInHeroes();
                gone++;
            }
        }
        HeroTracker.instance?.OnHeroDied();
        Debug.Log($"{hero.name} defeated. Morale penalty: {moralePenalty:F1}");
        CrimeReport.instance?.LogEvent($"{villain.name} defeated {hero.name}!");
        hero.die();
    }

    public void forceHeroicDefeat(Hero hero, Villain villain)
    {
        onHeroDefeat(hero, villain);
    }
}

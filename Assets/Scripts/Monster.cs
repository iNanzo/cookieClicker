using UnityEngine;
using System.Collections.Generic;

public class Monster : MonoBehaviour
{
    public MonsterData monsterData; // ScriptableObject reference
    private int currentHealth;
    private int attackPower;
    private MonsterTrait currentTrait; // Primary trait
    private MonsterSize sizeModifier; // Size modifier
    private StateMachine stateMachine;
    private Player player;

    void Start()
    {
        player = Object.FindFirstObjectByType<Player>();

        currentHealth = Random.Range(monsterData.minHP, monsterData.maxHP + 1);
        attackPower = Random.Range(monsterData.minDamage, monsterData.maxDamage + 1);

        // Randomly assign a Primary Trait (50% chance to have no trait)
        if (monsterData.possibleTraits.Length > 0 && Random.value > 0.5f)
        {
            currentTrait = monsterData.possibleTraits[Random.Range(0, monsterData.possibleTraits.Length)];
        }
        else
        {
            currentTrait = MonsterTrait.None;
        }

        // Assign a Size Modifier (50% chance to stay normal)
        if (monsterData.canBeLargeOrSmall)
        {
            float roll = Random.value;
            if (roll < 0.25f) sizeModifier = MonsterSize.Large;
            else if (roll > 0.75f) sizeModifier = MonsterSize.Small;
            else sizeModifier = MonsterSize.Normal;
        }

        // Apply Size Modifiers
        if (sizeModifier == MonsterSize.Large)
        {
            currentHealth = Mathf.RoundToInt(currentHealth * 1.2f); // 20% more HP
            attackPower = Mathf.RoundToInt(attackPower * 1.05f); // 5% more damage
        }
        else if (sizeModifier == MonsterSize.Small)
        {
            currentHealth = Mathf.RoundToInt(currentHealth * 0.9f); // 10% less HP
            attackPower = Mathf.RoundToInt(attackPower * 0.9f); // 10% less damage
        }

        Debug.Log($"{GetMonsterName()} spawned with {currentHealth} HP, {attackPower} Attack, and Trait: {currentTrait}");

        if (stateMachine != null)
        {
            stateMachine.UpdateMonsterHPText();
        }
    }

    void Update()
    {
        if (stateMachine.zoneActive())
        {
            Debug.Log("Destroying monster due to player death.");
            stateMachine.MonsterDefeated();
            Destroy(gameObject);
        }
    }

    public void SetStateMachine(StateMachine machine)
    {
        stateMachine = machine;
        if (stateMachine != null)
        {
            stateMachine.UpdateMonsterHPText();
        }
    }

    void OnMouseDown()
    {
        TakeDamage(1);
        if (currentHealth > 0)
        {
            Retaliate();
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (stateMachine != null)
        {
            stateMachine.UpdateMonsterHPText();
        }
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Retaliate()
    {
        if (player != null)
        {
            int damageDealt = Random.Range(monsterData.minDamage, monsterData.maxDamage + 1);

            if (currentTrait == MonsterTrait.Berserk)
            {
                damageDealt = Mathf.RoundToInt(damageDealt * 1.5f);
            }

            if (sizeModifier == MonsterSize.Large)
            {
                damageDealt = Mathf.RoundToInt(damageDealt * 1.05f);
            }
            else if (sizeModifier == MonsterSize.Small)
            {
                damageDealt = Mathf.RoundToInt(damageDealt * 0.9f);
            }

            if (currentTrait == MonsterTrait.Vampiric)
            {
                int healAmount = Mathf.RoundToInt(damageDealt * 0.1f);
                currentHealth += healAmount;
                Debug.Log($"{GetMonsterName()} healed for {healAmount} HP!");
            }

            player.TakeDamage(damageDealt);
            Debug.Log($"{GetMonsterName()} attacks for {damageDealt} damage!");

            // ✅ Instead of calling Destroy here, we set a flag for Update() to handle it
            if (player.GetCurrentHP() <= 0)
            {
                Debug.Log("Player died! Monster will despawn when zones reappear.");
            }
        }
    }

   void Die()
    {
        Debug.Log($"{GetMonsterName()} defeated!");

        // Calculate gold with trait & size scaling
        int goldReward = monsterData.GetGoldReward(currentTrait, sizeModifier);
        player.AddGold(goldReward);
        Debug.Log($"Player received {goldReward} gold!");

        // Handle item drop with drop rates
        if (monsterData.materialLootTable.Length > 0)
        {
            if (currentTrait == MonsterTrait.Golden)
            {
                // Golden monsters drop one of each item in their loot table
                foreach (var lootEntry in monsterData.materialLootTable)
                {
                    MaterialData droppedItem = Instantiate(lootEntry.material);
                    droppedItem.AssignRandomTraitAndSize();
                    player.AddToInventory(droppedItem.GetMaterialDescription());
                    Debug.Log($"{GetMonsterName()} dropped: {droppedItem.GetMaterialDescription()}");
                }
            }
            else
            {
                foreach (var lootEntry in monsterData.materialLootTable)
                {
                    if (Random.value * 100 <= lootEntry.dropChance)
                    {
                        MaterialData droppedItem = Instantiate(lootEntry.material);
                        droppedItem.AssignRandomTraitAndSize();
                        player.AddToInventory(droppedItem.GetMaterialDescription());
                        Debug.Log($"{GetMonsterName()} dropped: {droppedItem.GetMaterialDescription()}");
                    }
                }
            }
        }

        player.ResetHealth();
        stateMachine.MonsterDefeated();
        Destroy(gameObject);
    }

    // Returns the monster's formatted name with traits and size
    public string GetMonsterName()
    {
        List<string> modifiers = new List<string>();

        // Add size modifier if applicable
        if (sizeModifier != MonsterSize.Normal)
            modifiers.Add(sizeModifier.ToString());

        // Add trait if applicable
        if (currentTrait != MonsterTrait.None)
            modifiers.Add(currentTrait.ToString());

        // Combine all parts and return the name
        return string.Join(" ", modifiers) + " " + monsterData.monsterName;
    }


    // Returns the monster's current HP for UI display
    public int GetCurrentHP()
    {
        return currentHealth;
    }
}

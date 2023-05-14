using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;
using Unity.Collections;

public class Ability : NetworkBehaviour
{
    public bool Activated { private get; set; }
    public PlayerController Controller { protected get; set; }
    public Class PlayerClass { protected get; set; }
    public Weapon PlayerWeapon { protected get; set; }

    public UnityEvent OnActivate = new UnityEvent();

    public List<GameObject> hitTargets = new List<GameObject>();

    [Header("Ability Cooldowns")]
    public bool enableClassAbility;
    public float classAbilityCooldown;
    public bool enableAbility1;
    public float ability1Cooldown;
    public bool enableAbility2;
    public float ability2Cooldown;
    public bool enableAbility3;
    public float ability3Cooldown;

    [HideInInspector] public float classAbilityCDTime;
    [HideInInspector] public float ability1CDTime;
    [HideInInspector] public float ability2CDTime;
    [HideInInspector] public float ability3CDTime;

    [HideInInspector] public bool canClassAbility;
    [HideInInspector] public bool canAbility1;
    [HideInInspector] public bool canAbility2;
    [HideInInspector] public bool canAbility3;

#if false   //spell preparation mechanic

    [Header("Spell Preparation")]
    public float spellPrepCooldown;
    public float delayBetweenInputs;
    public bool preparingSpell = false;
    public bool castingSpell = false;
    public bool spell1 = false;
    public bool spell2 = false;
    public bool spell3 = false;
    public List<int> sequence;
    protected bool inputDelay;
    protected bool canSpellPrep;
    int inputCount = 0;
    float spellPrepCooldownTime;

#endif

    protected virtual void Awake()
    {
        Activated = false;
    }

    protected virtual void Start()
    {
        classAbilityCDTime = classAbilityCooldown;
        ability1CDTime = ability1Cooldown;
        ability2CDTime = ability2Cooldown;
        canClassAbility = true;
        canAbility1 = true;
        canAbility2 = true;
    }

    protected virtual void Update()
    {
        enabled = Activated;

#if false   //spell preparation mechanic

        if (Activated)
        {
            if (spellPrepCooldownTime < spellPrepCooldown)
            {
                spellPrepCooldownTime += Time.deltaTime;
            }
            else
                StopSpellPrepCooldown();
        }

#endif
    }

#if false   //spell preparation mechanic

    /// <summary>
    /// [ StartPrepareSpell() -> GetSpellInputs() -> StopPrepareSpell() -> StartCastingSpell() -> CastSpell() -> StopCastingSpell() ]
    /// is the intended order.
    /// </summary>
    protected void StartPrepareSpell()
    {
        preparingSpell = true;
        InitializeSpellInputs();
    }

    /// <summary>
    /// [ StartPrepareSpell() -> GetSpellInputs() -> StopPrepareSpell() -> StartCastingSpell() -> CastSpell() -> StopCastingSpell() ]
    /// is the intended order.
    /// </summary>
    protected void StopPrepareSpell()
    {
        preparingSpell = false;
        StartSpellPrepCooldown();
    }

    protected void InitializeSpellInputs()
    {
        spell1 = false;
        spell2 = false;
        spell3 = false;
        inputCount = 0;
        sequence = new List<int>();
        inputDelay = true;
    }

    /// <summary>
    /// [ StartPrepareSpell() -> GetSpellInputs() -> StopPrepareSpell() -> StartCastingSpell() -> CastSpell() -> StopCastingSpell() ]
    /// is the intended order.
    /// </summary>
    protected void GetSpellInputs(UnityAction callback, int maxInputs, bool consumeInput = true)
    {
        //end or cancel
        if(inputCount >= maxInputs || Input.GetKey(GameManager.bind_secondaryFire))
        {
            callback.Invoke();
            StartSpellPrepCooldown();
            return;
        }

        //ability 1
        if(Input.GetKeyDown(GameManager.bind_spell1) && !(consumeInput && spell1))
        {
            spell1 = true;
            sequence.Add(1);
            inputCount++;
        }

        //ability 2
        else if (Input.GetKeyDown(GameManager.bind_spell2) && !(consumeInput && spell2))
        {
            spell2 = true;
            sequence.Add(2);
            inputCount++;
        }

        //ability 3
        else if (Input.GetKeyDown(GameManager.bind_spell3) && !(consumeInput && spell3))
        {
            spell3 = true;
            sequence.Add(3);
            inputCount++;
        }
    }

    /// <summary>
    /// [ StartPrepareSpell() -> GetSpellInputs() -> StopPrepareSpell() -> StartCastingSpell() -> CastSpell() -> StopCastingSpell() ]
    /// is the intended order.
    /// </summary>
    protected void StartCastingSpell()
    {
        castingSpell = true;
    }

    /// <summary>
    /// [ StartPrepareSpell() -> GetSpellInputs() -> StopPrepareSpell() -> StartCastingSpell() -> CastSpell() -> StopCastingSpell() ]
    /// is the intended order.
    /// </summary>
    protected void StopCastingSpell()
    {
        castingSpell = false;
        controller.Weapon.SetControl(true, 0.5f);
    }

    /// <summary>
    /// [ StartPrepareSpell() -> GetSpellInputs() -> StopPrepareSpell() -> StartCastingSpell() -> CastSpell() -> StopCastingSpell() ]
    /// is the intended order.
    /// </summary>
    protected virtual void CastSpell()
    {
        //cancel
        if (Input.GetKeyDown(GameManager.bind_secondaryFire))
        {
            StopCastingSpell();
            return;
        }
    }

    private void ResetInputDelay()
    {
        inputDelay = true;
    }

    private void StartSpellPrepCooldown()
    {
        spellPrepCooldownTime = 0;
        canSpellPrep = false;
    }

    private void StopSpellPrepCooldown()
    {
        canSpellPrep = true;
    }

#endif
}

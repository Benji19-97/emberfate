using System;
using UnityEngine;

namespace Runtime.WorkInProgress
{
    public class Actor : MonoBehaviour
    {
        public int strength;
        public int dexterity;
        public int intelligence;
        public int attunement;
        public int faith;
        public int malevolence;
        public int maximumLife;
        public int maximumMana;
        public int maximumRage;
        public float movementSpeed;
        public float magicFind;
        public float goldFind;
        public float experience;
        public float lifeRegeneration;
        public float manaRegeneration;
        public float rageRegeneration;
        public float healthGlobeBonusFlat;
        public float healthGlobeBonusIncrease;
        public float healthGlobeBonusMore;

        
        public TraitListContainer TraitListContainer = new TraitListContainer();

        private void Start()
        {
            RegisterHandlers();
        }

        public void RegisterHandlers()
        {
            TraitListContainer.TraitsChangedEvent.AddListener(OnTraitsChanged);
        }

        private void OnTraitsChanged()
        {
            var query = new Query()
            {
                Actor = this,
                MustHaveTags = null,
                Result = 0,
                Tags = new TraitTag[1] {TraitTag.Life},
                TraitType = TraitType.AddsRemoves
            };
            maximumLife = (int) TraitListContainer.QueryTotalValue(ref query);
        }
    }
}
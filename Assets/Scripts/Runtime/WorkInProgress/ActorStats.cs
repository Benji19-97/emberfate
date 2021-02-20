// namespace Runtime.WorkInProgress
// {
//     public class ActorStats
//     {
//         public Actor Actor;
//
//         public int Strength { get; private set; }
//         public int Dexterity { get; private set; }
//         public int Intelligence { get; private set; }
//         public int Attunement { get; private set; }
//         public int Faith { get; private set; }
//         public int Malevolence { get; private set; }
//         public int MaximumLife { get; private set; }
//         public int MaximumMana { get; private set; }
//         public float MovementSpeed { get; private set; }
//         public float ItemFindBonus { get; private set; }
//         public float GoldFindBonus { get; private set; }
//         public float ExperienceBonus { get; private set; }
//         public float LifeRegeneration { get; private set; }
//         public float ManaRegeneration { get; private set; }
//         public float HealthGlobeBonus { get; private set; }
//         public float HealthGlobeBonusFlat { get; private set; }
//
//         public void QueryStats()
//         {
//             QueryAttributes();
//             MaximumLife = (int) Query.GetTotalValue(Actor,null, new TraitTag[] {TraitTag.Life});
//             MaximumMana = (int) Query.GetTotalValue(Actor,null, new TraitTag[] {TraitTag.Mana});
//             MovementSpeed = Query.GetTotalValue(Actor,null, new TraitTag[] {TraitTag.Speed, TraitTag.Movement});
//             ItemFindBonus = Query.GetTotalValue(Actor,null, new TraitTag[] {TraitTag.ItemFind});
//             GoldFindBonus = Query.GetTotalValue(Actor,null, new TraitTag[] {TraitTag.GoldFind});
//             ExperienceBonus = Query.GetTotalValue(Actor,null, new TraitTag[] {TraitTag.Experience});
//
//             QueryRegeneration();
//
//             HealthGlobeBonus = Query.GetTotalValue(Actor,new[] {TraitTag.Recovery}, new TraitTag[] {TraitTag.HealthGlobeFlat});
//             HealthGlobeBonusFlat = Query.GetTotalValue(Actor,new[] {TraitTag.Recovery}, new TraitTag[] {TraitTag.HealthGlobePercent});
//         }
//
//         private void QueryAttributes()
//         {
//             Strength = (int) Query.GetTotalValue(Actor,null, new TraitTag[] {TraitTag.Strength});
//             Dexterity = (int) Query.GetTotalValue(Actor, null, new TraitTag[] {TraitTag.Dexterity});
//             Intelligence = (int) Query.GetTotalValue(Actor,null, new TraitTag[] {TraitTag.Intelligence});
//             Attunement = (int) Query.GetTotalValue(Actor,null, new TraitTag[] {TraitTag.Attunement});
//             Faith = (int) Query.GetTotalValue(Actor,null, new TraitTag[] {TraitTag.Faith});
//             Malevolence = (int) Query.GetTotalValue(Actor,null, new TraitTag[] {TraitTag.Malevolence});
//         }
//
//         private void QueryRegeneration()
//         {
//             var lifeRegenerationFlat = Query.GetTotalValue(Actor,new[] {TraitTag.Recovery}, new TraitTag[] {TraitTag.Life, TraitTag.RegenerationFlat});
//             var lifeRegenerationPercent = Query.GetTotalValue(Actor,new[] {TraitTag.Recovery}, new TraitTag[] {TraitTag.Life, TraitTag.RegenerationPercent});
//             LifeRegeneration = lifeRegenerationFlat + (lifeRegenerationPercent * MaximumLife);
//
//             var manaRegenerationFlat = Query.GetTotalValue(Actor,new[] {TraitTag.Recovery}, new TraitTag[] {TraitTag.Mana, TraitTag.RegenerationFlat});
//             var manaRegenerationPercent = Query.GetTotalValue(Actor,new[] {TraitTag.Recovery}, new TraitTag[] {TraitTag.Mana, TraitTag.RegenerationPercent});
//             ManaRegeneration = manaRegenerationFlat + (manaRegenerationPercent * MaximumMana);
//         }
//
//
//     }
// }
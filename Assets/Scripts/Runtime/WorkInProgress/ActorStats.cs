namespace Runtime.WorkInProgress
{
    public class ActorStats
    {
        public Actor Actor;

        public int Strength { get; private set; }
        public int Dexterity { get; private set; }
        public int Intelligence { get; private set; }
        public int Attunement { get; private set; }
        public int Faith { get; private set; }
        public int Malevolence { get; private set; }
        public int MaximumLife { get; private set; }
        public int MaximumMana { get; private set; }
        public float MovementSpeed { get; private set; }
        public float ItemFindBonus { get; private set; }
        public float GoldFindBonus { get; private set; }
        public float ExperienceBonus { get; private set; }
        public float LifeRegeneration { get; private set; }
        public float ManaRegeneration { get; private set; }
        public float HealthGlobeBonus { get; private set; }
        public float HealthGlobeBonusFlat { get; private set; }

        public void QueryStats()
        {
            QueryAttributes();
            MaximumLife = (int) QueryValue(null, new TraitTag[] {TraitTag.Life});
            MaximumMana = (int) QueryValue(null, new TraitTag[] {TraitTag.Mana});
            MovementSpeed = (int) QueryValue(null, new TraitTag[] {TraitTag.Speed, TraitTag.Movement});
            ItemFindBonus = (int) QueryValue(null, new TraitTag[] {TraitTag.ItemFind});
            GoldFindBonus = (int) QueryValue(null, new TraitTag[] {TraitTag.GoldFind});
            ExperienceBonus = (int) QueryValue(null, new TraitTag[] {TraitTag.Experience});

            QueryRegeneration();

            HealthGlobeBonus = (int) QueryValue(new[] {TraitTag.Recovery}, new TraitTag[] {TraitTag.HealthGlobeFlat});
            HealthGlobeBonusFlat = (int) QueryValue(new[] {TraitTag.Recovery}, new TraitTag[] {TraitTag.HealthGlobePercent});
        }

        private void QueryAttributes()
        {
            Strength = (int) QueryValue(null, new TraitTag[] {TraitTag.Strength});
            Dexterity = (int) QueryValue(null, new TraitTag[] {TraitTag.Dexterity});
            Intelligence = (int) QueryValue(null, new TraitTag[] {TraitTag.Intelligence});
            Attunement = (int) QueryValue(null, new TraitTag[] {TraitTag.Attunement});
            Faith = (int) QueryValue(null, new TraitTag[] {TraitTag.Faith});
            Malevolence = (int) QueryValue(null, new TraitTag[] {TraitTag.Malevolence});
        }

        private void QueryRegeneration()
        {
            var lifeRegenerationFlat = (int) QueryValue(new[] {TraitTag.Recovery}, new TraitTag[] {TraitTag.Life, TraitTag.RegenerationFlat});
            var lifeRegenerationPercent = (int) QueryValue(new[] {TraitTag.Recovery}, new TraitTag[] {TraitTag.Life, TraitTag.RegenerationPercent});
            LifeRegeneration = lifeRegenerationFlat + (lifeRegenerationPercent * MaximumLife);

            var manaRegenerationFlat = (int) QueryValue(new[] {TraitTag.Recovery}, new TraitTag[] {TraitTag.Mana, TraitTag.RegenerationFlat});
            var manaRegenerationPercent = (int) QueryValue(new[] {TraitTag.Recovery}, new TraitTag[] {TraitTag.Mana, TraitTag.RegenerationPercent});
            ManaRegeneration = manaRegenerationFlat + (manaRegenerationPercent * MaximumMana);
        }

        private float QueryValue(TraitTag[] tags, TraitTag[] mustHaveTags)
        {
            var queryFlat = new Query()
            {
                MustHaveTags = mustHaveTags,
                Tags = tags,
                TraitOperation = TraitOperation.AddsRemoves
            };
            var queryFlatResult = Actor.TraitHolder.QueryTotalValue(ref queryFlat);

            var queryIncrease = new Query()
            {
                MustHaveTags = mustHaveTags,
                Tags = tags,
                TraitOperation = TraitOperation.IncreasesReduces
            };
            var queryIncreaseResult = Actor.TraitHolder.QueryTotalValue(ref queryIncrease);

            var queryMore = new Query()
            {
                MustHaveTags = mustHaveTags,
                Tags = tags,
                TraitOperation = TraitOperation.MoreLess
            };
            var queryMoreResult = Actor.TraitHolder.QueryTotalValue(ref queryMore);

            return queryFlatResult * (1 + queryIncreaseResult) * (1 + queryMoreResult);
        }
    }
}
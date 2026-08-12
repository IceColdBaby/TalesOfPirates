namespace Top.Legacy.Tables.Records
{
    public enum SkillFightType
    {
        LandLife = 0,
        Fight = 1,
        Sail = 2,
        SeaLife = 3,
    }

    public enum SkillType
    {
        Inborn = 0,
        Active = 1,
        Passive = 2,
    }

    public enum SkillSourceType
    {
        Human = 1,
        Boat = 2,
    }

    public enum SkillTargetHabitat
    {
        Land = 1,
        Sea = 2,
        LandOrSea = 3,
    }

    public enum SkillApplyTarget
    {
        Self = 1,
        Team = 2,
        Scene = 3,
        Enemy = 4,
        All = 5,
        PlayerDead = 6,
        ExceptSelf = 7,
        Repair = 17,
        Tree = 18,
        Mine = 19,
        Trade = 22,
        Fish = 28,
        Salvage = 29,
    }

    public enum SkillSelectTarget
    {
        None = 0,
        All = 1,
        Player = 2,
        Enemy = 3,
        PlayerAshes = 4,
        Monster = 5,
        MonsterRepairable = 6,
        MonsterTree = 7,
        MonsterMine = 8,
        MonsterFish = 9,
        MonsterDeadBoat = 10,
        Self = 11,
        Team = 12,
    }

    public class SkillInfoRecord : TableRecord
    {
        public SkillFightType FightType;
        public string JobRequirements;
        public string[] EquipRequirements;
        public string ConchRequirements;
        public int Phase;
        public SkillType Type;
        public bool Helpful;
        public int LevelDemand;
        public string PrerequisiteSkills;
        public int PointExpend;
        public SkillSourceType SourceType;
        public SkillTargetHabitat TargetHabitat;
        public int ApplyDistance;
        public SkillApplyTarget ApplyTarget;
        public int ApplyType;
        public int Angle;
        public int Radius;
        public int RangeShape;
        public string PrepareScript;
        public string RangeStateScript;
        public string UseSpScript;
        public string UseEndureScript;
        public string UseEnergyScript;
        public string SetRangeScript;
        public string UseScript;
        public string EffectScript;
        public string ActiveScript;
        public string InactiveScript;
        public int StateId;
        public string SelfAttr;
        public string SelfEffect;
        public string ItemExpend;
        public int BeingTime;
        public string TargetAttr;
        public int SplashParam;
        public int TargetPersistEffect;
        public int SplashPersistEffect;
        public int MorphId;
        public int SummonId;
        public int PreTime;
        public string CooldownScript;
        public int ActionHarm;
        public bool RandomPose;
        public int[] ActionPoses;
        public int ActionKeyFrame;
        public int AttackSound;
        public int[] ActionDummyLinks;
        public int[] ActionEffects;
        public int[] ActionEffectTypes;
        public int ItemDummyLink;
        public int[] ItemEffect1;
        public int[] ItemEffect2;
        public int SkyEffectActionKeyFrame;
        public int SkyEffectActionDummyLink;
        public int SkyEffectItemDummyLink;
        public int SkyEffect;
        public int SkySpeed;
        public int HitSound;
        public int TargetDummyLink;
        public int TargetEffectId;
        public int TargetEffectTime;
        public int AgroundEffectId;
        public int WaterEffectId;
        public string Icon;
        public int PlayCount;
        public int[] Operations;
        public string DescribeHint;
        public string EffectHint;
        public string ExpendHint;
        public SkillSelectTarget SelectTarget;
        public int PoseCount;
    }
}

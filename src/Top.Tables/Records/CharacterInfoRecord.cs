namespace Top.Tables.Records
{
    public enum CharacterModalType
    {
        MainCharacter = 1,
        Boat = 2,
        Employee = 3,
        Other = 4,
    }

    public enum CharacterControlType
    {
        None = 0,
        Player = 1,
        Npc = 2,
        NpcEvent = 3,
        Monster = 5,
        MonsterTree = 6,
        MonsterMine = 7,
        MonsterFish = 8,
        MonsterDeadBoat = 9,
        PlayerPet = 10,
        MonsterRepairable = 17,
    }

    public enum CharacterAiType
    {
        None = 0,
        AttackPassive = 1,
        AttackActive = 2,
    }

    public class CharacterInfoRecord : TableRecord
    {
        public string IconName;
        public CharacterModalType ModalType;
        public CharacterControlType ControlType;
        public int Model;
        public int SuitId;
        public int SuitCount;
        public int[] Parts;
        public int[] FeffIds;
        public int[] EffectActionIds;
        public int Shadow;
        public int ActionId;
        public int Transparency;
        public int FootfallSound;
        public int BreathSound;
        public int DeathSound;
        public bool Controllable;
        public int Territory;
        public int SeaHeight;
        public int[] EquipItemTypes;
        public float Length;
        public float Width;
        public float Height;
        public int Radius;
        public int[] BirthBehaviors;
        public int[] DeathBehaviors;
        public int BornEffect;
        public int DieEffect;
        public int DormancyAction;
        public int DieAction;
        public int[] HpEffects;
        public bool CanFace;
        public bool Cyclone;
        public int Script;
        public int Weapon;
        public int[] SkillIds;
        public int[] SkillRates;
        public int[] DropIds;
        public int[] DropRates;
        public int MaxDropCount;
        public float AllDropRate;
        public int PrefixLevel;
        public int[] QuestDropIds;
        public int[] QuestDropRates;
        public CharacterAiType Ai;
        public bool TurnWhenAttacked;
        public int Vision;
        public int Noise;
        public int GetExp;
        public bool Light;
        public int MobExp;
        public int Level;
        public int MaxHp;
        public int Hp;
        public int MaxSp;
        public int Sp;
        public int MinAttack;
        public int MaxAttack;
        public int PhysicalResist;
        public int Defense;
        public int Hit;
        public int Dodge;
        public int Critical;
        public int MagicFind;
        public int HpRecovery;
        public int SpRecovery;
        public int AttackSpeed;
        public int AttackDistance;
        public int ChaseDistance;
        public int MoveSpeed;
        public int CollectSpeed;
        public int Strength;
        public int Agility;
        public int Accuracy;
        public int Constitution;
        public int Spirit;
        public int Luck;
        public int LeftHandValue;
        public string Guild;
        public string Title;
        public string Job;
        public int Exp;
        public int NextExp;
        public int Fame;
        public int Ap;
        public int Tp;
        public int Gold;
        public int Spri;
        public int Stor;
        public int MaxSail;
        public int Sail;
        public int Stasa;
        public int Scsm;
        public int TStrength;
        public int TAgility;
        public int TAccuracy;
        public int TConstitution;
        public int TSpirit;
        public int TLuck;
        public int TMaxHp;
        public int TMaxSp;
        public int TAttack;
        public int TDefense;
        public int THit;
        public int TDodge;
        public int TMagicFind;
        public int TCritical;
        public int THpRecovery;
        public int TSpRecovery;
        public int TAttackSpeed;
        public int TAttackDistance;
        public int TMoveSpeed;
        public int TSpri;
        public int TScsm;
        public System.Numerics.Vector3 Scaling;
        public bool IsUseByQuest;
    }
}

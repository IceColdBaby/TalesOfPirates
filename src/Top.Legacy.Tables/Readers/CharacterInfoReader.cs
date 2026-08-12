using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Readers
{
    public static class CharacterInfoReader
    {
        public static CharacterInfoRecord Read(TableRow row)
        {
            var record = new CharacterInfoRecord();

            record.Id = row.NextInt();
            record.Name = row.NextString();
            record.IconName = row.NextString();
            record.ModalType = row.NextEnum<CharacterModalType>();
            record.ControlType = row.NextEnum<CharacterControlType>();
            record.Model = row.NextInt();
            record.SuitId = row.NextInt();
            record.SuitCount = row.NextInt();
            record.Parts = row.NextInts(8);
            record.FeffIds = row.NextIntList();
            // EeffID column; C++ stores the FeffID list length instead
            row.Skip();
            record.EffectActionIds = row.NextIntList();
            record.Shadow = row.NextInt();
            record.ActionId = row.NextInt();
            record.Transparency = row.NextInt();
            record.FootfallSound = row.NextInt();
            record.BreathSound = row.NextInt();
            record.DeathSound = row.NextInt();
            record.Controllable = row.NextBool();
            record.Territory = row.NextInt();
            record.SeaHeight = row.NextInt();
            record.EquipItemTypes = row.NextIntList();
            record.Length = row.NextFloat();
            record.Width = row.NextFloat();
            record.Height = row.NextFloat();
            record.Radius = row.NextInt();
            record.BirthBehaviors = row.NextIntList();
            record.DeathBehaviors = row.NextIntList();
            record.BornEffect = row.NextInt();
            record.DieEffect = row.NextInt();
            record.DormancyAction = row.NextInt();
            record.DieAction = row.NextInt();
            record.HpEffects = row.NextIntList();
            record.CanFace = row.NextBool();
            record.Cyclone = row.NextBool();
            record.Script = row.NextInt();
            record.Weapon = row.NextInt();
            record.SkillIds = row.NextIntList();
            record.SkillRates = row.NextIntList();
            record.DropIds = row.NextIntList();
            record.DropRates = row.NextIntList();
            record.MaxDropCount = row.NextInt();
            record.AllDropRate = row.NextFloat();
            record.PrefixLevel = row.NextInt();
            record.QuestDropIds = row.NextIntList();
            record.QuestDropRates = row.NextIntList();
            record.Ai = row.NextEnum<CharacterAiType>();
            record.TurnWhenAttacked = row.NextBool();
            record.Vision = row.NextInt();
            record.Noise = row.NextInt();
            record.GetExp = row.NextInt();
            record.Light = row.NextBool();
            record.MobExp = row.NextInt();
            record.Level = row.NextInt();
            record.MaxHp = row.NextInt();
            record.Hp = row.NextInt();
            record.MaxSp = row.NextInt();
            record.Sp = row.NextInt();
            record.MinAttack = row.NextInt();
            record.MaxAttack = row.NextInt();
            record.PhysicalResist = row.NextInt();
            record.Defense = row.NextInt();
            record.Hit = row.NextInt();
            record.Dodge = row.NextInt();
            record.Critical = row.NextInt();
            record.MagicFind = row.NextInt();
            record.HpRecovery = row.NextInt();
            record.SpRecovery = row.NextInt();
            record.AttackSpeed = row.NextInt();
            record.AttackDistance = row.NextInt();
            record.ChaseDistance = row.NextInt();
            record.MoveSpeed = row.NextInt();
            record.CollectSpeed = row.NextInt();
            record.Strength = row.NextInt();
            record.Agility = row.NextInt();
            record.Accuracy = row.NextInt();
            record.Constitution = row.NextInt();
            record.Spirit = row.NextInt();
            record.Luck = row.NextInt();
            record.LeftHandValue = row.NextInt();
            record.Guild = row.NextString();
            record.Title = row.NextString();
            record.Job = row.NextString();
            record.Exp = row.NextInt();
            record.NextExp = row.NextInt();
            record.Fame = row.NextInt();
            record.Ap = row.NextInt();
            record.Tp = row.NextInt();
            record.Gold = row.NextInt();
            record.Spri = row.NextInt();
            record.Stor = row.NextInt();
            record.MaxSail = row.NextInt();
            record.Sail = row.NextInt();
            record.Stasa = row.NextInt();
            record.Scsm = row.NextInt();
            record.TStrength = row.NextInt();
            record.TAgility = row.NextInt();
            record.TAccuracy = row.NextInt();
            record.TConstitution = row.NextInt();
            record.TSpirit = row.NextInt();
            record.TLuck = row.NextInt();
            record.TMaxHp = row.NextInt();
            record.TMaxSp = row.NextInt();
            record.TAttack = row.NextInt();
            record.TDefense = row.NextInt();
            record.THit = row.NextInt();
            record.TDodge = row.NextInt();
            record.TMagicFind = row.NextInt();
            record.TCritical = row.NextInt();
            record.THpRecovery = row.NextInt();
            record.TSpRecovery = row.NextInt();
            record.TAttackSpeed = row.NextInt();
            record.TAttackDistance = row.NextInt();
            record.TMoveSpeed = row.NextInt();
            record.TSpri = row.NextInt();
            record.TScsm = row.NextInt();
            record.Scaling = row.NextVector3();
            record.IsUseByQuest = row.NextBool();

            return record;
        }
    }
}

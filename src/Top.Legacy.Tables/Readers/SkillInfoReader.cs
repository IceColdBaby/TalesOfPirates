using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Readers
{
    public static class SkillInfoReader
    {
        public static SkillInfoRecord Read(TableRow row)
        {
            var record = new SkillInfoRecord
            {
                Id = row.NextInt(),
                Name = row.NextString(),
                FightType = row.NextEnum<SkillFightType>(),
                JobRequirements = row.NextString(),
                EquipRequirements = row.NextStrings(3),
                ConchRequirements = row.NextString(),
                Phase = row.NextInt(),
                Type = row.NextEnum<SkillType>(),
                Helpful = row.NextBool(),
                LevelDemand = row.NextInt(),
                PrerequisiteSkills = row.NextString(),
                PointExpend = row.NextInt(),
                SourceType = row.NextEnum<SkillSourceType>(),
                TargetHabitat = row.NextEnum<SkillTargetHabitat>(),
                ApplyDistance = row.NextInt(),
                ApplyTarget = row.NextEnum<SkillApplyTarget>(),
                ApplyType = row.NextInt(),
                Angle = row.NextInt(),
                Radius = row.NextInt(),
                RangeShape = row.NextInt(),
                PrepareScript = row.NextString(),
                RangeStateScript = row.NextString(),
                UseSpScript = row.NextString(),
                UseEndureScript = row.NextString(),
                UseEnergyScript = row.NextString(),
                SetRangeScript = row.NextString(),
                UseScript = row.NextString(),
                EffectScript = row.NextString(),
                ActiveScript = row.NextString(),
                InactiveScript = row.NextString(),
                StateId = row.NextInt(),
                SelfAttr = row.NextString(),
                SelfEffect = row.NextString(),
                ItemExpend = row.NextString(),
                BeingTime = row.NextInt(),
                TargetAttr = row.NextString(),
                SplashParam = row.NextInt(),
                TargetPersistEffect = row.NextInt(),
                SplashPersistEffect = row.NextInt(),
                MorphId = row.NextInt(),
                SummonId = row.NextInt(),
                PreTime = row.NextInt(),
                CooldownScript = row.NextString(),
                ActionHarm = row.NextInt(),
                RandomPose = row.NextBool(),
                ActionPoses = row.NextIntList(),
                ActionKeyFrame = row.NextInt(),
                AttackSound = row.NextInt(),
                ActionDummyLinks = row.NextIntList(),
                ActionEffects = row.NextIntList(),
                ActionEffectTypes = row.NextIntList(),
                ItemDummyLink = row.NextInt(),
                ItemEffect1 = row.NextIntList(),
                ItemEffect2 = row.NextIntList(),
                SkyEffectActionKeyFrame = row.NextInt(),
                SkyEffectActionDummyLink = row.NextInt(),
                SkyEffectItemDummyLink = row.NextInt(),
                SkyEffect = row.NextInt(),
                SkySpeed = row.NextInt(),
                HitSound = row.NextInt(),
                TargetDummyLink = row.NextInt(),
                TargetEffectId = row.NextInt(),
                TargetEffectTime = row.NextInt(),
                AgroundEffectId = row.NextInt(),
                WaterEffectId = row.NextInt(),
                Icon = row.NextString(),
                PlayCount = row.NextInt(),
                Operations = row.NextIntList(),
                DescribeHint = row.NextString(),
                EffectHint = row.NextString(),
                ExpendHint = row.NextString(),
            };

            record.SelectTarget = DeriveSelectTarget(record.ApplyTarget);
            record.PoseCount = TableText.CountLeadingNonZero(record.ActionPoses);

            return record;
        }

        private static SkillSelectTarget DeriveSelectTarget(SkillApplyTarget target)
        {
            return target switch
            {
                SkillApplyTarget.Self => SkillSelectTarget.Self,
                SkillApplyTarget.Team => SkillSelectTarget.Team,
                SkillApplyTarget.PlayerDead => SkillSelectTarget.PlayerAshes,
                SkillApplyTarget.Repair => SkillSelectTarget.MonsterRepairable,
                SkillApplyTarget.Tree => SkillSelectTarget.MonsterTree,
                SkillApplyTarget.Mine => SkillSelectTarget.MonsterMine,
                SkillApplyTarget.Fish => SkillSelectTarget.MonsterFish,
                SkillApplyTarget.Salvage => SkillSelectTarget.MonsterDeadBoat,
                SkillApplyTarget.Scene or SkillApplyTarget.All => SkillSelectTarget.All,
                SkillApplyTarget.Enemy => SkillSelectTarget.Enemy,
                _ => SkillSelectTarget.None
            };
        }
    }
}

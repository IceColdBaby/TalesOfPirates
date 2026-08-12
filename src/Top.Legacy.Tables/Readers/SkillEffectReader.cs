using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Readers
{
    public static class SkillEffectReader
    {
        public static SkillEffectRecord Read(TableRow row)
        {
            var r = new SkillEffectRecord
            {
                Id = row.NextInt(),
                Name = row.NextString(),
                Frequency = row.NextInt(),
                OnTransferScript = row.NextString(),
                AddStateScript = row.NextString(),
                RemoveStateScript = row.NextString(),
                AddType = row.NextInt(),
                CanCancel = row.NextBool(),
                CanMove = row.NextBool(),
                CanMagicSkill = row.NextBool(),
                CanGeneralSkill = row.NextBool(),
                CanTrade = row.NextBool(),
                CanUseItem = row.NextBool(),
                Unbeatable = row.NextBool(),
                CanBeItemTarget = row.NextBool(),
                CanBeSkillTarget = row.NextBool(),
                NoHide = row.NextBool(),
                NoShow = row.NextBool(),
                CanOperateItems = row.NextBool(),
                CanTalkToNpc = row.NextBool(),
                ReleaseEffectId = row.NextInt(),
                ScreenEffect = row.NextInt(),
                ActBehaviors = row.NextIntList(),
                ChargeLink = row.NextInt(),
                AreaEffect = row.NextInt(),
                ShowCenterOnly = row.NextBool(),
                ShowDizzy = row.NextBool(),
                Effect = row.NextInt(),
                EffectDummy = row.NextInt(),
                HitEffect = row.NextInt(),
                HitEffectDummy = row.NextInt(),
                Icon = row.NextInt(),
                LevelIcons = row.NextStringList(),
                Description = row.NextString(),
                Color = row.NextInt()
            };

            r.ActCount = TableText.CountLeadingNonZero(r.ActBehaviors);

            return r;
        }
    }
}

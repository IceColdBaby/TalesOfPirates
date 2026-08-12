using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Readers
{
    public static class ItemInfoReader
    {
        public static ItemInfoRecord Read(TableRow row)
        {
            var record = new ItemInfoRecord();
            record.Id = row.NextInt();
            record.Name = row.NextString();
            record.Icon = row.NextString();
            record.Modules = row.NextStrings(5);
            record.ShipFlag = row.NextInt();
            record.ShipType = row.NextInt();
            record.Type = row.NextEnum<ItemType>();
            // obtain prefix rate, set ID
            row.Skip(2);
            record.ForgeLevel = row.NextInt();
            record.ForgeSteady = row.NextInt();
            record.HasExclusiveId = row.NextBool();
            record.Tradeable = row.NextBool();
            record.Pickable = row.NextBool();
            record.Discardable = row.NextBool();
            record.Deletable = row.NextBool();
            record.PileMax = row.NextInt();
            record.Instanced = row.NextBool();
            record.Price = row.NextInt();
            record.Body = row.NextIntList();
            record.NeedLevel = row.NextInt();
            record.Jobs = row.NextIntList();
            // character nick, character reputation
            row.Skip(2);
            record.EquipSlots = row.NextIntList();
            record.NeedSlots = row.NextIntList();
            record.PickTo = row.NextEnum<ItemPickTo>();
            record.StrengthCoef = row.NextInt();
            record.AgilityCoef = row.NextInt();
            record.AccuracyCoef = row.NextInt();
            record.ConstitutionCoef = row.NextInt();
            record.SpiritCoef = row.NextInt();
            record.LuckCoef = row.NextInt();
            record.AttackSpeedCoef = row.NextInt();
            record.AttackDistanceCoef = row.NextInt();
            record.MinAttackCoef = row.NextInt();
            record.MaxAttackCoef = row.NextInt();
            record.DefenseCoef = row.NextInt();
            record.MaxHpCoef = row.NextInt();
            record.MaxSpCoef = row.NextInt();
            record.DodgeCoef = row.NextInt();
            record.HitCoef = row.NextInt();
            record.CriticalCoef = row.NextInt();
            record.MagicFindCoef = row.NextInt();
            record.HpRecoveryCoef = row.NextInt();
            record.SpRecoveryCoef = row.NextInt();
            record.MoveSpeedCoef = row.NextInt();
            record.CollectCoef = row.NextInt();
            record.StrengthValue = row.NextIntList();
            record.AgilityValue = row.NextIntList();
            record.AccuracyValue = row.NextIntList();
            record.ConstitutionValue = row.NextIntList();
            record.SpiritValue = row.NextIntList();
            record.LuckValue = row.NextIntList();
            record.AttackSpeedValue = row.NextIntList();
            record.AttackRangeValue = row.NextIntList();
            record.MinAttackValue = row.NextIntList();
            record.MaxAttackValue = row.NextIntList();
            record.DefenseValue = row.NextIntList();
            record.MaxHpValue = row.NextIntList();
            record.MaxSpValue = row.NextIntList();
            record.DodgeValue = row.NextIntList();
            record.HitValue = row.NextIntList();
            record.CriticalValue = row.NextIntList();
            record.MagicFindValue = row.NextIntList();
            record.HpRecoveryValue = row.NextIntList();
            record.SpRecoveryValue = row.NextIntList();
            record.MoveSpeedValue = row.NextIntList();
            record.CollectValue = row.NextIntList();
            record.PhysicalResist = row.NextIntList();
            record.LeftHandValue = row.NextInt();
            record.Energy = row.NextIntList();
            record.Endure = row.NextIntList();
            record.Holes = row.NextInt();
            // ship durability recovered, cannon quantity,
            row.Skip(8);
            // ship member count, member label, cargo capacity,
            // fuel consumption, cannonball flight speed,
            // ship movement speed
            record.AttrEffect = row.NextString();
            record.Drap = row.NextInt();
            record.BindEffects = row.NextIntList();
            record.BindEffectDummies = row.NextIntList();
            record.ItemEffect = row.NextIntList();
            record.AreaEffect = row.NextIntList();
            record.UseItemEffect = row.NextIntList();
            record.Description = row.NextString();

            if (record.Description == "0")
            {
                record.Description = string.Empty;
            }

            record.Cooldown = row.NextFloat();

            if (record.AttrEffect.Length != 0 && record.Cooldown == 0f)
            {
                record.Cooldown = 1f;
            }

            return record;
        }
    }
}

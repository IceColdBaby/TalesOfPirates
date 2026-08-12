using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Readers
{
    public static class ShipInfoReader
    {
        public static ShipInfoRecord Read(TableRow row)
        {
            return new ShipInfoRecord
            {
                Id = row.NextInt(),
                Name = row.NextString(),
                DeedItemId = row.NextInt(),
                CharacterId = row.NextInt(),
                PoseId = row.NextInt(),
                HullId = row.NextInt(),
                EngineOptions = row.NextIntList(),
                HeadOptions = row.NextIntList(),
                CannonOptions = row.NextIntList(),
                EquipmentOptions = row.NextIntList(),
                LevelLimit = row.NextInt(),
                ProfessionLimits = row.NextIntList(),
                Endurance = row.NextInt(),
                EnduranceRecovery = row.NextInt(),
                Defence = row.NextInt(),
                Resist = row.NextInt(),
                MinAttack = row.NextInt(),
                MaxAttack = row.NextInt(),
                AttackDistance = row.NextInt(),
                ReloadTime = row.NextInt(),
                SplashScope = row.NextInt(),
                Capacity = row.NextInt(),
                Supply = row.NextInt(),
                SupplyConsume = row.NextInt(),
                CannonSpeed = row.NextInt(),
                MoveSpeed = row.NextInt(),
                Description = row.NextString(),
                Param = row.NextInt()
            };
        }
    }
}

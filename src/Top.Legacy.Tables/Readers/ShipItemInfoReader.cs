using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Readers
{
    public static class ShipItemInfoReader
    {
        public static ShipItemInfoRecord Read(TableRow row)
        {
            return new ShipItemInfoRecord
            {
                Id = row.NextInt(),
                Name = row.NextString(),
                ModelId = row.NextInt(),
                Motors = row.NextInts(4),
                Price = row.NextInt(),
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

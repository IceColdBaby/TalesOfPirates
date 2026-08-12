using System;
using System.Collections.Generic;
using System.IO;
using Top.Legacy.Tables.Readers;
using Top.Legacy.Tables.Records;
using Top.Legacy.Text;

namespace Top.Legacy.Tables
{
    /// <summary>
    /// Reads one row table from a stream, resolving the table's reader from
    /// the requested record type.
    /// </summary>
    public static class TableFile
    {
        public static Table<T> Read<T>(Stream stream) where T : TableRecord
        {
            return typeof(T) switch
            {
                var t when t == typeof(AreaRecord) => ReadRows(stream, AreaSetReader.Read) as Table<T>,
                var t when t == typeof(CharacterInfoRecord) => ReadRows(stream, CharacterInfoReader.Read) as Table<T>,
                var t when t == typeof(CharacterLevelUpRecord) =>
                    ReadRows(stream, CharacterLevelUpReader.Read) as Table<T>,
                var t when t == typeof(CharacterPoseInfoRecord) =>
                    ReadRows(stream, CharacterPoseInfoReader.Read) as Table<T>,
                var t when t == typeof(ChatIconRecord) => ReadRows(stream, ChatIconsReader.Read) as Table<T>,
                var t when t == typeof(ElfSkillInfoRecord) => ReadRows(stream, ElfSkillInfoReader.Read) as Table<T>,
                var t when t == typeof(EventSoundRecord) => ReadRows(stream, EventSoundReader.Read) as Table<T>,
                var t when t == typeof(ForgeItemRecord) => ReadRows(stream, ForgeItemReader.Read) as Table<T>,
                var t when t == typeof(HairRecord) => ReadRows(stream, HairsReader.Read) as Table<T>,
                var t when t == typeof(ItemInfoRecord) => ReadRows(stream, ItemInfoReader.Read) as Table<T>,
                var t when t == typeof(ItemPreRecord) => ReadRows(stream, ItemPreReader.Read) as Table<T>,
                var t when t == typeof(ItemRefineEffectInfoRecord) =>
                    ReadRows(stream, ItemRefineEffectInfoReader.Read) as Table<T>,
                var t when t == typeof(ItemRefineInfoRecord) => ReadRows(stream, ItemRefineInfoReader.Read) as Table<T>,
                var t when t == typeof(ItemTypeRecord) => ReadRows(stream, ItemTypeReader.Read) as Table<T>,
                var t when t == typeof(JobEquipRecord) => ReadRows(stream, JobEquipReader.Read) as Table<T>,
                var t when t == typeof(LifeLevelUpRecord) => ReadRows(stream, LifeLevelUpReader.Read) as Table<T>,
                var t when t == typeof(MagicGroupInfoRecord) => ReadRows(stream, MagicGroupInfoReader.Read) as Table<T>,
                var t when t == typeof(MagicSingleInfoRecord) =>
                    ReadRows(stream, MagicSingleInfoReader.Read) as Table<T>,
                var t when t == typeof(MapInfoRecord) => ReadRows(stream, MapInfoReader.Read) as Table<T>,
                var t when t == typeof(MonsterInfoRecord) => ReadRows(stream, MonsterInfoReader.Read) as Table<T>,
                var t when t == typeof(MonsterListRecord) => ReadRows(stream, MonsterListReader.Read) as Table<T>,
                var t when t == typeof(MountInfoRecord) => ReadRows(stream, MountInfoReader.Read) as Table<T>,
                var t when t == typeof(MusicInfoRecord) => ReadRows(stream, MusicInfoReader.Read) as Table<T>,
                var t when t == typeof(NotifyRecord) => ReadRows(stream, NotifySetReader.Read) as Table<T>,
                var t when t == typeof(NpcListRecord) => ReadRows(stream, NpcListReader.Read) as Table<T>,
                var t when t == typeof(ObjectEventRecord) => ReadRows(stream, ObjectEventReader.Read) as Table<T>,
                var t when t == typeof(ResourceInfoRecord) => ReadRows(stream, ResourceInfoReader.Read) as Table<T>,
                var t when t == typeof(SailLevelUpRecord) => ReadRows(stream, SailLevelUpReader.Read) as Table<T>,
                var t when t == typeof(SceneEffectInfoRecord) =>
                    ReadRows(stream, SceneEffectInfoReader.Read) as Table<T>,
                var t when t == typeof(SceneObjectInfoRecord) =>
                    ReadRows(stream, SceneObjectInfoReader.Read) as Table<T>,
                var t when t == typeof(SelectCharacterRecord) =>
                    ReadRows(stream, SelectCharacterReader.Read) as Table<T>,
                var t when t == typeof(ServerRecord) => ReadRows(stream, ServerSetReader.Read) as Table<T>,
                var t when t == typeof(ShadeInfoRecord) => ReadRows(stream, ShadeInfoReader.Read) as Table<T>,
                var t when t == typeof(ShipInfoRecord) => ReadRows(stream, ShipInfoReader.Read) as Table<T>,
                var t when t == typeof(ShipItemInfoRecord) => ReadRows(stream, ShipItemInfoReader.Read) as Table<T>,
                var t when t == typeof(SkillEffectRecord) => ReadRows(stream, SkillEffectReader.Read) as Table<T>,
                var t when t == typeof(SkillInfoRecord) => ReadRows(stream, SkillInfoReader.Read) as Table<T>,
                var t when t == typeof(StoneInfoRecord) => ReadRows(stream, StoneInfoReader.Read) as Table<T>,
                var t when t == typeof(TerrainInfoRecord) => ReadRows(stream, TerrainInfoReader.Read) as Table<T>,
                _ => throw new NotSupportedException()
            };
        }

        private static Table<T> ReadRows<T>(Stream stream, Func<TableRow, T> read)
            where T : TableRecord
        {
            var lines = Gbk.ReadLines(stream);
            var records = new List<T>();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var comment = line.IndexOf("//", StringComparison.Ordinal);

                if (comment >= 0)
                {
                    line = line.Substring(0, comment);
                }

                var fields = TableText.SplitFields(line, '\t');

                if (fields.Count < 2)
                {
                    continue;
                }

                var row = new TableRow(i + 1, fields.ToArray());
                records.Add(read(row));
            }

            return new Table<T>(records);
        }
    }
}

namespace Top.Tables.Records
{
    public class SkillEffectRecord : TableRecord
    {
        public int Frequency;
        public string OnTransferScript;
        public string AddStateScript;
        public string RemoveStateScript;
        public int AddType;
        public bool CanCancel;
        public bool CanMove;
        public bool CanMagicSkill;
        public bool CanGeneralSkill;
        public bool CanTrade;
        public bool CanUseItem;
        public bool Unbeatable;
        public bool CanBeItemTarget;
        public bool CanBeSkillTarget;
        public bool NoHide;
        public bool NoShow;
        public bool CanOperateItems;
        public bool CanTalkToNpc;
        public int ReleaseEffectId;
        public int ScreenEffect;
        public int[] ActBehaviors;
        public int ChargeLink;
        public int AreaEffect;
        public bool ShowCenterOnly;
        public bool ShowDizzy;
        public int Effect;
        public int EffectDummy;
        public int HitEffect;
        public int HitEffectDummy;
        public int Icon;
        public string[] LevelIcons;
        public string Description;
        public int Color;
        public int ActCount;
    }
}

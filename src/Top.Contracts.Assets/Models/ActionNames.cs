namespace Top.Contracts.Assets.Models
{
    /// <summary>
    /// The slug each character action number carries in a clip name.
    /// </summary>
    public static class ActionNames
    {
        private static readonly string[] Names =
        {
            null,
            "waiting", "show", "sleep", "waiting2", "run",
            "run2", "attack", "attack1", "attack2", "power_attack",
            "skill1", "skill2", "skill3", "skill4", "skill5",
            "seat", "die", "wave", "cry", "jump",
            "anger", "dare", "gay", "emote7", "emote8",
            "emote9", "emote10", "mine", "collect", "seat2",
            "lean", "lean2", "happy", "happy7", "happy8",
            "happy9", "happy10", "reserved1", "falldown", "skill6",
            "skill7", "fly_waiting", "fly_run", "fly_show", "fly_seat",
            "skill12", "skill13", "skill14", "skill15", "skill16",
            "skill17", "skill18", "skill19", "skill20",
        };

        public static bool TryGetName(int actionNo, out string name)
        {
            name = actionNo > 0 && actionNo < Names.Length ? Names[actionNo] : null;

            return name != null;
        }
    }
}

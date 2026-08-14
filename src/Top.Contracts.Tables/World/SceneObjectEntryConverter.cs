using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Top.Contracts.Tables.World
{
    public class SceneObjectEntryConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(SceneObjectEntry);
        }

        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var json = JObject.Load(reader);
            var entry = Blank(json);

            serializer.Populate(json.CreateReader(), entry);

            return entry;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        private static SceneObjectEntry Blank(JObject json)
        {
            return (int?)json["type"] switch
            {
                3 => new PointLightEntry(),
                4 => new AmbientLightEntry(),
                5 => new FogEntry(),
                6 => new SoundEntry(),
                _ => json["sequence"] != null ? new FadeEntry() : new SceneObjectEntry(),
            };
        }
    }
}

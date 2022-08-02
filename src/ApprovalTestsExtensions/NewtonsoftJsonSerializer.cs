using Newtonsoft.Json;

namespace SmartAnalyzers.ApprovalTestsExtensions
{
    public interface IJsonSerializer
    {
        string Serialize(object? data);
    }

    public class NewtonsoftJsonSerializer : IJsonSerializer
    {
        private readonly JsonSerializerSettings? _settings;

        public NewtonsoftJsonSerializer(JsonSerializerSettings? settings = null)
        {
            _settings = settings ?? new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented
            };
        }

        public string Serialize(object? data)
        {
            if (data == null)
            {
                return "null";
            }
            return JsonConvert.SerializeObject(data, _settings);
        }
    }
}
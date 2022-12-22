using System.Text.Json.Serialization;
using WalliDO.Service.Enum;
using WalliDO.Service.Interfaces;
using WalliDO.Service.Models.Abstract;

namespace WalliDO.Service.Models
{
    public class BucketModel : StandartModel
    {
        public string? Name { get; set; }

        //[JsonConverter(typeof(JsonStringEnumConverter))]
        //public AccessPolicies AccessPolicy { get; set; }

        public bool Default { get; set; }


        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public virtual List<FileModel>? Files { get; set; }
    }
}

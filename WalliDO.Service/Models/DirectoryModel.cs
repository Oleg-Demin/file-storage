using System.Text.Json.Serialization;
using WalliDO.Service.Interfaces;
using WalliDO.Service.Models.Abstract;

namespace WalliDO.Service.Models
{
    public class DirectoryModel : StandartModel
    {
        public Guid Id { get; set; }

        public string? Name { get; set; }

        public DirectoryModel? Parent { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<FileModel>? Files { get; set; }
    }
}
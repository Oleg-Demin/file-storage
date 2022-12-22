using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using WalliDO.Service.Enum;
using WalliDO.Service.Interfaces;
using WalliDO.Service.Models.Abstract;

namespace WalliDO.Service.Models
{
    public class FileModel : StandartModel
    {
        public Guid Id { get; set; }

        [JsonPropertyName("name")]
        public string? OriginalName { get; set; }

        public bool Encription { get; set; }

        public string? ContentType { get; set; }

        [JsonIgnore]
        public DirectoryModel? Directory { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public BucketModel? Bucket { get; set; }



        [JsonIgnore]
        public string? Name { get; set; }

        [JsonIgnore]
        public Stream? Stream { get; set; }

        [JsonIgnore]
        public byte[] EncryptionKey { get; set; } = Array.Empty<byte>();
    }
}
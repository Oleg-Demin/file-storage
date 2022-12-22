using System.ComponentModel.DataAnnotations.Schema;
using WalliDO.Service.Data.Entity.Abstract;
using WalliDO.Service.Interfaces;

namespace WalliDO.Service.Data.Entity
{
    public class File : StandartEntity
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!;

        public string? OriginalName { get; set; }

        public byte[] EncryptionKey { get; set; } = Array.Empty<byte>();

        public string ContentType { get; set; } = null!;


        public Guid? DirectoryId { get; set; }
        public virtual Directory? Directory { get; set; }

        [ForeignKey($"{nameof(Bucket)}")]
        public string BucketName { get; set; } = null!;
        public virtual Bucket? Bucket { get; set; }
    }
}
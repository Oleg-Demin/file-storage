using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WalliDO.Service.Data.Entity.Abstract;
using WalliDO.Service.Enum;
using WalliDO.Service.Interfaces;

namespace WalliDO.Service.Data.Entity
{
    public class Bucket : StandartEntity
    {
        [Key]
        public string Name { get; set; } = null!;
        
        [Column(TypeName = "text")]
        public AccessPolicies AccessPolicy { get; set; } = AccessPolicies.Private;

        public bool Default { get; set; }


        public virtual List<File>? Files { get; set; }
    }
}

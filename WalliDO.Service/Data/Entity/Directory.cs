using WalliDO.Service.Data.Entity.Abstract;
using WalliDO.Service.Interfaces;

namespace WalliDO.Service.Data.Entity
{
    public class Directory : StandartEntity
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!;
        
        public Guid? ParentId { get; set; }
        public virtual Directory? Parent { get; set; }


        public virtual List<File>? Files { get; set; }
    }
}

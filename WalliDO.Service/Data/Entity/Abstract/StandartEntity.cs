using WalliDO.Service.Interfaces;

namespace WalliDO.Service.Data.Entity.Abstract
{
    public abstract class StandartEntity : IStandartEntity
    {
        public DateTime CreateDate { get; set ; }
        public DateTime? DeletedDate { get; set; }
        public Guid CreatedUserId { get; set; }
    }
}

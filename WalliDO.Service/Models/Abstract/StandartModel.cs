using WalliDO.Service.Interfaces;

namespace WalliDO.Service.Models.Abstract
{
    public abstract class StandartModel : IStandartModel
    {
        public DateTime CreateDate { get; set; }
        public DateTime? DeletedDate { get; set; }
        public Guid CreatedUserId { get; set; }
    }
}

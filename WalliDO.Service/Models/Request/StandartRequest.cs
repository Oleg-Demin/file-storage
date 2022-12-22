using WalliDO.Service.Interfaces;

namespace WalliDO.Service.Models.Request
{
    public class StandartRequest<T> : IStandartRequest where T : IStandartModel
    {
        public Guid UserId { get; set; }
        public string? UserRole { get; set; }

        //public int Page { get; set; } = 1;
        //public int Size { get; set; } = 20;
        public IEnumerable<T>? Items { get; set; }
    }
}

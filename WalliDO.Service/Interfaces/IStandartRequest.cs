namespace WalliDO.Service.Interfaces
{
    public interface IStandartRequest
    {
        Guid UserId { get; set; }
        string? UserRole { get; set; }
    }
}

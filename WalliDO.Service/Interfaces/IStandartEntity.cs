namespace WalliDO.Service.Interfaces
{
    public interface IStandartEntity
    {
        //Guid Id { get; set; }

        DateTime CreateDate { get; set; }

        DateTime? DeletedDate { get; set; }

        Guid CreatedUserId { get; set; }
    }
}

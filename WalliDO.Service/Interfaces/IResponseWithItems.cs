namespace WalliDO.Service.Interfaces
{
    public interface IResponseWithItems<T> : IStandartResponse where T : IStandartModel
    {
        IEnumerable<T>? Items { get; }

        //int? Page { get; }

        //int? Size { get; }

        int? Count { get; }
    }
}

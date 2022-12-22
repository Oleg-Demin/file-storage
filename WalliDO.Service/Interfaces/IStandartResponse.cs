using System.Text.Json.Serialization;
using WalliDO.Service.Enum;
using WalliDO.Service.Models.Response;

namespace WalliDO.Service.Interfaces
{
    public interface IStandartResponse
    {
        ResponseStatuses Status { get; }

        string? Message { get; }
    }
}

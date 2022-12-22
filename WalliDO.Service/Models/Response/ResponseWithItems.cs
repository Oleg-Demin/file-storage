using System.Text.Json.Serialization;
using WalliDO.Service.Enum;
using WalliDO.Service.Interfaces;
using WalliDO.Service.Models.Abstract;

namespace WalliDO.Service.Models.Response
{
    public class ResponseWithItems<T> : StandartResponse, IResponseWithItems<T> where T : StandartModel
    {
        public override ResponseWithItems<T> WhisStatus(ResponseStatuses status)
        {
            Status = status;
            return this;
        }

        public override ResponseWithItems<T> WhisMessage(string message)
        {
            Message = message;
            return this;
        }

        public ResponseWithItems<T> WhisItems(IEnumerable<T> items)
        {
            Items = items;
            Count = items.Count();
            return this;
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IEnumerable<T>? Items { get; private set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Count { get; private set; }
    }
}

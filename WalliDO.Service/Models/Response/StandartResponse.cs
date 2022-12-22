using Microsoft.AspNetCore.Http.Features;
using Minio.DataModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using WalliDO.Service.Enum;
using WalliDO.Service.Interfaces;
using WalliDO.Service.Models.Abstract;

namespace WalliDO.Service.Models.Response
{
    //public class StandartResponse<T> : IStandartResponse<T> where T : StandartModel
    //{
    //    public StandartResponse()
    //    {
    //        Status = ResponseStatuses.Success;
    //        Items = null;
    //        Count = null;
    //        Message = null;
    //    }

    //    public StandartResponse<T> WhisStatus(ResponseStatuses status)
    //    {
    //        Status = status;
    //        return this;
    //    }

    //    public StandartResponse<T> WhisMessage(string message)
    //    {
    //        Message = message;
    //        return this;
    //    }

    //    public StandartResponse<T> WhisItems(IEnumerable<T> items)
    //    {
    //        Items = items;
    //        Count = items.Count();
    //        return this;
    //    }

    //    //public StandartResponse<IT> WhisPage(int page)
    //    //{
    //    //    Page = page;
    //    //    return this;
    //    //}

    //    //public StandartResponse<IT> WhisSize(int size)
    //    //{
    //    //    Size = size;
    //    //    return this;
    //    //}

    //    [JsonConverter(typeof(JsonStringEnumConverter))]
    //    public ResponseStatuses Status { get; private set; }

    //    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    //    public string? Message { get; private set; }

    //    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    //    public IEnumerable<T>? Items { get; private set; }

    //    //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    //    //public int? Page { get; private set; }

    //    //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    //    //public int? Size { get; private set; }

    //    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    //    public int? Count { get; private set; }


    //    //public object Print()
    //    //{
    //    //    var a = Items;
    //    //    var b = a.AsQueryable().ElementType;

    //    //    if (b == typeof(BucketModel))
    //    //    {
    //    //        return new StandartResponse<BucketModel>().WhisStatus(Status).WhisMessage(Message).WhisItems((IEnumerable<BucketModel>)Items);
    //    //    }

    //    //    return null;
    //    //}
    //}

    public class StandartResponse : IStandartResponse
    {
        public StandartResponse()
        {
            Status = ResponseStatuses.Success;
            Message = null;
        }

        public virtual StandartResponse WhisStatus(ResponseStatuses status)
        {
            Status = status;
            return this;
        }

        public virtual StandartResponse WhisMessage(string message)
        {
            Message = message;
            return this;
        }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ResponseStatuses Status { get; protected set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Message { get; protected set; }
    }
}


//using System.Text.Json.Serialization;
//using WalliDO.Service.Enum;
//using WalliDO.Service.Interfaces;

//namespace WalliDO.Service.Models.Response
//{
//    public class StandartResponse<IT><T> where T : IT
//    {
//        public StandartResponse<IT>()
//        {
//            Status = ResponseStatuses.Success;
//            Items = null;
//            Count = null;
//            Message = null;
//        }

//        public StandartResponse<IT><T> WhisStatus(ResponseStatuses status)
//        {
//            Status = status;
//            return this;
//        }

//        public StandartResponse<IT><T> WhisMessage(string message)
//        {
//            Message = message;
//            return this;
//        }

//        public StandartResponse<IT><T> WhisItems(IEnumerable<T> items)
//        {
//            Items = items;
//            Count = items.Count();
//            return this;
//        }

//        [JsonConverter(typeof(JsonStringEnumConverter))]
//        public ResponseStatuses Status { get; private set; }

//        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
//        public string? Message { get; private set; }

//        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
//        public IEnumerable<T>? Items { get; private set; }

//        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
//        public int? Count { get; private set; }
//    }
//}

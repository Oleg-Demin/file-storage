using AutoMapper;
using WalliDO.Service.Data;
using WalliDO.Service.Enum;
using WalliDO.Service.Interfaces;
using WalliDO.Service.Models.Request;
using WalliDO.Service.Models.Response;
using WalliDO.Service.Services.Minio;

namespace WalliDO.Service.Services.Abstraction
{
    public abstract class StandartService
    {
        protected readonly ApplicationDbContext _context;
        protected readonly MinioService _minio;
        protected readonly IMapper _mapper;

        protected StandartService(
            ApplicationDbContext context,
            MinioService minio,
            IMapper mapper)
        {
            _context = context;
            _minio = minio;
            _mapper = mapper;
        }

        //static protected StandartResponse<IStandartModel> RequestValidation<T>(StandartRequest<T> request, out IEnumerable<T> bucketModels, out Guid userId, out string? userRole)
        //{
        //    userId = request.UserId;
        //    userRole = request.UserRole;

        //    if (request.Items == null || !request.Items.Any())
        //    {
        //        bucketModels = Array.Empty<T>();

        //        return new StandartResponse<IStandartModel>()
        //            .WhisStatus(ResponseStatuses.Success);
        //    }
        //    else if (request.Items is IEnumerable<T>)
        //    {
        //        bucketModels = request.Items;

        //        return new StandartResponse<IStandartModel>()
        //            .WhisStatus(ResponseStatuses.Success);
        //    }
        //    else
        //    {
        //        bucketModels = Array.Empty<T>();

        //        string message = $"Ошибка, в запросе представлен не тот список объектов что ожидалось";

        //        Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

        //        return new StandartResponse<IStandartModel>()
        //            .WhisStatus(ResponseStatuses.Fail)
        //            .WhisMessage(message);
        //    }
        //}

    }
}

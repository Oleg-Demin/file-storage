using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalliDO.Service.Controllers.Abstraction;
using WalliDO.Service.Models;
using WalliDO.Service.Models.Abstract;
using WalliDO.Service.Models.Request;
using WalliDO.Service.Models.Response;
using WalliDO.Service.Services;

namespace WalliDO.Service.Controllers
{
    public class BucketController : StandartController
    {
        private readonly BucketService _bucketService;

        public BucketController(BucketService bucketService)
        {
            _bucketService = bucketService;
        }

        /// <summary>
        /// ���������� ������ Bucket ��� ������ Buckets
        /// </summary>
        /// <remarks>
        /// 
        /// **�������� Buckets:**
        /// 
        ///     [
        ///         {
        ///             "name": "bucket1",
        ///             "default": false
        ///         },
        ///         {
        ///             "name": "bucket2",
        ///             "default": false
        ///         }
        ///     ]
        ///  
        /// **��������� Buckets � ������ ���� �� ��� �� ���������:**
        ///  
        ///     [
        ///         {
        ///             "name": "bucket1",
        ///             "default": false
        ///         },
        ///         {
        ///             "name": "bucket2",
        ///             "default": true
        ///         }
        ///     ]
        ///
        /// </remarks>
        /// <returns></returns>
        [HttpPost("[action]")]
        //[Authorize(Roles = "Admin")]
        public async Task<ActionResult> Add(IEnumerable<BucketModel> bucketModels)
        {
            var request = new StandartRequest<BucketModel>
            {
                UserId = CurrentUserId,
                Items = bucketModels
            };

            var addBucketResponse = await _bucketService.Add(request);

            return ActionResponse(addBucketResponse);
        }

        /// <summary>
        /// ���������� � Bucket �� ��� �����
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpGet("[action]/{name}")]
        //[Authorize(Roles = "Admin")]
        public ActionResult Info(string name)
        {
            var bucketModel = new BucketModel { Name = name };

            //var request = new StandartRequest<BucketModel>
            //{
            //    UserId = CurrentUserId,
            //    Items = new BucketModel[] { bucketModel }
            //};

            var infoBucketResponse = _bucketService.Info(bucketModel);

            return ActionResponse(infoBucketResponse);
        }

        /// <summary>
        /// ���������� � ���� Bucket �������
        /// </summary>
        /// <returns></returns>
        [HttpGet("[action]/All")]
        //[Authorize(Roles = "Admin")]
        public ActionResult Info()
        {
            //var request = new StandartRequest<BucketModel>
            //{
            //    UserId = CurrentUserId,
            //};

            //var infoBucketResponse = _bucketService.Info(request);

            var infoBucketResponse = _bucketService.Info();

            return ActionResponse(infoBucketResponse);
        }

        /// <summary>
        /// ��������� Bucket �� ��������� (Default == true) ��� ������� �������� �� ��������� c Bucket (Default == false)
        /// </summary>
        /// <remarks>
        /// **��������� Bucket �� ���������:**
        /// 
        ///     {
        ///         "name": "bucket",
        ///         "default": true
        ///     }
        ///  
        /// **������� � Bucket �������� "�� ���������":**
        ///  
        ///     {
        ///         "name": "bucket",
        ///         "default": false
        ///     }
        ///
        /// </remarks>
        /// <returns></returns>
        [HttpPut("[action]")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> �hangeDefault(BucketModel bucketModel)
        {
            //var request = new StandartRequest<BucketModel>
            //{
            //    UserId = CurrentUserId,
            //    Items = new BucketModel[] { bucketModel }
            //};

            var defaultBucketResponse = await _bucketService.�hangeDefault(bucketModel);

            return ActionResponse(defaultBucketResponse);
        }

        /// <summary>
        /// ������� Bucket ��� ������ Buckets
        /// </summary>
        /// <remarks>
        /// 
        /// **������� ������ Buckets:**
        /// 
        ///     [
        ///         {
        ///             "name": "bucket1"
        ///         },
        ///         {
        ///             "name": "bucket2"
        ///         }
        ///     ]
        ///
        /// </remarks>
        [HttpDelete("[action]")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Delete(IEnumerable<BucketModel> bucketModels)
        {
            var request = new StandartRequest<BucketModel>
            {
                UserId = CurrentUserId,
                Items = bucketModels
            };

            var deleteBucketResponseTest = await _bucketService.Delete(request);

            //var deleteBucketResponse = await _bucketService.Delete(request);

            return ActionResponse(deleteBucketResponseTest);
        }
    }
}
//using AutoMapper;
//using Microsoft.AspNetCore.Mvc;
//using WalliDO.Service.Controllers.Abstraction;
//using WalliDO.Service.Data;
//using WalliDO.Service.Enum;
//using WalliDO.Service.Models;
//using WalliDO.Service.Models.Response;
//using WalliDO.Service.Services;
//using Entity = WalliDO.Service.Data.Entity;

//namespace WalliDO.Service.Controllers
//{
//    public class DirectoryController : StandartController
//    {

//        private readonly DirectoryService _directoryService;

//        public DirectoryController(DirectoryService directoryService)
//        {
//            _directoryService = directoryService;
//        }

//        [HttpPost("[action]")]
//        public async Task<ActionResult> Add(DirectoryModel directory)
//        {
//            var addBucketResponse = await _directoryService.Add(directory);

//            return ActionResponse(addBucketResponse);
//        }

//        [HttpGet("[action]/{id}")]
//        public async Task<ActionResult> Info(Guid id)
//        {
//            var directory = new DirectoryModel { Id = id };

//            var infoDirectoryResponse = await _directoryService.Info(directory);

//            return ActionResponse(infoDirectoryResponse);
//        }
//        //a5362279-b635-434a-b412-f6f605f1699c
//        [HttpPut("[action]/{childId}")]
//        public async Task<ActionResult> ÑhangeParent(Guid childId, Guid? parentId = null)
//        {
//            var directoryModel = new DirectoryModel
//            {
//                Id = childId,
//                Parent = (parentId != null) ? new DirectoryModel { Id = (Guid)parentId } : null,
//            };

//            var changeParentDirectoryResponse = await _directoryService.ÑhangeParent(directoryModel);

//            return ActionResponse(changeParentDirectoryResponse);
//        }

//        [HttpGet("[action]")]
//        public async Task<ActionResult> ChildDirectories(Guid? id = null)
//        {
//            if (id == null)
//            {
//                var childDirectoriesResponse = await _directoryService.ChildDirectories();
                
//                return ActionResponse(childDirectoriesResponse);
//            }
//            else
//            {
//                var directory = new DirectoryModel { Id = (Guid)id };

//                var childDirectoriesResponse = await _directoryService.ChildDirectories(directory);

//                return ActionResponse(childDirectoriesResponse);
//            }
//        }
//    }
//}
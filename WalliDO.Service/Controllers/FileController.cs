using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalliDO.Service.Controllers.Abstraction;
using WalliDO.Service.Enum;
using WalliDO.Service.Interfaces;
using WalliDO.Service.Models;
using WalliDO.Service.Models.Abstract;
using WalliDO.Service.Models.Request;
using WalliDO.Service.Models.Response;
using WalliDO.Service.Services;

namespace WalliDO.Service.Controllers
{
    [DisableRequestSizeLimit]
    public class FileController : StandartController
    {
        private readonly FileService _fileService;

        public FileController(FileService fileService)
        {
            _fileService = fileService;
        }

        /// <summary>
        /// Добавление нового файла или группы файлов
        /// </summary>
        /// <param name="files"></param>
        /// <param name="encription"></param>
        /// <param name="bucket"></param>
        /// <remarks>
        /// 
        /// **files** - файл который необходимо загрузить
        /// 
        /// **encription** - необходимо ли шифровать файл _(по умолчанию файл не шифруется **[encription == false]**)_
        /// 
        /// **bucket** - указывает в какой bucket будет сохранен файл _(если ничего не указать то берется bucket по умолчанию)_
        /// 
        /// </remarks>
        /// <returns></returns>
        [HttpPost("[action]")]
        [Authorize(Roles = "Admin")]
        //[Authorize(Roles = "User,Admin")]
        public async Task<ActionResult> Add(IFormFileCollection files, bool encription = false, string? bucket = null/*, Guid? directory = null*/)
        {
            var fileModels = new List<FileModel>();
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file.FileName);

                string originalName = fileInfo.Name;
                string contentType = file.ContentType;

                var stream = new MemoryStream();
                await file.CopyToAsync(stream);

                fileModels.Add(new FileModel
                {
                    Bucket = new BucketModel { Name = bucket },
                    OriginalName = originalName,
                    ContentType = contentType,
                    Stream = stream,
                    Encription = encription,
                });
            }

            var request = new StandartRequest<FileModel>
            {
                UserId = CurrentUserId,
                UserRole = CurrentUserRole,
                Items = fileModels
            };

            var addFileResponse = await _fileService.Add(request);

            return ActionResponse(addFileResponse);
        }

        /// <summary>
        /// Получить файл по его id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("[action]/{id}")]
        //[Authorize(Roles = "User,Admin")]
        public async Task<ActionResult> Get(Guid id)
        {
            var fileModel = new FileModel { Id = id };

            var request = new StandartRequest<FileModel>
            {
                UserId = CurrentUserId,
                UserRole = CurrentUserRole,
                Items = new List<FileModel> { fileModel }
            };

            var getFileResponse = await _fileService.Get(request);

            if (getFileResponse.Status == ResponseStatuses.Fail)
            {
                return BadRequest(getFileResponse);
            }
            else if (getFileResponse.Status == ResponseStatuses.Fail)
            {
                return Ok(getFileResponse);
            }

            fileModel = ((ResponseWithItems<FileModel>)getFileResponse).Items!.First();

            if (fileModel.Stream == null)
            {
                string message = $"Ошибка при попытке получить файл из bucket, хранилища Minio:\nОтсутствует параметр модели {fileModel.Stream}";

                Console.WriteLine(message);

                return Ok(new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message));
            }

            if (fileModel.ContentType == null)
            {
                string message = $"Ошибка при попытке получить файл из bucket, хранилища Minio:\nОтсутствует параметр модели {fileModel.ContentType}";

                Console.WriteLine(message);

                return Ok(new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message));
            }

            if (fileModel.OriginalName == null)
            {
                string message = $"Ошибка при попытке получить файл из bucket, хранилища Minio:\nОтсутствует параметр модели {fileModel.OriginalName}";

                Console.WriteLine(message);

                return Ok(new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message));
            }

            return new FileStreamResult(fileModel.Stream, fileModel.ContentType)
            {
                FileDownloadName = fileModel.OriginalName
            };
        }

        /// <summary>
        /// Информация о файле по его id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("[action]/{id}")]
        //[Authorize(Roles = "User,Admin")]
        public async Task<ActionResult> Info(Guid id)
        {
            var fileModel = new FileModel { Id = id };

            var request = new StandartRequest<FileModel>
            {
                UserId = CurrentUserId,
                UserRole = CurrentUserRole,
                Items = new List<FileModel> { fileModel }
            };

            var infoFileResponse = await _fileService.Info(request);

            return ActionResponse(infoFileResponse);
        }

        /// <summary>
        /// Информация о файлах
        /// </summary>
        /// <remarks>
        /// 
        /// Информация о файлах не находящихся в корзине **[trash == false]**
        /// 
        /// Информация о файлах находящихся в корзиен **[trash == true]**
        /// 
        /// _(По умолчанию **false**)_
        /// 
        /// </remarks>
        /// <param name="trash"></param>
        /// <returns></returns>
        [HttpGet("[action]/All")]
        //[Authorize(Roles = "User,Admin")]
        public async Task<ActionResult> Info(bool trash = false)
        {
            var request = new StandartRequest<FileModel>
            {
                UserId = CurrentUserId,
                UserRole = CurrentUserRole
            };

            var infoFileResponse = await _fileService.Info(request, trash);

            return ActionResponse(infoFileResponse);
        }

        /// <summary>
        /// Перемецаем файлы в корзину
        /// </summary>
        /// <remarks>
        /// 
        /// **Перемещаем файлы в корзину:**
        /// 
        ///     [
        ///         {
        ///             "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        ///         },
        ///         {
        ///             "id": "46d334c7-7cd6-494d-b3b2-b5b216c3eef6"
        ///         }
        ///     ]
        ///
        /// </remarks>
        /// <returns></returns>
        [HttpPut("[action]")]
        //[Authorize(Roles = "User,Admin")]
        public async Task<ActionResult> MoveInTrash(IEnumerable<FileModel> fileModels)
        {
            var request = new StandartRequest<FileModel>
            {
                UserId = CurrentUserId,
                UserRole = CurrentUserRole,
                Items = fileModels
            };

            var inTrashFileResponse = await _fileService.MoveTrash(request);

            return ActionResponse(inTrashFileResponse);
        }

        /// <summary>
        /// Убираем файлы из корзины
        /// </summary>
        /// <remarks>
        /// 
        /// **Убираем файлы из корзины:**
        /// 
        ///     [
        ///         {
        ///             "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        ///         },
        ///         {
        ///             "id": "46d334c7-7cd6-494d-b3b2-b5b216c3eef6"
        ///         }
        ///     ]
        ///
        /// </remarks>
        /// <returns></returns>
        [HttpPut("[action]")]
        //[Authorize(Roles = "User,Admin")]
        public async Task<ActionResult> MoveOutTrash(IEnumerable<FileModel> fileModels)
        {
            var request = new StandartRequest<FileModel>
            {
                UserId = CurrentUserId,
                UserRole = CurrentUserRole,
                Items = fileModels
            };

            var inTrashFileResponse = await _fileService.MoveTrash(request, false);

            return ActionResponse(inTrashFileResponse);
        }

        /// <summary>
        /// Удаляем файлы
        /// </summary>
        /// <remarks>
        /// 
        /// **Удаляем файлы:**
        /// 
        /// _(Удалить можно только те файлы что находятся в корзине)_
        /// 
        ///     [
        ///         {
        ///             "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        ///         },
        ///         {
        ///             "id": "46d334c7-7cd6-494d-b3b2-b5b216c3eef6"
        ///         }
        ///     ]
        ///
        /// </remarks>
        /// <returns></returns>
        [HttpDelete("[action]")]
        [Authorize(Roles = "Admin")]
        //[Authorize(Roles = "User,Admin")]
        public async Task<ActionResult> Delete(IEnumerable<FileModel> fileModels)
        {
            var request = new StandartRequest<FileModel>
            {
                UserId = CurrentUserId,
                UserRole = CurrentUserRole,
                Items = fileModels
            };

            var deleteFileResponse = await _fileService.Delete(request);

            return ActionResponse(deleteFileResponse);
        }

    }
}
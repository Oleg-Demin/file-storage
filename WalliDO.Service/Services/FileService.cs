using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using WalliDO.Service.Data;
using WalliDO.Service.Enum;
using WalliDO.Service.Interfaces;
using WalliDO.Service.Models;
using WalliDO.Service.Models.Abstract;
using WalliDO.Service.Models.Request;
using WalliDO.Service.Models.Response;
using WalliDO.Service.Services.Abstraction;
using WalliDO.Service.Services.Minio;
using Entity = WalliDO.Service.Data.Entity;


namespace WalliDO.Service.Services
{
    public class FileService : StandartService
    {
        public FileService(ApplicationDbContext context, MinioService minio, IMapper mapper)
            : base(context, minio, mapper) { }

        public async Task<StandartResponse> Add(StandartRequest<FileModel> request)
        {
            var userId = request.UserId;
            var userRole = request.UserRole;

            StandartResponse response;

            //Проверяем наличие элементов
            response = FileModelsNotEmpty(request.Items);
            if (response.Status != ResponseStatuses.Success)
            {
                return response;
            }

            var fileModels = request.Items!;

            //Проверяем наличие параметра Stream
            response = FileModelsContainStream(fileModels);
            if (response.Status != ResponseStatuses.Success)
            {
                return response;
            }

            //Проверяем наличие параметра OriginalName
            response = FileModelsContainOriginalName(fileModels);
            if (response.Status != ResponseStatuses.Success)
            {
                return response;
            }

            //Проверяем наличие параметра ContentType
            response = FileModelsContainContentType(fileModels);
            if (response.Status != ResponseStatuses.Success)
            {
                return response;
            }

            //Провермяем у всех ли файлов указан Bucket в который нужно сохранить
            //файл и если тагого нет то ставим файлу bucket по умолчанию
            response = FileModelsContainBucket(fileModels);
            IEnumerable<FileModel>? fileModelsWithEmptyBucket = new List<FileModel>();
            if (response.Status != ResponseStatuses.Success)
            {
                fileModelsWithEmptyBucket = ((ResponseWithItems<FileModel>)response).Items ?? new List<FileModel>();

                Entity.Bucket? defaultBucket;

                try
                {
                    defaultBucket = await _context.Buckets
                        .AsNoTracking()
                        .Where(b => b.Default)
                        .FirstOrDefaultAsync();
                }
                catch (Exception ex)
                {
                    string message = $"Ошибка при попытке узнать bucket по умолчанию для сохранения файлов:\n{ex.Message}";

                    Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                    return new StandartResponse()
                        .WhisStatus(ResponseStatuses.Fail)
                        .WhisMessage(message);
                }

                if (defaultBucket == null)
                {
                    string message = $"Ошибка, у некоторых файлов отсутствует bucket в который нужно сохранить файла, а bucket по умолчанию не установлен администратором";

                    Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                    return new ResponseWithItems<FileModel>()
                        .WhisStatus(ResponseStatuses.Fail)
                        .WhisItems(fileModelsWithEmptyBucket)
                        .WhisMessage(message);
                }

                var defaultBucketModel = _mapper.Map<Entity.Bucket, BucketModel>(defaultBucket);
                foreach (var fileModel in fileModelsWithEmptyBucket)
                {
                    fileModel.Bucket = defaultBucketModel;
                }
            }

            //Конструкцию надо изменить
            var fileModelsWithNonEmptyBucket = fileModels.Except(fileModelsWithEmptyBucket ?? new List<FileModel>() );

            IEnumerable<string> bucketNames = fileModelsWithNonEmptyBucket
                .Select(f => f.Bucket!.Name!)  //Мы уже делали проверку на пустоту Bucket (поэтому !)
                .Distinct();

            var bucketNamesFromDB = await _context.Buckets
                .Where(b => bucketNames.Contains(b.Name))
                .Select(b => b.Name)
                .ToListAsync();

            var bucketNamesNotFromDB = bucketNames.Except(bucketNamesFromDB);

            if (bucketNamesNotFromDB.Any())
            {
                var filesWithUnfoundBucket = fileModels
                    .Where(f => bucketNamesNotFromDB
                    .Contains(f.Bucket!.Name)); //Мы уже делали проверку на пустоту Bucket (поэтому !)

                string message = $"Ошибка, в данных файлах указаны bucket которые не найдены в БД";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new ResponseWithItems<FileModel>()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisItems(filesWithUnfoundBucket)
                    .WhisMessage(message);
            }

            //Процесс загрузки файлов
            var filesUpload = new List<FileModel>();
            var filesNotUpload = new List<FileModel>();
            foreach (var fileModel in fileModels)
            {
                //Отправляем файлы в хранилище Minio
                string bucketName = fileModel.Bucket!.Name!; //Мы уже делали проверку на пустоту Bucket (поэтому !)
                Stream fileStream = fileModel.Stream!; //Мы уже делали проверку на пустоту Stream (поэтому !)

                var fileInfo = new FileInfo(fileModel.OriginalName!); //Мы уже делали проверку на пустоту OriginalName (поэтому !)

                string fileOriginalName = fileInfo.Name;
                string fileExtension = fileInfo.Extension;

                StandartResponse uploadFileResponse;
                if (fileModel.Encription)
                {
                    //Отправляем зашифрованный файл
                    Aes aesEncryption = Aes.Create();
                    aesEncryption.KeySize = 256;
                    aesEncryption.GenerateKey();
                    var fileEncryptionKey = aesEncryption.Key;

                    uploadFileResponse = await _minio.UploadFile(
                        fileStream,
                        fileExtension,
                        bucketName,
                        fileEncryptionKey
                        );

                    fileModel.EncryptionKey = fileEncryptionKey;
                }
                else
                {
                    //Отправляем не зашифрованный файл
                    uploadFileResponse = await _minio.UploadFile(
                        fileStream,
                        fileExtension,
                        bucketName
                        );
                }

                if (uploadFileResponse.Status != ResponseStatuses.Success)
                {
                    Console.WriteLine($"[x]{uploadFileResponse.Status}\n[x]{uploadFileResponse.Message}");

                    filesNotUpload.Add(fileModel);
                    continue; //break;
                }

                //Записываем сведения о файле в БД
                fileModel.Name = ((ResponseWithItems<FileModel>)uploadFileResponse).Items!.First().Name!;
                fileModel.CreateDate = DateTime.UtcNow;
                fileModel.CreatedUserId = userId;
                fileModel.Directory = null; //

                var fileEntity = _mapper.Map<FileModel, Entity.File>(fileModel);

                try
                {
                    await _context.AddAsync(fileEntity);
                    await _context.SaveChangesAsync();

                    var uploadFileModel = _mapper.Map<Entity.File, FileModel>(fileEntity);

                    filesUpload.Add(uploadFileModel);
                }
                catch (Exception ex)
                {
                    //Необходимо удалить файл из хранилища, ведь сведения о нем не записались в БД
                    var deleteFileResponse = await _minio.DeleteFile(
                        fileModel.Name,
                        fileModel.Bucket!.Name!); //Мы уже делали проверку на пустоту Bucket (поэтому !)

                    if (deleteFileResponse.Status != ResponseStatuses.Success)
                    {
                        Console.WriteLine($"[x]Ошибка при попытке удалить файл их хранилища Minio, так как информация о нем не сохранилась в БД:\n{deleteFileResponse.Message}");

                        filesNotUpload.Add(fileModel);
                        continue; //break;
                    }

                    string message = $"Ошибка при попытке сохранить данные о файле {fileModel.Name} в БД:\n{ex.Message}";

                    Console.WriteLine($"[x]{message}");

                    filesNotUpload.Add(fileModel);
                    continue; //break;
                }
            }

            if (filesNotUpload.Count == fileModels.Count())
            {
                string message = $"Не удалось сохранить файлы:";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }

            if (filesNotUpload.Any())
            {
                string message = $"Данные файлы не удалось сохранить:";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new ResponseWithItems<FileModel>()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisItems(filesUpload)
                    .WhisMessage(message);
            }

            return new ResponseWithItems<FileModel>()
                .WhisStatus(ResponseStatuses.Success)
                .WhisItems(filesUpload);
        }

        public async Task<StandartResponse> Get(StandartRequest<FileModel> request)
        {
            var userId = request.UserId;
            var userRole = request.UserRole;

            StandartResponse response;

            //Проверяем наличие элементов
            response = FileModelsNotEmpty(request.Items);
            if (response.Status != ResponseStatuses.Success)
            {
                return response;
            }

            var fileModels = request.Items!;

            //В данном запросе должен фигурировать всего 1 файл
            if (fileModels.Count() > 1)
            {
                string message = $"Ошибка, в данном запросе должен фигурировать всего 1 файл";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }

            var fileModel = fileModels.First();

            var id = fileModel.Id;

            var fileModelsContainId = FileModelsContainId(fileModels);
            if (fileModelsContainId.Status != ResponseStatuses.Success)
            {
                return fileModelsContainId;
            }

            Entity.File? file;

            try
            {
                file = await _context.Files
                    .Include(f => f.Bucket)
                    .Where(f => f.Id == id)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                string message = $"Ошибка при попытке получить данный о файле из БД:\n{ex.Message}";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }

            if (file == null)
            {
                string message = $"Файла с Id ({id}) не существует";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }

            if (file.Bucket == null)
            {
                string message = $"У файла с Id ({id}) отсутствует указатель в каком bucket он расположен в Minio сервис";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }

            if (file.CreatedUserId != userId && userRole != "Admin")
            {
                string message = $"У вас недостаточно прав на доступ к файлу с Id ({id})";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }

            string fileName = file.Name;
            string bucketName = file.Bucket.Name;
            var encryptionKey = file.EncryptionKey;

            StandartResponse getFileResponse = await _minio.GetFile(
                fileName,
                bucketName,
                encryptionKey);

            if (getFileResponse.Status == ResponseStatuses.Fail)
            {
                return getFileResponse;
            }

            Stream fileStream = ((ResponseWithItems<FileModel>)getFileResponse).Items!.First().Stream!;

            fileModel = _mapper.Map<Entity.File, FileModel>(file);
            fileModel.Stream = fileStream;

            return new ResponseWithItems<FileModel>()
                  .WhisStatus(ResponseStatuses.Success)
                  .WhisItems(new List<FileModel> { fileModel });
        }

        public async Task<StandartResponse> Info(StandartRequest<FileModel> request)
        {
            var userId = request.UserId;
            var userRole = request.UserRole;

            StandartResponse response;

            //Проверяем наличие элементов
            response = FileModelsNotEmpty(request.Items);
            if (response.Status != ResponseStatuses.Success)
            {
                return response;
            }

            var fileModels = request.Items!;

            //Проверяем наличие элементов в fileModels
            response = FileModelsNotEmpty(fileModels);
            if (response.Status != ResponseStatuses.Success)
            {
                return response;
            }
            
            //Проверяем наличие параметра Id
            response = FileModelsContainId(fileModels);
            if (response.Status != ResponseStatuses.Success)
            {
                return response;
            }

            var fileIds = fileModels.Select(f => f.Id);
            var filesInDB = new List<Entity.File>();

            try
            {
                filesInDB = await _context.Files
                    //.Include(f => f.Bucket)
                    .Include(f => f.Directory)
                    .Where(f => fileIds.Contains(f.Id))
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                string message = $"Ошибка при попытке получить данный о файлах из БД:\n{ex.Message}";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }

            //Проверяем все ли файлы найдены в БД
            if (filesInDB.Count != fileModels.Count())
            {
                var fileIdsInDB = filesInDB.Select(f => f.Id);

                var fileIdsNotDB = fileIds.Except(fileIdsInDB);

                var fileModelNotDB = fileModels.Where(f => fileIdsNotDB.Contains(f.Id));

                string message = $"Данные о этих файлах не найдены в БД";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new ResponseWithItems<FileModel>()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisItems(fileModelNotDB)
                    .WhisMessage(message);
            }

            if (userRole != "Admin")
            {
                var userFileIds = filesInDB
                    .Where(e => e.CreatedUserId == userId)
                    .Select(e => e.CreatedUserId);
                
                if (userFileIds.Count() != fileModels.Count())
                {
                    var notUserFileModels = fileModels
                        .Where(e => userFileIds.Contains(e.Id));

                    string message = $"У вас недостаточно прав на доступ к файлам:";

                    Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                    return new StandartResponse()
                        .WhisStatus(ResponseStatuses.Fail)
                        .WhisMessage(message);
                }
            }

            var responseItems = new List<FileModel>();
            foreach (var file in filesInDB)
            {
                var fileModel = _mapper.Map<Entity.File, FileModel>(file);
                //if (fileModel.Bucket != null)
                //{
                //    fileModel.Bucket.Files = null;
                //}
                if (fileModel.Directory != null)
                {
                    fileModel.Directory.Files = null;
                }

                responseItems.Add(fileModel);
            }

            return new ResponseWithItems<FileModel>()
                .WhisStatus(ResponseStatuses.Success)
                .WhisItems(responseItems);
        }

        public async Task<StandartResponse> Info(StandartRequest<FileModel> request, bool trash = false)
        {
            var userId = request.UserId;
            var userRole = request.UserRole;

            List<Entity.File>? files;

            try
            {
                if (userRole == "Admin")
                {
                    if (trash)
                    {
                        files = await _context.Files
                            //.Include(f => f.Bucket)
                            .Include(f => f.Directory)
                            .Where(f => f.DeletedDate != null)
                            .AsNoTracking()
                            .ToListAsync();
                    }
                    else
                    {
                        files = await _context.Files
                            //.Include(f => f.Bucket)
                            .Include(f => f.Directory)
                            .Where(f => f.DeletedDate == null)
                            .AsNoTracking()
                            .ToListAsync();
                    }
                }
                else
                {
                    if (trash)
                    {
                        files = await _context.Files
                            //.Include(f => f.Bucket)
                            .Include(f => f.Directory)
                            .Where(f => f.DeletedDate != null)
                            .Where(f => f.CreatedUserId == userId)
                            .AsNoTracking()
                            .ToListAsync();
                    }
                    else
                    {
                        files = await _context.Files
                            //.Include(f => f.Bucket)
                            .Include(f => f.Directory)
                            .Where(f => f.DeletedDate == null)
                            .Where(f => f.CreatedUserId == userId)
                            .AsNoTracking()
                            .ToListAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                string message = $"Ошибка при попытке получить данный о файле из БД:\n{ex.Message}";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }

            var responseItems = new List<FileModel>();
            foreach (var file in files)
            {
                var fileModel = _mapper.Map<Entity.File, FileModel>(file);
                //if (fileModel.Bucket != null)
                //{
                //    fileModel.Bucket.Files = null;
                //}
                if (fileModel.Directory != null)
                {
                    fileModel.Directory.Files = null;
                }

                responseItems.Add(fileModel);
            }

            return new ResponseWithItems<FileModel>()
                .WhisStatus(ResponseStatuses.Success)
                .WhisItems(responseItems);
        }

        //public async Task<StandartResponse> MoveInDirecroty(FileModel fileModel)
        //{
        //}

        //public async Task<StandartResponse> DirectoryFiles(BucketModel bucketModel)
        //{
        //}

        public async Task<StandartResponse> MoveTrash(StandartRequest<FileModel> request, bool trash = true)
        {
            var userId = request.UserId;
            var userRole = request.UserRole;

            StandartResponse response;

            //Проверяем наличие элементов
            response = FileModelsNotEmpty(request.Items);
            if (response.Status != ResponseStatuses.Success)
            {
                return response;
            }

            var fileModels = request.Items!;

            //Проверяем наличие элементов в fileModels
            response = FileModelsNotEmpty(fileModels);
            if (response.Status != ResponseStatuses.Success)
            {
                return response;
            }

            //Проверяем наличие параметра Id
            response = FileModelsContainId(fileModels);
            if (response.Status != ResponseStatuses.Success)
            {
                return response;
            }

            var fileIds = fileModels
                .Select(f => f.Id);

            List<Entity.File> filesInDB;

            try
            {
                filesInDB = await _context.Files
                    //.Include(f => f.Bucket)
                    .Include(f => f.Directory)
                    .Where(f => fileIds.Contains(f.Id))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                string message = $"Ошибка при попытке получить файлы из БД:\n{ex}";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }

            if (filesInDB.Count != fileModels.Count())
            {
                var fileIdsInDB = filesInDB.Select(f => f.Id);

                var fileIdsNotDB = fileIds.Except(fileIdsInDB);

                var fileModelNotDB = fileModels.Where(f => fileIdsNotDB.Contains(f.Id));

                string message = $"Данные о этих файлах не найдены в БД";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new ResponseWithItems<FileModel>()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisItems(fileModelNotDB)
                    .WhisMessage(message);
            }

            if (userRole != "Admin")
            {
                var userFileIds = filesInDB
                    .Where(e => e.CreatedUserId == userId)
                    .Select(e => e.CreatedUserId);

                if (userFileIds.Count() != fileModels.Count())
                {
                    var notUserFileModels = fileModels
                        .Where(e => userFileIds.Contains(e.Id));

                    string message = $"У вас недостаточно прав на доступ к файлам:";

                    Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                    return new StandartResponse()
                        .WhisStatus(ResponseStatuses.Fail)
                        .WhisMessage(message);
                }
            }

            var fileDeleteDates = filesInDB
                .Where(e => e.DeletedDate != null);

            if (trash) //Необходимо отправить файлы в корзину
            {
                foreach (var file in filesInDB)
                {
                    file.DeletedDate = DateTime.UtcNow;
                }
            }
            else //Необходимо вытащить файлы из корзины
            {
                foreach (var file in filesInDB)
                {
                    file.DeletedDate = null;
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                string message = $"Ошибка, не удалось сохранить данные об отправке файлов в корзину:\n\t{ex}";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }

            var responseFileModels = new List<FileModel>();

            foreach (var file in filesInDB)
            {
                var fileModel = _mapper.Map<Entity.File, FileModel>(file);
                if (fileModel.Directory != null)
                {
                    fileModel.Directory.Files = null;
                }

                responseFileModels.Add(fileModel);
            }


            return new ResponseWithItems<FileModel>()
                .WhisStatus(ResponseStatuses.Success)
                .WhisItems(responseFileModels);
        }

        public async Task<StandartResponse> MoveOutTrash(StandartRequest<FileModel> request)
        {
            var userId = request.UserId;
            var userRole = request.UserRole;

            StandartResponse response;

            //Проверяем наличие элементов
            response = FileModelsNotEmpty(request.Items);
            if (response.Status != ResponseStatuses.Success)
            {
                return response;
            }

            var fileModels = request.Items!;

            //Проверяем наличие элементов в fileModels
            response = FileModelsNotEmpty(fileModels);
            if (response.Status != ResponseStatuses.Success)
            {
                return response;
            }

            //Проверяем наличие параметра Id
            response = FileModelsContainId(fileModels);
            if (response.Status != ResponseStatuses.Success)
            {
                return response;
            }

            var fileIds = fileModels
                .Select(f => f.Id);

            List<Entity.File> filesInDB;

            try
            {
                filesInDB = await _context.Files
                    //.Include(f => f.Bucket)
                    .Include(f => f.Directory)
                    .Where(f => fileIds.Contains(f.Id))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                string message = $"Ошибка при попытке получить файлы из БД:\n{ex}";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }

            if (filesInDB.Count != fileModels.Count())
            {
                var fileIdsInDB = filesInDB.Select(f => f.Id);

                var fileIdsNotDB = fileIds.Except(fileIdsInDB);

                var fileModelNotDB = fileModels.Where(f => fileIdsNotDB.Contains(f.Id));

                string message = $"Данные о этих файлах не найдены в БД";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new ResponseWithItems<FileModel>()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisItems(fileModelNotDB)
                    .WhisMessage(message);
            }

            if (userRole != "Admin")
            {
                var userFileIds = filesInDB
                    .Where(e => e.CreatedUserId == userId)
                    .Select(e => e.CreatedUserId);

                if (userFileIds.Count() != fileModels.Count())
                {
                    var notUserFileModels = fileModels
                        .Where(e => userFileIds.Contains(e.Id));

                    string message = $"У вас недостаточно прав на доступ к файлам:";

                    Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                    return new StandartResponse()
                        .WhisStatus(ResponseStatuses.Fail)
                        .WhisMessage(message);
                }
            }

            foreach (var file in filesInDB)
            {
                file.DeletedDate = null;

                if (file.Directory?.DeletedDate != null)
                {
                    file.Directory = null;
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                string message = $"Ошибка, не удалось сохранить данные об отправке файлов в корзину:\n\t{ex}";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }

            var responseFileModels = new List<FileModel>();

            foreach (var file in filesInDB)
            {
                var fileModel = _mapper.Map<Entity.File, FileModel>(file);
                if (fileModel.Directory != null)
                {
                    fileModel.Directory.Files = null;
                }

                responseFileModels.Add(fileModel);
            }

            return new ResponseWithItems<FileModel>()
                .WhisStatus(ResponseStatuses.Success)
                .WhisItems(responseFileModels);
        }

        public async Task<StandartResponse> Delete(StandartRequest<FileModel> request)
        {
            var userId = request.UserId;
            var userRole = request.UserRole;

            StandartResponse response;

            //Проверяем наличие элементов
            response = FileModelsNotEmpty(request.Items);
            if (response.Status != ResponseStatuses.Success)
            {
                return response;
            }

            var fileModels = request.Items!;
            //Проверяем наличие элементов в fileModels
            response = FileModelsNotEmpty(fileModels);
            if (response.Status != ResponseStatuses.Success)
            {
                return response;
            }

            //Проверяем наличие параметра Id
            response = FileModelsContainId(fileModels);
            if (response.Status != ResponseStatuses.Success)
            {
                return response;
            }

            //Выгружаем данные о файлах из БД
            var fileIds = fileModels
                .Select(f => f.Id);

            List<Entity.File> filesInDB;

            try
            {
                filesInDB = await _context.Files
                    .Include(f => f.Bucket)
                    .Include(f => f.Directory)
                    .Where(f => fileIds.Contains(f.Id))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                string message = $"Ошибка при попытке получить файлы из БД:\n{ex}";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }


            //Проверяем все ли файлы найдены в БД
            if (filesInDB.Count != fileModels.Count())
            {
                var fileIdsInDB = filesInDB.Select(f => f.Id);

                var fileIdsNotDB = fileIds.Except(fileIdsInDB);

                var fileModelNotDB = fileModels.Where(f => fileIdsNotDB.Contains(f.Id));

                string message = $"Данные о этих файлах не найдены в БД";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new ResponseWithItems<FileModel>()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisItems(fileModelNotDB)
                    .WhisMessage(message);
            }

            if (userRole != "Admin")
            {
                var userFileIds = filesInDB
                    .Where(e => e.CreatedUserId == userId)
                    .Select(e => e.CreatedUserId);

                if (userFileIds.Count() != fileModels.Count())
                {
                    var notUserFileModels = fileModels
                        .Where(e => userFileIds.Contains(e.Id));

                    string message = $"У вас недостаточно прав на доступ к файлам:";

                    Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                    return new StandartResponse()
                        .WhisStatus(ResponseStatuses.Fail)
                        .WhisMessage(message);
                }
            }

            //Проверяем находятся ли все файлы их списка в корзине
            var fileWithEmptyDeleteDate = filesInDB
                .Where(f => f.DeletedDate == null);

            if (fileWithEmptyDeleteDate.Any())
            {
                var fileModelsWithEmptyDeleteDate = new List<FileModel>();
                foreach(var file in fileWithEmptyDeleteDate)
                {
                    var fileModel = _mapper.Map<Entity.File, FileModel>(file);
                    if (fileModel.Bucket != null)
                    {
                        fileModel.Bucket.Files = null;
                    }
                    if (fileModel.Directory != null)
                    {
                        fileModel.Directory.Files = null;
                    }

                    fileModelsWithEmptyDeleteDate.Add(fileModel);
                }

                string message = $"Ошибка, данные файлы находятся не в корзине";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new ResponseWithItems<FileModel>()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisItems(fileModelsWithEmptyDeleteDate)
                    .WhisMessage(message);
            }

            //Удаляем файлы поочередно
            var filesDelete = new List<FileModel>();
            var filesNotDelete = new List<FileModel>();
            foreach (var file in filesInDB)
            {
                var fileModel = _mapper.Map<Entity.File, FileModel>(file);
                if (fileModel.Bucket != null)
                {
                    fileModel.Bucket.Files = null;
                }
                if (fileModel.Directory != null)
                {
                    fileModel.Directory.Files = null;
                }

                string fileName = file.Name!; //Имя файла подгружается из базы (оно обязательно для заполнения)
                string bucketName = file.Bucket!.Name!;

                StandartResponse deleteFileResponse = await _minio.DeleteFile(
                    fileName,
                    bucketName);

                if (deleteFileResponse.Status != ResponseStatuses.Success)
                {
                    Console.WriteLine($"[x]{deleteFileResponse.Status}\n[x]{deleteFileResponse.Message}");

                    filesNotDelete.Add(fileModel);
                    continue;
                }

                try
                {
                    _context.Remove(file);
                    await _context.SaveChangesAsync();
                }
                catch
                {
                    Console.WriteLine($"[x]Ошибка при попытке удалить данные о файле из БД (На данный момент файл должен уже ужалится их хранилища а из БД нет):\n{deleteFileResponse.Message}");

                    filesNotDelete.Add(fileModel);

                    continue;
                }

                filesDelete.Add(fileModel);
            }

            if (filesNotDelete.Count == fileModels.Count())
            {
                string message = $"Не удалось удалить файлы:";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }

            if (filesNotDelete.Any())
            {
                string message = $"Данные файлы не удалось удалить:";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new ResponseWithItems<FileModel>()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisItems(filesNotDelete)
                    .WhisMessage(message);
            }

            return new StandartResponse()
                .WhisStatus(ResponseStatuses.Success);
        }




        private static StandartResponse FileModelsNotEmpty(IEnumerable<FileModel>? fileModels)
        {
            if (fileModels == null || !fileModels.Any())
            {
                string message = $"Ошибка, не указан перечень файлов";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }

            return new StandartResponse()
                .WhisStatus(ResponseStatuses.Success);
        }

        private static StandartResponse FileModelsContainId(IEnumerable<FileModel> fileModels)
        {
            var fileModelsWithEmptyId = fileModels
                .Where(f => f.Id == Guid.Empty);

            if (fileModelsWithEmptyId.Any())
            {
                string message = $"Ошибка, у данных файлов не указано поле {nameof(FileModel.Id)}";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new ResponseWithItems<FileModel>()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisItems(fileModelsWithEmptyId)
                    .WhisMessage(message);
            }

            return new StandartResponse()
                .WhisStatus(ResponseStatuses.Success);
        }

        private static StandartResponse FileModelsContainOriginalName(IEnumerable<FileModel> fileModels)
        {
            var fileModelsWithEmptyOriginalName = fileModels
                .Where(f => f.OriginalName == null);

            if (fileModelsWithEmptyOriginalName.Any())
            {
                string message = $"Ошибка, у данных файлов не указано поле {nameof(FileModel.OriginalName)}";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new ResponseWithItems<FileModel>()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisItems(fileModelsWithEmptyOriginalName)
                    .WhisMessage(message);
            }

            return new StandartResponse()
                .WhisStatus(ResponseStatuses.Success);
        }

        //private static StandartResponse FileModelsContainDirectory(params FileModel[] fileModels)
        //{
        //    var fileModelsWithEmptyOriginalName = fileModels
        //        .Where(f => f.Directory == null || f.Directory.Id == Guid.Empty);

        //    if (fileModelsWithEmptyOriginalName.Any())
        //    {
        //        string message = $"Ошибка, у данных файлов не указано поле {nameof(FileModel.Directory)}";

        //        Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

        //        return new StandartResponse()
        //            .WhisStatus(ResponseStatuses.Fail)
        //            .WhisItems(fileModelsWithEmptyOriginalName)
        //            .WhisMessage(message);
        //    }

        //    return new StandartResponse()
        //        .WhisStatus(ResponseStatuses.Success);
        //}

        private static StandartResponse FileModelsContainBucket(IEnumerable<FileModel> fileModels)
        {
            var fileModelsWithEmptyOriginalName = fileModels
                .Where(f => f.Bucket == null || string.IsNullOrEmpty(f.Bucket.Name));

            if (fileModelsWithEmptyOriginalName.Any())
            {
                string message = $"Ошибка, у данных файлов не указано поле {nameof(FileModel.Bucket)}";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new ResponseWithItems<FileModel>()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisItems(fileModelsWithEmptyOriginalName)
                    .WhisMessage(message);
            }

            return new StandartResponse()
                .WhisStatus(ResponseStatuses.Success);
        }

        private static StandartResponse FileModelsContainContentType(IEnumerable<FileModel> fileModels)
        {
            var fileModelsWithEmptyContentType = fileModels
                .Where(f => f.ContentType == null);

            if (fileModelsWithEmptyContentType.Any())
            {
                string message = $"Ошибка, у данных файлов не указано поле {nameof(FileModel.ContentType)}";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new ResponseWithItems<FileModel>()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisItems(fileModelsWithEmptyContentType)
                    .WhisMessage(message);
            }

            return new StandartResponse()
                .WhisStatus(ResponseStatuses.Success);
        }

        private static StandartResponse FileModelsContainStream(IEnumerable<FileModel> fileModels)
        {
            var fileModelsWithEmptyStream = fileModels
                .Where(f => f.Stream == null);

            if (fileModelsWithEmptyStream.Any())
            {
                string message = $"Ошибка, у данных файлов не указано поле {nameof(FileModel.Stream)}";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new ResponseWithItems<FileModel>()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisItems(fileModelsWithEmptyStream)
                    .WhisMessage(message);
            }

            return new StandartResponse()
                .WhisStatus(ResponseStatuses.Success);
        }
    }
}

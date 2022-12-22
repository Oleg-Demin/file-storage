using AutoMapper;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Minio.DataModel;
using System.Security.Claims;
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
    public class BucketService : StandartService
    {
        public BucketService( ApplicationDbContext context, MinioService minio, IMapper mapper)
            : base(context, minio, mapper) { }

        private delegate Entity.Bucket? GetBucketEntityDelegate(string bucketName);

        private delegate List<Entity.Bucket> GetBucketEntitiesDelegate(IEnumerable<string> bucketNames);

        private Entity.Bucket? GetBucketEntity(string bucketName)
        {
            return _context.Buckets
                .SingleOrDefault(b => b.Name == bucketName);
        }

        private Entity.Bucket? GetBucketEntityWithFiles(string bucketName)
        {
            return _context.Buckets
                .Include(b => b.Files)
                .SingleOrDefault(b => b.Name == bucketName);
        }

        private Entity.Bucket? GetBucketEntityAsToTracking(string bucketName)
        {
            return _context.Buckets
                .AsNoTracking()
                .SingleOrDefault(b => b.Name == bucketName);
        }

        private Entity.Bucket? GetBucketEntityWithFilesAsToTracking(string bucketName)
        {
            return _context.Buckets
                .AsNoTracking()
                .Include(b => b.Files)
                .SingleOrDefault(b => b.Name == bucketName);
        }

        //private Entity.Bucket? GetBucketEntityWithFiles(string bucketName, int size, int page)
        //{
        //    return _context.Buckets
        //        .Include(b => b.Files.Skip(size * (page - 1)).Take(size))
        //        .SingleOrDefault(b => b.Name == bucketName);
        //}

        //private Entity.Bucket? GetBucketEntityWithFilesAsToTracking(string bucketName, int size, int page)
        //{
        //    return _context.Buckets
        //        .AsNoTracking()
        //        .Include(b => b.Files.Skip(size * (page - 1)).Take(size))
        //        .SingleOrDefault(b => b.Name == bucketName);
        //}

        private StandartResponse Info(BucketModel bucketModel, GetBucketEntityDelegate getBucketEntity)
        {
            StandartResponse response;

            //Проверяем наличие параметра Name
            response = BucketModelContainName(bucketModel);
            if (response.Status != ResponseStatuses.Success)
            {
                return response;
            }

            var bucketName = bucketModel.Name!;

            //Выгружаем данные о bucket из БД
            Entity.Bucket? bucketEntity;
            try
            {
                bucketEntity = getBucketEntity(bucketName);
            }
            catch (Exception ex)
            {
                response = new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage($"Ошибка при попытке получить информацию о bucket \"{bucketName}\" из БД:\n{ex.Message}");

                ConsolePrintMessage(response.Status, response.Message);

                return response;
            }

            if (bucketEntity is null)
            {
                response = new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage($"Информация о bucket \"{bucketName}\" в БД не найдена");

                ConsolePrintMessage(response.Status, response.Message);

                return response;
            }

            //Формируем информацию о bucket для вывода
            bucketModel = _mapper.Map<Entity.Bucket, BucketModel>(bucketEntity);

            response = new ResponseWithItems<BucketModel>()
                .WhisStatus(ResponseStatuses.Success)
                .WhisMessage($"Информаци о bucket \"{bucketName}\" успешно найдена")
                .WhisItems(new List<BucketModel> { bucketModel });


            ConsolePrintMessage(response.Status, response.Message);

            return response;
        }




        public async Task<StandartResponse> Add(StandartRequest<BucketModel> request)
        {
            var userId = request.UserId;
            var userRole = request.UserRole;

            StandartResponse response;

            //Проверяем наличие элементов
            response = BucketModelsNotEmpty(request.Items);
            if (response.Status != ResponseStatuses.Success)
            {
                return response;
            }

            var bucketModels = request.Items!;

            //Проверяем наличие параметра Name
            response = BucketModelsContainName(bucketModels);
            if (response.Status != ResponseStatuses.Success)
            {
                return response;
            }

            //Проверяем сколько buckets поставили Default в true
            var bucketModelsDefault = bucketModels
                .Where(b => b.Default)
                .ToArray();

            if (bucketModelsDefault.Length > 1)
            {
                var message = $"Лишь одна bucket может быть по уполчанию";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new ResponseWithItems<BucketModel>()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisItems(bucketModelsDefault)
                    .WhisMessage(message);
            }

            //Создаем buckets
            var bucketAddEntities = new List<Entity.Bucket>();
            var bucketNotAddModels = new List<BucketModel>();
            foreach (var bucketModel in bucketModels)
            {
                var bucketName = bucketModel.Name!;

                //Добавляем buckets в хранилища Minio
                StandartResponse addBucketResponse = await _minio.AddBucket(bucketName);

                if (addBucketResponse.Status != ResponseStatuses.Success)
                {
                    var status = addBucketResponse.Status;
                    var message = addBucketResponse.Message;

                    Console.WriteLine($"[x]{status}\n[x]{message}");

                    bucketNotAddModels.Add(bucketModel);
                    continue;
                }

                //Добавляем buckets в БД
                var bucketEntity = new Entity.Bucket
                {
                    Name = bucketName,
                    CreateDate = DateTime.UtcNow,
                    CreatedUserId = userId
                };

                _context.Add(bucketEntity);
                bucketAddEntities.Add(bucketEntity);
            }

            //Сохраняем buckets в БД
            try
            {
                _context.SaveChanges();

                //Если необходимо то делаем одно из хранилищь по уполчанию
                if (bucketModelsDefault.Length == 1)
                {
                    BucketModel bucketModelDefault = bucketModelsDefault.First();
                    var responseChangeDefault = await СhangeDefault(
                        new StandartRequest<BucketModel>
                        {
                            Items = bucketModelsDefault
                        });

                    var status = responseChangeDefault.Status;
                    var message = responseChangeDefault.Message;

                    Console.WriteLine($"[x]{status}\n[x]{message}");
                }
            }
            catch (Exception ex)
            {
                string? message;
                ResponseStatuses status;

                foreach (var bucketName in bucketAddEntities.Select(b => b.Name))
                {
                    StandartResponse addBucketResponse = await _minio.DeleteBucket(bucketName);

                    if (addBucketResponse.Status != ResponseStatuses.Success)
                    {
                        status = addBucketResponse.Status;
                        message = addBucketResponse.Message;

                        Console.WriteLine($"[x]{status}\n[x]{message}");
                    }
                }

                message = $"Ошибка при попытке сохранить данные о backets в БД:\n{ex.Message}";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }

            if (bucketModels.Count() == bucketAddEntities.Count)
            {
                var responseBuckets = new List<BucketModel>();

                foreach (var bucketEntity in bucketAddEntities)
                {
                    responseBuckets.Add(_mapper.Map<Entity.Bucket, BucketModel>(bucketEntity));
                }

                return new ResponseWithItems<BucketModel>()
                    .WhisStatus(ResponseStatuses.Success)
                    .WhisItems(responseBuckets);
            }
            else
            {
                string message = "Не удалось создать следующие buckets";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new ResponseWithItems<BucketModel>()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisItems(bucketNotAddModels)
                    .WhisMessage(message);
            }
        }

        public async Task<StandartResponse> Add(BucketModel bucketModel, Guid userId)
        {
            StandartResponse response;

            //Проверяем наличие параметра Name
            response = BucketModelContainName(bucketModel);
            if (response.Status != ResponseStatuses.Success)
            {
                return response;
            }

            var bucketName = bucketModel.Name!;

            //Добавляем bucket в хранилища Minio
            response = await _minio.AddBucket(bucketName);

            //Проверяем создался ли bucket в хранилище Minio
            if (response.Status != ResponseStatuses.Success)
            {
                ConsolePrintMessage(response.Status, response.Message);

                return response;
            }

            //Добавляем bucket в контекст БД
            var bucketEntity = new Entity.Bucket
            {
                Name = bucketName,
                CreateDate = DateTime.UtcNow,
                CreatedUserId = userId
            };

            _context.Add(bucketEntity);

            //Добавляем buckets в БД
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                response = new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage($"Ошибка при попытке сохранить данные о backet \"{bucketName}\" в БД:\n{ex.Message}");

                ConsolePrintMessage(response.Status, response.Message);

                return response;
            }

            //Если нео
            if (bucketModel.Default)
            {
                response = await СhangeDefault(bucketModel);

                if (response.Status == ResponseStatuses.Fail)
                {
                    response = new ResponseWithItems<BucketModel>()
                        .WhisStatus(ResponseStatuses.Fail)
                        .WhisMessage($"Bucket \"{bucketName}\" успешно создан, но не удалось установить его как bucket по уполчанию:\n{response.Message}")
                        .WhisItems(new List<BucketModel> { bucketModel });
                    return response;
                }
            }

            //Формируем информацию о bucket для вывода
            bucketModel = _mapper.Map<Entity.Bucket, BucketModel>(bucketEntity);

            response = new ResponseWithItems<BucketModel>()
                .WhisStatus(ResponseStatuses.Success)
                .WhisMessage($"Bucket \"{bucketName}\" успешно создан")
                .WhisItems(new List<BucketModel> { bucketModel });

            ConsolePrintMessage(response.Status, response.Message);

            return response;
        }

        public StandartResponse Info()
        {
            StandartResponse response;

            List<Entity.Bucket> bucketEntities;
            try
            {
                bucketEntities = _context.Buckets
                    .AsNoTracking()
                    .ToList();
            }
            catch (Exception ex)
            {
                response = new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage($"Ошибка при попытке получить информацию о buckets из БД:\n{ex.Message}");

                ConsolePrintMessage(response.Status, response.Message);

                return response;
            }

            //if (userRole != "Admin")
            //{
            //    response = new StandartResponse()
            //        .WhisStatus(ResponseStatuses.Fail)
            //        .WhisMessage($"Не удается удалить bucket \"{bucketName}\", у вас недостает доступа к этому bucket");

            //    ConsolePrintMessage(response.Status, response.Message);

            //    return response;
            //}

            //if (bucketEntity.CreatedUserId != userId && userRole != "Admin")
            //{
            //    response = new StandartResponse()
            //        .WhisStatus(ResponseStatuses.Fail)
            //        .WhisMessage($" Не удается удалить bucket \"{bucketName}\", у вас недостает доступа к этому bucket");

            //    ConsolePrintMessage(response.Status, response.Message);

            //    return response;
            //}

            var bucketModels = new List<BucketModel>();
            //Формируем информацию о bucket для вывода
            foreach (var bucketEntity in bucketEntities)
            {
                var bucketModel = _mapper.Map<Entity.Bucket, BucketModel>(bucketEntity);

                bucketModels.Add(bucketModel);
            }
                
            response = new ResponseWithItems<BucketModel>()
                .WhisStatus(ResponseStatuses.Success)
                .WhisMessage($"Информаци о всех buckets успешно найдена")
                .WhisItems(bucketModels);

            ConsolePrintMessage(response.Status, response.Message);

            return response;
        }

        public StandartResponse Info(BucketModel bucketModel)
        {
            return Info(bucketModel, GetBucketEntityAsToTracking);
        }

        public StandartResponse InfoWithFiles(BucketModel bucketModel)
        {
            return Info(bucketModel, GetBucketEntityWithFilesAsToTracking);
        }

        public StandartResponse Info(StandartRequest<BucketModel> request)
        {
            var userId = request.UserId;
            var userRole = request.UserRole;

            StandartResponse response;

            var bucketModels = request.Items!;

            var bucketInforms = new List<BucketModel>();
            var bucketsInDB = new List<Entity.Bucket>();

            try
            {
                if (bucketModels == null || !bucketModels.Any())
                {
                    bucketsInDB = _context.Buckets
                        .AsNoTracking()
                        .ToList();
                }
                else
                {
                    //Проверяем наличие параметра Name
                    response = BucketModelsContainName(bucketModels.ToArray());
                    if (response.Status != ResponseStatuses.Success)
                    {
                        return response;
                    }

                    var bucketNames = bucketModels.Select(b => b.Name);

                    bucketsInDB = _context.Buckets
                        .Where(b => bucketNames.Contains(b.Name))
                        .AsNoTracking()
                        .ToList();

                    //Проверяем все ли buckets нашлись в БД
                    if (bucketModels.Count() != bucketsInDB.Count)
                    {
                        var bucketNamesInDB = bucketsInDB.Select(f => f.Name);

                        var bucketNamesNotDB = bucketNames.Except(bucketNamesInDB);

                        var bucketModelNotDB = bucketModels.Where(b => bucketNamesNotDB.Contains(b.Name));

                        string message = $"Данные о этих buckets не найдены в БД";

                        Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                        return new ResponseWithItems<BucketModel>()
                            .WhisStatus(ResponseStatuses.Fail)
                            .WhisItems(bucketModelNotDB)
                            .WhisMessage(message);
                    }
                }
            }
            catch (Exception ex)
            {
                string message = $"Ошибка при попытке получить информацию о всех buckets из БД:\n{ex}";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }

            foreach (var bucketInDB in bucketsInDB)
            {
                var bucketInfo = _mapper.Map<Entity.Bucket, BucketModel>(bucketInDB);

                bucketInforms.Add(bucketInfo);
            }

            return new ResponseWithItems<BucketModel>()
                .WhisStatus(ResponseStatuses.Success)
                .WhisItems(bucketInforms);
        }

        public StandartResponse Info(Guid userId)
        {
            StandartResponse response;

            List<Entity.Bucket> bucketEntities;
            try
            {
                bucketEntities = _context.Buckets
                    .Where(b => b.CreatedUserId == userId)
                    .AsNoTracking()
                    .ToList();
            }
            catch (Exception ex)
            {
                response = new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage($"Ошибка при попытке получить информацию о buckets из БД:\n{ex.Message}");

                ConsolePrintMessage(response.Status, response.Message);

                return response;
            }

            //if (userRole != "Admin")
            //{
            //    response = new StandartResponse()
            //        .WhisStatus(ResponseStatuses.Fail)
            //        .WhisMessage($"Не удается удалить bucket \"{bucketName}\", у вас недостает доступа к этому bucket");

            //    ConsolePrintMessage(response.Status, response.Message);

            //    return response;
            //}

            //if (bucketEntity.CreatedUserId != userId && userRole != "Admin")
            //{
            //    response = new StandartResponse()
            //        .WhisStatus(ResponseStatuses.Fail)
            //        .WhisMessage($" Не удается удалить bucket \"{bucketName}\", у вас недостает доступа к этому bucket");

            //    ConsolePrintMessage(response.Status, response.Message);

            //    return response;
            //}

            var bucketModels = new List<BucketModel>();
            //Формируем информацию о bucket для вывода
            foreach (var bucketEntity in bucketEntities)
            {
                var bucketModel = _mapper.Map<Entity.Bucket, BucketModel>(bucketEntity);

                bucketModels.Add(bucketModel);
            }

            response = new ResponseWithItems<BucketModel>()
                .WhisStatus(ResponseStatuses.Success)
                .WhisMessage($"Информаци о всех buckets успешно найдена")
                .WhisItems(bucketModels);

            ConsolePrintMessage(response.Status, response.Message);

            return response;
        }

        public async Task<StandartResponse> СhangeDefault(StandartRequest<BucketModel> request)
        {
            var userId = request.UserId;
            var userRole = request.UserRole;

            StandartResponse response;

            //Проверяем наличие элементов
            response = BucketModelsNotEmpty(request.Items);
            if (response.Status != ResponseStatuses.Success)
            {
                return response;
            }

            var bucketModels = request.Items!;

            //В данном запросе должен фигурировать всего 1 Bucket
            if (bucketModels.Count() > 1)
            {
                string message = $"Ошибка, в данном запросе должен фигурировать всего 1 Bucket";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }

            var bucketModel = bucketModels.First();

            //Проверяем наличие параметра Name
            response = BucketModelsContainName(bucketModels);
            if (response.Status != ResponseStatuses.Success)
            {
                return response;
            }

            string name = bucketModel.Name!;

            try
            {
                var buckets = _context.Buckets;

                if (bucketModel.Default)
                {
                    foreach (var backet in buckets)
                    {
                        backet.Default = false;
                    }
                }

                var defaultBucket = buckets.FirstOrDefault(b => b.Name == name);

                if (defaultBucket == null)
                {
                    string message = $"Bucket с Id ({name}) не существует";

                    Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                    return new StandartResponse()
                        .WhisStatus(ResponseStatuses.Fail)
                        .WhisMessage(message);
                }

                defaultBucket.Default = bucketModel.Default;

                bucketModel = _mapper.Map<Entity.Bucket, BucketModel>(defaultBucket);

                await _context.SaveChangesAsync();

                return new ResponseWithItems<BucketModel>()
                    .WhisStatus(ResponseStatuses.Success)
                    .WhisItems(new List<BucketModel> { bucketModel });
            }
            catch (Exception ex)
            {
                string message = $"Ошибка при попытке назначить bucket ({name}) как bucket по умолчанию:\n{ex}";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }
        }

        public async Task<StandartResponse> СhangeDefault(BucketModel bucketModel)
        {
            StandartResponse response = Info(bucketModel, GetBucketEntity);

            if (response is not ResponseWithItems<BucketModel> || response.Status == ResponseStatuses.Fail)
            {
                return response;
            }

            var responseWithItemsBucketModel = (ResponseWithItems<BucketModel>)response;

            var bucketEntity = _mapper.Map<BucketModel, Entity.Bucket>(responseWithItemsBucketModel.Items!.First());

            var bucketName = bucketModel.Name!;

            //Устанавливаем всем bucket параметр Default в false
            try
            {
                //Устанавливаем всем bucket параметр Default в false
                var bucketsOnDB = _context.Buckets;

                if (bucketModel.Default)
                {
                    foreach (var backet in bucketsOnDB)
                    {
                        backet.Default = false;
                    }
                }

                //Устанавливаем необходимому bucket значение Default в true
                bucketEntity.Default = bucketModel.Default;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                response = new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage($"Ошибка при попытке назначить bucket \"{bucketName}\" как bucket по умолчанию:\n{ex.Message}");

                ConsolePrintMessage(response.Status, response.Message);

                return response;
            }

            //Формируем информацию о bucket для вывода
            bucketModel = _mapper.Map<Entity.Bucket, BucketModel>(bucketEntity);

            response = new ResponseWithItems<BucketModel>()
                .WhisStatus(ResponseStatuses.Success)
                .WhisMessage($"Для bucket \"{bucketName}\" параметр {nameof(BucketModel.Default)} успешно установлен в значение {bucketModel.Default}")
                .WhisItems(new List<BucketModel> { bucketModel });

            ConsolePrintMessage(response.Status, response.Message);

            return response;
        }

        public async Task<StandartResponse> Delete(StandartRequest<BucketModel> request)
        {
            var userId = request.UserId;
            var userRole = request.UserRole;

            StandartResponse response;

            //Проверяем наличие элементов
            response = BucketModelsNotEmpty(request.Items);
            if (response.Status != ResponseStatuses.Success)
            {
                return response;
            }

            var bucketModels = request.Items!;

            //Проверяем наличие параметра Name
            response = BucketModelsContainName(bucketModels.ToArray());
            if (response.Status != ResponseStatuses.Success)
            {
                return response;
            }

            //Выгружаем данные о buckets из БД
            var bucketNames = bucketModels.Select(b => b.Name!);

            var bucketsInDB = new List<Entity.Bucket>();
            try
            {
                bucketsInDB = _context.Buckets
                    .Include(b => b.Files)
                    .Where(b => bucketNames.Contains(b.Name))
                    .ToList();
            }
            catch (Exception ex)
            {
                string message = $"Ошибка при попытке получить информацию о buckets из БД:\n{ex}";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }

            //Проверяем все ли buckets нашлись в БД
            if (bucketModels.Count() != bucketsInDB.Count)
            {
                var bucketNamesInDB = bucketsInDB.Select(f => f.Name);

                var bucketNamesNotDB = bucketNames.Except(bucketNamesInDB);

                var bucketModelNotDB = bucketModels.Where(b => bucketNamesNotDB.Contains(b.Name));

                string message = $"Данные о этих buckets не найдены в БД";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new ResponseWithItems<BucketModel>()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisItems(bucketModelNotDB)
                    .WhisMessage(message);
            }

            //Проверяем существуют ли не пустые buckets
            var bucketsHavingFiles = new List<Entity.Bucket>();

            foreach (var bucket in bucketsInDB)
            {
                if (bucket.Files != null && bucket.Files.Count != 0)
                {
                    string message = $"Ошибка при попытке удалить bucket из хранилища Minio:\nВнутри tBucket находятся файлы";

                    Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                    bucketsHavingFiles.Add(bucket);
                }
            }

            if (bucketsHavingFiles.Any())
            {
                var bucketHavingFilesModels = new List<BucketModel>();

                foreach  (var bucket in bucketsHavingFiles)
                {
                    var bucketModel = _mapper.Map<Entity.Bucket, BucketModel>(bucket);
                    bucketModel.Files = null;
                    bucketHavingFilesModels.Add(bucketModel);
                }

                string message = $"Нельзя удалить эти не пустые buckets";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new ResponseWithItems<BucketModel>()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisItems(bucketHavingFilesModels)
                    .WhisMessage(message);
            }

            //Удаляем buckets их хранилища Minio
            var bucketDeleted = new List<Entity.Bucket>();
            var bucketModelsNotDeleted = new List<BucketModel>();

            foreach (var bucket in bucketsInDB)
            {
                var responseDeleteBucket = await _minio.DeleteBucket(bucket.Name);

                if (responseDeleteBucket.Status != ResponseStatuses.Success)
                {
                    var status = responseDeleteBucket.Status;
                    var message = responseDeleteBucket.Message;

                    Console.WriteLine($"[x]{status}\n[x]{message}");

                    var bucketModel = _mapper.Map<Entity.Bucket, BucketModel>(bucket);

                    bucketModelsNotDeleted.Add(bucketModel);
                    continue;
                }

                bucketDeleted.Add(bucket);
            }

            //Удаляем buckets из БД
            try
            {
                _context.RemoveRange(bucketDeleted);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var message = $"Ошибка при попытке удалить данные о backets из БД:\n{ex.Message}";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }


            if (bucketModelsNotDeleted.Any())
            {
                //Buckets которые не удалось удалить их Minio
                string message = $"Данные файлы bucket не удалось удалить их хранилища Minio";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new ResponseWithItems<BucketModel>()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisItems(bucketModelsNotDeleted)
                    .WhisMessage(message);
            }

            return new StandartResponse()
                .WhisStatus(ResponseStatuses.Success);
        }

        public async Task<StandartResponse> Delete(BucketModel bucketModel)
        {
            StandartResponse response = Info(bucketModel, GetBucketEntityWithFiles);

            if (response is not ResponseWithItems<BucketModel> || response.Status == ResponseStatuses.Fail)
            {
                return response;
            }

            var responseWithItemsBucketModel = (ResponseWithItems<BucketModel>)response;

            bucketModel = responseWithItemsBucketModel.Items!.First();

            var bucketEntity = _mapper.Map<BucketModel, Entity.Bucket>(bucketModel);

            var bucketName = bucketModel.Name!;

            if (bucketEntity.Files is not null && bucketEntity.Files.Count != 0)
            {
                response = new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage($"Bucket \"{bucketName}\" нельзя удалить: в базе указано что в нем находятся файлы");

                ConsolePrintMessage(response.Status, response.Message);

                return response;
            }

            //Удаляем bucket из хранилища Minio
            response = await _minio.DeleteBucket(bucketName);

            //Проверяем удалился ли bucket из хранилища Minio
            if (response.Status != ResponseStatuses.Success)
            {
                ConsolePrintMessage(response.Status, response.Message);

                return response;
            }

            //Удаляем buckets из контекста БД
            _context.Remove(bucketEntity);

            //Удаляем buckets из БД
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                response = new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage($"Ошибка при попытке удалить данные о backets из БД:\n{ex.Message}");

                ConsolePrintMessage(response.Status, response.Message);

                return response;
            }

            response = new StandartResponse()
                .WhisStatus(ResponseStatuses.Success)
                .WhisMessage($"Bucket \"{bucketName}\" успешно удален");

            ConsolePrintMessage(response.Status, response.Message);

            return response;
        }





        private static StandartResponse BucketModelsNotEmpty(IEnumerable<BucketModel>? bucketModels)
        {
            if (bucketModels == null || !bucketModels.Any())
            {
                string message = $"Ошибка, не указан перечень Buckets";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }

            return new StandartResponse()
                .WhisStatus(ResponseStatuses.Success);
        }

        private static StandartResponse BucketModelsContainName(IEnumerable<BucketModel> bucketModels)
        {
            var fileModelsWithEmptyId = bucketModels
                .Where(f => string.IsNullOrEmpty(f.Name));

            if (fileModelsWithEmptyId.Any())
            {
                string message = $"Ошибка, у данных buckets не указано поле {nameof(BucketModel.Name)}";

                Console.WriteLine($"[x]{ResponseStatuses.Fail}\n[x]{message}");

                return new ResponseWithItems<BucketModel>()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisItems(fileModelsWithEmptyId)
                    .WhisMessage(message);
            }

            return new StandartResponse()
                .WhisStatus(ResponseStatuses.Success);
        }

        private static StandartResponse BucketModelContainName(BucketModel bucketModel)
        {
            StandartResponse response;

            var bucketName = bucketModel.Name;

            if (string.IsNullOrWhiteSpace(bucketName))
            {
                response = new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage($"У bucket \"{bucketName}\" не верно заполнено поле {nameof(BucketModel.Name)}");

                ConsolePrintMessage(response.Status, response.Message);

                return response;
            }

            return new StandartResponse()
                .WhisStatus(ResponseStatuses.Success)
                .WhisMessage($"У bucket \"{bucketName}\" поле {nameof(BucketModel.Name)} заполнено верно");
        }

        private static void ConsolePrintMessage(ResponseStatuses status, string? message)
        {
            Console.WriteLine();
            Console.WriteLine($"Status: {status}");
            Console.WriteLine($"Message: {message}");
            Console.WriteLine();
        }
    }
}

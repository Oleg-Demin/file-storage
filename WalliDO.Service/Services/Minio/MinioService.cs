using Minio;
using Minio.DataModel;
using WalliDO.Service.Enum;
using WalliDO.Service.Interfaces;
using WalliDO.Service.Models;
using WalliDO.Service.Models.Response;

namespace WalliDO.Service.Services.Minio
{
    public class MinioService
    {
        //Настройки доступа для подключения к Minio
        private static readonly MinioClient Minio = new MinioClient()
            .WithEndpoint("minio.wallido.ru")
            .WithCredentials("minio-access-key", "minio-secret-key")
            .WithSSL()
            .Build();

        private static string GenerateName()
        {
            return $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}";
        }

        public async Task<StandartResponse> GetBuckets()
        {
            try
            {
                var listBucketsResult = await Minio.ListBucketsAsync();

                var bucketNames = listBucketsResult.Buckets.Select(x => x.Name);

                var bucketModels = new List<BucketModel>();
                foreach (var bucketName in bucketNames)
                {
                    bucketModels.Add(new BucketModel { Name = bucketName });
                }

                return new ResponseWithItems<BucketModel>()
                    .WhisStatus(ResponseStatuses.Success)
                    .WhisItems(bucketModels);
            }
            catch (Exception ex)
            {
                string message = $"Ошибка при попытке узнать список имен buckets в хранилища Minio:\n{ex.Message}";

                Console.WriteLine(message);

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }

        }

        public async Task<StandartResponse> BucketExists(string bucket, ResponseStatuses yes, ResponseStatuses no )
        {
            try
            {
                var bucketExistsArgs = new BucketExistsArgs()
                    .WithBucket(bucket);

                bool found = await Minio.BucketExistsAsync(bucketExistsArgs);

                if (found)
                {
                    string message = $"Bucket с именем {bucket} найден в хранилище Minio";

                    Console.WriteLine(message);

                    return new StandartResponse()
                        .WhisStatus(yes)
                        .WhisMessage(message);
                }
                else
                {
                    string message = $"Bucket с именем {bucket} не найден в хранилище Minio";

                    Console.WriteLine(message);

                    return new StandartResponse()
                        .WhisStatus(no)
                        .WhisMessage(message);
                }
            }
            catch (Exception ex)
            {
                string message = $"Ошибка при попытке узнать есть ли {nameof(bucket)} с наименованием ({bucket}) в хранилища Minio:\n{ex.Message}";

                Console.WriteLine(message);

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }
        }

        public async Task<StandartResponse> AddBucket(string bucket)
        {
            var responseBucketExists = await BucketExists(
                bucket: bucket,
                yes: ResponseStatuses.Fail,
                no: ResponseStatuses.Success);

            if (responseBucketExists.Status != ResponseStatuses.Success)
            {
                Console.WriteLine(responseBucketExists.Message);

                return responseBucketExists;
            }

            try
            {
                var makeBucketArgs = new MakeBucketArgs()
                    //.WithLocation("ru-east-1")
                    .WithBucket(bucket);

                await Minio.MakeBucketAsync(
                    makeBucketArgs
                );

                string message = $"Bucket \"{bucket}\" успешно создан в хранилища Minio";

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Success)
                    .WhisMessage(message);
            }
            catch (Exception ex)
            {
                string message = $"Ошибка при попытке создать bucket \"{bucket}\" в хранилища Minio:\n{ex.Message}";

                Console.WriteLine(message);

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }
        }

        public async Task<StandartResponse> DeleteBucket(string bucket)
        {
            var responseBucketExists = await BucketExists(
                bucket: bucket,
                yes: ResponseStatuses.Success,
                no: ResponseStatuses.Fail);

            if (responseBucketExists.Status != ResponseStatuses.Success)
            {
                Console.WriteLine(responseBucketExists.Message);

                return responseBucketExists;
            }

            try
            {
                var makeBucketArgs = new RemoveBucketArgs()
                    .WithBucket(bucket);

                await Minio.RemoveBucketAsync(
                    makeBucketArgs
                );

                string message = $"Bucket \"{bucket}\" успешно удален из хранилища Minio";

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Success)
                    .WhisMessage(message);
            }
            catch (Exception ex)
            {
                string message = $"Ошибка при попытке удалить bucket \"{bucket}\" из хранилища Minio:\n{ex.Message}";

                Console.WriteLine(message);

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }
        }

        public async Task<StandartResponse> UploadFile(Stream stream, string extension, string bucket)
        {
            //Проверяем правильность заполнения параметра bucket
            if (string.IsNullOrEmpty(bucket) || string.IsNullOrWhiteSpace(bucket))
            {
                string message = $"Ошибка при попытке сохранить файл в {nameof(bucket)} \"{bucket}\" хранилища Minio:\nПеременная {nameof(bucket)} пуста";

                Console.WriteLine(message);

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }

            //Проверяем наличие bucket в хранилище Minio
            var responseBucketExists = await BucketExists(
                bucket: bucket,
                yes: ResponseStatuses.Success,
                no: ResponseStatuses.Fail);

            if (responseBucketExists.Status != ResponseStatuses.Success)
            {
                Console.WriteLine(responseBucketExists.Message);

                return responseBucketExists;
            }

            //Генерируем новое имя файла для хранения его в Minio
            string name = GenerateName() + extension;

            //Сохраняем файл в хранилище Minio
            stream.Position = 0;

            try
            {
                PutObjectArgs args = new PutObjectArgs()
                    .WithBucket(bucket)
                    .WithObject(name)
                    .WithStreamData(stream)
                    .WithObjectSize(stream.Length)
                    .WithContentType("application/octet-stream");

                await Minio.PutObjectAsync(args);
                
                return new ResponseWithItems<FileModel>()
                    .WhisStatus(ResponseStatuses.Success)
                    .WhisItems(new List<FileModel> { new FileModel { Name = name } });
            }
            catch (Exception ex)
            {
                string message = $"Ошибка при попытке сохранить файл в {nameof(bucket)} \"{bucket}\" хранилища Minio:\n{ex.Message}";

                Console.WriteLine(message);

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }
            finally
            {
                stream.Close();
            }
        }

        public async Task<StandartResponse> UploadFile(Stream stream, string extension, string bucket, byte[] encryptionKey)
        {
            //Проверяем правильность заполнения параметра bucket
            if (string.IsNullOrEmpty(bucket) || string.IsNullOrWhiteSpace(bucket))
            {
                string message = $"Ошибка при попытке сохранить файл в {nameof(bucket)} ({bucket}) хранилища Minio:\nПеременная {nameof(bucket)} пуста";

                Console.WriteLine(message);

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }

            //Проверяем наличие bucket в хранилище Minio
            var responseBucketExists = await BucketExists(
                bucket: bucket,
                yes: ResponseStatuses.Success,
                no: ResponseStatuses.Fail);

            if (responseBucketExists.Status != ResponseStatuses.Success)
            {
                Console.WriteLine(responseBucketExists.Message);

                return responseBucketExists;
            }

            //Генерируем новое имя файла для хранения его в Minio
            string name = GenerateName() + extension;

            //Сохраняем файл в хранилище Minio
            stream.Position = 0;

            try
            {
                PutObjectArgs args = new PutObjectArgs()
                    .WithBucket(bucket)
                    .WithObject(name)
                    .WithStreamData(stream)
                    .WithObjectSize(stream.Length)
                    .WithContentType("application/octet-stream");

                if (encryptionKey.Any())
                {
                    var ssec = new SSEC(encryptionKey);

                    args.WithServerSideEncryption(ssec);
                }

                await Minio.PutObjectAsync(args);

                return new ResponseWithItems<FileModel>()
                    .WhisStatus(ResponseStatuses.Success)
                    .WhisItems(new List<FileModel> { new FileModel { Name = name } });
            }
            catch (Exception ex)
            {
                string message = $"Ошибка при попытке сохранить файл в {nameof(bucket)} ({bucket}) хранилища Minio:\n{ex.Message}";

                Console.WriteLine(message);

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }
            finally
            {
                stream.Close();
            }
        }

        public async Task<StandartResponse> DeleteFile(string name, string bucket)
        {
            //Проверяем правильность заполнения параметра bucket
            if (string.IsNullOrEmpty(bucket) || string.IsNullOrWhiteSpace(bucket))
            {
                string message = $"Ошибка при попытке удалить файл в {nameof(bucket)} ({bucket}) хранилища Minio:\nПеременная {nameof(bucket)} пуста";

                Console.WriteLine(message);

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }

            //Проверяем наличие bucket в хранилище Minio
            var responseBucketExists = await BucketExists(
                bucket: bucket,
                yes: ResponseStatuses.Success,
                no: ResponseStatuses.Fail);

            if (responseBucketExists.Status != ResponseStatuses.Success)
            {
                Console.WriteLine(responseBucketExists.Message);

                return responseBucketExists;
            }

            //Проверяем правильность заполнения параметра name
            if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            {
                string message = $"Ошибка при попытке удалить файл в {nameof(bucket)} ({bucket}) хранилища Minio:\nПеременная {nameof(name)} пуста";

                Console.WriteLine(message);

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }

            //Удаляем файл из хранилища Minio
            try
            {
                RemoveObjectArgs args = new RemoveObjectArgs()
                    .WithBucket(bucket)
                    .WithObject(name);

                await Minio.RemoveObjectAsync(args);

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Success);
            }
            catch (Exception ex)
            {
                string message = $"Ошибка при попытке удалить файл ({name}) в {nameof(bucket)} ({bucket}) хранилища Minio:\n{ex.Message}";

                Console.WriteLine(message);

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }
        }

        public async Task<StandartResponse> GetFile(string name, string bucket)
        {
            //Проверяем правильность заполнения параметра bucket
            if (string.IsNullOrEmpty(bucket) || string.IsNullOrWhiteSpace(bucket))
            {
                string message = $"Ошибка при попытке получить файл из {nameof(bucket)} ({bucket}) хранилища Minio:\nПеременная {nameof(bucket)} пуста";

                Console.WriteLine(message);

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }

            //Проверяем наличие bucket в хранилище Minio
            var responseBucketExists = await BucketExists(
                bucket: bucket,
                yes: ResponseStatuses.Success,
                no: ResponseStatuses.Fail);

            if (responseBucketExists.Status != ResponseStatuses.Success)
            {
                Console.WriteLine(responseBucketExists.Message);

                return responseBucketExists;
            }

            //Проверяем правильность заполнения параметра name
            if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            {
                string message = $"Ошибка при попытке получить файл из {nameof(bucket)} ({bucket}) хранилища Minio:\nПеременная {nameof(name)} пуста";

                Console.WriteLine(message);

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }

            //Получаем файл их хранилища Minio
            try
            {
                var fileStream = new MemoryStream();
                GetObjectArgs args = new GetObjectArgs()
                    .WithBucket(bucket)
                    .WithObject(name)
                    .WithCallbackStream(stream => stream.CopyTo(fileStream));

                var stat = await Minio.GetObjectAsync(args);

                fileStream.Position = 0;

                return new ResponseWithItems<FileModel>()
                    .WhisStatus(ResponseStatuses.Success)
                    .WhisItems(new List<FileModel> { new FileModel { Name = name, Bucket = new BucketModel { Name = bucket }, Stream = fileStream } });
            }
            catch (Exception ex)
            {
                string message = $"Ошибка при попытке получить файл ({name}) из {nameof(bucket)} ({bucket}) хранилища Minio:\n{ex.Message}";

                Console.WriteLine(message);

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }
        }

        public async Task<StandartResponse> GetFile(string name, string bucket, byte[] encryptionKey)
        {
            //Проверяем правильность заполнения параметра bucket
            if (string.IsNullOrEmpty(bucket) || string.IsNullOrWhiteSpace(bucket))
            {
                string message = $"Ошибка при попытке получить файл из {nameof(bucket)} ({bucket}) хранилища Minio:\nПеременная {nameof(bucket)} пуста";

                Console.WriteLine(message);

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }

            //Проверяем наличие bucket в хранилище Minio
            var responseBucketExists = await BucketExists(
                bucket: bucket,
                yes: ResponseStatuses.Success,
                no: ResponseStatuses.Fail);

            if (responseBucketExists.Status != ResponseStatuses.Success)
            {
                Console.WriteLine(responseBucketExists.Message);

                return responseBucketExists;
            }

            //Проверяем правильность заполнения параметра name
            if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            {
                string message = $"Ошибка при попытке получить файл из {nameof(bucket)} ({bucket}) хранилища Minio:\nПеременная {nameof(name)} пуста";

                Console.WriteLine(message);

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }

            //Получаем файл их хранилища Minio
            try
            {
                var fileStream = new MemoryStream();
                GetObjectArgs args = new GetObjectArgs()
                    .WithBucket(bucket)
                    .WithObject(name)
                    .WithCallbackStream(stream => stream.CopyTo(fileStream));

                if (encryptionKey.Any())
                {
                    var ssec = new SSEC(encryptionKey);

                    args.WithServerSideEncryption(ssec);
                }

                var stat = await Minio.GetObjectAsync(args);

                fileStream.Position = 0;

                return new ResponseWithItems<FileModel>()
                    .WhisStatus(ResponseStatuses.Success)
                    .WhisItems(new List<FileModel> { new FileModel { Name = name, Bucket = new BucketModel { Name = bucket }, Stream = fileStream } });
            }
            catch (Exception ex)
            {
                string message = $"Ошибка при попытке получить файл ({name}) из {nameof(bucket)} ({bucket}) хранилища Minio:\n{ex.Message}";

                Console.WriteLine(message);

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }
        }

    }
}

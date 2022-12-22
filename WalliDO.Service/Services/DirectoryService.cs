using AutoMapper;
using Microsoft.EntityFrameworkCore;
using WalliDO.Service.Data;
using WalliDO.Service.Enum;
using WalliDO.Service.Interfaces;
using WalliDO.Service.Models;
using WalliDO.Service.Models.Abstract;
using WalliDO.Service.Models.Response;
using WalliDO.Service.Services.Minio;
using Entity = WalliDO.Service.Data.Entity;


namespace WalliDO.Service.Services
{
    public class DirectoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public DirectoryService(
            ApplicationDbContext context,
            IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<StandartResponse> Add(DirectoryModel directoryModel)
        {
            if (directoryModel.Name == null)
            {
                string message = $"Ошибка при попытке сохранить данные о directory в БД:\nОтсутствует наименование directory";

                Console.WriteLine(message);

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }

            try
            {
                var directoryEntity = new Entity.Directory
                {
                    Name = directoryModel.Name,
                    CreateDate = DateTime.UtcNow,
                    CreatedUserId = new Guid() //
                };

                await _context.AddAsync(directoryEntity);
                await _context.SaveChangesAsync();

                if (directoryModel.Parent == null)
                {
                    directoryModel = _mapper.Map<Entity.Directory, DirectoryModel>(directoryEntity);
                }
                else
                {
                    var parent = directoryModel.Parent;
                    directoryModel = _mapper.Map<Entity.Directory, DirectoryModel>(directoryEntity);
                    directoryModel.Parent = parent;

                    var changeParentDirectoryResponse = await СhangeParent(directoryModel);

                    if (changeParentDirectoryResponse.Status != ResponseStatuses.Success)
                    {
                        _context.Remove(directoryEntity);
                        _context.SaveChanges();

                        return changeParentDirectoryResponse;
                    }

                    directoryModel = ((ResponseWithItems<DirectoryModel>)changeParentDirectoryResponse).Items!.First();
                }


                return new ResponseWithItems<DirectoryModel>()
                    .WhisStatus(ResponseStatuses.Success)
                    .WhisItems(new List<DirectoryModel> { directoryModel });
            }
            catch (Exception ex)
            {
                string message = $"Ошибка при попытке сохранить данные о directory в БД:\n{ex.Message}";

                Console.WriteLine(message);

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }
        }

        public async Task<StandartResponse> Info(DirectoryModel directoryModel)
        {
            try
            {
                var directory = await _context.Directories
                    .AsNoTracking()
                    .Where(d => d.Id == directoryModel.Id)
                    .FirstOrDefaultAsync();

                if (directory == null)
                {
                    string message = $"Ошибка при попытке получить информацию о директори:\nДиректория не найден в БД";

                    Console.WriteLine(message);

                    return new StandartResponse()
                        .WhisStatus(ResponseStatuses.Fail)
                        .WhisMessage(message);
                }

                directoryModel = _mapper.Map<Entity.Directory, DirectoryModel>(directory);

                return new ResponseWithItems<DirectoryModel>()
                    .WhisStatus(ResponseStatuses.Success)
                    .WhisItems(new List<DirectoryModel> { directoryModel });
            }
            catch (Exception ex)
            {
                string message = $"Ошибка при попытке получить информацию о директори:\n{ex.Message}";

                Console.WriteLine(message);

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }
        }

        public async Task<StandartResponse> СhangeParent(DirectoryModel directoryModel)
        {
            if (directoryModel.Id == Guid.Empty)
            {
                string message = $"Ошибка при попытке назначить родительскую директори:\n\tId потомка отсутствует";

                Console.WriteLine(message);

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }

            try
            {
                var child = _context.Directories
                    .Where(d => d.Id == directoryModel.Id)
                    .FirstOrDefault();

                if (child == null)
                {
                    string message = $"Ошибка при попытке назначить родительскую директори:\nДиректория потомок не найден в БД";

                    Console.WriteLine(message);

                    return new StandartResponse()
                        .WhisStatus(ResponseStatuses.Fail)
                        .WhisMessage(message);
                }

                Entity.Directory? parent = null;

                if (directoryModel.Parent == null || directoryModel.Parent.Id == Guid.Empty)
                {
                    child.ParentId = null;
                    
                    directoryModel = _mapper.Map<Entity.Directory, DirectoryModel>(child);
                }
                else
                {
                    parent = _context.Directories
                        .Where(d => d.Id == directoryModel.Parent.Id)
                        .FirstOrDefault();

                    if (parent == null)
                    {
                        string message = $"Ошибка при попытке назначить родительскую директори:\nРодительская директория не найдена в БД";

                        Console.WriteLine(message);

                        return new StandartResponse()
                            .WhisStatus(ResponseStatuses.Fail)
                            .WhisMessage(message);
                    }

                    child.ParentId = parent.Id;

                    directoryModel = _mapper.Map<Entity.Directory, DirectoryModel>(child);
                    directoryModel.Parent = _mapper.Map<Entity.Directory, DirectoryModel>(parent);

                    var responseChildDirectories = await ChildDirectories(directoryModel);

                    if (responseChildDirectories.Status != ResponseStatuses.Success)
                    {
                        return responseChildDirectories;
                    }

                    var childDirectories = ((ResponseWithItems<DirectoryModel>)responseChildDirectories).Items!;

                    var childIds = childDirectories.Select(e => e.Id);
                    var parentId = parent.Id;

                    if (childIds.Contains(parentId))
                    {
                        string message = $"Ошибка при попытке назначить родительскую директори:\nНельзя назначить директорию потомка директорией родителем";

                        Console.WriteLine(message);

                        return new StandartResponse()
                            .WhisStatus(ResponseStatuses.Fail)
                            .WhisMessage(message);
                    }
                }

                await _context.SaveChangesAsync();

                return new ResponseWithItems<DirectoryModel>()
                    .WhisStatus(ResponseStatuses.Success)
                    .WhisItems(new List<DirectoryModel> { directoryModel });
            }
            catch (Exception ex)
            {
                string message = $"Ошибка при попытке назначить родительскую директори:\n{ex.Message}";

                Console.WriteLine(message);

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }
        }

        //public async Task<StandartResponse> MoveInTrash(DirectoryModel directoryModel)
        //{
        //}

        //public async Task<StandartResponse> MoveOutTrash(DirectoryModel directoryModel)
        //{
        //}

        public async Task<StandartResponse> ChildDirectories(DirectoryModel? directoryModel = null)
        {
            List<Entity.Directory> entityDirectories;
            try
            {
                if (directoryModel == null)
                {
                    entityDirectories = await _context.Directories
                        .AsNoTracking()
                        .Where(d => d.ParentId == null)
                        .ToListAsync();
                }
                else
                {
                    entityDirectories = await _context.Directories
                        .AsNoTracking()
                        .Where(d => d.ParentId == directoryModel.Id)
                        .ToListAsync();
                }
            }
            catch (Exception ex)
            {
                string message = $"Ошибка при попытке получить список дочерних директорий из БД:\n{ex.Message}";

                Console.WriteLine(message);

                return new StandartResponse()
                    .WhisStatus(ResponseStatuses.Fail)
                    .WhisMessage(message);
            }

            var directoryModels = new List<DirectoryModel>();

            foreach (var entitydirectory in entityDirectories)
            {
                directoryModels.Add(_mapper.Map<Entity.Directory, DirectoryModel>(entitydirectory));
            }

            return new ResponseWithItems<DirectoryModel>()
                .WhisStatus(ResponseStatuses.Success)
                .WhisItems(directoryModels);

        }

        //public async Task<StandartResponse> Delete(DirectoryModel directoryModel)
        //{
        //}
    }
}

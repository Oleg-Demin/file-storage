using AutoMapper;
using WalliDO.Service.Enum;
using Entity = WalliDO.Service.Data.Entity;

namespace WalliDO.Service.Models.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Entity.File, FileModel>()
                .MaxDepth(2)
                .ForMember(
                dst => dst.Encription,
                opt => opt.MapFrom(
                    src => src.EncryptionKey.Any())
                );
            CreateMap<FileModel, Entity.File>()
                .ForMember(
                dst => dst.Bucket,
                opt => opt.Ignore())
                .ForMember(
                dst => dst.Directory,
                opt => opt.Ignore())
                .ForMember(
                dst => dst.Bucket,
                opt => opt.AllowNull())
                .ForMember(
                dst => dst.Directory,
                opt => opt.AllowNull());

            CreateMap<Entity.Directory, DirectoryModel>()
                .MaxDepth(2);
            CreateMap<DirectoryModel, Entity.Directory>()
                .ForMember(
                dst => dst.Parent,
                opt => opt.Ignore())
                .ForMember(
                dst => dst.Parent,
                opt => opt.AllowNull());

            CreateMap<Entity.Bucket, BucketModel>()
                .MaxDepth(2)
                .ForMember(
                dst => dst.Files,
                opt => opt.AllowNull());
            CreateMap<BucketModel, Entity.Bucket>();

            //CreateMap<List<Entity.Bucket>, List<DirectoryModel>>();
            //CreateMap<List<DirectoryModel>, List<Entity.Bucket>>();

        }
    }
}

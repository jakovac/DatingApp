using System.Linq;
using API.DTOs;
using API.Entities;
using API.Extensions;
using AutoMapper;

namespace API.Helpers
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<AppUser, MemberDto>()
                .ForMember(dest => dest.PhotoUrl, opt => opt.MapFrom(src => src.Photos.FirstOrDefault(x => x.IsMain).Url))
            //prvo dajemo destinaciju u koju ce da se mapira, posle kazemo odakle zelimo da se mapira i source do podatka
                .ForMember(dest => dest.Age, opt => opt.MapFrom(src => src.DateOfBirth.CalculateAge()));
            CreateMap<Photo, PhotoDto>();
        //sada hocemo obrnuto u odnosu na gore, za update da idemo iz memberDto u AppUser
            CreateMap<MemberUpdateDto, AppUser>();
            CreateMap<RegisterDto, AppUser>();
            CreateMap<Message, MessageDto>()
                .ForMember(dest => dest.SenderPhotoUrl, opt => opt.MapFrom(
                    src => src.Sender.Photos.FirstOrDefault(x => x.IsMain).Url))
                .ForMember(dest => dest.RecipientPhotoUrl, opt => opt.MapFrom(
                    src => src.Recipient.Photos.FirstOrDefault(x => x.IsMain).Url));
        }
        //ovo koristimo ako dependency koju uvozimo pa moramo da dodamo u ApplicatioServiceExtensions
    }

}
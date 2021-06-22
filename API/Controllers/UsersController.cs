using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Authorize]
    public class UsersController : BaseApiController
    {
        //public DataContext _context { get; }
        // private readonly IUserRepository _unitOfWork.UserRepository;
        private readonly IMapper _mapper;
        private readonly IPhotoService _photoService;
        private readonly IUnitOfWork _unitOfWork;

        public UsersController(IUnitOfWork unitOfWork, /*IUserRepository userRepository*//*DataContext context*/ IMapper mapper, IPhotoService photoService)
        {
            _unitOfWork = unitOfWork;
            _photoService = photoService;
            _mapper = mapper;
            //_context = context;
            // _unitOfWork.UserRepository = userRepository;
        }

        // [Authorize(Roles ="Admin")]
        //[AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers([FromQuery] UserParams userParams)
        {
            //var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());
            var gender = await _unitOfWork.UserRepository.GetUserGender(User.GetUsername());
            userParams.CurrentUsername = User.GetUsername();

            if (string.IsNullOrEmpty(userParams.Gender))
                userParams.Gender = gender == "male" ? "female" : "male";

            var users = await _unitOfWork.UserRepository.GetMembersAsync(userParams);
            //var users = await _unitOfWork.UserRepository.GetUsersAsync();

            //var usertToReturn = _mapper.Map<IEnumerable<MemberDto>>(users);
            //return await _context.Users.ToListAsync();
            Response.AddPaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages);

            return Ok(users);
        }

        // [Authorize(Roles = "Member")]
        // api/users/3
        //[HttpGet("{id}")]
        [HttpGet("{username}", Name = "GetUser")]
        public async Task<ActionResult<MemberDto>> GetUser(/*int id*/string username)
        {
            return await _unitOfWork.UserRepository.GetMemberAsync(username);
            // var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(username);
            // return await _context.Users.FindAsync(id);
            //return _mapper.Map<MemberDto>(user);
        }

        [HttpPut]
        public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto) //ne treba da nista saljemo natrag, teorija je da 
        //klijent ima sve podatke o entitety koji update-ujemo tako da ne treba da vracamo nista, tj. user object
        {
            //prvo treba negde da cuvamo usera i njegov username, ne zelimo da verujemo useru da ce nam da ti username vec ga cupamo
            //iz mesta gde ga autentifikujemo, a to je token. Claims prinicples
            var username = User.GetUsername();//User.FindFirst(ClaimTypes.NameIdentifier)?.Value; //ovo bi trebalo da nam da username iz tokena
            //i njega cemo da update-ujemo
            var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(username);

            _mapper.Map(memberUpdateDto, user); //ovo nam omogucava da izbegnemo manuelno mapiranje.
            _unitOfWork.UserRepository.Update(user);

            if (await _unitOfWork.Complete()) return NoContent();

            return BadRequest("Failed to update user");
        }

        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
        {
            var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());

            var result = await _photoService.AddPhotoAsync(file);

            if (result.Error != null) BadRequest(result.Error.Message);

            var photo = new Photo
            {
                Url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId
            };

            if (user.Photos.Count == 0)
            {
                photo.IsMain = true;
            }
            user.Photos.Add(photo);

            if (await _unitOfWork.Complete())
            {

                //return _mapper.Map<PhotoDto>(photo);
                // return CreatedAtRoute("GetUser", _mapper.Map<PhotoDto>(photo));
                return CreatedAtRoute("GetUser", new { username = user.UserName }, _mapper.Map<PhotoDto>(photo));
            }

            return BadRequest("Problem adding photo");
        }


        [HttpPut("set-main-photo/{photoId}")]
        public async Task<ActionResult> SetMainPhoto(int photoId)
        {
            var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());

            var photo = user.Photos.FirstOrDefault(x => x.ID == photoId);

            if (photo.IsMain) return BadRequest("This is already your main photo");

            var currentMain = user.Photos.FirstOrDefault(x => x.IsMain);
            if (currentMain != null) currentMain.IsMain = false;
            photo.IsMain = true;

            if (await _unitOfWork.Complete()) return NoContent();

            return BadRequest("Failed to set main photo");
        }

        [HttpDelete("delete-photo/{photoId}")]
        public async Task<ActionResult> DeletePhoto(int photoId)
        {
            var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());

            var photo = user.Photos.FirstOrDefault(x => x.ID == photoId);

            if (photo == null) return NotFound();

            if (photo.IsMain) return BadRequest("You cannot delete your main photo");

            if (photo.PublicId != null)
            {
                var result = await _photoService.DeletePhotoAsync(photo.PublicId);
                if (result.Error != null) return BadRequest(result.Error.Message);
            }

            user.Photos.Remove(photo);

            if (await _unitOfWork.Complete()) return Ok();

            return BadRequest("Failed to delete the photo");
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Authorize]
    public class UsersController : BaseApiController
    {
        //public DataContext _context { get; }
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public UsersController(IUserRepository userRepository/*DataContext context*/, IMapper mapper)
        {
            _mapper = mapper;
            //_context = context;
            _userRepository = userRepository;
        }

        //[AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers()
        {
            var users = await _userRepository.GetMembersAsync();
            //var users = await _userRepository.GetUsersAsync();

            //var usertToReturn = _mapper.Map<IEnumerable<MemberDto>>(users);
            //return await _context.Users.ToListAsync();
            return Ok(users);
        }
        //[Authorize]
        // api/users/3
        //[HttpGet("{id}")]
        [HttpGet("{username}")]
        public async Task<ActionResult<MemberDto>> GetUser(/*int id*/string username)
        {
            return await _userRepository.GetMemberAsync(username);
            // var user = await _userRepository.GetUserByUsernameAsync(username);
            // return await _context.Users.FindAsync(id);
            //return _mapper.Map<MemberDto>(user);
        }

        [HttpPut]
        public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto) //ne treba da nista saljemo natrag, teorija je da 
        //klijent ima sve podatke o entitety koji update-ujemo tako da ne treba da vracamo nista, tj. user object
        {
            //prvo treba negde da cuvamo usera i njegov username, ne zelimo da verujemo useru da ce nam da ti username vec ga cupamo
            //iz mesta gde ga autentifikujemo, a to je token. Claims prinicples
            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; //ovo bi trebalo da nam da username iz tokena
            //i njega cemo da update-ujemo
            var user = await _userRepository.GetUserByUsernameAsync(username);

            _mapper.Map(memberUpdateDto, user); //ovo nam omogucava da izbegnemo manuelno mapiranje.
            _userRepository.Update(user);

            if(await _userRepository.SaveAllAsync()) return NoContent();

            return BadRequest("Failed to update user");
        }
    }
}
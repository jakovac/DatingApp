using System.Collections.Generic;
using System.Linq;
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
    }
}
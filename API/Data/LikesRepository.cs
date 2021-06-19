
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class LikesRepository : ILikesRepository
    {
        public DataContext _context { get; }
        public LikesRepository(DataContext context)
        {
            _context = context;
        }

        public async Task<UserLike> GetUserLike(int sourceUserID, int likedUserID)
        {
            return await _context.Likes.FindAsync(sourceUserID, likedUserID);
        }

        public async Task<PagedList<LikeDto>> GetUserLikes(LikesParams likesParams)
        {
            var users = _context.Users.OrderBy(u => u.UserName).AsQueryable();
            var likes = _context.Likes.AsQueryable();

            if(likesParams.Predicate == "liked") //trenutno logovani user koga je lajkovao
            {
                likes = likes.Where(like => like.SourceUserID == likesParams.UserID);
                users = likes.Select(like => like.LikedUser); //useri iz Liked table
            }
            if(likesParams.Predicate == "likedBy")
            {
                likes = likes.Where(like => like.LikedUserID == likesParams.UserID);
                users = likes.Select(like => like.SourceUser); //useri koji su lajkovali trenutno logovanog
            }

            var likedUsers = users.Select(user => new LikeDto {
                Username = user.UserName,
                KnownAs = user.KnownAs,
                Age = user.DateOfBirth.CalculateAge(),
                PhotoUrl = user.Photos.FirstOrDefault(p => p.IsMain).Url,
                City = user.City,
                ID = user.ID
            });
            return await PagedList<LikeDto>.CreateAcync(likedUsers, likesParams.PageNumber, likesParams.PageSize);
        }

        public async Task<AppUser> GetUserWithLikes(int userID)
        {
            return await _context.Users
                .Include(x => x.LikedUsers)
                .FirstOrDefaultAsync(x => x.ID == userID);
        }
    }
}
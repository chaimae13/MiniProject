using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniProject_GMD.Context;
using MiniProject_GMD.Models;
using MiniProject_GMD.Models.DTO;
using MiniProject_GMD.Services;

namespace MiniProject_GMD.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly FakeUserData _fakeUserData;


        public UsersController(AppDbContext context, FakeUserData fakeUserData)
        {
            _context = context;
            _fakeUserData = fakeUserData;
        }

 


        // GET: api/Users
        [HttpGet("generate")]
        public async Task<IActionResult> GenerateUsers([FromQuery]  int count)
        {
            var users = _fakeUserData.GeneratePersons(count);
            var json = JsonSerializer.Serialize(users);

            var bytes = Encoding.UTF8.GetBytes(json);

            return File(bytes, "application/json", "users.json");
        }


        [HttpPost("batch")]
        public async Task<IActionResult> UploadUserFile(IFormFile file)
   
            {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded or file is empty.");

            List<User> users = new List<User>();
            try
            {
                using (var stream = new StreamReader(file.OpenReadStream()))
                {
                    string jsonContent = await stream.ReadToEndAsync();
                    users = JsonSerializer.Deserialize<List<User>>(jsonContent);
                }
            }
            catch (JsonException)
            {
                return BadRequest("Invalid JSON format.");
            }

            if (users == null || users.Count == 0)
                return BadRequest("No valid users found in the file.");

            int totalRecords = users.Count;
            int successfullyImported = 0;
            int notImported = 0;

            foreach (var user in users)
            {
                // Check for duplicates based on email and username
                bool exists = await _context.Users.AnyAsync(u => u.Email == user.Email || u.Username == user.Username);

                if (!exists)
                {
                    // Hash the password before saving to the database
                    user.Password = PasswordHasher.HashPassword(user.Password);
                    _context.Users.Add(user);
                    successfullyImported++;
                }
                else
                {
                    notImported++;
                }
            }

            await _context.SaveChangesAsync();

            // Prepare summary response
            var result = new
            {
                TotalRecords = totalRecords,
                SuccessfullyImported = successfullyImported,
                NotImported = notImported
            };

            return Ok(result);
        }


        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] LoginDto userObj)
        {
            if (userObj == null)
                return BadRequest();

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Username == userObj.Username);

            if (user == null)
                return NotFound(new { Message = "User not found!" });

         
            if (!PasswordHasher.VerifyPassword(userObj.Password, user.Password))
            {
                return BadRequest(new { Message = "Credentials are incorrect" });
            }
            var accessToken = PasswordHasher.CreateJwt(user);
            

            await _context.SaveChangesAsync();

            return Ok(new 
            {
                AccessToken = accessToken,
               
            });
        }


        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<User>> GetMyProfile()
        {

            var userEmail = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            var user = await _context.Users.FirstOrDefaultAsync(r => r.Email == userEmail);
            

          

            return user;
        }


        [Authorize(Roles = "admin")]
        [HttpGet("username")]
        public async Task<ActionResult<User>> GetAllUser()
        {

            var userEmail = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            var user = await _context.Users.Where(user => user.Email != userEmail).ToListAsync();

            return Ok(user);
        }


        [HttpGet("all")]
        public async Task<ActionResult<User>> GetAll()
        {

            var user = await _context.Users.ToListAsync();

            return Ok(user);
        }

        [Authorize(Roles = "admin")]
        [HttpGet("{username}")]
        public async Task<ActionResult<User>> GetAllUsers(string username)
        {

            var user = await _context.Users.Where(user => user.Username == username).ToListAsync();

            return Ok(user);
        }


    }
}

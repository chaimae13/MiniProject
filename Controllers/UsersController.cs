using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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
        public async Task<IActionResult> GenerateUsers([FromQuery] int count)
        {
            try
            {
                var users = _fakeUserData.GeneratePersons(count);
                var json = JsonSerializer.Serialize(users);
                var bytes = Encoding.UTF8.GetBytes(json);
                return File(bytes, "application/json", "users.json");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Error generating users", Details = ex.Message });
            }
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

                if (users == null || users.Count == 0)
                    return BadRequest("No valid users found in the file.");

                int totalRecords = users.Count;
                int successfullyImported = 0;
                int notImported = 0;

                foreach (var user in users)
                {
                    bool exists = await _context.Users.AnyAsync(u => u.Email == user.Email || u.Username == user.Username);

                    if (!exists)
                    {
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

                var result = new
                {
                    TotalRecords = totalRecords,
                    SuccessfullyImported = successfullyImported,
                    NotImported = notImported
                };

                return Ok(result);
            }
            catch (JsonException)
            {
                return BadRequest("Invalid JSON format.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Error processing user file", Details = ex.Message });
            }
        }

        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] LoginDto userObj)
        {
            if (userObj == null)
                return BadRequest(new { Message = "Invalid login data" });

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(x => x.Username == userObj.Username);

                if (user == null)
                    return NotFound(new { Message = "User not found" });

                if (!PasswordHasher.VerifyPassword(userObj.Password, user.Password))
                    return BadRequest(new { Message = "Credentials are incorrect" });

                var accessToken = PasswordHasher.CreateJwt(user);
                await _context.SaveChangesAsync();

                return Ok(new { AccessToken = accessToken });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Authentication failed", Details = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<User>> GetMyProfile()
        {
            try
            {
                var userEmail = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized(new { Message = "User email not found in token" });

                var user = await _context.Users.FirstOrDefaultAsync(r => r.Email == userEmail);

                if (user == null)
                    return NotFound(new { Message = "User profile not found" });

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Error retrieving profile", Details = ex.Message });
            }
        }

        [Authorize(Roles = "admin")]
        [HttpGet("{username}")]
        public async Task<ActionResult<User>> GetAllUsers(string username)
        {
            try
            {
                var users = await _context.Users.Where(user => user.Username == username).ToListAsync();

                if (!users.Any())
                    return NotFound(new { Message = "No users found with the given username" });

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Error retrieving users", Details = ex.Message });
            }
        }
    }
}

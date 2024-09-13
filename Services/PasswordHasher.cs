using Microsoft.IdentityModel.Tokens;
using MiniProject_GMD.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MiniProject_GMD.Services
{
    public class PasswordHasher
    {
        private static RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
        private static readonly int SaltSize = 16;
        private static readonly int HashSize = 20;
        private static readonly int Iterations = 10000;

        public static string HashPassword(string password)
        {
            byte[] salt;
            rngCsp.GetBytes(salt = new byte[SaltSize]);

            var key = new Rfc2898DeriveBytes(password, salt, Iterations);
            var hash = key.GetBytes(HashSize);

            var hashBytes = new byte[SaltSize + HashSize];
            Array.Copy(salt, 0, hashBytes, 0, SaltSize);
            Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

            var base64Hash = Convert.ToBase64String(hashBytes);

            return base64Hash;
        }

        public static bool VerifyPassword(string password, string base64Hash)
        {
            var hashBytes = Convert.FromBase64String(base64Hash);

            var salt = new byte[SaltSize];
            Array.Copy(hashBytes, 0, salt, 0, SaltSize);

            var key = new Rfc2898DeriveBytes(password, salt, Iterations);
            byte[] hash = key.GetBytes(HashSize);

            for (var i = 0; i < HashSize; i++)
            {
                if (hashBytes[i + SaltSize] != hash[i])
                    return false;
            }
            return true;
        }

        public static string CreateJwt(User user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();

            var key = Encoding.ASCII.GetBytes("veryverysceret......thiskeyislongenoughnow!!");
            var identity = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(ClaimTypes.Email,$"{user.Email}")
            });

            var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = identity,
                Expires = DateTime.Now.AddDays(15),
                SigningCredentials = credentials
            };
            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            return jwtTokenHandler.WriteToken(token);
        }

    }

}

﻿using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MiniProject_GMD.Models
{
    public enum UserRoles
    {
        admin,
        user
    }

    public class User
    {
        [Key]
        [JsonIgnore] 
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime BirthDate { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string Avatar { get; set; }
        public string Company { get; set; }
        public string JobPosition { get; set; }
        public string Mobile { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public UserRoles Role { get; set; }


    }
}

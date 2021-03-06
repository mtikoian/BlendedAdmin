﻿using System.ComponentModel.DataAnnotations;

namespace BlendedAdmin.Models.Users
{
    public class ChangePassowrdModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        [Required]
        public string Password { get; set; }
        public bool Succeeded { get; internal set; }
    }
}

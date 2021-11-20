using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CelebAppServer.Models
{
    public enum Gender
    {
        Male = 1,
        Female = 2,
        Trans = 3,
        Genderqueer = 4
    }
    public class CelebrityItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime BirthDate { get; set; }
        public Gender Gender { get; set; }
        public string Role { get; set; }
        public string ImageSource { get; set; }

        public CelebrityItem(string name, DateTime birthDate, Gender gender, string role, string imageSource, int id)
        {
            Name = name;
            BirthDate = birthDate;
            Gender = gender;
            Role = role;
            ImageSource = imageSource;
            Id = id;
        }

    }
}
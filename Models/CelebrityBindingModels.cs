using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CelebAppServer.Models
{
    public class DeleteCelebrityBindingModels
    {
        [Required]
        [Display(Name = "CelebrityId")]
        public int CelebrityId { get; set; }
    }

    public class StoreCelebrityBindingModels
    {
        public List<CelebrityItem> Celebrities;
    }
}
﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Heavy.Web.ViewModels
{
    public class RoleEditViewModel
    {
        public string Id { get; set; }

        [Required]
        [Display(Name = "角色名称")]
        public string RoleName { get; set; }

        public List<string> Users { get; set; }
    }
}

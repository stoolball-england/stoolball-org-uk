using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Stoolball.Web.Account
{
    public class ResetPasswordRequest
    {
            [Required]
            [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
            public string Email { get; set; }
    }
}
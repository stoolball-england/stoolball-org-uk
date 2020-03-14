using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Stoolball.Web.Account
{
    public class MyAccountUpdate
    {
        [Required]
        public string Name { get; set; }
    }
}

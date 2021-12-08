using System.ComponentModel.DataAnnotations;

namespace Stoolball.Web.Account
{
    public class PersonalDetailsFormData
    {
        [Required]
        public string Name { get; set; }
    }
}

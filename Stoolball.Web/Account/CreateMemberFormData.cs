using System.ComponentModel.DataAnnotations;
using Umbraco.Cms.Web.Website.Models;

namespace Stoolball.Web.Account
{
    public class CreateMemberFormData : RegisterModel
    {
        [RegularExpression("^((?!tronlink|TRONLINK|TronLink).)*$", ErrorMessage = "You cannot create an account with that name")]
        public new string? Name { get => base.Name; set => base.Name = value; }
    }
}

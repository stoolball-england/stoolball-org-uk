using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Website.Models;

namespace Stoolball.Web.Account
{
    public class CreateMemberExecuter : ICreateMemberExecuter
    {
        public async Task<IActionResult> CreateMember(Func<RegisterModel, Task<IActionResult>> executeFunction, CreateMemberFormData model)
        {
            if (executeFunction is null)
            {
                throw new ArgumentNullException(nameof(executeFunction));
            }

            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var registerModel = new RegisterModel
            {
                Name = model.Name,
                Email = model.Email,
                Password = model.Password,
                ConfirmPassword = model.ConfirmPassword,
                Username = model.Username,
                MemberTypeAlias = model.MemberTypeAlias,
                MemberProperties = model.MemberProperties,
                AutomaticLogIn = model.AutomaticLogIn,
                UsernameIsEmail = model.UsernameIsEmail,
                RedirectUrl = model.RedirectUrl
            };

            return await executeFunction(registerModel);
        }
    }
}
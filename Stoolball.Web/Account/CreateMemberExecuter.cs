using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Website.Models;

namespace Stoolball.Web.Account
{
    public class CreateMemberExecuter : ICreateMemberExecuter
    {
        public async Task<IActionResult> CreateMember(Func<RegisterModel, Task<IActionResult>> executeFunction, RegisterModel model)
        {
            if (executeFunction is null)
            {
                throw new ArgumentNullException(nameof(executeFunction));
            }

            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            return await executeFunction(model);
        }
    }
}
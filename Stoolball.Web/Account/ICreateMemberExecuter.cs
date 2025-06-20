using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Website.Models;

namespace Stoolball.Web.Account
{
    public interface ICreateMemberExecuter
    {
        Task<IActionResult> CreateMember(Func<RegisterModel, Task<IActionResult>> executeFunction, CreateMemberFormData model);
    }
}
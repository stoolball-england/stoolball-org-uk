using System;
using System.Web.Mvc;
using Umbraco.Web.Models;

namespace Stoolball.Web.Account
{
    public interface ICreateMemberExecuter
    {
        ActionResult CreateMember(Func<RegisterModel, ActionResult> executeFunction, RegisterModel model);
    }
}
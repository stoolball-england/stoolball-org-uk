using System;
using System.Web.Mvc;
using Umbraco.Web.Models;

namespace Stoolball.Web.Account
{
    public class CreateMemberExecuter : ICreateMemberExecuter
    {
        public ActionResult CreateMember(Func<RegisterModel, ActionResult> executeFunction, RegisterModel model)
        {
            if (executeFunction is null)
            {
                throw new ArgumentNullException(nameof(executeFunction));
            }

            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            return executeFunction(model);
        }
    }
}
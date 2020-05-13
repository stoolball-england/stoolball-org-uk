using Stoolball.Clubs;
using System.Web.Mvc;
using Umbraco.Web.Mvc;

namespace Stoolball.Web.Clubs
{
    public class EditClubSurfaceController : SurfaceController
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        public ActionResult Edit([Bind(Prefix = "Club")]Club club)
        {
            var viewModel = new ClubViewModel(CurrentPage)
            {
                Club = club,
                IsAuthorized = Members.IsMemberAuthorized(null, new[] { "Administrators" }, null)
            };

            if (viewModel.IsAuthorized && ModelState.IsValid)
            {
                // save the club

                // redirect back to the club
                //  return Redirect(Request.RawUrl.Substring(0, Request.RawUrl.Length - "/edit".Length));
            }

            //ModelState.AddModelError("Club.ClubName", $"{club.ClubName} is not valid");
            //ModelState.AddModelError(string.Empty, $"The whole form is not valid");


            viewModel.Metadata.PageTitle = $"Edit {club.ClubName}";
            return View("EditClub", viewModel);
        }
    }
}
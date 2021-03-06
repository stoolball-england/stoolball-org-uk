﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ASP
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Web;
    using System.Web.Helpers;
    using System.Web.Mvc;
    using System.Web.Mvc.Ajax;
    using System.Web.Mvc.Html;
    using System.Web.Routing;
    using System.Web.Security;
    using System.Web.UI;
    using System.Web.WebPages;
    
    #line 3 "..\..\Views\ResetPassword.cshtml"
    using ClientDependency.Core.Mvc;
    
    #line default
    #line hidden
    
    #line 2 "..\..\Views\ResetPassword.cshtml"
    using ContentModels = Umbraco.Web.PublishedModels;
    
    #line default
    #line hidden
    using Examine;
    
    #line 4 "..\..\Views\ResetPassword.cshtml"
    using Stoolball.Web.Account;
    
    #line default
    #line hidden
    
    #line 5 "..\..\Views\ResetPassword.cshtml"
    using Stoolball.Web.Email;
    
    #line default
    #line hidden
    using Umbraco.Core;
    using Umbraco.Core.Models;
    using Umbraco.Core.Models.PublishedContent;
    using Umbraco.Web;
    using Umbraco.Web.Mvc;
    using Umbraco.Web.PublishedModels;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Views/ResetPassword.cshtml")]
    public partial class _Views_ResetPassword_cshtml : Umbraco.Web.Mvc.UmbracoViewPage<ContentModels.ResetPassword>
    {
        public _Views_ResetPassword_cshtml()
        {
        }
        public override void Execute()
        {
            
            #line 6 "..\..\Views\ResetPassword.cshtml"
  
    Html.EnableClientValidation();
    Html.EnableUnobtrusiveJavaScript();
    Html.RequiresJs("~/scripts/jquery.validate.min.js");
    Html.RequiresJs("~/scripts/jquery.validate.unobtrusive.min.js");

            
            #line default
            #line hidden
WriteLiteral("\r\n<div");

WriteLiteral(" class=\"container-xl\"");

WriteLiteral(">\r\n    <h1>");

            
            #line 13 "..\..\Views\ResetPassword.cshtml"
   Write(Model.Name);

            
            #line default
            #line hidden
WriteLiteral("</h1>\r\n\r\n");

            
            #line 15 "..\..\Views\ResetPassword.cshtml"
    
            
            #line default
            #line hidden
            
            #line 15 "..\..\Views\ResetPassword.cshtml"
     if (User.Identity.IsAuthenticated && (!Model.ShowPasswordResetSuccessful.HasValue || !Model.ShowPasswordResetSuccessful.Value))
    {
        
            
            #line default
            #line hidden
            
            #line 17 "..\..\Views\ResetPassword.cshtml"
   Write(Html.Partial("_Logout"));

            
            #line default
            #line hidden
            
            #line 17 "..\..\Views\ResetPassword.cshtml"
                                
    }
    else
    {
        if (!Model.ShowPasswordResetSuccessful.HasValue)
        {
            
            
            #line default
            #line hidden
            
            #line 23 "..\..\Views\ResetPassword.cshtml"
                                                                      
            if (Model.PasswordResetTokenValid)
            {
                
            
            #line default
            #line hidden
            
            #line 26 "..\..\Views\ResetPassword.cshtml"
                                                                             
                using (Html.BeginUmbracoForm<ResetPasswordSurfaceController>("UpdatePassword"))
                {
                    
            
            #line default
            #line hidden
            
            #line 29 "..\..\Views\ResetPassword.cshtml"
               Write(Html.AntiForgeryToken());

            
            #line default
            #line hidden
            
            #line 29 "..\..\Views\ResetPassword.cshtml"
                                            
                    var resetPasswordUpdate = new ResetPasswordUpdate
                    {
                        PasswordResetToken = Model.PasswordResetToken
                    };

                    
            
            #line default
            #line hidden
            
            #line 35 "..\..\Views\ResetPassword.cshtml"
               Write(Html.HiddenFor(m => resetPasswordUpdate.PasswordResetToken));

            
            #line default
            #line hidden
            
            #line 35 "..\..\Views\ResetPassword.cshtml"
                                                                                

            
            #line default
            #line hidden
WriteLiteral("                    <div");

WriteLiteral(" class=\"form-group\"");

WriteLiteral(">\r\n");

WriteLiteral("                        ");

            
            #line 37 "..\..\Views\ResetPassword.cshtml"
                   Write(Html.LabelFor(m => resetPasswordUpdate.NewPassword, "New password"));

            
            #line default
            #line hidden
WriteLiteral("\r\n");

            
            #line 38 "..\..\Views\ResetPassword.cshtml"
                        
            
            #line default
            #line hidden
            
            #line 38 "..\..\Views\ResetPassword.cshtml"
                           var describedBy = "form-new-password"; 
            
            #line default
            #line hidden
WriteLiteral("\r\n");

            
            #line 39 "..\..\Views\ResetPassword.cshtml"
                        
            
            #line default
            #line hidden
            
            #line 39 "..\..\Views\ResetPassword.cshtml"
                         if (!string.IsNullOrEmpty(Model.PasswordHelp))
                        {
                            describedBy = "form-new-password form-new-password-help";

            
            #line default
            #line hidden
WriteLiteral("                            <p");

WriteLiteral(" class=\"form-text\"");

WriteLiteral(" id=\"form-new-password-help\"");

WriteLiteral("><small>");

            
            #line 42 "..\..\Views\ResetPassword.cshtml"
                                                                               Write(Model.PasswordHelp);

            
            #line default
            #line hidden
WriteLiteral("</small></p>\r\n");

            
            #line 43 "..\..\Views\ResetPassword.cshtml"
                        }

            
            #line default
            #line hidden
WriteLiteral("                        ");

            
            #line 44 "..\..\Views\ResetPassword.cshtml"
                   Write(Html.PasswordFor(m => resetPasswordUpdate.NewPassword, new { @class = "form-control", required = "required", aria_describedby = describedBy, autocorrect = "off", autocapitalize = "off" }));

            
            #line default
            #line hidden
WriteLiteral("\r\n");

WriteLiteral("                        ");

            
            #line 45 "..\..\Views\ResetPassword.cshtml"
                   Write(Html.ValidationMessageFor(m => resetPasswordUpdate.NewPassword, null, new { id = "form-new-password" }));

            
            #line default
            #line hidden
WriteLiteral("\r\n                    </div>\r\n");

            
            #line 47 "..\..\Views\ResetPassword.cshtml"


            
            #line default
            #line hidden
WriteLiteral("                    <button");

WriteLiteral(" class=\"btn btn-primary\"");

WriteLiteral(">");

            
            #line 48 "..\..\Views\ResetPassword.cshtml"
                                               Write(Model.UpdatePasswordButton);

            
            #line default
            #line hidden
WriteLiteral("</button>\r\n");

            
            #line 49 "..\..\Views\ResetPassword.cshtml"
                }
            }
            else
            {
                
            
            #line default
            #line hidden
            
            #line 53 "..\..\Views\ResetPassword.cshtml"
                                                               
                
            
            #line default
            #line hidden
            
            #line 54 "..\..\Views\ResetPassword.cshtml"
           Write(Html.ProtectEmailAddresses(Model.PasswordResetTokenInvalid));

            
            #line default
            #line hidden
            
            #line 54 "..\..\Views\ResetPassword.cshtml"
                                                                            
            }
        }
        else
        {
            
            
            #line default
            #line hidden
            
            #line 59 "..\..\Views\ResetPassword.cshtml"
                                                        
            if (Model.ShowPasswordResetSuccessful.Value)
            {
                
            
            #line default
            #line hidden
            
            #line 62 "..\..\Views\ResetPassword.cshtml"
                                                   
                
            
            #line default
            #line hidden
            
            #line 63 "..\..\Views\ResetPassword.cshtml"
           Write(Html.ProtectEmailAddresses(Model.PasswordResetSuccessful));

            
            #line default
            #line hidden
            
            #line 63 "..\..\Views\ResetPassword.cshtml"
                                                                          
            }
            else
            {
                
            
            #line default
            #line hidden
            
            #line 67 "..\..\Views\ResetPassword.cshtml"
                                                       
                
            
            #line default
            #line hidden
            
            #line 68 "..\..\Views\ResetPassword.cshtml"
           Write(Html.ProtectEmailAddresses(Model.PasswordResetFailed));

            
            #line default
            #line hidden
            
            #line 68 "..\..\Views\ResetPassword.cshtml"
                                                                      
            }

            
            #line default
            #line hidden
WriteLiteral("            <p><a");

WriteLiteral(" href=\"/\"");

WriteLiteral(" class=\"btn btn-primary btn-back\"");

WriteLiteral(">");

            
            #line 70 "..\..\Views\ResetPassword.cshtml"
                                                       Write(Model.HomeButton);

            
            #line default
            #line hidden
WriteLiteral("</a></p>\r\n");

            
            #line 71 "..\..\Views\ResetPassword.cshtml"
        }
    }

            
            #line default
            #line hidden
WriteLiteral("</div>");

        }
    }
}
#pragma warning restore 1591

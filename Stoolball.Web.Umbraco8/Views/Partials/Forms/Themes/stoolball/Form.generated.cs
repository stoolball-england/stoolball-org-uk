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
    using Examine;
    using Umbraco.Core;
    using Umbraco.Core.Models;
    using Umbraco.Core.Models.PublishedContent;
    
    #line 2 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
    using Umbraco.Forms.Core.Enums;
    
    #line default
    #line hidden
    
    #line 6 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
    using Umbraco.Forms.Core.Providers.FieldTypes;
    
    #line default
    #line hidden
    
    #line 3 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
    using Umbraco.Forms.Mvc;
    
    #line default
    #line hidden
    
    #line 5 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
    using Umbraco.Forms.Mvc.BusinessLogic;
    
    #line default
    #line hidden
    
    #line 4 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
    using Umbraco.Forms.Mvc.Models;
    
    #line default
    #line hidden
    using Umbraco.Web;
    using Umbraco.Web.Mvc;
    using Umbraco.Web.PublishedModels;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Views/Partials/Forms/Themes/stoolball/Form.cshtml")]
    public partial class _Views_Partials_Forms_Themes_stoolball_Form_cshtml : WebViewPage<Umbraco.Forms.Web.Models.FormViewModel>
    {
        public _Views_Partials_Forms_Themes_stoolball_Form_cshtml()
        {
        }
        public override void Execute()
        {
WriteLiteral("\r\n<div");

WriteLiteral(" class=\"umbraco-forms-page\"");

WriteLiteral(">\r\n\r\n");

            
            #line 10 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
    
            
            #line default
            #line hidden
            
            #line 10 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
     if (string.IsNullOrEmpty(Model.CurrentPage.Caption) == false)
    {

            
            #line default
            #line hidden
WriteLiteral("        <h2");

WriteLiteral(" class=\"umbraco-forms-caption\"");

WriteLiteral(">");

            
            #line 12 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                     Write(Model.CurrentPage.Caption);

            
            #line default
            #line hidden
WriteLiteral("</h2>\r\n");

            
            #line 13 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
    }

            
            #line default
            #line hidden
WriteLiteral("\r\n");

            
            #line 15 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
    
            
            #line default
            #line hidden
            
            #line 15 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
     if (Model.ShowValidationSummary)
    {
        
            
            #line default
            #line hidden
            
            #line 17 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
   Write(Html.ValidationSummary(false));

            
            #line default
            #line hidden
            
            #line 17 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                      
    }

            
            #line default
            #line hidden
WriteLiteral("\r\n\r\n");

            
            #line 21 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
    
            
            #line default
            #line hidden
            
            #line 21 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
     foreach (FieldsetViewModel fs in Model.CurrentPage.Fieldsets)
    {

            if (!string.IsNullOrEmpty(fs.Caption))
            {

            
            #line default
            #line hidden
WriteLiteral("                ");

WriteLiteral("<section class=\"umbraco-forms-fieldset\" id=\"");

            
            #line 26 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                                         Write(fs.Id);

            
            #line default
            #line hidden
WriteLiteral("\" aria-labelledby=\"");

            
            #line 26 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                                                                  Write(fs.Id);

            
            #line default
            #line hidden
WriteLiteral("-label\">\r\n");

WriteLiteral("                <p");

WriteLiteral(" class=\"h3\"");

WriteAttribute("id", Tuple.Create(" id=\"", 813), Tuple.Create("\"", 830)
            
            #line 27 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
, Tuple.Create(Tuple.Create("", 818), Tuple.Create<System.Object, System.Int32>(fs.Id
            
            #line default
            #line hidden
, 818), false)
, Tuple.Create(Tuple.Create("", 824), Tuple.Create("-label", 824), true)
);

WriteLiteral(">");

            
            #line 27 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                           Write(fs.Caption);

            
            #line default
            #line hidden
WriteLiteral("</p>\r\n");

            
            #line 28 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
            }
            else
            {

            
            #line default
            #line hidden
WriteLiteral("                ");

WriteLiteral("<div class=\"umbraco-forms-fieldset\" id=\"");

            
            #line 31 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                                     Write(fs.Id);

            
            #line default
            #line hidden
WriteLiteral("\">\r\n");

            
            #line 32 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
            }


            
            #line default
            #line hidden
WriteLiteral("            <div");

WriteLiteral(" class=\"row\"");

WriteLiteral(">\r\n\r\n");

            
            #line 36 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                
            
            #line default
            #line hidden
            
            #line 36 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                 foreach (var c in fs.Containers)
                {

            
            #line default
            #line hidden
WriteLiteral("                    <div");

WriteAttribute("class", Tuple.Create(" class=\"", 1109), Tuple.Create("\"", 1163)
, Tuple.Create(Tuple.Create("", 1117), Tuple.Create("umbraco-forms-container", 1117), true)
            
            #line 38 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
, Tuple.Create(Tuple.Create(" ", 1140), Tuple.Create<System.Object, System.Int32>("col-md-" + c.Width
            
            #line default
            #line hidden
, 1141), false)
);

WriteLiteral(">\r\n\r\n");

            
            #line 40 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                        
            
            #line default
            #line hidden
            
            #line 40 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                         foreach (FieldViewModel f in c.Fields)
                        {
                            bool hidden = f.HasCondition && f.ConditionActionType == FieldConditionActionType.Show;


            
            #line default
            #line hidden
WriteLiteral("                        <div");

WriteAttribute("class", Tuple.Create(" class=\"", 1408), Tuple.Create("\"", 1486)
, Tuple.Create(Tuple.Create("", 1416), Tuple.Create("form-group", 1416), true)
            
            #line 44 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
, Tuple.Create(Tuple.Create(" ", 1426), Tuple.Create<System.Object, System.Int32>(Html.GetFormFieldWrapperClass(f.FieldTypeName)
            
            #line default
            #line hidden
, 1427), false)
            
            #line 44 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
              , Tuple.Create(Tuple.Create(" ", 1474), Tuple.Create<System.Object, System.Int32>(f.CssClass
            
            #line default
            #line hidden
, 1475), false)
);

WriteLiteral(" ");

            
            #line 44 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                                                                                               if (hidden) {
            
            #line default
            #line hidden
WriteLiteral(" ");

WriteLiteral(" style=\"display: none\" ");

WriteLiteral("  ");

            
            #line 44 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                                                                                                                                                   } 
            
            #line default
            #line hidden
WriteLiteral(">\r\n\r\n");

            
            #line 46 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                            
            
            #line default
            #line hidden
            
            #line 46 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                             if (f.FieldType.GetType() != typeof(CheckBox) && f.FieldType.GetType() != typeof(RadioButtonList) && f.FieldType.GetType() != typeof(CheckBoxList))
                            {
                                if (!f.HideLabel)
                                {

            
            #line default
            #line hidden
WriteLiteral("                                    <label");

WriteAttribute("for", Tuple.Create(" for=\"", 1888), Tuple.Create("\"", 1899)
            
            #line 50 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
, Tuple.Create(Tuple.Create("", 1894), Tuple.Create<System.Object, System.Int32>(f.Id
            
            #line default
            #line hidden
, 1894), false)
);

WriteAttribute("id", Tuple.Create(" id=\"", 1900), Tuple.Create("\"", 1916)
            
            #line 50 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
, Tuple.Create(Tuple.Create("", 1905), Tuple.Create<System.Object, System.Int32>(f.Id
            
            #line default
            #line hidden
, 1905), false)
, Tuple.Create(Tuple.Create("", 1910), Tuple.Create("-label", 1910), true)
);

WriteLiteral(" class=\"umbraco-forms-label\"");

WriteLiteral(">\r\n");

WriteLiteral("                                        ");

            
            #line 51 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                   Write(f.Caption);

            
            #line default
            #line hidden
WriteLiteral(" ");

            
            #line 51 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                                    if (f.ShowIndicator)
                                        {

            
            #line default
            #line hidden
WriteLiteral("                                            <span");

WriteLiteral(" class=\"umbraco-forms-indicator\"");

WriteLiteral(">");

            
            #line 53 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                                                             Write(Model.Indicator);

            
            #line default
            #line hidden
WriteLiteral("</span>\r\n");

            
            #line 54 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                        }

            
            #line default
            #line hidden
WriteLiteral("                                    </label>\r\n");

            
            #line 56 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                }

                                if (!string.IsNullOrEmpty(f.ToolTip) && f.FieldType.GetType() != typeof(DataConsent))
                                {

            
            #line default
            #line hidden
WriteLiteral("                                    <p");

WriteLiteral(" class=\"form-text\"");

WriteAttribute("id", Tuple.Create(" id=\"", 2508), Tuple.Create("\"", 2526)
            
            #line 60 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
, Tuple.Create(Tuple.Create("", 2513), Tuple.Create<System.Object, System.Int32>(f.Id
            
            #line default
            #line hidden
, 2513), false)
, Tuple.Create(Tuple.Create("", 2518), Tuple.Create("-tooltip", 2518), true)
);

WriteLiteral("><small>");

            
            #line 60 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                                                              Write(f.ToolTip);

            
            #line default
            #line hidden
WriteLiteral("</small></p>\r\n");

            
            #line 61 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                }
                            }

            
            #line default
            #line hidden
WriteLiteral("                            ");

            
            #line 63 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                             if (f.FieldType.GetType() == typeof(RadioButtonList) || f.FieldType.GetType() == typeof(CheckBoxList))
                            {

            
            #line default
            #line hidden
WriteLiteral("                                ");

WriteLiteral("<fieldset>\r\n");

            
            #line 66 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                if (!f.HideLabel || !string.IsNullOrEmpty(f.ToolTip))
                                {

            
            #line default
            #line hidden
WriteLiteral("                                    <legend");

WriteAttribute("id", Tuple.Create(" id=\"", 3000), Tuple.Create("\"", 3016)
            
            #line 68 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
, Tuple.Create(Tuple.Create("", 3005), Tuple.Create<System.Object, System.Int32>(f.Id
            
            #line default
            #line hidden
, 3005), false)
, Tuple.Create(Tuple.Create("", 3010), Tuple.Create("-label", 3010), true)
);

WriteLiteral(" class=\"umbraco-forms-label\"");

WriteLiteral(">\r\n");

            
            #line 69 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                    
            
            #line default
            #line hidden
            
            #line 69 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                     if (!f.HideLabel)
                                    {

            
            #line default
            #line hidden
WriteLiteral("                                        <p");

WriteAttribute("class", Tuple.Create(" class=\"", 3185), Tuple.Create("\"", 3245)
            
            #line 71 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
, Tuple.Create(Tuple.Create("", 3193), Tuple.Create<System.Object, System.Int32>(string.IsNullOrEmpty(f.ToolTip) ? "mb-0" : "mb-2"
            
            #line default
            #line hidden
, 3193), false)
);

WriteLiteral(">\r\n");

WriteLiteral("                                            ");

            
            #line 72 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                       Write(f.Caption);

            
            #line default
            #line hidden
WriteLiteral("\r\n");

            
            #line 73 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                            
            
            #line default
            #line hidden
            
            #line 73 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                             if (f.ShowIndicator)
                                            {

            
            #line default
            #line hidden
WriteLiteral("                                            <span");

WriteLiteral(" class=\"umbraco-forms-indicator\"");

WriteLiteral(">");

            
            #line 75 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                                                             Write(Model.Indicator);

            
            #line default
            #line hidden
WriteLiteral("</span>\r\n");

            
            #line 76 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                            }

            
            #line default
            #line hidden
WriteLiteral("                                        </p>\r\n");

            
            #line 78 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                    }

            
            #line default
            #line hidden
WriteLiteral("                                    ");

            
            #line 79 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                     if (!string.IsNullOrEmpty(f.ToolTip))
                                    {

            
            #line default
            #line hidden
WriteLiteral("                                        <p");

WriteLiteral(" class=\"form-text\"");

WriteAttribute("id", Tuple.Create(" id=\"", 3833), Tuple.Create("\"", 3851)
            
            #line 81 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
, Tuple.Create(Tuple.Create("", 3838), Tuple.Create<System.Object, System.Int32>(f.Id
            
            #line default
            #line hidden
, 3838), false)
, Tuple.Create(Tuple.Create("", 3843), Tuple.Create("-tooltip", 3843), true)
);

WriteLiteral("><small>");

            
            #line 81 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                                                                  Write(f.ToolTip);

            
            #line default
            #line hidden
WriteLiteral("</small></p>\r\n");

            
            #line 82 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                    }

            
            #line default
            #line hidden
WriteLiteral("                                    </legend>\r\n");

            
            #line 84 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                }
                            }

            
            #line default
            #line hidden
WriteLiteral("                            <div");

WriteLiteral(" class=\"umbraco-forms-field-wrapper\"");

WriteLiteral(">\r\n\r\n");

WriteLiteral("                                ");

            
            #line 88 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                           Write(Html.Partial(FormThemeResolver.GetFieldView(Model, f), f));

            
            #line default
            #line hidden
WriteLiteral("\r\n\r\n");

            
            #line 90 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                
            
            #line default
            #line hidden
            
            #line 90 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                 if (Model.ShowFieldValidaton)
                                {
                                    
            
            #line default
            #line hidden
            
            #line 92 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                               Write(Html.ValidationMessage(f.Id));

            
            #line default
            #line hidden
            
            #line 92 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                                                 
                                }

            
            #line default
            #line hidden
WriteLiteral("\r\n                            </div>\r\n");

            
            #line 96 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                            
            
            #line default
            #line hidden
            
            #line 96 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                             if (f.FieldType.GetType() == typeof(RadioButtonList) || f.FieldType.GetType() == typeof(CheckBoxList))
                            {

            
            #line default
            #line hidden
WriteLiteral("                                ");

WriteLiteral("</fieldset>\r\n");

            
            #line 99 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                            }

            
            #line default
            #line hidden
WriteLiteral("\r\n                        </div>\r\n");

            
            #line 102 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                        }

            
            #line default
            #line hidden
WriteLiteral("\r\n                    </div>\r\n");

            
            #line 105 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                }

            
            #line default
            #line hidden
WriteLiteral("            </div>\r\n");

            
            #line 107 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
            if (!string.IsNullOrEmpty(fs.Caption))
            {

            
            #line default
            #line hidden
WriteLiteral("                ");

WriteLiteral("</section>\r\n");

            
            #line 110 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
            }
            else
            {

            
            #line default
            #line hidden
WriteLiteral("                ");

WriteLiteral("</div>\r\n");

            
            #line 114 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
            }
        }

            
            #line default
            #line hidden
WriteLiteral("\r\n    <input");

WriteLiteral(" type=\"text\"");

WriteAttribute("name", Tuple.Create(" name=\"", 5035), Tuple.Create("\"", 5083)
            
            #line 117 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
, Tuple.Create(Tuple.Create("", 5042), Tuple.Create<System.Object, System.Int32>(Model.FormId.ToString().Replace("-", "")
            
            #line default
            #line hidden
, 5042), false)
);

WriteLiteral(" class=\"umbraco-forms-formid\"");

WriteLiteral(" />\r\n\r\n\r\n    <div");

WriteLiteral(" class=\"umbraco-forms-navigation row-fluid\"");

WriteLiteral(">\r\n\r\n        <div");

WriteLiteral(" class=\"col-md-12\"");

WriteLiteral(">\r\n");

            
            #line 123 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
            
            
            #line default
            #line hidden
            
            #line 123 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
             if (Model.IsMultiPage)
            {
                if (!Model.IsFirstPage)
                {

            
            #line default
            #line hidden
WriteLiteral("                    <input");

WriteLiteral(" class=\"btn prev cancel\"");

WriteLiteral("\r\n                           type=\"submit\"");

WriteAttribute("value", Tuple.Create("\r\n                           value=\"", 5415), Tuple.Create("\"", 5473)
            
            #line 129 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
, Tuple.Create(Tuple.Create("", 5451), Tuple.Create<System.Object, System.Int32>(Model.PreviousCaption
            
            #line default
            #line hidden
, 5451), false)
);

WriteLiteral("\r\n                           name=\"__prev\"");

WriteLiteral(" />\r\n");

            
            #line 131 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                }

                if (!Model.IsLastPage)
                {

            
            #line default
            #line hidden
WriteLiteral("                    <button");

WriteLiteral(" class=\"btn btn-secondary next\"");

WriteLiteral(" name=\"__next\"");

WriteLiteral(">");

            
            #line 135 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                                                    Write(Model.NextCaption);

            
            #line default
            #line hidden
WriteLiteral("</button>\r\n");

            
            #line 136 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                }

                if (Model.IsLastPage)
                {

            
            #line default
            #line hidden
WriteLiteral("                    <button");

WriteLiteral(" class=\"btn btn-primary\"");

WriteLiteral(" name=\"__next\"");

WriteLiteral(">");

            
            #line 140 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                                             Write(Model.SubmitCaption);

            
            #line default
            #line hidden
WriteLiteral("</button>\r\n");

            
            #line 141 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                }
            }
            else
            {

            
            #line default
            #line hidden
WriteLiteral("                <button");

WriteLiteral(" class=\"btn btn-primary\"");

WriteLiteral(" name=\"__next\"");

WriteLiteral(">");

            
            #line 145 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
                                                         Write(Model.SubmitCaption);

            
            #line default
            #line hidden
WriteLiteral("</button>\r\n");

            
            #line 146 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
            }

            
            #line default
            #line hidden
WriteLiteral("        </div>\r\n    </div>\r\n</div>\r\n\r\n");

            
            #line 151 "..\..\Views\Partials\Forms\Themes\stoolball\Form.cshtml"
Write(Html.Partial("Forms/Themes/default/ScrollToFormScript"));

            
            #line default
            #line hidden
        }
    }
}
#pragma warning restore 1591
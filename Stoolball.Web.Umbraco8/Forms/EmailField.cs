using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Web;
using Umbraco.Forms.Core;
using Umbraco.Forms.Core.Data.Storage;
using Umbraco.Forms.Core.Enums;
using Umbraco.Forms.Core.Models;

namespace Stoolball.Web.Forms
{
    /// <summary>
    /// An Umbraco Forms field type that validates an email address
    /// </summary>
    /// <seealso cref="Umbraco.Forms.Core.FieldType" />
    public class EmailField : FieldType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Email"/> class.
        /// </summary>
        public EmailField()
        {
            Id = new Guid("aa962af9-868a-484a-938c-a60e95a9f3cb");
            Name = "Email address";
            Description = "An email address. Triggers a keypad with an @ on touch screens.";
            DataType = FieldDataType.String;
            FieldTypeViewName = "FieldType.Email.cshtml";
            Icon = "icon-message";
            HideLabel = false;
            SupportsPreValues = false;
            SupportsRegex = false;
            SortOrder = 25;
        }

        /// <summary>
        /// Validates the field.
        /// </summary>
        /// <param name="form">The form.</param>
        /// <param name="field">The field.</param>
        /// <param name="postedValues">The posted values.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public override IEnumerable<string> ValidateField(Form form, Field field, IEnumerable<object> postedValues, HttpContextBase context, IFormStorage formStorage)
        {
            var errorMessages = new List<string>(base.ValidateField(form, field, postedValues, context, formStorage));

            if (postedValues != null)
            {
                foreach (string value in postedValues)
                {
                    if (!ValidateEmailAddress(value))
                    {
                        errorMessages.Add("Please enter a valid email address");
                        break;
                    }
                }
            }

            return errorMessages;
        }

        /// <summary>
        /// Validates an email address based on the format of the address. Does not check that the address exists.
        /// </summary>
        /// <param name="emailAddress">The email address.</param>
        /// <returns></returns>
        public bool ValidateEmailAddress(string emailAddress)
        {
            // this is not a required field validator
            if (string.IsNullOrEmpty(emailAddress)) { return true; }

            // Do the regex validation
            if (!Regex.IsMatch(emailAddress, @"^[a-zA-Z0-9'_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$"))
            {
                return false;
            }

            // But that doesn't catch everything so try the Microsoft one
            try
            {
#pragma warning disable CA1806 // Do not ignore method results
                new MailAddress(emailAddress);
#pragma warning restore CA1806 // Do not ignore method results
            }
            catch (FormatException)
            {
                // If it can't create a MailAddress object, it's an invalid address
                return false;
            }

            // Even that doesn't catch everything and some fail on SmtpClient.Send(), 
            // so check for other common patterns
            if (emailAddress.Contains(".@"))
            {
                return false;
            }

            if (emailAddress.EndsWith(".", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Some characters and combinations are allowed in the local part of the address but not the domain
            if (!IsValidDomain(emailAddress))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks whether the domain part of an email address is valid
        /// </summary>
        /// <param name="emailAddress"></param>
        /// <returns></returns>
        private static bool IsValidDomain(string emailAddress)
        {
            var domain = emailAddress.Substring(emailAddress.IndexOf("@", StringComparison.Ordinal) + 1);
            if (domain.Contains(".."))
            {
                return false;
            }

            if (domain.StartsWith(".", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (domain.Contains("'"))
            {
                return false;
            }
            return true;
        }

    }
}
# Validation messages

Form validation errors come from several places. This page explains where each kind of message is generated, and how to make sure it reads correctly.

All messages should use sentence case, for example "The match date field is required." and not "The Match date field is required."

## Where messages come from

### Data annotations

Properties on models and form data classes are validated using attributes from `System.ComponentModel.DataAnnotations`, such as `[Required]`, `[MaxLength]` and `[Range]`. The message is created by the attribute, and is used in two places:

- on the server, in `ModelState`, when a form is posted
- in the browser, where it is written into `data-val-*` attributes on the input (for example `data-val-required`) and shown by jQuery validation (`jquery.validate` and `jquery.validate.unobtrusive`, loaded by `_JQueryValidate.cshtml`)

Both come from the same attribute, so they always match.

If an attribute does not specify an `ErrorMessage` it uses a default message which includes the name of the property. That name is the value of `[Display(Name = "...")]` or `[DisplayName("...")]` if there is one, otherwise the C# property name. For example `[Display(Name = "Match date")] [Required]` gives "The Match date field is required." by default.

`[Display(Name = "...")]` is also used for the label of the field, so it is written for the start of a sentence, with a capital letter.

### Model binding

Before any attribute is checked, MVC tries to convert the posted text to the type of the property. If that fails, MVC creates its own message, such as "The field points for home team must be a number." for numeric properties and "The value 'x' is not valid for match date." for others, such as dates.

### Custom validation code

Controllers can add their own errors with `ModelState.AddModelError`, usually when a rule depends on more than one property or on the database, for example in `CreateSeasonSurfaceController`. Custom attributes, such as `MemberNameAttribute`, set their own `ErrorMessage`. `EditSeasonTeamsSurfaceController` replaces the model binding message for dates with "Enter a date in YYYY-MM-DD format."

### Views

A view can replace the message for a field, either with the second parameter of `Html.ValidationMessageFor(m => m.Property, "message")` or by setting `data_val_*` attributes on the input. This replaces the message from the model, so check the view when a message doesn't appear as expected. Examples are `CreateMember.cshtml` and `EditSeasonResultsTable.cshtml`.

## Sentence case

The default messages put the name of the property in the middle of a sentence, where it should not start with a capital letter. `Stoolball.Web/Validation/SentenceCaseValidationMetadataProvider.cs` fixes this for data annotations, and `MvcOptionsExtensions.UseSentenceCaseValidationMessages()` registers it, and a version of the two model binding messages, in `Startup.cs`.

For each property, the provider looks at its validation attributes. If an attribute has no `ErrorMessage` of its own, the provider gives it one, using the lower-cased display name:

| Attribute | Message |
| --- | --- |
| `[Required]` | The match date field is required. |
| `[MaxLength(100)]`, `[StringLength(100)]` | The street address must be 100 characters or fewer. |
| `[StringLength(10, MinimumLength = 4)]` | The code must be between 4 and 10 characters. |
| `[Range(1, 12)]` | The field balls bowled must be between 1 and 12. |
| `[RegularExpression]` | The field name must match the regular expression 'pattern'. Always write an `ErrorMessage` instead. |
| `[EmailAddress]` | Enter a valid email address. |

Attributes that are not in this table, such as `[Compare]`, are not changed, so give them an `ErrorMessage`.

The first letter of the display name is lower-cased unless:

- it begins with a proper noun, from the list in `SentenceCase.cs`, which currently has Facebook, Instagram, Stoolball England and Twitter. "Stoolball" on its own is not a proper noun.
- the first word has other capital letters, as in "YouTube" or "URL"

If you add a display name which starts with a proper noun that is not on the list, add it to the list.

## Writing your own messages

Write an `ErrorMessage` when the default would not read well, or when it needs to say more than the name of the field. The provider leaves it alone. Write it as a complete sentence in sentence case, beginning with the name of the field or a verb:

```csharp
[Range(0, 1000000, ErrorMessage = "Runs must be a number, 0 or more")]
[Required(ErrorMessage = "Select who can play.")]
```

Always write an `ErrorMessage` for properties where the display name is a question, such as "Who can play?". The question is a good label but cannot be put in the middle of a sentence.

Any text you write must not rely on `{0}` for the display name, because it would not be lower-cased. Write the name of the field in the message.

## Testing

- `Stoolball.Web.UnitTests/Validation` tests the provider and the model binding messages.
- The Playwright tests can check the `data-val-*` attributes on a form that is available without signing in. See `Playwright/tests/ResetPasswordRequest.spec.ts`.

//------------------------------------------------------------------------------
// <auto-generated>
//   This code was generated by a tool.
//
//    Umbraco.ModelsBuilder.Embedded v9.3.0+eea02137ae0b709861b45ded11882279a990c421
//
//   Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Linq.Expressions;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Infrastructure.ModelsBuilder;
using Umbraco.Cms.Core;
using Umbraco.Extensions;

namespace Stoolball.Web.Models
{
	/// <summary>Personal Details</summary>
	[PublishedModel("personalDetails")]
	public partial class PersonalDetails : PublishedContentModel, IDesign, IMetadata
	{
		// helpers
#pragma warning disable 0109 // new is redundant
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "9.3.0+eea02137ae0b709861b45ded11882279a990c421")]
		public new const string ModelTypeAlias = "personalDetails";
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "9.3.0+eea02137ae0b709861b45ded11882279a990c421")]
		public new const PublishedItemType ModelItemType = PublishedItemType.Content;
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "9.3.0+eea02137ae0b709861b45ded11882279a990c421")]
		[return: global::System.Diagnostics.CodeAnalysis.MaybeNull]
		public new static IPublishedContentType GetModelContentType(IPublishedSnapshotAccessor publishedSnapshotAccessor)
			=> PublishedModelUtility.GetModelContentType(publishedSnapshotAccessor, ModelItemType, ModelTypeAlias);
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "9.3.0+eea02137ae0b709861b45ded11882279a990c421")]
		[return: global::System.Diagnostics.CodeAnalysis.MaybeNull]
		public static IPublishedPropertyType GetModelPropertyType<TValue>(IPublishedSnapshotAccessor publishedSnapshotAccessor, Expression<Func<PersonalDetails, TValue>> selector)
			=> PublishedModelUtility.GetModelPropertyType(GetModelContentType(publishedSnapshotAccessor), selector);
#pragma warning restore 0109

		private IPublishedValueFallback _publishedValueFallback;

		// ctor
		public PersonalDetails(IPublishedContent content, IPublishedValueFallback publishedValueFallback)
			: base(content, publishedValueFallback)
		{
			_publishedValueFallback = publishedValueFallback;
		}

		// properties

		///<summary>
		/// 'Save' button: Label for the form submit button.
		///</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "9.3.0+eea02137ae0b709861b45ded11882279a990c421")]
		[global::System.Diagnostics.CodeAnalysis.MaybeNull]
		[ImplementPropertyType("saveButton")]
		public virtual string SaveButton => this.Value<string>(_publishedValueFallback, "saveButton");

		///<summary>
		/// Saved: Displays when an update is saved successfully.
		///</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "9.3.0+eea02137ae0b709861b45ded11882279a990c421")]
		[global::System.Diagnostics.CodeAnalysis.MaybeNull]
		[ImplementPropertyType("saved")]
		public virtual global::Umbraco.Cms.Core.Strings.IHtmlEncodedString Saved => this.Value<global::Umbraco.Cms.Core.Strings.IHtmlEncodedString>(_publishedValueFallback, "saved");

		///<summary>
		/// Header photo: The photo which appears across the site header. Leave blank to use the one from the parent page.
		///</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "9.3.0+eea02137ae0b709861b45ded11882279a990c421")]
		[global::System.Diagnostics.CodeAnalysis.MaybeNull]
		[ImplementPropertyType("headerPhoto")]
		public virtual global::Umbraco.Cms.Core.Models.PublishedContent.IPublishedContent HeaderPhoto => global::Stoolball.Web.Models.Design.GetHeaderPhoto(this, _publishedValueFallback);

		///<summary>
		/// Custom stylesheet
		///</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "9.3.0+eea02137ae0b709861b45ded11882279a990c421")]
		[global::System.Diagnostics.CodeAnalysis.MaybeNull]
		[ImplementPropertyType("stylesheet")]
		public virtual string Stylesheet => global::Stoolball.Web.Models.Design.GetStylesheet(this, _publishedValueFallback);

		///<summary>
		/// Description: A short summary of this page that may appear in search results
		///</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "9.3.0+eea02137ae0b709861b45ded11882279a990c421")]
		[global::System.Diagnostics.CodeAnalysis.MaybeNull]
		[ImplementPropertyType("description")]
		public virtual string Description => global::Stoolball.Web.Models.Metadata.GetDescription(this, _publishedValueFallback);

		///<summary>
		/// Don't show this in menus
		///</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "9.3.0+eea02137ae0b709861b45ded11882279a990c421")]
		[ImplementPropertyType("umbracoNaviHide")]
		public virtual bool UmbracoNaviHide => global::Stoolball.Web.Models.Metadata.GetUmbracoNaviHide(this, _publishedValueFallback);

		///<summary>
		/// URL segment: Sets the URL for this page. Defaults to the page title.
		///</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "9.3.0+eea02137ae0b709861b45ded11882279a990c421")]
		[global::System.Diagnostics.CodeAnalysis.MaybeNull]
		[ImplementPropertyType("umbracoUrlName")]
		public virtual string UmbracoUrlName => global::Stoolball.Web.Models.Metadata.GetUmbracoUrlName(this, _publishedValueFallback);
	}
}
//------------------------------------------------------------------------------
// <auto-generated>
//   This code was generated by a tool.
//
//    Umbraco.ModelsBuilder.Embedded v8.7.0
//
//   Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Web;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.ModelsBuilder.Embedded;

namespace Umbraco.Web.PublishedModels
{
	/// <summary>Form</summary>
	[PublishedModel("form")]
	public partial class Form : PublishedContentModel, IDesign, IMetadata
	{
		// helpers
#pragma warning disable 0109 // new is redundant
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "8.7.0")]
		public new const string ModelTypeAlias = "form";
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "8.7.0")]
		public new const PublishedItemType ModelItemType = PublishedItemType.Content;
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "8.7.0")]
		public new static IPublishedContentType GetModelContentType()
			=> PublishedModelUtility.GetModelContentType(ModelItemType, ModelTypeAlias);
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "8.7.0")]
		public static IPublishedPropertyType GetModelPropertyType<TValue>(Expression<Func<Form, TValue>> selector)
			=> PublishedModelUtility.GetModelPropertyType(GetModelContentType(), selector);
#pragma warning restore 0109

		// ctor
		public Form(IPublishedContent content)
			: base(content)
		{ }

		// properties

		///<summary>
		/// Form
		///</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "8.7.0")]
		[ImplementPropertyType("formPicker")]
		public object FormPicker => this.Value("formPicker");

		///<summary>
		/// Introduction
		///</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "8.7.0")]
		[ImplementPropertyType("introduction")]
		public global::System.Web.IHtmlString Introduction => this.Value<global::System.Web.IHtmlString>("introduction");

		///<summary>
		/// Header photo: The photo which appears across the site header.
		///</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "8.7.0")]
		[ImplementPropertyType("headerPhoto")]
		public global::Umbraco.Core.Models.PublishedContent.IPublishedContent HeaderPhoto => global::Umbraco.Web.PublishedModels.Design.GetHeaderPhoto(this);

		///<summary>
		/// URL segment: Sets the URL for this page. Defaults to the page title.
		///</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "8.7.0")]
		[ImplementPropertyType("umbracoUrlName")]
		public string UmbracoUrlName => global::Umbraco.Web.PublishedModels.Metadata.GetUmbracoUrlName(this);
	}
}
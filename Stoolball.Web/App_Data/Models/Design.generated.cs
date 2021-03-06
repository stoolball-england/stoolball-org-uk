//------------------------------------------------------------------------------
// <auto-generated>
//   This code was generated by a tool.
//
//    Umbraco.ModelsBuilder.Embedded v8.11.3
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
	// Mixin Content Type with alias "design"
	/// <summary>_Design</summary>
	public partial interface IDesign : IPublishedContent
	{
		/// <summary>Header photo</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "8.11.3")]
		global::Umbraco.Core.Models.PublishedContent.IPublishedContent HeaderPhoto { get; }

		/// <summary>Custom stylesheet</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "8.11.3")]
		string Stylesheet { get; }
	}

	/// <summary>_Design</summary>
	[PublishedModel("design")]
	public partial class Design : PublishedContentModel, IDesign
	{
		// helpers
#pragma warning disable 0109 // new is redundant
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "8.11.3")]
		public new const string ModelTypeAlias = "design";
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "8.11.3")]
		public new const PublishedItemType ModelItemType = PublishedItemType.Content;
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "8.11.3")]
		public new static IPublishedContentType GetModelContentType()
			=> PublishedModelUtility.GetModelContentType(ModelItemType, ModelTypeAlias);
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "8.11.3")]
		public static IPublishedPropertyType GetModelPropertyType<TValue>(Expression<Func<Design, TValue>> selector)
			=> PublishedModelUtility.GetModelPropertyType(GetModelContentType(), selector);
#pragma warning restore 0109

		// ctor
		public Design(IPublishedContent content)
			: base(content)
		{ }

		// properties

		///<summary>
		/// Header photo: The photo which appears across the site header. Leave blank to use the one from the parent page.
		///</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "8.11.3")]
		[ImplementPropertyType("headerPhoto")]
		public global::Umbraco.Core.Models.PublishedContent.IPublishedContent HeaderPhoto => GetHeaderPhoto(this);

		/// <summary>Static getter for Header photo</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "8.11.3")]
		public static global::Umbraco.Core.Models.PublishedContent.IPublishedContent GetHeaderPhoto(IDesign that) => that.Value<global::Umbraco.Core.Models.PublishedContent.IPublishedContent>("headerPhoto");

		///<summary>
		/// Custom stylesheet
		///</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "8.11.3")]
		[ImplementPropertyType("stylesheet")]
		public string Stylesheet => GetStylesheet(this);

		/// <summary>Static getter for Custom stylesheet</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "8.11.3")]
		public static string GetStylesheet(IDesign that) => that.Value<string>("stylesheet");
	}
}

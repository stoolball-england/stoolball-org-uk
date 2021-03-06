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
	// Mixin Content Type with alias "metadata"
	/// <summary>_Metadata</summary>
	public partial interface IMetadata : IPublishedContent
	{
		/// <summary>Description</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "8.11.3")]
		string Description { get; }

		/// <summary>Don't show this in menus</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "8.11.3")]
		bool UmbracoNaviHide { get; }

		/// <summary>URL segment</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "8.11.3")]
		string UmbracoUrlName { get; }
	}

	/// <summary>_Metadata</summary>
	[PublishedModel("metadata")]
	public partial class Metadata : PublishedContentModel, IMetadata
	{
		// helpers
#pragma warning disable 0109 // new is redundant
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "8.11.3")]
		public new const string ModelTypeAlias = "metadata";
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "8.11.3")]
		public new const PublishedItemType ModelItemType = PublishedItemType.Content;
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "8.11.3")]
		public new static IPublishedContentType GetModelContentType()
			=> PublishedModelUtility.GetModelContentType(ModelItemType, ModelTypeAlias);
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "8.11.3")]
		public static IPublishedPropertyType GetModelPropertyType<TValue>(Expression<Func<Metadata, TValue>> selector)
			=> PublishedModelUtility.GetModelPropertyType(GetModelContentType(), selector);
#pragma warning restore 0109

		// ctor
		public Metadata(IPublishedContent content)
			: base(content)
		{ }

		// properties

		///<summary>
		/// Description: A short summary of this page that may appear in search results
		///</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "8.11.3")]
		[ImplementPropertyType("description")]
		public string Description => GetDescription(this);

		/// <summary>Static getter for Description</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "8.11.3")]
		public static string GetDescription(IMetadata that) => that.Value<string>("description");

		///<summary>
		/// Don't show this in menus
		///</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "8.11.3")]
		[ImplementPropertyType("umbracoNaviHide")]
		public bool UmbracoNaviHide => GetUmbracoNaviHide(this);

		/// <summary>Static getter for Don't show this in menus</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "8.11.3")]
		public static bool GetUmbracoNaviHide(IMetadata that) => that.Value<bool>("umbracoNaviHide");

		///<summary>
		/// URL segment: Sets the URL for this page. Defaults to the page title.
		///</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "8.11.3")]
		[ImplementPropertyType("umbracoUrlName")]
		public string UmbracoUrlName => GetUmbracoUrlName(this);

		/// <summary>Static getter for URL segment</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "8.11.3")]
		public static string GetUmbracoUrlName(IMetadata that) => that.Value<string>("umbracoUrlName");
	}
}

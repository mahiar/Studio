﻿
//------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by a tool.
//
// Changes to this file may cause incorrect behavior and will be lost if
// the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WebEndpoint
{
	using global::AbstractEndpoint;
	using global::Microsoft.VisualStudio.Patterning.Runtime;
	using global::System;
	using global::System.Collections.Generic;
	using global::System.ComponentModel;
	using global::System.ComponentModel.Design;
	using global::System.Drawing.Design;
	using Runtime = global::Microsoft.VisualStudio.Patterning.Runtime;

	///	<summary>
	///	Description for AbstractEndpoint.DefaultView.Components.ComponentLink
	///	</summary>
	[Description("Description for AbstractEndpoint.DefaultView.Components.ComponentLink")]
	[ToolkitInterface(ExtensionId ="83300ea2-15b7-4013-8212-ed68d99da5b2", DefinitionId = "7363fc34-259c-4064-8166-f97e4bd140fa", ProxyType = typeof(ComponentLink))]
	[System.CodeDom.Compiler.GeneratedCode("Pattern Toolkit Automation Library", "1.2.19.0")]
	public partial interface IComponentLink : IToolkitInterface
	{ 
		///	<summary>
		///	Description for AbstractEndpoint.DefaultView.Components.ComponentLink.ComponentId
		///	</summary>
		[Description("Description for AbstractEndpoint.DefaultView.Components.ComponentLink.ComponentId")]
		[DisplayName("Component Id")]
		[Category("General")]
		String ComponentId { get; set; }

		///	<summary>
		///	Description for AbstractEndpoint.DefaultView.Components.ComponentLink.ComponentName
		///	</summary>
		[Description("Description for AbstractEndpoint.DefaultView.Components.ComponentLink.ComponentName")]
		[DisplayName("Component Name")]
		[Category("General")]
		[TypeConverter(typeof(ComponentReferenceConverter))]
		[Editor(typeof(ComponentReferenceEditor), typeof(UITypeEditor))]
		String ComponentName { get; set; }

		///	<summary>
		///	Description for AbstractEndpoint.DefaultView.Components.ComponentLink.Order
		///	</summary>
		[Description("Description for AbstractEndpoint.DefaultView.Components.ComponentLink.Order")]
		[DisplayName("Order")]
		[Category("General")]
		Int64 Order { get; set; }
		
		///	<summary>
		///	Notes for this element.
		///	</summary>
		[Description("Notes for this element.")]
		[Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
		String Notes { get; set; }
		
		///	<summary>
		///	The InTransaction.
		///	</summary>
		Boolean InTransaction { get;  }
		
		///	<summary>
		///	The IsSerializing.
		///	</summary>
		Boolean IsSerializing { get;  }
		
		///	<summary>
		///	The name of this element instance.
		///	</summary>
		[ParenthesizePropertyName(true)]
		[Description("The name of this element instance.")]
		String InstanceName { get; set; }
		
		///	<summary>
		///	The order of this element relative to its siblings.
		///	</summary>
		[ReadOnly(true)]
		[Description("The order of this element relative to its siblings.")]
		Double InstanceOrder { get; set; }
		
		///	<summary>
		///	The references of this element.
		///	</summary>
		[Description("The references of this element.")]
		IEnumerable<IReference> References { get;  }

		/// <summary>
		/// Gets the parent element.
		/// </summary>
		IEndpointComponents Parent { get; }
		
		///	<summary>
		///	Deletes this element from the store.
		///	</summary>
		void Delete();

		/// <summary>
		/// Gets the generic <see cref="Runtime.IElement"/> underlying element.
		/// </summary>
		Runtime.IElement AsElement();
	}
}


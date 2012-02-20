﻿/////////////////////////////////////////////////////////////////////////////
//
// MVC3 @Html.CheckBoxList() custom extension v.1.4 (beta 2)
// by Mikhail T. (devnoob), 2011-2012
// http://www.codeproject.com/KB/user-controls/MvcCheckBoxList_Extension.aspx
//
// Since version 1.4, contains code by Josh Russo, 2012-02-10
// from his version of this control:
// https://docs.google.com/leaf?id=0BwPweKbpL7dbYWFjZGRjMjAtOWRmZi00OTVlLWExZDgtOTAwZWEyNDA5MTI0
//
// Since version 1.2, contains portions of code from article:
// 'Better ASP MVC Select HtmlHelper'
// by Sacha Barber, 2011
// http://sachabarber.net/?p=1007
//
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Web.Mvc;
using System.Dynamic;

/// <summary>
/// @Html.CheckBoxList2(...) main functions
/// </summary>
internal static class MvcCheckBoxList_Main {
	static MvcCheckBoxList_Main() {
		resetCounters();
	}
	// properties
	internal static string no_data_message = "No Records...";
	internal static string empty_name_message = "Name of the CheckBoxList cannot be null or empty";


	// Main functions

	/// <summary>
	/// Model-Independent main function
	/// </summary>
	/// <param name="htmlHelper">MVC Html helper class that is being extended</param>
	/// <param name="listName">Name of each checkbox in a list (use this name to POST list values array back to the controller)</param>
	/// <param name="dataList">List of name/value pairs to be used as source data for the list</param>
	/// <param name="htmlAttributes">Each checkbox HTML tag attributes (e.g. 'new { class="somename" }')</param>
	/// <param name="wrapInfo">Settings for HTML wrapper of the list (e.g. 'new HtmlListInfo2(HtmlTag2.vertical_columns, 2, new { style="color:green;" })')</param>
	/// <param name="disabledValues">String array of values to disable</param>
	/// <param name="position">Direction of the list (e.g. 'Position2.Horizontal' or 'Position2.Vertical')</param>
	/// <returns>HTML string containing checkbox list</returns>
	internal static MvcHtmlString CheckBoxList
		(HtmlHelper htmlHelper, string listName, List<SelectListItem> dataList,
		 object htmlAttributes, HtmlListInfo wrapInfo, string[] disabledValues,
		 Position position = Position.Horizontal) {
		// validation
		if (dataList == null || dataList.Count == 0) return MvcHtmlString.Create(no_data_message);
		if (String.IsNullOrEmpty(listName)) throw new ArgumentException(empty_name_message, "listName");
		var numberOfItems = dataList.Count;

		// set up table/list html wrapper, if applicable
		var htmlWrapper = createHtmlWrapper(wrapInfo, numberOfItems, position);

		// create checkbox list
		var sb = new StringBuilder();
		sb.Append(htmlWrapper.wrap_open);
		htmlwrap_rowbreak_counter = 0;

		// create list of selected values
		var selectedValues = dataList.Where(x => x.Selected).Select(s => s.Value);

		// create list of checkboxes based on data
		foreach (var r in dataList) {
			// create checkbox element
			sb = createCheckBoxListElement(sb, htmlHelper, null, htmlWrapper, htmlAttributes, selectedValues,
			                               disabledValues, listName, r.Value, r.Text);
		}
		sb.Append(htmlWrapper.wrap_close);

		// return checkbox list
		return MvcHtmlString.Create(sb.ToString());
	}

	/// <summary>
	/// Model-Based main function
	/// </summary>
	/// <typeparam name="TModel">Current ViewModel</typeparam>
	/// <typeparam name="TItem">ViewModel Item</typeparam>
	/// <typeparam name="TValue">ViewModel Item type of the value</typeparam>
	/// <typeparam name="TKey">ViewModel Item type of the key</typeparam>
	/// <param name="htmlHelper">MVC Html helper class that is being extended</param>
	/// <param name="modelMetadata">Model Metadata</param>
	/// <param name="listName">Name of each checkbox in a list (use this name to POST list values array back to the controller)</param>
	/// <param name="sourceDataExpr">Data list to be used as a source for the list (set in viewmodel)</param>
	/// <param name="valueExpr">Data list value type to be used as checkbox 'Value'</param>
	/// <param name="textToDisplayExpr">Data list value type to be used as checkbox 'Text'</param>
	/// <param name="htmlAttributesExpr">Data list HTML tag attributes, to allow override of htmlAttributes for each checkbox (e.g. 'item => new { data_relation_id = item.RelationID }')</param>
	/// <param name="selectedValuesExpr">Data list of selected items (should be of same data type as a source list)</param>
	/// <param name="htmlAttributes">Each checkbox HTML tag attributes (e.g. 'new { class="somename" }')</param>
	/// <param name="wrapInfo">Settings for HTML wrapper of the list (e.g. 'new HtmlListInfo2(HtmlTag2.vertical_columns, 2, new { style="color:green;" })')</param>
	/// <param name="disabledValues">String array of values to disable</param>
	/// <param name="position">Direction of the list (e.g. 'Position2.Horizontal' or 'Position2.Vertical')</param>
	/// <returns>HTML string containing checkbox list</returns>
	internal static MvcHtmlString CheckBoxList_ModelBased<TModel, TItem, TValue, TKey>
		(HtmlHelper<TModel> htmlHelper,
		 ModelMetadata modelMetadata,
		 string listName,
		 Expression<Func<TModel, IEnumerable<TItem>>> sourceDataExpr,
		 Expression<Func<TItem, TValue>> valueExpr,
		 Expression<Func<TItem, TKey>> textToDisplayExpr,
		 Expression<Func<TItem, object>> htmlAttributesExpr,
		 Expression<Func<TModel, IEnumerable<TItem>>> selectedValuesExpr,
		 object htmlAttributes,
		 HtmlListInfo wrapInfo,
		 string[] disabledValues,
		 Position position = Position.Horizontal) {
		// validation
		if (sourceDataExpr.Body.ToString() == "null") return MvcHtmlString.Create(no_data_message);
		if (string.IsNullOrEmpty(listName)) throw new ArgumentException(empty_name_message, "listName");

		// set properties
		var model = htmlHelper.ViewData.Model;
		var sourceData = sourceDataExpr.Compile()(model).ToList();
		var valueFunc = valueExpr.Compile();
		var textToDisplayFunc = textToDisplayExpr.Compile();
		var selectedItems = new List<TItem>();
		if (selectedValuesExpr != null) {
			var _selectedItems = selectedValuesExpr.Compile()(model);
			if (_selectedItems != null) selectedItems = _selectedItems.ToList();
		}
		var selectedValues = selectedItems.Select(s => valueFunc(s).ToString()).ToList();

		// validate source data
		if (!sourceData.Any()) return MvcHtmlString.Create(no_data_message);

		// set html properties for each checkbox from model object
		Func<TItem, object, object> _valueHtmlAttributesFunc = (item, baseAttributes) => baseAttributes;
		if (htmlAttributesExpr != null) {
			var valueHtmlAttributesFunc = htmlAttributesExpr.Compile();
			_valueHtmlAttributesFunc = (item, baseAttributes) => {
			                           	var baseAttrDict = baseAttributes.toDictionary();
			                           	var itemAttrDict = valueHtmlAttributesFunc(item).toDictionary();
			                           	var result = new ExpandoObject();
			                           	var d = result as IDictionary<string, object>;
			                           	foreach (var pair in baseAttrDict.Concat(itemAttrDict))
			                           		d[pair.Key] = pair.Value;
			                           	return result;
			                           };
		}

		// set up table/list html wrapper, if applicable
		var numberOfItems = sourceData.Count;
		var htmlWrapper = createHtmlWrapper(wrapInfo, numberOfItems, position);

		// create checkbox list
		var sb = new StringBuilder();
		sb.Append(htmlWrapper.wrap_open);
		htmlwrap_rowbreak_counter = 0;

		// create list of checkboxes based on data
		foreach (var item in sourceData) {
			// get checkbox value and text
			var itemValue = valueFunc(item).ToString();
			var itemText = textToDisplayFunc(item).ToString();

			// create checkbox element
			sb = createCheckBoxListElement(sb, htmlHelper, modelMetadata, htmlWrapper, _valueHtmlAttributesFunc(item, htmlAttributes),
			                               selectedValues, disabledValues, listName, itemValue, itemText);
		}
		sb.Append(htmlWrapper.wrap_close);

		// return checkbox list
		return MvcHtmlString.Create(sb.ToString());
	}

	/// <summary>
	/// Creates an HTML wrapper for the checkbox list
	/// </summary>
	/// <param name="wrapInfo">Settings for HTML wrapper of the list (e.g. 'new HtmlListInfo2(HtmlTag2.vertical_columns, 2, new { style="color:green;" })')</param>
	/// <param name="numberOfItems">Count of all items in the list</param>
	/// <param name="position">Direction of the list (e.g. 'Position2.Horizontal' or 'Position2.Vertical')</param>
	/// <returns>HTML wrapper information</returns>
	private static htmlWrapperInfo createHtmlWrapper
		(HtmlListInfo wrapInfo, int numberOfItems, Position position) {
		var w = new htmlWrapperInfo();

		if (wrapInfo != null) {
			// creating custom layouts
			switch (wrapInfo.htmlTag) {
					// creates user selected number of float sections with
					// vertically sorted checkboxes
				case HtmlTag.vertical_columns: {
					if (wrapInfo.Columns <= 0) wrapInfo.Columns = 1;
					// calculate number of rows
					var rows = Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(numberOfItems)
					                                        / Convert.ToDecimal(wrapInfo.Columns)));
					if (numberOfItems <= 4 &&
					    (numberOfItems <= wrapInfo.Columns || numberOfItems - wrapInfo.Columns == 1))
						rows = numberOfItems;
					w.separator_max_counter = rows;

					// create wrapped raw html tag
					var wrapRow = htmlElementTag.div;
					var wrapHtml_builder = new TagBuilder(wrapRow.ToString());
					var user_html_attributes = wrapInfo.htmlAttributes.toDictionary();

					// create raw style and merge it with user provided style (if applicable)
					var defaultSectionStyle = "float:left; margin-right:30px; line-height:25px;";
					object style;
					user_html_attributes.TryGetValue("style", out style);
					if (style != null) // if user style is set, use it
						wrapHtml_builder.MergeAttribute("style", defaultSectionStyle + " " + style);
					else // if not set, add only default style
						wrapHtml_builder.MergeAttribute("style", defaultSectionStyle);

					// merge it with other user provided attributes (e.g.: class)
					user_html_attributes.Remove("style");
					wrapHtml_builder.MergeAttributes(user_html_attributes);

					// build wrapped raw html tag 
					w.wrap_open = wrapHtml_builder.ToString(TagRenderMode.StartTag);
					w.wrap_rowbreak = "</" + wrapRow + "> " +
					                  wrapHtml_builder.ToString(TagRenderMode.StartTag);
					w.wrap_close = wrapHtml_builder.ToString(TagRenderMode.EndTag) +
					               " <div style=\"clear:both;\"></div>";
					w.append_to_element = "<br/>";
				}
					break;
					// creates an html <table> with checkboxes sorted horizontally
				case HtmlTag.table: {
					if (wrapInfo.Columns <= 0) wrapInfo.Columns = 1;
					w.separator_max_counter = wrapInfo.Columns;

					var wrapHtml_builder = new TagBuilder(htmlElementTag.table.ToString());
					wrapHtml_builder.MergeAttributes(wrapInfo.htmlAttributes.toDictionary());
					wrapHtml_builder.MergeAttribute("cellspacing", "0"); // for IE7 compatibility

					var wrapRow = htmlElementTag.tr;
					w.wrap_element = htmlElementTag.td;
					w.wrap_open = wrapHtml_builder.ToString(TagRenderMode.StartTag) +
					              "<" + wrapRow + ">";
					w.wrap_rowbreak = "</" + wrapRow + "><" + wrapRow + ">";
					w.wrap_close = "</" + wrapRow + ">" +
					               wrapHtml_builder.ToString(TagRenderMode.EndTag);
				}
					break;
					// creates an html unordered (bulleted) list of checkboxes in one column
				case HtmlTag.ul: {
					var wrapHtml_builder = new TagBuilder(htmlElementTag.ul.ToString());
					wrapHtml_builder.MergeAttributes(wrapInfo.htmlAttributes.toDictionary());
					wrapHtml_builder.MergeAttribute("cellspacing", "0"); // for IE7 compatibility

					w.wrap_element = htmlElementTag.li;
					w.wrap_open = wrapHtml_builder.ToString(TagRenderMode.StartTag);
					w.wrap_close = wrapHtml_builder.ToString(TagRenderMode.EndTag);
				}
					break;
			}
		}
			// default setting creates vertical or horizontal column of checkboxes
		else {
			if (position == Position.Horizontal) w.append_to_element = " &nbsp; ";
			if (position == Position.Vertical) w.append_to_element = "<br/>";
		}

		return w;
	}
	/// <summary>
	/// Counter to count when to insert HTML code that brakes checkbox list
	/// </summary>
	private static int htmlwrap_rowbreak_counter { get; set; }
	/// <summary>
	/// Counter to be used on a label linked to each checkbox in the list
	/// </summary>
	private static int linked_label_counter { get; set; }
	/// <summary>
	/// Creates an an individual checkbox
	/// </summary>
	/// <param name="sb">String builder of checkbox list</param>
	/// <param name="modelMetadata">Model Metadata</param>
	/// <param name="htmlWrapper">MVC Html helper class that is being extended</param>
	/// <param name="htmlAttributesForCheckBox">Each checkbox HTML tag attributes (e.g. 'new { class="somename" }')</param>
	/// <param name="selectedValues">List of strings of selected values</param>
	/// <param name="disabledValues">List of strings of disabled values</param>
	/// <param name="name">Name of the checkbox list (same for all checkboxes)</param>
	/// <param name="itemValue">Value of the checkbox</param>
	/// <param name="itemText">Text to be displayed next to checkbox</param>
	/// <param name="htmlHelper">HtmlHelper passed from view model</param>
	/// <returns>String builder of checkbox list</returns>
	private static StringBuilder createCheckBoxListElement
		(StringBuilder sb, HtmlHelper htmlHelper, ModelMetadata modelMetadata, htmlWrapperInfo htmlWrapper,
		 object htmlAttributesForCheckBox, IEnumerable<string> selectedValues, IEnumerable<string> disabledValues,
		 string name, string itemValue, string itemText) {
		// get full name from view model
		var fullName = htmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(name);

		// create checkbox tag
		var builder = new TagBuilder("input");
		if (selectedValues.Any(x => x == itemValue)) builder.MergeAttribute("checked", "checked");
		builder.MergeAttributes(htmlAttributesForCheckBox.toDictionary());
		builder.MergeAttribute("type", "checkbox");
		builder.MergeAttribute("value", itemValue);
		builder.MergeAttribute("name", fullName);

		// create linked label tag
		var link_name = name + linked_label_counter++;
		builder.GenerateId(link_name);
		var linked_label_builder = new TagBuilder("label");
		linked_label_builder.MergeAttribute("for", link_name.Replace(".", "_"));
		linked_label_builder.MergeAttributes(htmlAttributesForCheckBox.toDictionary());
		linked_label_builder.InnerHtml = itemText;

		// if there are any errors for a named field, we add the css attribute
		ModelState modelState;
		if (htmlHelper.ViewData.ModelState.TryGetValue(fullName, out modelState))
			if (modelState.Errors.Count > 0)
				builder.AddCssClass(HtmlHelper.ValidationInputCssClassName);
		builder.MergeAttributes(htmlHelper.GetUnobtrusiveValidationAttributes(name, modelMetadata));

		// open checkbox tag wrapper
		sb.Append(htmlWrapper.wrap_element != htmlElementTag.None ? "<" + htmlWrapper.wrap_element + ">" : "");

		// build hidden tag for disabled checkbox (so the value will post)
		if (disabledValues != null && disabledValues.ToList().Any(x => x == itemValue)) {
			// set main checkbox to be disabled
			builder.MergeAttribute("disabled", "disabled");

			// create a hidden input with checkbox value
			// so it can be posted if checked
			if (selectedValues.Any(x => x == itemValue)) {
				var hidden_builder = new TagBuilder("input");
				hidden_builder.MergeAttribute("type", "hidden");
				hidden_builder.MergeAttribute("value", itemValue);
				hidden_builder.MergeAttribute("name", name);
				sb.Append(hidden_builder.ToString(TagRenderMode.Normal));
			}
		}

		// create checkbox tag
		sb.Append(builder.ToString(TagRenderMode.Normal));
		sb.Append(linked_label_builder.ToString(TagRenderMode.Normal));

		// close checkbox tag wrapper
		sb.Append(htmlWrapper.wrap_element != htmlElementTag.None ? "</" + htmlWrapper.wrap_element + ">" : "");

		// add element ending
		sb.Append(htmlWrapper.append_to_element);

		// add table column break, if applicable
		htmlwrap_rowbreak_counter += 1;
		if (htmlwrap_rowbreak_counter == htmlWrapper.separator_max_counter) {
			sb.Append(htmlWrapper.wrap_rowbreak);
			htmlwrap_rowbreak_counter = 0;
		}

		// return string builder with checkbox html markup
		return sb;
	}


	// Additional functions

	/// <summary>
	/// Convert object to Dictionary of strings and objects
	/// </summary>
	/// <param name="_object">Object of Dictionary of strings and objects (e.g. 'new { name="value" }')</param>
	/// <returns>Dictionary of strings and objects</returns>
	private static IDictionary<string, object> toDictionary(this object _object) {
		if (_object == null) return new Dictionary<string, object>();
		if (_object is IDictionary<string, object>) return (IDictionary<string, object>) _object;
		var object_properties = TypeDescriptor.GetProperties(_object);
		var dictionary = new Dictionary<string, object>(object_properties.Count);
		foreach (PropertyDescriptor property in object_properties) {
			var name = property.Name.Replace("_", "-");
			// JRR - Added the Replace call. This is the standard used by MVC http://www.asp.net/whitepapers/mvc3-release-notes#0.1__Toc274034227
			var value = property.GetValue(_object);
			dictionary.Add(name, value ?? "");
		}
		return dictionary;
	}
	/// <summary>
	/// Reset all counters
	/// </summary>
	private static void resetCounters() {
		linked_label_counter = 0;
		htmlwrap_rowbreak_counter = 0;
	}
}

/// <summary>
/// @Html.CheckBoxList2(...) extensions/overloads
/// </summary>
public static class MvcCheckBoxList_Extensions {
	// Regular CheckBoxList extensions

	/// <summary>
	/// Model-Independent function
	/// </summary>
	/// <param name="htmlHelper">MVC Html helper class that is being extended</param>
	/// <param name="listName">Name of each checkbox in a list (use this name to POST list values array back to the controller)</param>
	/// <param name="dataList">List of name/value pairs to be used as source data for the list</param>
	/// <param name="position">Direction of the list (e.g. 'Position2.Horizontal' or 'Position2.Vertical')</param>
	/// <returns>HTML string containing checkbox list</returns>	
	public static MvcHtmlString CheckBoxList
		(this HtmlHelper htmlHelper, string listName, List<SelectListItem> dataList,
		 Position position = Position.Horizontal) {
		return htmlHelper.CheckBoxList
			(listName, dataList, null, null, null, position);
	}
	/// <summary>
	/// Model-Independent function
	/// </summary>
	/// <param name="htmlHelper">MVC Html helper class that is being extended</param>
	/// <param name="listName">Name of each checkbox in a list (use this name to POST list values array back to the controller)</param>
	/// <param name="dataList">List of name/value pairs to be used as source data for the list</param>
	/// <param name="htmlAttributes">Each checkbox HTML tag attributes (e.g. 'new { class="somename" }')</param>
	/// <param name="position">Direction of the list (e.g. 'Position2.Horizontal' or 'Position2.Vertical')</param>
	/// <returns>HTML string containing checkbox list</returns>
	public static MvcHtmlString CheckBoxList
		(this HtmlHelper htmlHelper, string listName, List<SelectListItem> dataList,
		 object htmlAttributes, Position position = Position.Horizontal) {
		return htmlHelper.CheckBoxList
			(listName, dataList, htmlAttributes, null, null, position);
	}
	/// <summary>
	/// Model-Independent function
	/// </summary>
	/// <param name="htmlHelper">MVC Html helper class that is being extended</param>
	/// <param name="listName">Name of each checkbox in a list (use this name to POST list values array back to the controller)</param>
	/// <param name="dataList">List of name/value pairs to be used as source data for the list</param>
	/// <param name="htmlAttributes">Each checkbox HTML tag attributes (e.g. 'new { class="somename" }')</param>
	/// <param name="disabledValues">String array of values to disable</param>
	/// <param name="position">Direction of the list (e.g. 'Position2.Horizontal' or 'Position2.Vertical')</param>
	/// <returns>HTML string containing checkbox list</returns>
	public static MvcHtmlString CheckBoxList
		(this HtmlHelper htmlHelper, string listName, List<SelectListItem> dataList,
		 object htmlAttributes, string[] disabledValues, Position position = Position.Horizontal) {
		return htmlHelper.CheckBoxList
			(listName, dataList, htmlAttributes, null, disabledValues, position);
	}
	/// <summary>
	/// Model-Independent function
	/// </summary>
	/// <param name="htmlHelper">MVC Html helper class that is being extended</param>
	/// <param name="listName">Name of each checkbox in a list (use this name to POST list values array back to the controller)</param>
	/// <param name="dataList">List of name/value pairs to be used as source data for the list</param>
	/// <param name="wrapInfo">Settings for HTML wrapper of the list (e.g. 'new HtmlListInfo2(HtmlTag2.vertical_columns, 2, new { style="color:green;" })')</param>
	/// <returns>HTML string containing checkbox list</returns>
	public static MvcHtmlString CheckBoxList
		(this HtmlHelper htmlHelper, string listName, List<SelectListItem> dataList,
		 HtmlListInfo wrapInfo) {
		return htmlHelper.CheckBoxList
			(listName, dataList, null, wrapInfo, null);
	}
	/// <summary>
	/// Model-Independent function
	/// </summary>
	/// <param name="htmlHelper">MVC Html helper class that is being extended</param>
	/// <param name="listName">Name of each checkbox in a list (use this name to POST list values array back to the controller)</param>
	/// <param name="dataList">List of name/value pairs to be used as source data for the list</param>
	/// <param name="wrapInfo">Settings for HTML wrapper of the list (e.g. 'new HtmlListInfo2(HtmlTag2.vertical_columns, 2, new { style="color:green;" })')</param>
	/// <param name="disabledValues">String array of values to disable</param>
	/// <returns>HTML string containing checkbox list</returns>
	public static MvcHtmlString CheckBoxList
		(this HtmlHelper htmlHelper, string listName, List<SelectListItem> dataList,
		 HtmlListInfo wrapInfo, string[] disabledValues) {
		return htmlHelper.CheckBoxList
			(listName, dataList, null, wrapInfo, disabledValues);
	}
	/// <summary>
	/// Model-Independent function
	/// </summary>
	/// <param name="htmlHelper">MVC Html helper class that is being extended</param>
	/// <param name="listName">Name of each checkbox in a list (use this name to POST list values array back to the controller)</param>
	/// <param name="dataList">List of name/value pairs to be used as source data for the list</param>
	/// <param name="htmlAttributes">Each checkbox HTML tag attributes (e.g. 'new { class="somename" }')</param>
	/// <param name="wrapInfo">Settings for HTML wrapper of the list (e.g. 'new HtmlListInfo2(HtmlTag2.vertical_columns, 2, new { style="color:green;" })')</param>
	/// <param name="disabledValues">String array of values to disable</param>
	/// <param name="position">Direction of the list (e.g. 'Position2.Horizontal' or 'Position2.Vertical')</param>
	/// <returns>HTML string containing checkbox list</returns>
	public static MvcHtmlString CheckBoxList
		(this HtmlHelper htmlHelper, string listName, List<SelectListItem> dataList,
		 object htmlAttributes, HtmlListInfo wrapInfo, string[] disabledValues,
		 Position position = Position.Horizontal) {
		return MvcCheckBoxList_Main.CheckBoxList
			(htmlHelper, listName, dataList, htmlAttributes, wrapInfo, disabledValues, position);
	}


	// Model-based CheckBoxList extensions

	/// <summary>
	/// Model-Based function (For...)
	/// </summary>
	/// <typeparam name="TModel">Current ViewModel</typeparam>
	/// <typeparam name="TItem">ViewModel Item</typeparam>
	/// <typeparam name="TValue">ViewModel Item type of the value</typeparam>
	/// <typeparam name="TKey">ViewModel Item type of the key</typeparam>
	/// <typeparam name="TProperty">ViewModel property</typeparam>
	/// <param name="htmlHelper">MVC Html helper class that is being extended</param>
	/// <param name="listNameExpr">ViewModel Item type to serve as a name of each checkbox in a list (use this name to POST list values array back to the controller)</param>
	/// <param name="sourceDataExpr">Data list to be used as a source for the list (set in viewmodel)</param>
	/// <param name="valueExpr">Data list value type to be used as checkbox 'Value'</param>
	/// <param name="textToDisplayExpr">Data list value type to be used as checkbox 'Text'</param>
	/// <param name="selectedValuesExpr">Data list of selected items (should be of same data type as a source list)</param>
	/// <param name="htmlAttributesExpr">Data list HTML tag attributes for each checkbox</param>
	/// <returns>HTML string containing checkbox list</returns>
	public static MvcHtmlString CheckBoxListFor<TModel, TProperty, TItem, TValue, TKey>
		(this HtmlHelper<TModel> htmlHelper,
		 Expression<Func<TModel, TProperty>> listNameExpr,
		 Expression<Func<TModel, IEnumerable<TItem>>> sourceDataExpr,
		 Expression<Func<TItem, TValue>> valueExpr,
		 Expression<Func<TItem, TKey>> textToDisplayExpr,
		 Expression<Func<TModel, IEnumerable<TItem>>> selectedValuesExpr,
		 Expression<Func<TItem, dynamic>> htmlAttributesExpr = null) {
		var modelMetadata = ModelMetadata.FromLambdaExpression(listNameExpr, htmlHelper.ViewData);
		return MvcCheckBoxList_Main.CheckBoxList_ModelBased
			(htmlHelper, modelMetadata, listNameExpr.toProperty(), sourceDataExpr, valueExpr,
			 textToDisplayExpr, htmlAttributesExpr, selectedValuesExpr, null, null, null);
	}
	/// <summary>
	/// Model-Based function
	/// </summary>
	/// <typeparam name="TModel">Current ViewModel</typeparam>
	/// <typeparam name="TItem">ViewModel Item</typeparam>
	/// <typeparam name="TValue">ViewModel Item type of the value</typeparam>
	/// <typeparam name="TKey">ViewModel Item type of the key</typeparam>
	/// <param name="htmlHelper">MVC Html helper class that is being extended</param>
	/// <param name="listName">Name of each checkbox in a list (use this name to POST list values array back to the controller)</param>
	/// <param name="sourceDataExpr">Data list to be used as a source for the list (set in viewmodel)</param>
	/// <param name="valueExpr">Data list value type to be used as checkbox 'Value'</param>
	/// <param name="textToDisplayExpr">Data list value type to be used as checkbox 'Text'</param>
	/// <param name="selectedValuesExpr">Data list of selected items (should be of same data type as a source list)</param>
	/// <param name="htmlAttributesExpr">Data list HTML tag attributes for each checkbox</param>
	/// <returns>HTML string containing checkbox list</returns>
	public static MvcHtmlString CheckBoxList<TModel, TItem, TValue, TKey>
		(this HtmlHelper<TModel> htmlHelper,
		 string listName,
		 Expression<Func<TModel, IEnumerable<TItem>>> sourceDataExpr,
		 Expression<Func<TItem, TValue>> valueExpr,
		 Expression<Func<TItem, TKey>> textToDisplayExpr,
		 Expression<Func<TModel, IEnumerable<TItem>>> selectedValuesExpr,
		 Expression<Func<TItem, dynamic>> htmlAttributesExpr = null) {
		return MvcCheckBoxList_Main.CheckBoxList_ModelBased
			(htmlHelper, null, listName, sourceDataExpr, valueExpr, textToDisplayExpr, htmlAttributesExpr,
			 selectedValuesExpr, null, null, null);
	}

	/// <summary>
	/// Model-Based function (For...)
	/// </summary>
	/// <typeparam name="TModel">Current ViewModel</typeparam>
	/// <typeparam name="TItem">ViewModel Item</typeparam>
	/// <typeparam name="TValue">ViewModel Item type of the value</typeparam>
	/// <typeparam name="TKey">ViewModel Item type of the key</typeparam>
	/// <typeparam name="TProperty">ViewModel property</typeparam>
	/// <param name="htmlHelper">MVC Html helper class that is being extended</param>
	/// <param name="listNameExpr">ViewModel Item type to serve as a name of each checkbox in a list (use this name to POST list values array back to the controller)</param>
	/// <param name="sourceDataExpr">Data list to be used as a source for the list (set in viewmodel)</param>
	/// <param name="valueExpr">Data list value type to be used as checkbox 'Value'</param>
	/// <param name="textToDisplayExpr">Data list value type to be used as checkbox 'Text'</param>
	/// <param name="selectedValuesExpr">Data list of selected items (should be of same data type as a source list)</param>
	/// <param name="position">Direction of the list (e.g. 'Position2.Horizontal' or 'Position2.Vertical')</param>
	/// <param name="htmlAttributesExpr">Data list HTML tag attributes for each checkbox</param>
	/// <returns>HTML string containing checkbox list</returns>
	public static MvcHtmlString CheckBoxListFor<TModel, TProperty, TItem, TValue, TKey>
		(this HtmlHelper<TModel> htmlHelper,
		 Expression<Func<TModel, TProperty>> listNameExpr,
		 Expression<Func<TModel, IEnumerable<TItem>>> sourceDataExpr,
		 Expression<Func<TItem, TValue>> valueExpr,
		 Expression<Func<TItem, TKey>> textToDisplayExpr,
		 Expression<Func<TModel, IEnumerable<TItem>>> selectedValuesExpr,
		 Position position,
		 Expression<Func<TItem, dynamic>> htmlAttributesExpr = null) {
		var modelMetadata = ModelMetadata.FromLambdaExpression(listNameExpr, htmlHelper.ViewData);
		return MvcCheckBoxList_Main.CheckBoxList_ModelBased
			(htmlHelper, modelMetadata, listNameExpr.toProperty(), sourceDataExpr, valueExpr,
			 textToDisplayExpr, htmlAttributesExpr, selectedValuesExpr, null, null, null, position);
	}
	/// <summary>
	/// Model-Based function
	/// </summary>
	/// <typeparam name="TModel">Current ViewModel</typeparam>
	/// <typeparam name="TItem">ViewModel Item</typeparam>
	/// <typeparam name="TValue">ViewModel Item type of the value</typeparam>
	/// <typeparam name="TKey">ViewModel Item type of the key</typeparam>
	/// <param name="htmlHelper">MVC Html helper class that is being extended</param>
	/// <param name="listName">Name of each checkbox in a list (use this name to POST list values array back to the controller)</param>
	/// <param name="sourceDataExpr">Data list to be used as a source for the list (set in viewmodel)</param>
	/// <param name="valueExpr">Data list value type to be used as checkbox 'Value'</param>
	/// <param name="textToDisplayExpr">Data list value type to be used as checkbox 'Text'</param>
	/// <param name="selectedValuesExpr">Data list of selected items (should be of same data type as a source list)</param>
	/// <param name="position">Direction of the list (e.g. 'Position2.Horizontal' or 'Position2.Vertical')</param>
	/// <param name="htmlAttributesExpr">Data list HTML tag attributes for each checkbox</param>
	/// <returns>HTML string containing checkbox list</returns>
	public static MvcHtmlString CheckBoxList<TModel, TItem, TValue, TKey>
		(this HtmlHelper<TModel> htmlHelper,
		 string listName,
		 Expression<Func<TModel, IEnumerable<TItem>>> sourceDataExpr,
		 Expression<Func<TItem, TValue>> valueExpr,
		 Expression<Func<TItem, TKey>> textToDisplayExpr,
		 Expression<Func<TModel, IEnumerable<TItem>>> selectedValuesExpr,
		 Position position,
		 Expression<Func<TItem, dynamic>> htmlAttributesExpr = null) {
		return MvcCheckBoxList_Main.CheckBoxList_ModelBased
			(htmlHelper, null, listName, sourceDataExpr, valueExpr, textToDisplayExpr, htmlAttributesExpr,
			 selectedValuesExpr, null, null, null, position);
	}

	/// <summary>
	/// Model-Based function (For...)
	/// </summary>
	/// <typeparam name="TModel">Current ViewModel</typeparam>
	/// <typeparam name="TItem">ViewModel Item</typeparam>
	/// <typeparam name="TValue">ViewModel Item type of the value</typeparam>
	/// <typeparam name="TKey">ViewModel Item type of the key</typeparam>
	/// <typeparam name="TProperty">ViewModel property</typeparam>
	/// <param name="htmlHelper">MVC Html helper class that is being extended</param>
	/// <param name="listNameExpr">ViewModel Item type to serve as a name of each checkbox in a list (use this name to POST list values array back to the controller)</param>
	/// <param name="sourceDataExpr">Data list to be used as a source for the list (set in viewmodel)</param>
	/// <param name="valueExpr">Data list value type to be used as checkbox 'Value'</param>
	/// <param name="textToDisplayExpr">Data list value type to be used as checkbox 'Text'</param>
	/// <param name="selectedValuesExpr">Data list of selected items (should be of same data type as a source list)</param>
	/// <param name="htmlAttributes">Each checkbox HTML tag attributes (e.g. 'new { class="somename" }')</param>
	/// <param name="htmlAttributesExpr">Data list HTML tag attributes for each checkbox</param>
	/// <returns>HTML string containing checkbox list</returns>
	public static MvcHtmlString CheckBoxListFor<TModel, TProperty, TItem, TValue, TKey>
		(this HtmlHelper<TModel> htmlHelper,
		 Expression<Func<TModel, TProperty>> listNameExpr,
		 Expression<Func<TModel, IEnumerable<TItem>>> sourceDataExpr,
		 Expression<Func<TItem, TValue>> valueExpr,
		 Expression<Func<TItem, TKey>> textToDisplayExpr,
		 Expression<Func<TModel, IEnumerable<TItem>>> selectedValuesExpr,
		 object htmlAttributes,
		 Expression<Func<TItem, dynamic>> htmlAttributesExpr = null) {
		var modelMetadata = ModelMetadata.FromLambdaExpression(listNameExpr, htmlHelper.ViewData);
		return MvcCheckBoxList_Main.CheckBoxList_ModelBased
			(htmlHelper, modelMetadata, listNameExpr.toProperty(), sourceDataExpr, valueExpr,
			 textToDisplayExpr, htmlAttributesExpr, selectedValuesExpr, htmlAttributes, null, null);
	}
	/// <summary>
	/// Model-Based function
	/// </summary>
	/// <typeparam name="TModel">Current ViewModel</typeparam>
	/// <typeparam name="TItem">ViewModel Item</typeparam>
	/// <typeparam name="TValue">ViewModel Item type of the value</typeparam>
	/// <typeparam name="TKey">ViewModel Item type of the key</typeparam>
	/// <param name="htmlHelper">MVC Html helper class that is being extended</param>
	/// <param name="listName">Name of each checkbox in a list (use this name to POST list values array back to the controller)</param>
	/// <param name="sourceDataExpr">Data list to be used as a source for the list (set in viewmodel)</param>
	/// <param name="valueExpr">Data list value type to be used as checkbox 'Value'</param>
	/// <param name="textToDisplayExpr">Data list value type to be used as checkbox 'Text'</param>
	/// <param name="selectedValuesExpr">Data list of selected items (should be of same data type as a source list)</param>
	/// <param name="htmlAttributes">Each checkbox HTML tag attributes (e.g. 'new { class="somename" }')</param>
	/// <param name="htmlAttributesExpr">Data list HTML tag attributes for each checkbox</param>
	/// <returns>HTML string containing checkbox list</returns>
	public static MvcHtmlString CheckBoxList<TModel, TItem, TValue, TKey>
		(this HtmlHelper<TModel> htmlHelper,
		 string listName,
		 Expression<Func<TModel, IEnumerable<TItem>>> sourceDataExpr,
		 Expression<Func<TItem, TValue>> valueExpr,
		 Expression<Func<TItem, TKey>> textToDisplayExpr,
		 Expression<Func<TModel, IEnumerable<TItem>>> selectedValuesExpr,
		 object htmlAttributes,
		 Expression<Func<TItem, dynamic>> htmlAttributesExpr = null) {
		return MvcCheckBoxList_Main.CheckBoxList_ModelBased
			(htmlHelper, null, listName, sourceDataExpr, valueExpr, textToDisplayExpr, htmlAttributesExpr,
			 selectedValuesExpr, htmlAttributes, null, null);
	}

	/// <summary>
	/// Model-Based function (For...)
	/// </summary>
	/// <typeparam name="TModel">Current ViewModel</typeparam>
	/// <typeparam name="TItem">ViewModel Item</typeparam>
	/// <typeparam name="TValue">ViewModel Item type of the value</typeparam>
	/// <typeparam name="TKey">ViewModel Item type of the key</typeparam>
	/// <typeparam name="TProperty">ViewModel property</typeparam>
	/// <param name="htmlHelper">MVC Html helper class that is being extended</param>
	/// <param name="listNameExpr">ViewModel Item type to serve as a name of each checkbox in a list (use this name to POST list values array back to the controller)</param>
	/// <param name="sourceDataExpr">Data list to be used as a source for the list (set in viewmodel)</param>
	/// <param name="valueExpr">Data list value type to be used as checkbox 'Value'</param>
	/// <param name="textToDisplayExpr">Data list value type to be used as checkbox 'Text'</param>
	/// <param name="selectedValuesExpr">Data list of selected items (should be of same data type as a source list)</param>
	/// <param name="htmlAttributes">Each checkbox HTML tag attributes (e.g. 'new { class="somename" }')</param>
	/// <param name="position">Direction of the list (e.g. 'Position2.Horizontal' or 'Position2.Vertical')</param>
	/// <param name="htmlAttributesExpr">Data list HTML tag attributes, to allow override of htmlAttributes for each checkbox</param>
	/// <returns>HTML string containing checkbox list</returns>
	public static MvcHtmlString CheckBoxListFor<TModel, TProperty, TItem, TValue, TKey>
		(this HtmlHelper<TModel> htmlHelper,
		 Expression<Func<TModel, TProperty>> listNameExpr,
		 Expression<Func<TModel, IEnumerable<TItem>>> sourceDataExpr,
		 Expression<Func<TItem, TValue>> valueExpr,
		 Expression<Func<TItem, TKey>> textToDisplayExpr,
		 Expression<Func<TModel, IEnumerable<TItem>>> selectedValuesExpr,
		 object htmlAttributes,
		 Position position,
		 Expression<Func<TItem, dynamic>> htmlAttributesExpr = null) {
		var modelMetadata = ModelMetadata.FromLambdaExpression(listNameExpr, htmlHelper.ViewData);
		return MvcCheckBoxList_Main.CheckBoxList_ModelBased
			(htmlHelper, modelMetadata, listNameExpr.toProperty(), sourceDataExpr, valueExpr, textToDisplayExpr,
			 htmlAttributesExpr, selectedValuesExpr, htmlAttributes, null, null, position);
	}
	/// <summary>
	/// Model-Based function
	/// </summary>
	/// <typeparam name="TModel">Current ViewModel</typeparam>
	/// <typeparam name="TItem">ViewModel Item</typeparam>
	/// <typeparam name="TValue">ViewModel Item type of the value</typeparam>
	/// <typeparam name="TKey">ViewModel Item type of the key</typeparam>
	/// <param name="htmlHelper">MVC Html helper class that is being extended</param>
	/// <param name="listName">Name of each checkbox in a list (use this name to POST list values array back to the controller)</param>
	/// <param name="sourceDataExpr">Data list to be used as a source for the list (set in viewmodel)</param>
	/// <param name="valueExpr">Data list value type to be used as checkbox 'Value'</param>
	/// <param name="textToDisplayExpr">Data list value type to be used as checkbox 'Text'</param>
	/// <param name="selectedValuesExpr">Data list of selected items (should be of same data type as a source list)</param>
	/// <param name="htmlAttributes">Each checkbox HTML tag attributes (e.g. 'new { class="somename" }')</param>
	/// <param name="position">Direction of the list (e.g. 'Position2.Horizontal' or 'Position2.Vertical')</param>
	/// <param name="htmlAttributesExpr">Data list HTML tag attributes, to allow override of htmlAttributes for each checkbox</param>
	/// <returns>HTML string containing checkbox list</returns>
	public static MvcHtmlString CheckBoxList<TModel, TItem, TValue, TKey>
		(this HtmlHelper<TModel> htmlHelper,
		 string listName,
		 Expression<Func<TModel, IEnumerable<TItem>>> sourceDataExpr,
		 Expression<Func<TItem, TValue>> valueExpr,
		 Expression<Func<TItem, TKey>> textToDisplayExpr,
		 Expression<Func<TModel, IEnumerable<TItem>>> selectedValuesExpr,
		 object htmlAttributes,
		 Position position,
		 Expression<Func<TItem, dynamic>> htmlAttributesExpr = null) {
		return MvcCheckBoxList_Main.CheckBoxList_ModelBased
			(htmlHelper, null, listName, sourceDataExpr, valueExpr, textToDisplayExpr, htmlAttributesExpr,
			 selectedValuesExpr, htmlAttributes, null, null, position);
	}

	/// <summary>
	/// Model-Based function (For...)
	/// </summary>
	/// <typeparam name="TModel">Current ViewModel</typeparam>
	/// <typeparam name="TItem">ViewModel Item</typeparam>
	/// <typeparam name="TValue">ViewModel Item type of the value</typeparam>
	/// <typeparam name="TKey">ViewModel Item type of the key</typeparam>
	/// <typeparam name="TProperty">ViewModel property</typeparam>
	/// <param name="htmlHelper">MVC Html helper class that is being extended</param>
	/// <param name="listNameExpr">ViewModel Item type to serve as a name of each checkbox in a list (use this name to POST list values array back to the controller)</param>
	/// <param name="sourceDataExpr">Data list to be used as a source for the list (set in viewmodel)</param>
	/// <param name="valueExpr">Data list value type to be used as checkbox 'Value'</param>
	/// <param name="textToDisplayExpr">Data list value type to be used as checkbox 'Text'</param>
	/// <param name="selectedValuesExpr">Data list of selected items (should be of same data type as a source list)</param>
	/// <param name="htmlAttributes">Each checkbox HTML tag attributes (e.g. 'new { class="somename" }')</param>
	/// <param name="disabledValues">String array of values to disable</param>
	/// <param name="position">Direction of the list (e.g. 'Position2.Horizontal' or 'Position2.Vertical')</param>
	/// <param name="htmlAttributesExpr">Data list HTML tag attributes, to allow override of htmlAttributes for each checkbox</param>
	/// <returns>HTML string containing checkbox list</returns>
	public static MvcHtmlString CheckBoxListFor<TModel, TProperty, TItem, TValue, TKey>
		(this HtmlHelper<TModel> htmlHelper,
		 Expression<Func<TModel, TProperty>> listNameExpr,
		 Expression<Func<TModel, IEnumerable<TItem>>> sourceDataExpr,
		 Expression<Func<TItem, TValue>> valueExpr,
		 Expression<Func<TItem, TKey>> textToDisplayExpr,
		 Expression<Func<TModel, IEnumerable<TItem>>> selectedValuesExpr,
		 object htmlAttributes,
		 string[] disabledValues,
		 Position position,
		 Expression<Func<TItem, dynamic>> htmlAttributesExpr = null) {
		var modelMetadata = ModelMetadata.FromLambdaExpression(listNameExpr, htmlHelper.ViewData);
		return MvcCheckBoxList_Main.CheckBoxList_ModelBased
			(htmlHelper, modelMetadata, listNameExpr.toProperty(), sourceDataExpr, valueExpr, textToDisplayExpr,
			 htmlAttributesExpr, selectedValuesExpr, htmlAttributes, null, disabledValues, position);
	}
	/// <summary>
	/// Model-Based function
	/// </summary>
	/// <typeparam name="TModel">Current ViewModel</typeparam>
	/// <typeparam name="TItem">ViewModel Item</typeparam>
	/// <typeparam name="TValue">ViewModel Item type of the value</typeparam>
	/// <typeparam name="TKey">ViewModel Item type of the key</typeparam>
	/// <param name="htmlHelper">MVC Html helper class that is being extended</param>
	/// <param name="listName">Name of each checkbox in a list (use this name to POST list values array back to the controller)</param>
	/// <param name="sourceDataExpr">Data list to be used as a source for the list (set in viewmodel)</param>
	/// <param name="valueExpr">Data list value type to be used as checkbox 'Value'</param>
	/// <param name="textToDisplayExpr">Data list value type to be used as checkbox 'Text'</param>
	/// <param name="selectedValuesExpr">Data list of selected items (should be of same data type as a source list)</param>
	/// <param name="htmlAttributes">Each checkbox HTML tag attributes (e.g. 'new { class="somename" }')</param>
	/// <param name="disabledValues">String array of values to disable</param>
	/// <param name="position">Direction of the list (e.g. 'Position2.Horizontal' or 'Position2.Vertical')</param>
	/// <param name="htmlAttributesExpr">Data list HTML tag attributes, to allow override of htmlAttributes for each checkbox</param>
	/// <returns>HTML string containing checkbox list</returns>
	public static MvcHtmlString CheckBoxList<TModel, TItem, TValue, TKey>
		(this HtmlHelper<TModel> htmlHelper,
		 string listName,
		 Expression<Func<TModel, IEnumerable<TItem>>> sourceDataExpr,
		 Expression<Func<TItem, TValue>> valueExpr,
		 Expression<Func<TItem, TKey>> textToDisplayExpr,
		 Expression<Func<TModel, IEnumerable<TItem>>> selectedValuesExpr,
		 object htmlAttributes,
		 string[] disabledValues,
		 Position position,
		 Expression<Func<TItem, dynamic>> htmlAttributesExpr = null) {
		return MvcCheckBoxList_Main.CheckBoxList_ModelBased
			(htmlHelper, null, listName, sourceDataExpr, valueExpr, textToDisplayExpr, htmlAttributesExpr,
			 selectedValuesExpr, htmlAttributes, null, disabledValues, position);
	}

	/// <summary>
	/// Model-Based function (For...)
	/// </summary>
	/// <typeparam name="TModel">Current ViewModel</typeparam>
	/// <typeparam name="TItem">ViewModel Item</typeparam>
	/// <typeparam name="TValue">ViewModel Item type of the value</typeparam>
	/// <typeparam name="TKey">ViewModel Item type of the key</typeparam>
	/// <typeparam name="TProperty">ViewModel property</typeparam>
	/// <param name="htmlHelper">MVC Html helper class that is being extended</param>
	/// <param name="listNameExpr">ViewModel Item type to serve as a name of each checkbox in a list (use this name to POST list values array back to the controller)</param>
	/// <param name="sourceDataExpr">Data list to be used as a source for the list (set in viewmodel)</param>
	/// <param name="valueExpr">Data list value type to be used as checkbox 'Value'</param>
	/// <param name="textToDisplayExpr">Data list value type to be used as checkbox 'Text'</param>
	/// <param name="selectedValuesExpr">Data list of selected items (should be of same data type as a source list)</param>
	/// <param name="wrapInfo">Settings for HTML wrapper of the list (e.g. 'new HtmlListInfo2(HtmlTag2.vertical_columns, 2, new { style="color:green;" })')</param>
	/// <param name="htmlAttributesExpr">Data list HTML tag attributes for each checkbox</param>
	/// <returns>HTML string containing checkbox list</returns>
	public static MvcHtmlString CheckBoxListFor<TModel, TProperty, TItem, TValue, TKey>
		(this HtmlHelper<TModel> htmlHelper,
		 Expression<Func<TModel, TProperty>> listNameExpr,
		 Expression<Func<TModel, IEnumerable<TItem>>> sourceDataExpr,
		 Expression<Func<TItem, TValue>> valueExpr,
		 Expression<Func<TItem, TKey>> textToDisplayExpr,
		 Expression<Func<TModel, IEnumerable<TItem>>> selectedValuesExpr,
		 HtmlListInfo wrapInfo,
		 Expression<Func<TItem, dynamic>> htmlAttributesExpr = null) {
		var modelMetadata = ModelMetadata.FromLambdaExpression(listNameExpr, htmlHelper.ViewData);
		return MvcCheckBoxList_Main.CheckBoxList_ModelBased
			(htmlHelper, modelMetadata, listNameExpr.toProperty(), sourceDataExpr, valueExpr, textToDisplayExpr,
			 htmlAttributesExpr, selectedValuesExpr, null, wrapInfo, null);
	}
	/// <summary>
	/// Model-Based function
	/// </summary>
	/// <typeparam name="TModel">Current ViewModel</typeparam>
	/// <typeparam name="TItem">ViewModel Item</typeparam>
	/// <typeparam name="TValue">ViewModel Item type of the value</typeparam>
	/// <typeparam name="TKey">ViewModel Item type of the key</typeparam>
	/// <param name="htmlHelper">MVC Html helper class that is being extended</param>
	/// <param name="listName">Name of each checkbox in a list (use this name to POST list values array back to the controller)</param>
	/// <param name="sourceDataExpr">Data list to be used as a source for the list (set in viewmodel)</param>
	/// <param name="valueExpr">Data list value type to be used as checkbox 'Value'</param>
	/// <param name="textToDisplayExpr">Data list value type to be used as checkbox 'Text'</param>
	/// <param name="selectedValuesExpr">Data list of selected items (should be of same data type as a source list)</param>
	/// <param name="wrapInfo">Settings for HTML wrapper of the list (e.g. 'new HtmlListInfo2(HtmlTag2.vertical_columns, 2, new { style="color:green;" })')</param>
	/// <param name="htmlAttributesExpr">Data list HTML tag attributes for each checkbox</param>
	/// <returns>HTML string containing checkbox list</returns>
	public static MvcHtmlString CheckBoxList<TModel, TItem, TValue, TKey>
		(this HtmlHelper<TModel> htmlHelper,
		 string listName,
		 Expression<Func<TModel, IEnumerable<TItem>>> sourceDataExpr,
		 Expression<Func<TItem, TValue>> valueExpr,
		 Expression<Func<TItem, TKey>> textToDisplayExpr,
		 Expression<Func<TModel, IEnumerable<TItem>>> selectedValuesExpr,
		 HtmlListInfo wrapInfo,
		 Expression<Func<TItem, dynamic>> htmlAttributesExpr = null) {
		return MvcCheckBoxList_Main.CheckBoxList_ModelBased
			(htmlHelper, null, listName, sourceDataExpr, valueExpr, textToDisplayExpr, htmlAttributesExpr,
			 selectedValuesExpr, null, wrapInfo, null);
	}

	/// <summary>
	/// Model-Based function (For...)
	/// </summary>
	/// <typeparam name="TModel">Current ViewModel</typeparam>
	/// <typeparam name="TItem">ViewModel Item</typeparam>
	/// <typeparam name="TValue">ViewModel Item type of the value</typeparam>
	/// <typeparam name="TKey">ViewModel Item type of the key</typeparam>
	/// <typeparam name="TProperty">ViewModel property</typeparam>
	/// <param name="htmlHelper">MVC Html helper class that is being extended</param>
	/// <param name="listNameExpr">ViewModel Item type to serve as a name of each checkbox in a list (use this name to POST list values array back to the controller)</param>
	/// <param name="sourceDataExpr">Data list to be used as a source for the list (set in viewmodel)</param>
	/// <param name="valueExpr">Data list value type to be used as checkbox 'Value'</param>
	/// <param name="textToDisplayExpr">Data list value type to be used as checkbox 'Text'</param>
	/// <param name="selectedValuesExpr">Data list of selected items (should be of same data type as a source list)</param>
	/// <param name="wrapInfo">Settings for HTML wrapper of the list (e.g. 'new HtmlListInfo2(HtmlTag2.vertical_columns, 2, new { style="color:green;" })')</param>
	/// <param name="disabledValues">String array of values to disable</param>
	/// <param name="htmlAttributesExpr">Data list HTML tag attributes for each checkbox</param>
	/// <returns>HTML string containing checkbox list</returns>
	public static MvcHtmlString CheckBoxListFor<TModel, TProperty, TItem, TValue, TKey>
		(this HtmlHelper<TModel> htmlHelper,
		 Expression<Func<TModel, TProperty>> listNameExpr,
		 Expression<Func<TModel, IEnumerable<TItem>>> sourceDataExpr,
		 Expression<Func<TItem, TValue>> valueExpr,
		 Expression<Func<TItem, TKey>> textToDisplayExpr,
		 Expression<Func<TModel, IEnumerable<TItem>>> selectedValuesExpr,
		 HtmlListInfo wrapInfo,
		 string[] disabledValues,
		 Expression<Func<TItem, dynamic>> htmlAttributesExpr = null) {
		var modelMetadata = ModelMetadata.FromLambdaExpression(listNameExpr, htmlHelper.ViewData);
		return MvcCheckBoxList_Main.CheckBoxList_ModelBased
			(htmlHelper, modelMetadata, listNameExpr.toProperty(), sourceDataExpr, valueExpr, textToDisplayExpr,
			 htmlAttributesExpr, selectedValuesExpr, null, wrapInfo, disabledValues);
	}
	/// <summary>
	/// Model-Based function
	/// </summary>
	/// <typeparam name="TModel">Current ViewModel</typeparam>
	/// <typeparam name="TItem">ViewModel Item</typeparam>
	/// <typeparam name="TValue">ViewModel Item type of the value</typeparam>
	/// <typeparam name="TKey">ViewModel Item type of the key</typeparam>
	/// <param name="htmlHelper">MVC Html helper class that is being extended</param>
	/// <param name="listName">Name of each checkbox in a list (use this name to POST list values array back to the controller)</param>
	/// <param name="sourceDataExpr">Data list to be used as a source for the list (set in viewmodel)</param>
	/// <param name="valueExpr">Data list value type to be used as checkbox 'Value'</param>
	/// <param name="textToDisplayExpr">Data list value type to be used as checkbox 'Text'</param>
	/// <param name="selectedValuesExpr">Data list of selected items (should be of same data type as a source list)</param>
	/// <param name="wrapInfo">Settings for HTML wrapper of the list (e.g. 'new HtmlListInfo2(HtmlTag2.vertical_columns, 2, new { style="color:green;" })')</param>
	/// <param name="disabledValues">String array of values to disable</param>
	/// <param name="htmlAttributesExpr">Data list HTML tag attributes for each checkbox</param>
	/// <returns>HTML string containing checkbox list</returns>
	public static MvcHtmlString CheckBoxList<TModel, TItem, TValue, TKey>
		(this HtmlHelper<TModel> htmlHelper,
		 string listName,
		 Expression<Func<TModel, IEnumerable<TItem>>> sourceDataExpr,
		 Expression<Func<TItem, TValue>> valueExpr,
		 Expression<Func<TItem, TKey>> textToDisplayExpr,
		 Expression<Func<TModel, IEnumerable<TItem>>> selectedValuesExpr,
		 HtmlListInfo wrapInfo,
		 string[] disabledValues,
		 Expression<Func<TItem, dynamic>> htmlAttributesExpr = null) {
		return MvcCheckBoxList_Main.CheckBoxList_ModelBased
			(htmlHelper, null, listName, sourceDataExpr, valueExpr, textToDisplayExpr, htmlAttributesExpr,
			 selectedValuesExpr, null, wrapInfo, disabledValues);
	}

	/// <summary>
	/// Model-Based function (For...)
	/// </summary>
	/// <typeparam name="TModel">Current ViewModel</typeparam>
	/// <typeparam name="TItem">ViewModel Item</typeparam>
	/// <typeparam name="TValue">ViewModel Item type of the value</typeparam>
	/// <typeparam name="TKey">ViewModel Item type of the key</typeparam>
	/// <typeparam name="TProperty">ViewModel property</typeparam>
	/// <param name="htmlHelper">MVC Html helper class that is being extended</param>
	/// <param name="listNameExpr">ViewModel Item type to serve as a name of each checkbox in a list (use this name to POST list values array back to the controller)</param>
	/// <param name="sourceDataExpr">Data list to be used as a source for the list (set in viewmodel)</param>
	/// <param name="valueExpr">Data list value type to be used as checkbox 'Value'</param>
	/// <param name="textToDisplayExpr">Data list value type to be used as checkbox 'Text'</param>
	/// <param name="selectedValuesExpr">Data list of selected items (should be of same data type as a source list)</param>
	/// <param name="htmlAttributes">Each checkbox HTML tag attributes (e.g. 'new { class="somename" }')</param>
	/// <param name="wrapInfo">Settings for HTML wrapper of the list (e.g. 'new HtmlListInfo2(HtmlTag2.vertical_columns, 2, new { style="color:green;" })')</param>
	/// <param name="disabledValues">String array of values to disable</param>
	/// <param name="htmlAttributesExpr">Data list HTML tag attributes, to allow override of htmlAttributes for each checkbox</param>
	/// <returns>HTML string containing checkbox list</returns>
	public static MvcHtmlString CheckBoxListFor<TModel, TProperty, TItem, TValue, TKey>
		(this HtmlHelper<TModel> htmlHelper,
		 Expression<Func<TModel, TProperty>> listNameExpr,
		 Expression<Func<TModel, IEnumerable<TItem>>> sourceDataExpr,
		 Expression<Func<TItem, TValue>> valueExpr,
		 Expression<Func<TItem, TKey>> textToDisplayExpr,
		 Expression<Func<TModel, IEnumerable<TItem>>> selectedValuesExpr,
		 object htmlAttributes,
		 HtmlListInfo wrapInfo,
		 string[] disabledValues,
		 Expression<Func<TItem, dynamic>> htmlAttributesExpr = null) {
		var modelMetadata = ModelMetadata.FromLambdaExpression(listNameExpr, htmlHelper.ViewData);
		return MvcCheckBoxList_Main.CheckBoxList_ModelBased
			(htmlHelper, modelMetadata, listNameExpr.toProperty(), sourceDataExpr, valueExpr, textToDisplayExpr,
			 htmlAttributesExpr, selectedValuesExpr, htmlAttributes, wrapInfo, disabledValues);
	}
	/// <summary>
	/// Model-Based function
	/// </summary>
	/// <typeparam name="TModel">Current ViewModel</typeparam>
	/// <typeparam name="TItem">ViewModel Item</typeparam>
	/// <typeparam name="TValue">ViewModel Item type of the value</typeparam>
	/// <typeparam name="TKey">ViewModel Item type of the key</typeparam>
	/// <param name="htmlHelper">MVC Html helper class that is being extended</param>
	/// <param name="listName">Name of each checkbox in a list (use this name to POST list values array back to the controller)</param>
	/// <param name="sourceDataExpr">Data list to be used as a source for the list (set in viewmodel)</param>
	/// <param name="valueExpr">Data list value type to be used as checkbox 'Value'</param>
	/// <param name="textToDisplayExpr">Data list value type to be used as checkbox 'Text'</param>
	/// <param name="selectedValuesExpr">Data list of selected items (should be of same data type as a source list)</param>
	/// <param name="htmlAttributes">Each checkbox HTML tag attributes (e.g. 'new { class="somename" }')</param>
	/// <param name="wrapInfo">Settings for HTML wrapper of the list (e.g. 'new HtmlListInfo2(HtmlTag2.vertical_columns, 2, new { style="color:green;" })')</param>
	/// <param name="disabledValues">String array of values to disable</param>
	/// <param name="htmlAttributesExpr">Data list HTML tag attributes, to allow override of htmlAttributes for each checkbox</param>
	/// <returns>HTML string containing checkbox list</returns>
	public static MvcHtmlString CheckBoxList<TModel, TItem, TValue, TKey>
		(this HtmlHelper<TModel> htmlHelper,
		 string listName,
		 Expression<Func<TModel, IEnumerable<TItem>>> sourceDataExpr,
		 Expression<Func<TItem, TValue>> valueExpr,
		 Expression<Func<TItem, TKey>> textToDisplayExpr,
		 Expression<Func<TModel, IEnumerable<TItem>>> selectedValuesExpr,
		 object htmlAttributes,
		 HtmlListInfo wrapInfo,
		 string[] disabledValues,
		 Expression<Func<TItem, dynamic>> htmlAttributesExpr = null) {
		return MvcCheckBoxList_Main.CheckBoxList_ModelBased
			(htmlHelper, null, listName, sourceDataExpr, valueExpr, textToDisplayExpr, htmlAttributesExpr,
			 selectedValuesExpr, htmlAttributes, wrapInfo, disabledValues);
	}


	// Additional functions

	/// <summary>
	/// Convert lambda expression to property name
	/// </summary>
	/// <typeparam name="TModel">Current ViewModel</typeparam>
	/// <typeparam name="TItem">ViewModel Item</typeparam>
	/// <param name="propertyExpression">Lambda expression of property value</param>
	/// <returns>Property value string</returns>
	private static string toProperty<TModel, TItem>
		(this Expression<Func<TModel, TItem>> propertyExpression) {
		// v.1.4
		return ExpressionHelper.GetExpressionText(propertyExpression);

		// v.1.3c
		//var lambda = propertyExpression as LambdaExpression;
		//var expression = lambda.Body.ToString();
		//return expression.Substring(expression.IndexOf('.') + 1);

		// v.1.2
		//// return property name only
		//var lambda = propertyExpression as LambdaExpression;
		//MemberExpression memberExpression;
		//if (lambda.Body is UnaryExpression) {
		//  var unaryExpression = lambda.Body as UnaryExpression;
		//  memberExpression = unaryExpression.Operand as MemberExpression;
		//}
		//else
		//  memberExpression = lambda.Body as MemberExpression;
		//var propertyInfo = memberExpression.Member as PropertyInfo;
		//return propertyInfo.Name;
	}
}


// Additional enums and classes for options/settings of a checkbox list

/// <summary>
/// Sets type of HTML wrapper to use on a checkbox list
/// </summary>
public enum HtmlTag {
	ul,
	table,
	vertical_columns
}
/// <summary>
/// Sets display direction of a checkbox list
/// </summary>
public enum Position {
	Horizontal,
	Vertical
}
/// <summary>
/// Sets settings of an HTML wrapper that is used on a checkbox list
/// </summary>
public class HtmlListInfo {
	public HtmlListInfo(HtmlTag htmlTag, int columns = 0, object htmlAttributes = null) {
		this.htmlTag = htmlTag;
		Columns = columns;
		this.htmlAttributes = htmlAttributes;
	}

	public HtmlTag htmlTag { get; set; }
	public int Columns { get; set; }
	public object htmlAttributes { get; set; }
}


// Additional internal enums and classes for options/settings of a checkbox list

/// <summary>
/// Sets local type of HTML element that is used on an HTML wrapper
/// </summary>
internal enum htmlElementTag {
	None,
	tr,
	td,
	li,
	div,
	table,
	ul
}
/// <summary>
/// Sets local settings of an HTML wrapper that is used on a checkbox list
/// </summary>
internal class htmlWrapperInfo {
	public string wrap_open = String.Empty;
	public string wrap_rowbreak = String.Empty;
	public string wrap_close = String.Empty;
	public htmlElementTag wrap_element = htmlElementTag.None;
	public string append_to_element = String.Empty;
	public int separator_max_counter;
}
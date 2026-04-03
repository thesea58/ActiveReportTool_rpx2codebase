using RpxCodeGenerator.Core.Models;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RpxCodeGenerator.Core.Generators;

/// <summary>
/// Sinh code C# để khởi tạo sections và controls từ cấu trúc RPX
/// </summary>
public class CodeGenerator
{
	private readonly StringBuilder _builder = new();
	private int _indentLevel = 0;
	private const string INDENT = "    ";

	/// <summary>
	/// Tạo code C# initialization từ RpxDocument
	/// </summary>
	public string Generate(RpxDocument rpxDoc)
	{
		_builder.Clear();
		_indentLevel = 0;

		WriteLine("// Auto-generated code from RPX file: " + rpxDoc.DocumentName);
		WriteLine("// Generated at: " + DateTime.Now);
		WriteLine();
		WriteLine("namespace YourNamespace.Reports;");
		WriteLine();
		WriteLine("/// <summary>");
		WriteLine("/// Auto-generated report initializer");
		WriteLine("/// </summary>");
		WriteLine($"public partial class {SanitizeClassName(rpxDoc.DocumentName)}Initializer");
		WriteLine("{");
		_indentLevel++;

		GenerateInitializationCode(rpxDoc);

		_indentLevel--;
		WriteLine("}");

		return _builder.ToString();
	}

	/// <summary>
	/// Tạo code khởi tạo sections và controls
	/// </summary>
	private void GenerateInitializationCode(RpxDocument rpxDoc)
	{
		WriteLine("/// <summary>");
		WriteLine("/// Initialize sections and controls from report");
		WriteLine("/// </summary>");
		WriteLine("public void InitializeReportSections()");
		WriteLine("{");
		_indentLevel++;

		foreach (var section in rpxDoc.Sections)
		{
			GenerateSection(section);
			WriteLine();
		}

		_indentLevel--;
		WriteLine("}");
	}

	/// <summary>
	/// Sinh code cho một section
	/// </summary>
	private void GenerateSection(RpxSection section)
	{
		var varName = section.Name[0] + section.Name.Substring(1);
		var sectionType = GetSectionVariableType(section);

		WriteLine($"// {section.Type}: {section.Name}");
		WriteLine($"{sectionType} {varName} = this.Sections[\"{section.Name}\"] as {sectionType};");

		if (section.Controls.Count > 0)
		{
			_indentLevel++;
			foreach (var control in section.Controls)
			{
				GenerateControl(varName, control);
			}
			_indentLevel--;
		}
	}

	/// <summary>
	/// Sinh code cho một control
	/// </summary>
	private void GenerateControl(string sectionVarName, RpxControl control)
	{
		var controlVarName = control.Name[0] + control.Name.Substring(1);
		var controlClass = GetControlType(control);

		WriteLine($"{controlClass} {controlVarName} = {sectionVarName}.Controls[\"{control.Name}\"] as {controlClass};");

		if (control.Properties.Count > 0)
		{
			WriteControlProperties(controlVarName, control);
		}
	}

	/// <summary>
	/// Phát sinh code để set properties cho control nếu cần
	/// </summary>
	private void WriteControlProperties(string varName, RpxControl control)
	{
		var importantProps = new[] { "Visible", "Left", "Top", "Width", "Height" };
		var relevantProps = control.Properties
			.Where(p => importantProps.Contains(p.Key))
			.ToList();

		if (relevantProps.Count == 0)
			return;

		_indentLevel++;
		foreach (var prop in relevantProps)
		{
			var value = FormatPropertyValue(prop.Key, prop.Value);
			WriteLine($"if ({varName} != null) {varName}.{prop.Key} = {value};");
		}
		_indentLevel--;
	}

	/// <summary>
	/// Format giá trị property theo loại
	/// </summary>
	private string FormatPropertyValue(string propertyName, string value)
	{
		if (propertyName.Equals("Visible", StringComparison.OrdinalIgnoreCase))
		{
			return value == "0" ? "false" : "true";
		}

		if (int.TryParse(value, out _))
		{
			return value;
		}

		return $"\"{value}\"";
	}

	/// <summary>
	/// Xác định loại control từ Type
	/// </summary>
	private string GetControlType(RpxControl control)
	{
		return control.Type switch
		{
			"AR.Label" => "Label",
			"AR.Field" => "TextField",
			"AR.TextBox" => "TextBox",
			"AR.Line" => "Line",
			"AR.Rectangle" => "Rectangle",
			"AR.Image" => "Image",
			"AR.CheckBox" => "CheckBox",
			"AR.ComboBox" => "ComboBox",
			"AR.Subreport" => "Subreport",
			_ => "ARControl"
		};
	}

	/// <summary>
	/// Xác định kiểu section theo nhóm type ActiveReports hỗ trợ; còn lại dùng Section chung.
	/// </summary>
	private string GetSectionVariableType(RpxSection section)
	{
		return section.Type switch
		{
			"ReportHeader" => "ReportHeader",
			"PageHeader" => "PageHeader",
			"GroupHeader" => "GroupHeader",
			"Group" => "Group",
			"PageFooter" => "PageFooter",
			"ReportFooter" => "ReportFooter",
			_ => "Section"
		};
	}

	/// <summary>
	/// Sinh code để lấy tất cả controls từ một section với type casting
	/// </summary>
	public string GenerateTypedControlsExtraction(RpxDocument rpxDoc)
	{
		_builder.Clear();
		_indentLevel = 0;

		WriteLine("// Auto-generated type-safe control extraction");
		WriteLine("// Generated at: " + DateTime.Now);
		WriteLine();
		WriteLine("namespace YourNamespace.Reports;");
		WriteLine();
		WriteLine("/// <summary>");
		WriteLine("/// Auto-generated control accessor");
		WriteLine("/// </summary>");
		WriteLine($"public partial class {SanitizeClassName(rpxDoc.DocumentName)}Controls");
		WriteLine("{");
		_indentLevel++;

		foreach (var section in rpxDoc.Sections)
		{
			if (section.Controls.Count == 0)
				continue;

			var sectionVarName = section.Name[0] + section.Name.Substring(1);
			var sectionType = GetSectionVariableType(section);
			WriteLine($"/// <summary>Extract controls from {section.Name}</summary>");
			WriteLine($"public void Extract{section.Name}Controls()");
			WriteLine("{");
			_indentLevel++;

			WriteLine($"{sectionType} {sectionVarName} = this.Sections[\"{section.Name}\"] as {sectionType};");
			WriteLine();

			var textBoxControls = section.Controls.Where(c => c.Type == "AR.Field" || c.Type == "AR.TextBox");
			foreach (var control in textBoxControls)
			{
				var varName = control.Name[0] + control.Name.Substring(1);
				WriteLine($"TextBox {varName} = {sectionVarName}.Controls[\"{control.Name}\"] as TextBox;");
			}

			_indentLevel--;
			WriteLine("}");
			WriteLine();
		}

		WriteLine("}");

		return _builder.ToString();
	}

	/// <summary>
	/// Sinh code để lấy tất cả controls từ một section với type casting
	/// </summary>
	public string GenerateTypedControlsExtraction2(RpxDocument rpxDoc)
	{
		_builder.Clear();
		_indentLevel = 0;
		string namereport = rpxDoc.DocumentName;
		string sys = namereport.Length >= 8  ? namereport.Substring(0, 1) : "";
		string nsp = namereport.Length >= 8 ?
							"K" + sys + namereport.Substring(2, 6)
							: "NotFound";
		WriteLine("// Auto-generated type-safe control extraction");
		WriteLine("// Ver: 2026/03/31");
		WriteLine("// Generated at: " + DateTime.Now);
		WriteLine();
		WriteLine("using GrapeCity.ActiveReports.SectionReportModel;");
		WriteLine("using KKReport;");
		WriteLine("using Section = GrapeCity.ActiveReports.SectionReportModel.Section;");
		WriteLine("using TextAlignment = GrapeCity.ActiveReports.Document.Section.TextAlignment;");
		WriteLine("using TextBox = GrapeCity.ActiveReports.SectionReportModel.TextBox;");
		WriteLine("using Label = GrapeCity.ActiveReports.SectionReportModel.Label;");
		WriteLine();
		WriteLine($"namespace {nsp};");
		WriteLine($"/// {namereport}帳票クラス");
		WriteLine("/// レイアウトの読み込みおよびスクリプトのイベント紐付けを行う");
		WriteLine("/// </summary>");

		WriteLine($"public class {namereport}: ReportClass");
		WriteLine("{");
		_indentLevel++;

		WriteLine("#region ARコントロールを初期化する");

		foreach (var section in rpxDoc.Sections)
		{
			//if (section.Controls.Count == 0)
			//	continue;

			var sectionVarName = section.Name[0] + section.Name.Substring(1);
			var sectionType = GetSectionVariableType(section);
			_indentLevel++;
			WriteLine();

			WriteLine($"private {sectionType} {sectionVarName};");

			var textBoxControls = section.Controls.Where(c => c.Type == "AR.Field" || c.Type == "AR.TextBox");
			foreach (var control in textBoxControls)
			{
				var varName = control.Name[0] + control.Name.Substring(1);
				WriteLine($"private TextBox {varName};");
			}
			// write subreport
			var subreportControls = section.Controls.Where(c => c.Type == "AR.Subreport");
			foreach (var control in subreportControls)
			{
				WriteLine($"private SubReport {control.Name};");
			}
			_indentLevel--;
		}
		WriteLine("#endregion");
		WriteLine();
		//contructor

		Write($@"public {namereport}()
	{{
		// クラス名をレポート名として取得する
		string className = this.GetType().Name;
		this.LoadLayout(Path.Combine(K{sys}.K{sys}cls.RPTパス, $""{{className}}.rpx""));

		// イベントを紐付け (self = this)
		this.AttachEvents();
	}}

	/// <summary>
	/// IReportScript実装: イベントを設定する
	/// </summary>
	public void AttachEvents()
	{{");
		_indentLevel++;
		WriteLine();

		WriteLine("#region 各セクションおよびコントロール参照を取得");
		foreach (var section in rpxDoc.Sections)
		{
			// in ra section ke ca k hong co control trong no. 
			//if (section.Controls.Count == 0)
			//	continue;

			var sectionVarName = section.Name[0] + section.Name.Substring(1);
			var sectionType = GetSectionVariableType(section);
			WriteLine();
			WriteLine($"this.{sectionVarName} = this.Sections[\"{section.Name}\"] as {sectionType};");

			// write textbox
			var textBoxControls = section.Controls.Where(c => c.Type == "AR.Field" || c.Type == "AR.TextBox");
			foreach (var control in textBoxControls)
			{
				var varName = control.Name[0] + control.Name.Substring(1);
				WriteLine($"this.{varName} = this.{sectionVarName}.Controls[\"{control.Name}\"] as TextBox;");
			}

			// write subreport
			var subreportControls = section.Controls.Where(c => c.Type == "AR.Subreport");
			foreach (var control in subreportControls)
			{
				var varName = control.Name;
				WriteLine($"this.{varName} = this.{sectionVarName}.Controls[\"{control.Name}\"] as Subreport;");
			}
		}
		_indentLevel--;

		WriteLine("#endregion");
		WriteLine();
		WriteLine("// レポートイベントを登録（初期化・データ処理・書式設定）");
		WriteLine("}");
		WriteLine("#region RpxのScript | ActiveReportイベント定義");
		WriteLine(rpxDoc.Script);
		WriteLine("#endregion");
		_indentLevel--;
		WriteLine("}");

		return _builder.ToString();
	}

	/// <summary>
	/// Sinh code summary/overview của report
	/// </summary>
	public string GenerateReportSummary(RpxDocument rpxDoc)
	{
		_builder.Clear();
		_indentLevel = 0;

		WriteLine("// RPX Report Summary");
		WriteLine($"// Document: {rpxDoc.DocumentName}");
		WriteLine($"// Version: {rpxDoc.Version}");
		WriteLine();
		WriteLine("Report Structure:");
		WriteLine("─" + new string('─', 50));

		foreach (var section in rpxDoc.Sections)
		{
			WriteLine($"  Section: {section.Name} ({section.Type})");
			WriteLine($"    Controls: {section.Controls.Count}");

			var controlsByType = section.Controls.GroupBy(c => c.Type);
			foreach (var group in controlsByType)
			{
				WriteLine($"      - {GetControlType(group.First())}: {group.Count()}");
			}
		}

		WriteLine("─" + new string('─', 50));
		WriteLine();
		WriteLine("Control Table (copy to Excel):");
		WriteLine("No.\tFieldName\tType\tSection");

		var index = 1;
		foreach (var section in rpxDoc.Sections)
		{
			foreach (var control in section.Controls)
			{
				string controlType = (control.Type is "AR.Field" or "AR.TextBox")
					? "TextBox"
					: GetControlType(control);
				string[] lsControlAccept = { "TextBox", "Subreport" };
				if (controlType != null && lsControlAccept.Contains(controlType))
				{
					WriteLine($"{index}\t{control.Name}\t{controlType}\t{section.Name}");
					index++;
				}

			}
		}
		return _builder.ToString();
	}

	private void Write(string text)
	{
		_builder.Append(GetIndent() + text);
	}

	private void WriteLine(string text = "")
	{
		_builder.AppendLine(GetIndent() + text);
	}

	private string GetIndent()
	{
		return string.Concat(Enumerable.Repeat(INDENT, _indentLevel));
	}

	/// <summary>
	/// Sanitize tên file thành valid C# class name
	/// </summary>
	private string SanitizeClassName(string name)
	{
		var sanitized = Regex.Replace(name, @"[^a-zA-Z0-9_]", "");

		if (string.IsNullOrEmpty(sanitized) || char.IsDigit(sanitized[0]))
		{
			sanitized = "_" + sanitized;
		}

		return sanitized;
	}
}

using System.Text;
using DevExpress.Spreadsheet;

namespace TinyChat.Samples.DocumentAttachments;

/// <summary>Extracts all Excel worksheets as named CSV sections, preserving separate data regions.</summary>
public sealed class SpreadsheetAttachmentContentProvider(int maximumTextLength = 200_000, bool includeFormulas = false) : DevExpressDocumentContentProvider(maximumTextLength)
{
	/// <inheritdoc />
	public override IReadOnlyList<string> FileExtensions { get; } = [".xls", ".xlsx", ".xlsm", ".xlsb", ".xlt", ".xltx", ".xltm"];
	/// <inheritdoc />
	public override bool CanHandle(string fileName, string mediaType) => base.CanHandle(fileName, mediaType) || mediaType.Equals("application/vnd.ms-excel", StringComparison.OrdinalIgnoreCase) || mediaType.Equals("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", StringComparison.OrdinalIgnoreCase);
	/// <summary>Gets whether formula expressions accompany formatted cell values.</summary>
	public bool IncludeFormulas { get; } = includeFormulas;

	/// <inheritdoc />
	protected override DocumentTextExtraction ExtractText(ChatFileAttachment attachment, CancellationToken cancellationToken)
	{
		using var stream = new MemoryStream(attachment.Data.ToArray(), writable: false);
		using var workbook = new Workbook();
		workbook.LoadDocument(stream);
		var builder = new StringBuilder();
		var hasValues = false;
		foreach (var sheet in workbook.Worksheets)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var range = sheet.GetDataRange();
			builder.AppendLine($"\nWorksheet: {sheet.Name} (first row: {range.TopRowIndex + 1}, first column: {range.LeftColumnIndex + 1})");
			for (var row = range.TopRowIndex; row <= range.BottomRowIndex; row++)
			{
				cancellationToken.ThrowIfCancellationRequested();
				for (var col = range.LeftColumnIndex; col <= range.RightColumnIndex; col++)
				{
					cancellationToken.ThrowIfCancellationRequested();
					var cell = sheet.Cells[row, col];
					var text = cell.DisplayText;
					hasValues |= !string.IsNullOrWhiteSpace(text) || !string.IsNullOrEmpty(cell.Formula);
					if (IncludeFormulas && !string.IsNullOrEmpty(cell.Formula))
						text += $" [formula: {cell.Formula}]";
					if (col > range.LeftColumnIndex)
						builder.Append(',');
					builder.Append('"').Append(text.Replace("\"", "\"\"")).Append('"');
					if (builder.Length > MaximumTextLength)
						return LimitText(builder.ToString());
				}
				builder.AppendLine();
			}
		}
		return hasValues ? LimitText(builder.ToString()) : LimitText(string.Empty);
	}
}

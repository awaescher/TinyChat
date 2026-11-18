namespace TinyChat;

/// <summary>
/// Reports the height an input control needs so that its editor can hold the buttons drawn over its lower corners.
/// </summary>
internal interface IChatInputMinimumHeightControl
{
	/// <summary>Occurs when <see cref="MinimumHeight"/> changes.</summary>
	event EventHandler? MinimumHeightChanged;

	/// <summary>Gets the smallest height at which the editor still holds its buttons.</summary>
	int MinimumHeight { get; }
}

/// <summary>
/// Lets a split container keep the input panel at least as high as the input control needs.
/// </summary>
internal interface IChatSplitContainerMinimumInputHeight
{
	/// <summary>Gets or sets the smallest height of the input panel.</summary>
	int MinimumInputHeight { get; set; }
}

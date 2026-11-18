using System.ComponentModel;

namespace TinyChat;

public partial class ChatControl
{
	private bool _showInputMenuButton = true;
	private EventHandler<ChatInputMenuOpeningEventArgs>? _inputMenuOpening;

	/// <summary>
	/// Occurs before the input menu opens. Handlers can add, remove, or replace entries, or cancel the menu.
	/// Subscribing shows the menu button even when no built-in entry is available.
	/// </summary>
	public event EventHandler<ChatInputMenuOpeningEventArgs>? InputMenuOpening
	{
		add
		{
			_inputMenuOpening += value;
			UpdateInputMenuButton();
		}
		remove
		{
			_inputMenuOpening -= value;
			UpdateInputMenuButton();
		}
	}

	/// <summary>
	/// Gets or sets whether the button in the lower left corner of the input that opens the input menu is shown.
	/// The button is hidden while the menu has no entries, for example when file attachments are not allowed.
	/// </summary>
	[Category("Chat")]
	[Description("Gets or sets whether the button in the lower left corner of the input that opens the input menu is shown.")]
	[DefaultValue(true)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public bool ShowInputMenuButton
	{
		get => _showInputMenuButton;
		set
		{
			if (_showInputMenuButton == value)
				return;

			_showInputMenuButton = value;
			UpdateInputMenuButton();
		}
	}

	/// <summary>
	/// Opens the input menu next to its button, if the input control supports a menu and it has entries.
	/// </summary>
	public void OpenInputMenu()
	{
		if (InputControl is not IChatInputMenuControl menuControl)
			return;

		var args = new ChatInputMenuOpeningEventArgs(CreateInputMenuItems());
		_inputMenuOpening?.Invoke(this, args);
		if (args.Cancel || args.Items.Count == 0)
			return;

		menuControl.ShowMenu(args.Items.ToArray());
	}

	/// <summary>
	/// Shows a file dialog and adds the selected files to the attachment draft in the chat input.
	/// </summary>
	/// <param name="cancellationToken">The token used to cancel file loading.</param>
	/// <returns>The number of files added to the draft.</returns>
	public async Task<int> AttachFilesFromDialogAsync(CancellationToken cancellationToken = default)
	{
		if (!AllowFileAttachments)
			return 0;

		var filePaths = ShowAttachFilesDialog(DisplayTexts.AttachFilesDialogTitle, CreateAttachmentFileDialogFilter());
		if (filePaths is not { Count: > 0 })
			return 0;

		return await AddFilesToInputAsync(filePaths, cancellationToken).ConfigureAwait(true);
	}

	/// <summary>
	/// Creates the entries of the input menu. Override to add, remove, or replace entries;
	/// the <see cref="InputMenuOpening"/> event runs afterwards.
	/// </summary>
	/// <returns>The entries in display order.</returns>
	protected virtual IList<ChatInputMenuItem> CreateInputMenuItems()
	{
		var items = new List<ChatInputMenuItem>();
		if (AllowFileAttachments)
			items.Add(new ChatInputMenuItem(ChatInputMenuItemKind.AttachFiles, DisplayTexts.AttachFiles, AttachFilesFromMenu));

		return items;
	}

	/// <summary>
	/// Gets whether the input menu button should be shown when <see cref="ShowInputMenuButton"/> is enabled.
	/// Override together with <see cref="CreateInputMenuItems"/> when custom entries do not depend on built-in ones.
	/// </summary>
	/// <returns><see langword="true"/> when the menu can have entries; otherwise, <see langword="false"/>.</returns>
	protected virtual bool HasInputMenuItems() => AllowFileAttachments || _inputMenuOpening is not null;

	/// <summary>
	/// Shows the dialog that selects files to attach.
	/// </summary>
	/// <param name="title">The localized dialog title.</param>
	/// <param name="filter">The file filter in the format of <see cref="FileDialog.Filter"/>.</param>
	/// <returns>The selected file paths, or <see langword="null"/> when the dialog was cancelled.</returns>
	protected virtual IReadOnlyList<string>? ShowAttachFilesDialog(string title, string filter)
	{
		using var dialog = new OpenFileDialog
		{
			CheckFileExists = true,
			Filter = filter,
			Multiselect = true,
			RestoreDirectory = true,
			Title = title
		};

		return dialog.ShowDialog(FindForm()) == DialogResult.OK ? dialog.FileNames : null;
	}

	/// <summary>
	/// Creates the file dialog filter from the extensions of the <see cref="AttachmentContentProviders"/>.
	/// </summary>
	/// <returns>The filter in the format of <see cref="FileDialog.Filter"/>.</returns>
	protected virtual string CreateAttachmentFileDialogFilter()
	{
		var allFiles = $"{DisplayTexts.AllFiles}|*.*";
		var patterns = string.Join(";", SupportedAttachmentFileExtensions.Select(extension => "*" + extension));
		return patterns.Length == 0 ? allFiles : $"{DisplayTexts.SupportedAttachmentFiles}|{patterns}|{allFiles}";
	}

	private async void AttachFilesFromMenu()
	{
		try
		{
			await AttachFilesFromDialogAsync().ConfigureAwait(true);
		}
		catch (OperationCanceledException)
		{
			// File loading was cancelled; nothing was added to the draft.
		}
		catch (Exception ex)
		{
			ReportAttachmentError(ex);
		}
	}

	private void ConfigureInputMenu(IChatInputControl inputControl)
	{
		if (inputControl is not IChatInputMenuControl menuControl)
			return;

		menuControl.MenuRequested += (_, _) => OpenInputMenu();
	}

	private void UpdateInputMenuButton()
	{
		if (InputControl is not IChatInputMenuControl menuControl)
			return;

		menuControl.MenuButtonText = DisplayTexts.InputMenu;
		menuControl.ShowMenuButton = ShowInputMenuButton && HasInputMenuItems();
	}
}

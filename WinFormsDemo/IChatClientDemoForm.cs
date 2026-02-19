using TinyChat;

namespace WinFormsDemo;

/// <summary>
/// A demonstration form showing how to use IChatClient integration
/// </summary>
public partial class IChatClientDemoForm : Form
{
    private ChatControl chatControl = null!;
    private CheckBox streamingCheckBox = null!;
    private ComboBox serviceKeyComboBox = null!;
    private Label infoLabel = null!;

    public IChatClientDemoForm()
    {
        InitializeComponent();
        SetupForm();
    }

    private void SetupForm()
    {
        this.Text = "TinyChat - IChatClient Demo";
        this.Size = new Size(800, 600);
        this.StartPosition = FormStartPosition.CenterScreen;

        // Create info panel
        var topPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 80,
            BackColor = SystemColors.Control
        };

        infoLabel = new Label
        {
            Text = "This demo shows automatic IChatClient integration.\nType a message and the ChatControl will automatically call the registered IChatClient.",
            Location = new Point(10, 10),
            Size = new Size(600, 40),
            AutoSize = false
        };
        topPanel.Controls.Add(infoLabel);

        streamingCheckBox = new CheckBox
        {
            Text = "Use Streaming",
            Location = new Point(10, 55),
            Checked = true,
            Width = 120
        };
        streamingCheckBox.CheckedChanged += (s, e) => 
        {
            if (chatControl != null)
                chatControl.UseStreaming = streamingCheckBox.Checked;
        };
        topPanel.Controls.Add(streamingCheckBox);

        var serviceKeyLabel = new Label
        {
            Text = "Service Key:",
            Location = new Point(140, 57),
            AutoSize = true
        };
        topPanel.Controls.Add(serviceKeyLabel);

        serviceKeyComboBox = new ComboBox
        {
            Location = new Point(230, 55),
            Width = 150,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        serviceKeyComboBox.Items.AddRange(new object[] { "(Default)", "premium" });
        serviceKeyComboBox.SelectedIndex = 0;
        serviceKeyComboBox.SelectedIndexChanged += (s, e) =>
        {
            if (chatControl != null)
            {
                chatControl.ChatClientServiceKey = serviceKeyComboBox.SelectedIndex == 0 
                    ? null 
                    : serviceKeyComboBox.SelectedItem?.ToString();
            }
        };
        topPanel.Controls.Add(serviceKeyComboBox);

        this.Controls.Add(topPanel);

        // Create chat control
        chatControl = new ChatControl
        {
            Dock = DockStyle.Fill,
            ServiceProvider = TestIChatClientDemo.CreateServiceProviderWithMockChatClient(),
            UseStreaming = true,
            AssistantSenderName = "AI Assistant"
        };

        this.Controls.Add(chatControl);
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();
        this.ResumeLayout(false);
    }
}

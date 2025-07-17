using DevExpress.LookAndFeel;
using DevExpress.Skins;
using DevExpress.UserSkins;
using DevExpress.XtraSpreadsheet.Layout;
using System.Collections.Generic;
using System.Linq;
using TinyChat;

namespace DevExpressDemo;

public class DXChatControl : ChatControl
{
	protected override IChatMessageHistoryControl CreateMessageHistoryControl() => new StackPanelMessageHistoryControl();

	protected override IChatMessageControl CreateMessageControl(IChatMessage message) => new DXChatMessageControl { Message = message };

	protected override IChatInputControl CreateChatInputControl() => new DXChatInputControl();
}

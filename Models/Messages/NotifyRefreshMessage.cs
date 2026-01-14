using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiDesktopApp1.Models.Messages
{
    public class NotifyRefreshMessage : ValueChangedMessage<RefreshType>
    {
        public NotifyRefreshMessage(RefreshType value) : base(value)
        {
        }
    }
}

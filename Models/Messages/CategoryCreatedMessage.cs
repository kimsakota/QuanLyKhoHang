using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiDesktopApp1.Models.Messages
{
    public sealed class CategoryCreatedMessage : ValueChangedMessage<CategoryModel>
    {
        public CategoryCreatedMessage(CategoryModel value) : base(value)
        {
        }
    }
}

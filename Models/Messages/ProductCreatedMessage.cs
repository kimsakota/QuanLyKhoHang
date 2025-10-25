using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiDesktopApp1.Models;

namespace UiDesktopApp1.Models.Messages
{
    public sealed class ProductCreatedMessage : ValueChangedMessage<ProductModel>
    {
        public ProductCreatedMessage(ProductModel value) : base(value) { }
    }
}

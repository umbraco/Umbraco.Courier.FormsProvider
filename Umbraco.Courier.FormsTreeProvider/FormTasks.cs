using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Courier.UI.CourierTasks;

namespace Umbraco.Courier.FormsProvider
{
   public class FormTasks: ItemTaskProvider
    {
        public FormTasks()
        {
            this.Name = "Contour form tasks";
            this.Description = "Provides UI Tasks for the Umbraco backend";
            this.Id = new Guid("D2C26C8E-82CC-40D2-974F-1E65282A47E3");
            this.ItemProviderId = Constants.ProviderId;
            this.TreeAlias = "form";
        }

        public override string GetItemIdentifier(object treeNodeId)
        {
            return treeNodeId.ToString();
            
        }
    }
}

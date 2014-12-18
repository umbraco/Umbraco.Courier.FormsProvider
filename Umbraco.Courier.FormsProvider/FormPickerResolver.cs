using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Umbraco.Courier.FormsProvider
{
    class FormPickerResolver : Umbraco.Courier.DataResolvers.PropertyDataResolverProvider
    {
        public override string EditorAlias
        {
            get { return Constants.FormPickerAlias; }
        }

        public override void PackagingProperty(Core.Item item, ItemProviders.ContentProperty propertyData)
        {
            Guid g;
            if (propertyData.Value != null && Guid.TryParse(propertyData.Value.ToString(), out g))
            {
                item.Dependencies.Add(g.ToString(), Constants.ProviderId);
            }
        }
    }
}

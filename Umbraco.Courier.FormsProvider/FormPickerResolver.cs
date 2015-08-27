using System;
using Umbraco.Courier.Core;
using Umbraco.Courier.DataResolvers;
using Umbraco.Courier.ItemProviders;

namespace Umbraco.Courier.FormsProvider
{
    public class FormPickerResolver : PropertyDataResolverProvider
    {
        public override string EditorAlias
        {
            get { return Constants.FormPickerAlias; }
        }

        public override void PackagingProperty(Item item, ContentProperty propertyData)
        {
            Guid g;
            if (propertyData.Value != null && Guid.TryParse(propertyData.Value.ToString(), out g))
            {
                item.Dependencies.Add(g.ToString(), Constants.ProviderId);
            }
        }
    }
}

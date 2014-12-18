using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Courier.Core;
using Umbraco.Courier.ItemProviders;

namespace Umbraco.Courier.FormsProvider
{
    public class FormMacroResolver : ItemDataResolverProvider
    {
        private List<string> macroDataTypes = Context.Current.Settings.GetConfigurationCollection("/configuration/itemDataResolvers/macros/add", true);

        public override List<Type> ResolvableTypes
        {
            get { return new List<Type>() { typeof(ContentPropertyData) }; }
        }

        public override bool ShouldExecute(Item item, Umbraco.Courier.Core.Enums.ItemEvent itemEvent)
        {
            if (item.GetType() == typeof(ContentPropertyData))
            {
                ContentPropertyData cpd = (ContentPropertyData)item;
                foreach (var cp in cpd.Data)
                    if (cp.Value != null && macroDataTypes.Contains(cp.PropertyEditorAlias.ToLower()))
                        return true;
            }
            return false;
        }

        public override void Packaging(Item item)
        {
            if (item.GetType() == typeof(ContentPropertyData))
            {
                ContentPropertyData cpd = (ContentPropertyData)item;
                foreach (var prop in cpd.Data.Where(x => x.Value != null && macroDataTypes.Contains(x.PropertyEditorAlias.ToLower()) ))
                {
                    string content = prop.Value.ToString();
                    AddFormDependencies(content, item);
                }
            }
        }

        private void AddFormDependencies(string content, Item item)
        {
            var macroTags = Umbraco.Courier.Core.Helpers.Dependencies.ReferencedMacrosInstring(content);

            foreach (var tag in macroTags.Where(x => x.Alias == Constants.FormPickerAlias).ToList())
            {
                Guid guid;

                if (
                    tag.Attributes.ContainsKey(Constants.FormPickerAttributeAlias) && 
                    !string.IsNullOrEmpty(tag.Attributes[Constants.FormPickerAttributeAlias]) && 
                    Guid.TryParse(tag.Attributes[Constants.FormPickerAttributeAlias], out guid))
                {
                    Dependency formDep = new Dependency();
                    formDep.ItemId = new ItemIdentifier(guid.ToString(), Constants.ProviderId);
                    formDep.Name = "Form from form picker";

                    item.Dependencies.Add(formDep);
                }
            }
        }
    }
}

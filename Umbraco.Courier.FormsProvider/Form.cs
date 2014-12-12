using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Courier.Core;

namespace Umbraco.Courier.FormsProvider
{
    public class UmbracoForm : Item
    {
        public UmbracoForm(){
            WorkflowSettings = new SerializableDictionary<Guid, SerializableDictionary<string, string>>();
            PrevalueSourceSettings = new SerializableDictionary<Guid, SerializableDictionary<string, string>>();
            FieldSettings = new SerializableDictionary<Guid, SerializableDictionary<string, string>>();
            DataSourceSettings = new SerializableDictionary<string, string>();
        }

        public SerializableDictionary<Guid, SerializableDictionary<string, string>> WorkflowSettings { get; set; }
        public SerializableDictionary<Guid, SerializableDictionary<string, string>> PrevalueSourceSettings { get; set; }

        public SerializableDictionary<Guid, SerializableDictionary<string, string>> FieldSettings { get; set; }

        public SerializableDictionary<string, string> DataSourceSettings { get; set; }
        public Guid DataSourceId { get; set; }
    }
}

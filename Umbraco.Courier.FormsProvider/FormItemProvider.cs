using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Courier.Core;

namespace Umbraco.Courier.FormsProvider
{
    public class FormItemProvider : ItemProvider
    {
        public FormItemProvider()
        {
            this.Name = "Forms";
            this.Description = "Extracts forms, accepts a form guid as a valid identifier";
            this.Id = Constants.ProviderId;
            this.ExtractionDirectory = "forms";
            this.Index = 20;
            this.ProviderIcon = "/umbraco/images/umbraco/icon_form.gif";
        }

        public override List<SystemItem> AvailableSystemItems()
        {
            List<SystemItem> items = new List<SystemItem>();

            Umbraco.Forms.Data.Storage.FormStorage fs = new Umbraco.Forms.Data.Storage.FormStorage();

            foreach (var f in fs.GetAllForms())
            {
                SystemItem item = new SystemItem();
                item.ItemId = new ItemIdentifier(f.Id.ToString(), this.Id);
                item.Name = f.Name;
                item.Description = f.Name;
                item.Icon = "icon_form.gif";
                item.HasChildren = false;
                items.Add(item);
            }
            return items;
        }


        public override SystemItem FindSystemItem(string id)
        {
            Umbraco.Forms.Data.Storage.FormStorage fs = new Umbraco.Forms.Data.Storage.FormStorage();
            var form = fs.GetForm(Guid.Parse(id));

            if (form != null)
            {
                SystemItem sys = new SystemItem();
                sys.HasChildren = false;
                sys.Icon = "icon_form.gif";
                sys.ItemId = new ItemIdentifier(id, this.Id);
                sys.Name = form.Name;
                return sys;
            }

            return null;
        }


        public override Item HandleDeserialize(ItemIdentifier id, byte[] item)
        {
            return Umbraco.Courier.Core.Serialization.Serializer.Deserialize<UmbracoForm>(item);
        }

        public override Item HandleExtract(Item item)
        {
            var umbracoform = (UmbracoForm)item;

            //migrate workflowsettings
            if (umbracoform.WorkflowSettings.Count > 0)
            {
                using (var wfs = new Umbraco.Forms.Data.Storage.WorkflowStorage())
                {
                    foreach (var workflowSetting in umbracoform.WorkflowSettings)
                    {
                        var workflow = wfs.GetWorkflow(workflowSetting.Key);
                        if (workflow != null)
                        {
                            workflow.Settings = ReplaceSettings(workflow.Settings, workflowSetting.Value) as Dictionary<string,string>;
                            wfs.UpdateWorkflow(workflow);
                        }
                    }
                }
            }
            

            //migrate datasource settings
            if (umbracoform.DataSourceId != Guid.Empty)
            {
                using (var dss = new Umbraco.Forms.Data.Storage.DataSourceStorage())
                {
                    var datasource = dss.GetDataSource(umbracoform.DataSourceId);
                    if (datasource != null)
                    {
                        datasource.Settings = ReplaceSettings(datasource.Settings, umbracoform.DataSourceSettings) as Dictionary<string, string>;
                        dss.UpdateDataSource(datasource);
                    }
                }
            }


            //migrate prevaluesource settings
            if (umbracoform.PrevalueSourceSettings.Count > 0)
            {
                using (var pvs = new Umbraco.Forms.Data.Storage.PrevalueSourceStorage())
                {
                    foreach(var prevalueSettings in umbracoform.PrevalueSourceSettings){
                        var prevalueSource = pvs.GetPrevalueSource(prevalueSettings.Key);
                        if (prevalueSource != null)
                        {
                            prevalueSource.Settings = ReplaceSettings(prevalueSource.Settings, prevalueSettings.Value) as Dictionary<string, string>;
                            pvs.UpdatePreValueSource(prevalueSource);
                        }
                    }
                }
            }


            //migrate field settings
            if (umbracoform.FieldSettings.Count > 0)
            {
                using (var fs = new Umbraco.Forms.Data.Storage.FormStorage())
                {
                    var form = fs.GetForm(Guid.Parse(umbracoform.ItemId.Id));
                    if (form != null)
                    {
                        foreach (var fieldSetting in umbracoform.FieldSettings)
                        {
                            var field = form.AllFields.Where(x => x.Id == fieldSetting.Key).FirstOrDefault();
                            if (field != null)
                                field.Settings = ReplaceSettings(field.Settings, fieldSetting.Value);
                        }
                        fs.UpdateForm(form);
                    }
                }
            }

            return item;
        }

        public override Item HandlePack(ItemIdentifier id)
        {
            var fs = new Umbraco.Forms.Data.Storage.FormStorage();
            var form = fs.GetForm(Guid.Parse(id.Id));

            if(form == null)
                return null;
            
            var item = new UmbracoForm();
            item.ItemId = id;

            //Handle forms and fields
            var formPath = Umbraco.Forms.Core.Configuration.ContourFolder + "/data/forms/" + id.Id + ".json";
            item.Resources.Add(formPath);

            //Parse field settings and field prevalue source settings
            foreach (var field in form.AllFields)
            {
                var parsed = ParseSettings(field.FieldType.Settings(), field.Settings, item);
                if (parsed.Count > 0)
                    item.FieldSettings.Add(field.Id, parsed);

                if (field.PreValueSourceId != Guid.Empty)
                {
                    var prvParsed = ParseSettings(field.PreValueSource.Type.Settings(), field.PreValueSource.Settings, item);
                    
                    if (prvParsed.Count > 0)
                        item.PrevalueSourceSettings.Add(field.PreValueSourceId, prvParsed);

                    item.Resources.Add(Umbraco.Forms.Core.Configuration.ContourFolder + "/data/prevalueSources/" + field.PreValueSourceId + ".json");
                }
            }



            //parse data source settings
            if (form.HasDataSource())
            {
                
                using (var dss = new Umbraco.Forms.Data.Storage.DataSourceStorage())
                {
                    var dataSource = dss.GetDataSource(form.DataSource.Id);

                    if (dataSource != null)
                    {
                        var dsParsed = ParseSettings(dataSource.Type.Settings(), dataSource.Settings, item);

                        if (dsParsed.Count > 0)
                            item.DataSourceSettings = dsParsed;

                        //add as file to item
                        item.Resources.Add(Umbraco.Forms.Core.Configuration.ContourFolder + "/data/datasources/" + form.DataSource.Id.ToString() + ".json");
                    }
                    
                }
                
                
            }

            //Parse workflow settings
            if (form.WorkflowIds.Count > 0)
            {
                using (var wfs = new Umbraco.Forms.Data.Storage.WorkflowStorage())
                {
                    foreach (var wfId in form.WorkflowIds)
                    {
                        var workflow = wfs.GetWorkflow(wfId);
                        if (workflow != null)
                        {
                            var wfParsed = ParseSettings(workflow.Type.Settings(), workflow.Settings, item);

                            //add file to item 
                            item.Resources.Add(Umbraco.Forms.Core.Configuration.ContourFolder + "/data/workflows/" + wfId.ToString() + ".json");
                        }
                    }    
                }
            }

            return item;
        }

        private SerializableDictionary<string, string> ParseSettings(IDictionary<string, Umbraco.Forms.Core.Attributes.Setting> settingsScema, IDictionary<string, string> settings, UmbracoForm item)
        {
            SerializableDictionary<string, string> settingsMap = new SerializableDictionary<string, string>();

            if (settingsScema != null && settings != null)
            {
                foreach (var setting in settingsScema)
                {
                    if (settings.ContainsKey(setting.Key))
                    {
                        var settingValue = settings[setting.Key];

                        if (string.IsNullOrEmpty(settingValue) == false)
                        {
                            //if the settings view is a node picker of some sort
                            if (setting.Value.view.StartsWith("pickers."))
                            {
                                int id = 0;
                                if (int.TryParse(settingValue, out id))
                                {
                                    var tuple = ExecutionContext.DatabasePersistence.GetUniqueIdWithType(id);
                                    if (tuple != null)
                                    {
                                        //guid
                                        var itemGuid = tuple.Item1.ToString();
                                        var nodeObjectType = tuple.Item2;
                                        var itemProvider = Umbraco.Courier.ItemProviders.NodeObjectTypes.GetCourierProviderFromNodeObjectType(nodeObjectType);
                                        settingsMap[setting.Key] = itemGuid;
                                        
                                        if (itemProvider.HasValue)
                                            item.Dependencies.Add(itemGuid, itemProvider.Value);
                                    }
                                }
                            }

                            //if the settings view is a file upload
                            if (setting.Value.view == "file")
                                item.Resources.Add(settingValue);
                        }
                    }
                }
            }

            return settingsMap;
        }

        private IDictionary<string, string> ReplaceSettings(IDictionary<string, string> current, IDictionary<string, string> replacement)
        {
            foreach (var replacementSetting in replacement)
            {
                if (current.ContainsKey(replacementSetting.Key))
                {
                    Guid guid = Guid.Empty;
                    if (Guid.TryParse(replacementSetting.Value, out guid))
                    {
                        var id = ExecutionContext.DatabasePersistence.GetNodeId(guid);
                        if (id > null)
                            current[replacementSetting.Key] = id.ToString();
                    }
                }
            }

            return current;
        }

    }
}


using BExIS.Dlm.Entities.Data;
using BExIS.Dlm.Entities.DataStructure;
using BExIS.Dlm.Entities.MetadataStructure;
using BExIS.Dlm.Services.Administration;
using BExIS.Dlm.Services.DataStructure;
using BExIS.Dlm.Services.MetadataStructure;
using BExIS.Emm.Entities.Event;
using BExIS.Emm.Services.Event;
using BExIS.Security.Entities.Objects;
using BExIS.Security.Services.Objects;
using BExIS.Xml.Helpers;
using BExIS.Xml.Helpers.Mapping;
using System;
using System.IO;
using System.Linq;
using System.Xml;
using Vaiona.Persistence.Api;
using Vaiona.Utils.Cfg;
using Vaiona.Web.Mvc.Modularity;


namespace BExIS.Modules.EMM.UI.Helpers
{
    public class EMMSeedDataGenerator : IModuleSeedDataGenerator
    {
       

        public void GenerateSeedData()
        {
            EntityManager entityManager = new EntityManager();

            try
            {


                #region create Entities

                // Entities
                Entity entity = entityManager.Entities.Where(e => e.Name.ToUpperInvariant() == "Event".ToUpperInvariant()).FirstOrDefault();

                if (entity == null)
                {
                    entity = new Entity();
                    entity.Name = "Event";
                    entity.EntityType = typeof(Event);
                    entity.EntityStoreType = typeof(EventStore);
                    entity.UseMetadata = true;
                    entity.Securable = true;

                    entityManager.Create(entity);
                }
                #endregion

                #region SECURITY

                OperationManager operationManager = new OperationManager();
                FeatureManager featureManager = new FeatureManager();

                try
                {
                    Feature rootEventManagementFeature = featureManager.FeatureRepository.Get().FirstOrDefault(f => f.Name.Equals("Event Management"));
                    if (rootEventManagementFeature == null) rootEventManagementFeature = featureManager.Create("Event Management", "Event Management");

                    Feature eventRegistrationFeature = featureManager.FeatureRepository.Get().FirstOrDefault(f => f.Name.Equals("Event Registration"));
                    if (eventRegistrationFeature == null) eventRegistrationFeature = featureManager.Create("Event Registration", "Event Registration", rootEventManagementFeature);

                    Feature eventAdministrationFeature = featureManager.FeatureRepository.Get().FirstOrDefault(f => f.Name.Equals("Event Administration"));
                    if (eventAdministrationFeature == null) eventAdministrationFeature = featureManager.Create("Event Administration", "Event Administration", rootEventManagementFeature);

                    Feature eventRegistrationResultFeature = featureManager.FeatureRepository.Get().FirstOrDefault(f => f.Name.Equals("Event Registration Result"));
                    if (eventRegistrationResultFeature == null) eventRegistrationResultFeature = featureManager.Create("Event Registration Result", "Event Registration Result", rootEventManagementFeature);

                    operationManager.Create("EMM", "EventRegistration", "*", eventRegistrationFeature);
                    operationManager.Create("EMM", "Event", "*", eventAdministrationFeature);
                    operationManager.Create("EMM", "EventRegistrationResult", "*", eventRegistrationResultFeature);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    featureManager.Dispose();
                    operationManager.Dispose();
                }

                #endregion

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                entityManager.Dispose();
            }
        }


        

        public void Dispose()
        {
            // nothing to do for now...
        }

    }
}
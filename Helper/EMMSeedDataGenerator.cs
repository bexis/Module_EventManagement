
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


                #region create entities

                // Entities
                Entity entity = entityManager.Entities.Where(e => e.Name.ToUpperInvariant() == "Event".ToUpperInvariant()).FirstOrDefault();

                if (entity == null)
                {
                    entity = new Entity();
                    entity.Name = "Event";
                    entity.EntityType = typeof(Event);
                    entity.EntityStoreType = typeof(EventManager);
                    entity.UseMetadata = true;
                    entity.Securable = true;

                    entityManager.Create(entity);
                }



                #endregion

                #region SECURITY
                //workflows = größere sachen, vielen operation
                //operations = einzelne actions

                //1.controller-> 1.Operation




                //Feature DataCollectionFeature = featureManager.FeatureRepository.Get().FirstOrDefault(f => f.Name.Equals("Data Collection"));
                //if (DataCollectionFeature == null) DataCollectionFeature = featureManager.Create("Data Collection", "Data Collection");

                //Feature DatasetCreationFeature = featureManager.FeatureRepository.Get().FirstOrDefault(f => f.Name.Equals("Data Creation"));
                //if (DatasetCreationFeature == null) DatasetCreationFeature = featureManager.Create("Data Creation", "Data Creation", DataCollectionFeature);

                //Feature DatasetUploadFeature = featureManager.FeatureRepository.Get().FirstOrDefault(f => f.Name.Equals("Dataset Upload"));
                //if (DatasetUploadFeature == null) DatasetUploadFeature = featureManager.Create("Dataset Upload", "Dataset Upload", DataCollectionFeature);

                //Feature MetadataManagementFeature = featureManager.FeatureRepository.Get().FirstOrDefault(f => f.Name.Equals("Metadata Management"));
                //if (MetadataManagementFeature == null) MetadataManagementFeature = featureManager.Create("Metadata Management", "Metadata Management", DataCollectionFeature);


                //#region Help Workflow

                //operationManager.Create("DCM", "Help", "*");

                //#endregion

                //#region Create Dataset Workflow

                //operationManager.Create("DCM", "CreateDataset", "*", DatasetCreationFeature);
                //operationManager.Create("DCM", "Form", "*");

                //#endregion

                //#region Update Dataset Workflow

                //operationManager.Create("DCM", "Push", "*", DatasetUploadFeature);
                //operationManager.Create("DCM", "Submit", "*", DatasetUploadFeature);
                //operationManager.Create("DCM", "SubmitDefinePrimaryKey", "*", DatasetUploadFeature);
                //operationManager.Create("DCM", "SubmitGetFileInformation", "*", DatasetUploadFeature);
                //operationManager.Create("DCM", "SubmitSelectAFile", "*", DatasetUploadFeature);
                //operationManager.Create("DCM", "SubmitSpecifyDataset", "*", DatasetUploadFeature);
                //operationManager.Create("DCM", "SubmitSummary", "*", DatasetUploadFeature);
                //operationManager.Create("DCM", "SubmitValidation", "*", DatasetUploadFeature);

                //#endregion

                //#region Easy Upload

                //operationManager.Create("DCM", "EasyUpload", "*", DatasetUploadFeature);
                //operationManager.Create("DCM", "EasyUploadSelectAFile", "*", DatasetUploadFeature);
                //operationManager.Create("DCM", "EasyUploadSelectAreas", "*", DatasetUploadFeature);
                //operationManager.Create("DCM", "EasyUploadSheetDataStructure", "*", DatasetUploadFeature);
                //operationManager.Create("DCM", "EasyUploadSheetSelectMetaData", "*", DatasetUploadFeature);
                //operationManager.Create("DCM", "EasyUploadSummary", "*", DatasetUploadFeature);
                //operationManager.Create("DCM", "EasyUploadVerification", "*", DatasetUploadFeature);

                //#endregion

                //#region Metadata Managment Workflow

                //operationManager.Create("DCM", "ImportMetadataStructure", "*", MetadataManagementFeature);
                //operationManager.Create("DCM", "ImportMetadataStructureReadSource", "*", MetadataManagementFeature);
                //operationManager.Create("DCM", "ImportMetadataStructureSelectAFile", "*", MetadataManagementFeature);
                //operationManager.Create("DCM", "ImportMetadataStructureSetParameters", "*", MetadataManagementFeature);
                //operationManager.Create("DCM", "ImportMetadataStructureSummary", "*", MetadataManagementFeature);
                //operationManager.Create("DCM", "ManageMetadataStructure", "*", MetadataManagementFeature);
                //operationManager.Create("DCM", "SubmitSpecifyDataset", "*", MetadataManagementFeature);

                //#endregion

                //#region public available

                //operationManager.Create("DCM", "Form", "*");

                //#endregion

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
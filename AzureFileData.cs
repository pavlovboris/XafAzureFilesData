using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Azure.Storage.Sas;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Dropbox.Api;
using Dropbox.Api.Files;
using System;
using System.ComponentModel;
using System.IO;
using Xpand.Extensions.StreamExtensions;

namespace DSERP.Module.BusinessObjects.ThirPartyBO
{
    [DefaultProperty("FileName")]
    public class AzureFileData : BaseObject, IFileData, IEmptyCheckable
    {

        private string fileName = "";

        [Persistent]
        private int size;
        static string defaultFolder = System.Configuration.ConfigurationManager.AppSettings["AzureDefaultFolder"];
        public int Size
        {
            get { return size; }
        }

        public AzureFileData(Session session)
            : base(session)
        {
        }
        
        public Uri GetReadUri()
        {
            return AzureShareFile().GenerateSasUri(ShareFileSasPermissions.Read,DateTimeOffset.Now.AddHours(1));
        }

        public ShareFileClient AzureShareFile()
        {
            var shareClient = new ShareClient(System.Configuration.ConfigurationManager.AppSettings["AzureConnectionString"], System.Configuration.ConfigurationManager.AppSettings["AzureFileShare"]);

            var azureDirectory = shareClient.GetDirectoryClient(defaultFolder);

            var azureFile = azureDirectory.GetFileClient(fileName);

            return azureFile;
        }

        public virtual void LoadFromStream(string fileName, Stream stream)
        {

            // System.Threading.Tasks.Task task = new System.Threading.Tasks.Task(Upload);
            //task.Start();
            //task.Wait();

            Guard.ArgumentNotNull(stream, "stream");
            FileName = fileName;
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);

            var azureFile = AzureShareFile();  // azureDirectory.GetFileClient(fileName);

            azureFile.Create(stream.Length);
            stream.Position = 0;
            azureFile.UploadRange(new HttpRange(0,stream.Length),stream);

            //TODO add the sharing info
        }

   
        public virtual void SaveToStream(Stream stream)
        {


            var azureFile = AzureShareFile();



            var file = azureFile.Download();
            var TaskResponse = System.Threading.Tasks.Task.Run(() => azureFile.DownloadAsync());
            TaskResponse.Wait();

            var t = System.Threading.Tasks.Task.Factory.FromAsync(stream.BeginWrite, stream.EndWrite, TaskResponse.Result.Value.Content.Bytes(), 0, Size, null);
            t.Wait();
            stream.Flush();

        }
        public void Clear()
        {
            //TODO delete
            //Content = null;


            var azureFile = AzureShareFile();

            azureFile?.DeleteIfExists();
            FileName = String.Empty;
        }
        public override string ToString()
        {
            return FileName;
        }
        [Size(260)]
        public string FileName
        {
            get { return fileName; }
            set { SetPropertyValue("FileName", ref fileName, value); }
        }

        protected override void OnDeleted()
        {
            base.OnDeleted();

            //var azureFile = AzureShareFile();

            //azureFile?.DeleteIfExists();
        }

        #region IEmptyCheckable Members
        [NonPersistent, MemberDesignTimeVisibility(false)]
        public bool IsEmpty
        {
            get { return string.IsNullOrEmpty(FileName); }
        }
        #endregion


        public override void AfterConstruction()
        {
            base.AfterConstruction();
            // Place your initialization code here (https://documentation.devexpress.com/eXpressAppFramework/CustomDocument112834.aspx).
        }
       
    }
}

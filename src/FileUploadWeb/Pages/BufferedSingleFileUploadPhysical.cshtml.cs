using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Configuration;
using SampleApp.Utilities;

namespace SampleApp.Pages
{
    public class BufferedSingleFileUploadPhysicalModel : PageModel
    {
        private readonly long _fileSizeLimit;
        private readonly string[] _permittedExtensions = { ".zip" };
        private readonly string _targetFilePath;
        private readonly string _destinationStorageAccount;

        public BufferedSingleFileUploadPhysicalModel(IConfiguration config)
        {
            _fileSizeLimit = config.GetValue<long>("FileSizeLimit");

            // To save physical files to a path provided by configuration:
            _targetFilePath = config.GetValue<string>("StoredFilesPath");

            //_destinationStorageAccount = config.GetValue<string>("DestinationStorageAccount");
            _destinationStorageAccount = Environment.GetEnvironmentVariable("DestinationStorageAccount");


            // To save physical files to the temporary files folder, use:
            //_targetFilePath = Path.GetTempPath();
        }

        [BindProperty]
        public BufferedSingleFileUploadPhysical FileUpload { get; set; }

        public string Result { get; private set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostUploadAsync()
        {
            if (!ModelState.IsValid)
            {
                Result = "Please correct the form.";

                return Page();
            }

            var formFileContent =
                await FileHelpers.ProcessFormFile<BufferedSingleFileUploadPhysical>(
                    FileUpload.FormFile, ModelState, _permittedExtensions,
                    _fileSizeLimit);

            if (!ModelState.IsValid)
            {
                Result = "Please correct the form.";

                return Page();
            }


            // For the file name of the uploaded file stored
            // server-side, use Path.GetRandomFileName to generate a safe
            // random file name.
            var trustedFileNameForFileStorage = this.FileUpload.FormFile.FileName;

            //// upload to storage account

            //var sourceStorageConnectionString = Environment.GetEnvironmentVariable("SourceStorageAccount");

            //var localZipFile = SetLocalPath(name);

            // Check whether the connection string can be parsed.
            CloudStorageAccount storageAccount;
            if (CloudStorageAccount.TryParse(_destinationStorageAccount, out storageAccount))
            {
                // If the connection string is valid, proceed with operations against Blob
                // storage here.
                CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(_targetFilePath);
                await cloudBlobContainer.CreateIfNotExistsAsync();
                CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(trustedFileNameForFileStorage);
                await cloudBlockBlob.UploadFromByteArrayAsync(formFileContent, 0, formFileContent.Length);
                //await cloudBlockBlob.BeginUploadFromByteArray(formFileContent);
                //await cloudBlockBlob.DownloadToFileAsync(localZipFile, FileMode.Create);
            }
            else
            {
                // Otherwise, let the user know that they need to define the environment variable.

                return RedirectToPage("./Error");
            }

            // **WARNING!**
            // In the following example, the file is saved without
            // scanning the file's contents. In most production
            // scenarios, an anti-virus/anti-malware scanner API
            // is used on the file before making the file available
            // for download or for use by other systems. 
            // For more information, see the topic that accompanies 
            // this sample.

            // using (var fileStream = System.IO.File.Create(filePath))
            // {
            //     await fileStream.WriteAsync(formFileContent);

            //     // To work directly with a FormFile, use the following
            //     // instead:
            //     //await FileUpload.FormFile.CopyToAsync(fileStream);
            // }

            return RedirectToPage("./Index");
        }
    }

    public class BufferedSingleFileUploadPhysical
    {
        [Required]
        [Display(Name = "File")]
        public IFormFile FormFile { get; set; }

        [Display(Name = "Note")]
        [StringLength(50, MinimumLength = 0)]
        public string Note { get; set; }
    }
}

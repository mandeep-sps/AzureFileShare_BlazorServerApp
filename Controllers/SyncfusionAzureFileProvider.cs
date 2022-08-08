
using FileManagerSample.Extensions;
using FileManagerSample.Models;

namespace filemanager.Server.Controllers
{
    [Route("api/[controller]")]
    public class SyncfusionAzureFileProviderController : Controller
    {
        #region Local Properties
        private readonly IConfiguration configuration;
        private readonly HelperClass _helperclass;
        [Obsolete]
        private Microsoft.Extensions.Hosting.IHostingEnvironment _hostingEnvironment;
        string root = string.Empty;
        public PhysicalFileProvider operation;
        public string basePath;
        public string AzureFileShare = "test2";
        public string res;
        CloudStorageAccount storageAccount;
        CloudFileClient cloudFileClient;
        CloudFileShare fileShare;
        CloudFileDirectory fileDirectory;
        private readonly AzureFileProvider _operation1;
        #endregion

        #region Constructor
        [Obsolete]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public SyncfusionAzureFileProviderController(Microsoft.Extensions.Hosting.IHostingEnvironment
            hostingEnvironment,
            IConfiguration configuration, HelperClass helperClass, AzureFileProvider operation1)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            _helperclass = helperClass;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            this.basePath = hostingEnvironment.ContentRootPath;
            this.configuration = configuration;
            res = configuration.GetSection("fileShareConnectionString").Value;
            root = string.Empty; //configuration.GetSection("AzureFileLocation").Value;
            this.operation = new PhysicalFileProvider();
            _operation1 = operation1;
            _hostingEnvironment = hostingEnvironment;
        }
        #endregion

        #region Blazor File Manager Operations

        /// <summary>
        /// File Manager Read, Dlete Operations
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>

        [Route("AzureFileOperations")]
        public async Task<object?> AzureFileOperationsAsync([FromBody] Syncfusion.EJ2.FileManager.Base.FileManagerDirectoryContent args)
        {
            try
            {
                switch (args.Action)
                {
                    case "read":
                        var Data = await _operation1.GetAzureSMBFilesAsync(args.Path, args.Data);
                        return Json(this.ToCamelCase(Data));
                    case "delete":
                        await DeleteAzurFilesAsync(args.Path, args.Names);
                        return this.ToCamelCase(this.operation.Delete(args.Path, args.Names, null));
                    case "details":
                        return this.ToCamelCase(this.operation.Details(args.Path, args.Names, null));
                    case "create":
                        var data = await _operation1.CreateAsync(args.Path, args.Name, args.Data);
                        return this.ToCamelCase(data);
                    case "search":
                        return this.ToCamelCase(this.operation.Search(args.Path, args.SearchString, args.ShowHiddenItems, args.CaseSensitive, null));
                    case "copy":
                        await CopyFilesAsync(args.Path, args.TargetPath, args.Names);
                        this.ToCamelCase(this.operation.Copy(args.Path, args.TargetPath, args.Names, args.RenameFiles, null, null));
                        return this.ToCamelCase(this.operation.Delete(args.TargetPath, args.Names, null));
                    case "move":
                        await MoveFilesAsync(args.Path, args.TargetPath, args.Names);
                        this.ToCamelCase(this.operation.Move(args.Path, args.TargetPath, args.Names, args.RenameFiles, null, null));
                        return this.ToCamelCase(this.operation.Delete(args.TargetPath, args.Names, null));
                }

                return null;
            }
            catch (Exception)
            {

                return null;
            }
        }

        /// <summary>
        ///  Get Image and Showing in the popup window
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        [Route("GetImage")]
        public async Task<IActionResult> GetImage(Syncfusion.EJ2.FileManager.Base.FileManagerDirectoryContent args)
        {
            return await _operation1.GetImage(args.Path, args.Id, false, null, null);
        }
        /// <summary>
        /// This method is responsible for downloading any file
        /// </summary>
        /// <param name="downloadInput"></param>
        /// <returns></returns>
        [Route("Download")]
        public IActionResult Download(string downloadInput)
        {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            Syncfusion.Blazor.FileManager.FileManagerDirectoryContent args =
                JsonConvert.DeserializeObject<Syncfusion.Blazor.FileManager.FileManagerDirectoryContent>(downloadInput);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            return operation.Download(args.Path, args.Names);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }

        /// <summary>
        /// Upload the files on Azure Storage file Share SMB 3.0
        /// </summary>
        /// <param name="path"></param>
        /// <param name="uploadFiles"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        [Route("Upload")]
        public async Task<IActionResult> Upload(string path, IList<IFormFile> uploadFiles, string action)
        {
            await UploadFileOnAzure(uploadFiles, path);
            return Json("");
        }

        // Downloads the selected file(s) and folder(s)
        [Route("AzureDownload")]
        public async Task<IActionResult> AzureDownload(string downloadInput)
        {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            Syncfusion.EJ2.FileManager.Base.FileManagerDirectoryContent args = JsonConvert.DeserializeObject<Syncfusion.EJ2.FileManager.Base.FileManagerDirectoryContent>(downloadInput);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            var data = await _operation1.DownloadAsync(args.Path, args.Names, args.Data);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            byte[] byteArray = data.stream.ToArray();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            //Clean up the memory stream
            data.stream.Flush();
            return File(byteArray, "application/" + Path.GetExtension(data.fileName), data.fileName);
            // return fileStreamResult;
        }

        #endregion

        #region Azure Storage files Functions
        /// <summary>
        /// Create Root folder and Sub folder on Azure File storage//
        /// </summary>
        /// <param name="foldername"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task CreateAzureStorageFolderAsync(string foldername, string path)
        {

            fileDirectory = await AzureShareConfigSettingsAsync();
            if (path == "/")
            {
                var folder = fileDirectory.GetDirectoryReference(foldername);
                await folder.CreateIfNotExistsAsync();
            }
            else
            {
                string newpath = path.Remove(0, 1);
                var folder = fileDirectory.GetDirectoryReference(newpath + foldername);
                await folder.CreateIfNotExistsAsync();
            }

            // return Task.CompletedTask;
        }

        /// <summary>
        /// Copy the Files One folder to Another folder using Azure Storage file share
        /// </summary>
        /// <param name="path"></param>
        /// <param name="targetPath"></param>
        /// <param name="names"></param>
        /// <returns></returns>
        private async Task MoveFilesAsync(string path, string targetPath, string[] names)
        {
            try
            {
                storageAccount = CloudStorageAccount.Parse(res);
                cloudFileClient = storageAccount.CreateCloudFileClient();
                fileShare = cloudFileClient.GetShareReference(AzureFileShare);
                await fileShare.CreateIfNotExistsAsync();
                await fileShare.ExistsAsync();
                fileDirectory = fileShare.GetRootDirectoryReference();
                CloudFileDirectory rootDir = fileShare.GetRootDirectoryReference();
                string CurrentPath = path.Remove(0, 1);
                var directory = rootDir.GetDirectoryReference(CurrentPath);
                if (Path.HasExtension(names[0]))
                {
                    string Fname = names[0];
                    CloudFile Sourcefile = directory.GetFileReference(Fname);
                    if (await Sourcefile.ExistsAsync())
                    {
                        var memoryStream = new MemoryStream();
                        // await Sourcefile.DownloadToStreamAsync(memoryStream);
                        string DestPath = targetPath.Remove(0, 1);
                        var destDir = rootDir.GetDirectoryReference(DestPath);
                        await destDir.ExistsAsync();
                        string fileName = Sourcefile.Uri.Segments.Last();
                        CloudFile destinationFile = destDir.GetFileReference(fileName);
                        //await destinationFile.UploadFromStreamAsync(memoryStream);
                        await destinationFile.StartCopyAsync(Sourcefile);
                        await Sourcefile.DeleteIfExistsAsync();
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        /// <summary>
        /// Copy & Paste file into Another folder//
        /// </summary>
        /// <param name="path"></param>
        /// <param name="targetPath"></param>
        /// <param name="names"></param>
        /// <returns></returns>
        private async Task CopyFilesAsync(string path, string targetPath, string[] names)
        {
            try
            {
                storageAccount = CloudStorageAccount.Parse(res);
                cloudFileClient = storageAccount.CreateCloudFileClient();
                fileShare = cloudFileClient.GetShareReference(AzureFileShare);
                await fileShare.CreateIfNotExistsAsync();
                await fileShare.ExistsAsync();
                fileDirectory = fileShare.GetRootDirectoryReference();
                CloudFileDirectory rootDir = fileShare.GetRootDirectoryReference();
                string CurrentPath = path.Remove(0, 1);
                var directory = rootDir.GetDirectoryReference(CurrentPath);
                if (Path.HasExtension(names[0]))
                {
                    string Fname = names[0];
                    CloudFile Sourcefile = directory.GetFileReference(Fname);
                    if (await Sourcefile.ExistsAsync())
                    {
                        var memoryStream = new MemoryStream();
                        string DestPath = targetPath.Remove(0, 1);
                        var destDir = rootDir.GetDirectoryReference(DestPath);
                        await destDir.ExistsAsync();
                        string fileName = string.Empty;
                        Guid uniqueId = Guid.NewGuid();
                        fileName = Sourcefile.Name.Replace(" ", "");
                        string fName = uniqueId.ToString() + "_" + fileName;
                        CloudFile destinationFile = destDir.GetFileReference(fName);
                        await destinationFile.StartCopyAsync(Sourcefile);

                    }
                }
            }
            catch (Exception)
            {

            }

        }


        /// <summary>
        /// Upload the File on Azure Storage file Share Via IFormFile //
        /// </summary>
        /// <param name="formFile"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task UploadFileOnAzure(IList<IFormFile> formFile, string path)
        {
            try
            {
                string UUID = _helperclass.Create_UUID();
                var firstfile = formFile.FirstOrDefault();
                string ext = System.IO.Path.GetExtension(firstfile.FileName);
                Stream pdfFileStream = null;
                if (ext == ".pdf")
                {
                    // Copy the file Into Temp Storage//
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\TempFiles", firstfile.FileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await firstfile.CopyToAsync(fileStream);
                    }
                    pdfFileStream = _helperclass.CompressPDF(filePath, firstfile.FileName);
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
                fileDirectory = await AzureShareConfigSettingsAsync();
                CloudFileDirectory rootDir = fileShare.GetRootDirectoryReference();
                string fileName = "";
                if (await rootDir.ExistsAsync())
                {
                    string newpath = string.Empty;
                    if (path != "/")
                    {
                        if (path.Contains("/") == true)
                        {
                            newpath = path.Remove(0, 1);

                        }
                        else
                        {
                            newpath = path;
                        }
                        CloudFileDirectory sampleDir = rootDir.GetDirectoryReference(newpath);
                        if (await sampleDir.ExistsAsync())
                        {
                            fileName = firstfile.FileName.Replace(" ", "");
                            fileName = UUID + "_" + fileName;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                            CloudFile file = sampleDir.GetFileReference(fileName);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                            file.Properties.ContentType = firstfile.ContentType;
                            Stream fileStream = firstfile.OpenReadStream();
                            if (ext == ".pdf")
                            {
                                await file.UploadFromStreamAsync(pdfFileStream);
                            }
                            else
                            {
                                await file.UploadFromStreamAsync(fileStream);
                            }
                        }
                    }
                    else
                    {

#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        fileName = firstfile.FileName.Replace(" ", "");
                        fileName = UUID + "_" + fileName;
                        CloudFile file = fileDirectory.GetFileReference(fileName);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                        file.Properties.ContentType = firstfile.ContentType;
                        Stream fileStream = firstfile.OpenReadStream();
                        if (ext == ".pdf")
                        {
                            await file.UploadFromStreamAsync(pdfFileStream);
                        }
                        else
                        {
                            await file.UploadFromStreamAsync(fileStream);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Delete file from Azure Storage file share//
        /// </summary>
        /// <param name="path"></param>
        /// <param name="Filenames"></param>
        /// <returns></returns>
        public async Task DeleteAzurFilesAsync(string path, string[] Filenames)
        {
            try
            {
                fileDirectory = await AzureShareConfigSettingsAsync();
                CloudFileDirectory folder;
                if (((path == "/" || path == "\\") || path.EndsWith("\\") || path.EndsWith("/")) && !Path.HasExtension(Filenames[0]))
                {
                    if (path.StartsWith("\\") && path.EndsWith("\\"))
                    {
                        if (path == "\\")
                        {
                            path = path.Remove(0, 1);
                        }
                        //Filenames[0] = Filenames[0].Remove(path.LastIndexOf("\\"),1);
                    }
                    var fullpath = path + Filenames[0];
                    folder = fileDirectory.GetDirectoryReference(fullpath);
                }
                else
                {
                    if (path != "/" && path != "")
                    {
                        path = path.Remove(0, 1);
                    }
                    if (path == "/" || path == "")
                    {
                        folder = fileDirectory;
                    }
                    else
                    {
                        folder = fileDirectory.GetDirectoryReference(path);
                    }
                }
                List<IListFileItem> results = new List<IListFileItem>();
                FileContinuationToken? token = null;
                do
                {
                    FileResultSegment resultSegment = folder.ListFilesAndDirectoriesSegmentedAsync
                        (token).GetAwaiter().GetResult();

                    if (Path.HasExtension(Filenames[0]))
                    {
                        results.Add(folder.GetFileReference(Filenames[0]));
                    }
                    else
                    {
                        results.AddRange(resultSegment.Results);
                    }
                    token = resultSegment.ContinuationToken;
                    if (resultSegment.Results.Count() == 0)
                    {
                        if (await folder.ExistsAsync())
                            await folder.DeleteIfExistsAsync();
                        return;
                    }
                }
                while (token != null);
                foreach (IListFileItem listItem in results)
                {
                    if (listItem.GetType() == typeof(CloudFile))
                    {
                        CloudFile file = (CloudFile)listItem;
                        await file.DeleteIfExistsAsync();
                        // Do whatever
                    }
                    else if (listItem.GetType() == typeof(CloudFileDirectory))
                    {
                        CloudFileDirectory dir = (CloudFileDirectory)listItem;
                        await dir.DeleteIfExistsAsync();
                        // Do whatever
                    }
                }
                if (!Path.HasExtension(Filenames[0]))
                {
                    await fileDirectory.DeleteIfExistsAsync();
                }
            }
            catch (Exception)
            {

            }
        }

        public async Task DownloadFilesAsync(string res, string path)
        {
            fileDirectory = await AzureShareConfigSettingsAsync();
            await downloadCloudFilesAsync(fileDirectory, root, path);
        }

        /// <summary>
        /// Downloads Directories, Sub drectories and Files from Azure Storage files Share//
        /// </summary>
        /// <param name="rootDir"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<List<IListFileItem>> downloadCloudFilesAsync(CloudFileDirectory rootDir,
           string path, string azureFolderPath)
        {
            List<IListFileItem> results = new List<IListFileItem>();
            FileContinuationToken? token = null;
            if (azureFolderPath == "/")
            {
                // Getting the root files data and Folders//
                do
                {
                    FileResultSegment resultSegment =
                        rootDir.ListFilesAndDirectoriesSegmentedAsync(token).GetAwaiter().GetResult();
                    results.AddRange(resultSegment.Results);
                    token = resultSegment.ContinuationToken;
                }
                while (token != null);
                foreach (IListFileItem listItem in results)
                {
                    if (listItem is CloudFile file)
                    {
                        //get the cloudfile's propertities and metadata 
                        await file.FetchAttributesAsync();
                        string FileName = file.Name;
                        bool FileStatus = _helperclass.FileExists(root, FileName);
                        if (FileStatus == false)
                            await file.DownloadToFileAsync(Path.Combine(path, file.Name), FileMode.Create);

                    }
                    else if (listItem is CloudFileDirectory dir)
                    {
                        var folderPath = Path.Combine(path, dir.Name);
                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }
                        //await downloadCloudFilesAsync(dir, folderPath);
                    }
                }
                return results;
            }
            else
            {
                //getting the Sub Directories and files//
                string CurrentPath = azureFolderPath.Remove(0, 1);
                var destDir = rootDir.GetDirectoryReference(CurrentPath);
                do
                {
                    FileResultSegment resultSegment =
                        destDir.ListFilesAndDirectoriesSegmentedAsync(token).GetAwaiter().GetResult();
                    results.AddRange(resultSegment.Results);
                    token = resultSegment.ContinuationToken;
                }
                while (token != null);
                foreach (IListFileItem listItem in results)
                {
                    if (listItem is CloudFile file)
                    {
                        //get the cloudfile's propertities and metadata 
                        await file.FetchAttributesAsync();
                        string fileName = file.Name;
                        string finalPath = path + "\\" + destDir.Name;
                        bool FileStatus = FileExists(root, fileName);
                        if (FileStatus == false)
                            await file.DownloadToFileAsync(Path.Combine(finalPath, file.Name), FileMode.Create);

                    }
                    else if (listItem is CloudFileDirectory dir)
                    {
                        string combineDir = destDir.Name + "\\" + dir.Name;
                        var folderPath = Path.Combine(path, combineDir);
                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }
                        //await downloadCloudFilesAsync(dir, folderPath,azureFolderPath);
                    }
                }
                return results;

            }
#pragma warning disable CS0162 // Unreachable code detected
            return results;
#pragma warning restore CS0162 // Unreachable code detected
        }

        /// <summary>
        /// Check file Exits or not So we can stop duplicates files download process//
        /// </summary>
        /// <param name="rootpath"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        private bool FileExists(string rootpath, string filename)
        {
            if (System.IO.File.Exists(Path.Combine(rootpath, filename)))
                return true;

            foreach (string subDir in Directory.GetDirectories(rootpath, "*", SearchOption.AllDirectories))
            {
                if (System.IO.File.Exists(Path.Combine(subDir, filename)))
                    return true;
            }
            return false;
        }
        /// <summary>
        /// Extension Method//
        /// </summary>
        /// <param name="userData"></param>
        /// <returns></returns>
        public string ToCamelCase(object userData)
        {
            var data = JsonConvert.SerializeObject(userData, new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                }
            });
            return data;
        }

        /// <summary>
        /// Azure Config Settings
        /// </summary>
        /// <returns></returns>
        public async Task<CloudFileDirectory> AzureShareConfigSettingsAsync()
        {
            storageAccount = CloudStorageAccount.Parse(res);
            cloudFileClient = storageAccount.CreateCloudFileClient();
            fileShare = cloudFileClient.GetShareReference(AzureFileShare);
            await fileShare.CreateIfNotExistsAsync();
            await fileShare.ExistsAsync();
            fileDirectory = fileShare.GetRootDirectoryReference();
            return fileDirectory;
        }

        #endregion
    }


}
using Google.Apis.Drive.v3;
using printingshop.application.Interfaces;

namespace printingshop.infrastructure.Drive
{
    public class DriveProvider : IGoogleDriveService
    {
        private readonly DriveService _driveService;
        private const string PrintQueueFolderName = "Print Queue";
        private string? _printQueueFolderId;

        public DriveProvider(DriveService driveService)
        {
            _driveService = driveService ?? throw new ArgumentNullException(nameof(driveService));
        }

        public async Task<string> UploadAsync(Stream stream, string fileName)
        {
            try
            {
                var folderId = await GetOrCreatePrintQueueFolderAsync();

                var fileMetadata = new Google.Apis.Drive.v3.Data.File
                {
                    Name = fileName,
                    Parents = [folderId],
                    MimeType = GetMimeType(fileName)
                };

                var uploadRequest = _driveService.Files.Create(fileMetadata, stream, fileMetadata.MimeType);
                uploadRequest.Fields = "id";

                var uploadedFile = await uploadRequest.UploadAsync();

                if (uploadedFile.Status != Google.Apis.Upload.UploadStatus.Completed)
                    throw new Exception("File upload failed");

                var createdFile = uploadRequest.ResponseBody;
                return createdFile?.Id ?? throw new Exception("Upload succeeded but file ID is null");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error uploading file to Google Drive: {ex.Message}", ex);
            }
        }

        public async Task<string> CreateFolderAsync(string folderName, string? parentFolderId = null)
        {
            try
            {
                var parents = string.IsNullOrEmpty(parentFolderId)
                    ? new List<string> { "root" }
                    : new List<string> { parentFolderId };

                var folderMetadata = new Google.Apis.Drive.v3.Data.File
                {
                    Name = folderName,
                    MimeType = "application/vnd.google-apps.folder",
                    Parents = parents
                };

                var createRequest = _driveService.Files.Create(folderMetadata);
                createRequest.Fields = "id";

                var createdFolder = await createRequest.ExecuteAsync();
                return createdFolder?.Id ?? throw new Exception("Folder creation succeeded but folder ID is null");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating folder in Google Drive: {ex.Message}", ex);
            }
        }

        private async Task<string> GetOrCreatePrintQueueFolderAsync()
        {
            if (!string.IsNullOrEmpty(_printQueueFolderId))
                return _printQueueFolderId;

            try
            {
                // Search for existing Print Queue folder
                var listRequest = _driveService.Files.List();
                listRequest.Q = $"name='{PrintQueueFolderName}' and mimeType='application/vnd.google-apps.folder' and trashed=false";
                listRequest.Spaces = "drive";
                listRequest.Fields = "files(id, name)";

                var listResult = await listRequest.ExecuteAsync();
                var existingFolder = listResult.Files?.FirstOrDefault();

                if (existingFolder != null)
                {
                    _printQueueFolderId = existingFolder.Id;
                    return _printQueueFolderId ?? throw new Exception("Existing folder found but ID is null");
                }

                // Create new Print Queue folder
                _printQueueFolderId = await CreateFolderAsync(PrintQueueFolderName);
                return _printQueueFolderId ?? throw new Exception("New folder created but ID is null");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting or creating Print Queue folder: {ex.Message}", ex);
            }
        }

        private static string GetMimeType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".txt" => "text/plain",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };
        }
    }
}

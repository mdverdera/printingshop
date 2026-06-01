namespace printingshop.application.Interfaces
{
    public interface IGoogleDriveService
    {
        /// <summary>
        /// Upload file to Google Drive in Print Queue folder
        /// </summary>
        Task<string> UploadAsync(Stream stream, string fileName);

        /// <summary>
        /// Create folder in Google Drive
        /// </summary>
        Task<string> CreateFolderAsync(string folderName, string? parentFolderId = null);
    }
}

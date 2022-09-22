using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace PrimeNumbersNow.Helpers
{
    internal class HelpFileSavePicker
    {
        public static async Task<StorageFile> GetStorageFileForJsonAsync(string fileName)
        {
            FileSavePicker fileSavePicker = new FileSavePicker();

            fileSavePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            // Dropdown of file types the user can save the file as
            fileSavePicker.FileTypeChoices.Add(".json-file", new List<string>() { ".json" });
            fileSavePicker.CommitButtonText = "Save .json-file";
            // Default file name if the user does not type one in or select a file to replace
            fileSavePicker.SuggestedFileName = HelpFileName.GetNewNumbersFileName(fileName);
            fileSavePicker.SettingsIdentifier = "Export primenumbers";

            StorageFile storageFile = await fileSavePicker.PickSaveFileAsync();
            return storageFile;
        }
    }
}

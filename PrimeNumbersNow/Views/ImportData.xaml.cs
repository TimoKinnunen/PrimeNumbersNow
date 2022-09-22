using Newtonsoft.Json;
using PrimeNumbersNow.Helpers;
using PrimeNumbersNow.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace PrimeNumbersNow.Views
{
    public sealed partial class ImportData : Page
    {
        private readonly MainPage mainPage;

        private CancellationTokenSource ImportCancellationTokenSource;

        CancellationToken ImportCancellationToken;

        private StorageFile pickedStorageFile;

        private List<PrimeNumberItem> primeNumberItems;

        public ImportData()
        {
            this.InitializeComponent();

            // do cache the state of the UI when suspending/navigating
            // this is necessary for PrimeNumbersNow when navigating
            //NavigationCacheMode = NavigationCacheMode.Required;

            mainPage = MainPage.CurrentMainPage;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // code here
            // code here
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // code here
            if (ImportCancellationToken.CanBeCanceled)
            {
                ImportCancellationTokenSource.Cancel();
            }
            if (ImportCancellationTokenSource != null)
            {
                ImportCancellationTokenSource.Dispose();
            }
            // code here
        }

        #region MenuAppBarButton
        private void HomeAppBarButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            mainPage.GoToHomePage();
        }
        #endregion MenuAppBarButton

        private async void ImportDataButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (ImportDataButton.Content.ToString() == "Cancel")
            {
                if (ImportCancellationToken.CanBeCanceled)
                {
                    ImportCancellationTokenSource.Cancel();
                }
                ImportDataButton.Content = "Import data";
                return;
            }

            if (ImportDataButton.Content.ToString() == "Import data")
            {
                mainPage.NotifyUser("Pick a .json-file please.", NotifyType.StatusMessage);

                FilenameTextBlock.Text = String.Empty;
                ImportedRecordsCountTextBlock.Text = String.Empty;

                pickedStorageFile = await HelpFileOpenPicker.PickJsonFileAsync();
                if (pickedStorageFile == null)
                {
                    mainPage.NotifyUser("Operation cancelled.", NotifyType.ErrorMessage);
                    return;
                }

                StartImportDataProgressRing();
                using (IRandomAccessStream stream = await pickedStorageFile.OpenAsync(FileAccessMode.Read))
                {
                    ulong size = stream.Size;
                    using (IInputStream inputStream = stream.GetInputStreamAt(0))
                    {
                        using (DataReader dataReader = new DataReader(inputStream))
                        {
                            uint numBytesLoaded = await dataReader.LoadAsync((uint)size);
                            string jsonData = dataReader.ReadString(numBytesLoaded);
                            primeNumberItems = await Task.Run(() => JsonConvert.DeserializeObject<List<PrimeNumberItem>>(jsonData)).ConfigureAwait(false);
                        }
                    }
                }

                if (primeNumberItems == null)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        mainPage.NotifyUser($"File's {pickedStorageFile.Name} content is empty or not correct formatted. Nothing to import.", NotifyType.StatusMessage);
                        StopImportDataProgressRing();
                    });
                    return;
                }

                if (primeNumberItems.Count > 0)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        ImportDataButton.Content = "Cancel";
                        mainPage.NotifyUser("Please wait or cancel.", NotifyType.StatusMessage);
                        StartImportDataProgressRing();

                        try
                        {
                            await App.PrimeNumberRepo.DeleteDatabaseTableAsync();

                            await App.PrimeNumberRepo.CreateTableAsync();

                            ImportCancellationTokenSource = new CancellationTokenSource();
                            ImportCancellationToken = ImportCancellationTokenSource.Token;

                            //add cancellationToken
                            await Task.Run(async () => await ImportDataAsync(), ImportCancellationToken).ConfigureAwait(false);
                        }
                        catch (TaskCanceledException)
                        {
                            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                mainPage.NotifyUser("Task was cancelled.", NotifyType.ErrorMessage);
                            });
                        }
                        catch (Exception ex)
                        {
                            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                mainPage.NotifyUser(ex.Message, NotifyType.ErrorMessage);
                            });
                        }
                        finally
                        {
                            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                mainPage.NotifyUser("Done.", NotifyType.StatusMessage);
                                StopImportDataProgressRing();
                                ImportDataButton.Content = "Import data";
                            });
                        }
                    });
                }
            }
        }

        private async Task ImportDataAsync()
        {
            int recordCount = 0;
            int numberItemsCount = primeNumberItems.Count;
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                FilenameTextBlock.Text = $"Processing file {pickedStorageFile.Name} containing {numberItemsCount} primenumbers.";
            });
            foreach (PrimeNumberItem primeNumberItem in primeNumberItems)
            {
                if (ImportCancellationTokenSource.IsCancellationRequested)
                {
                    break;
                }
                int x = await App.PrimeNumberRepo.AddNewPrimeNumberItemAsStringAsync(primeNumberItem.PrimeNumber).ConfigureAwait(false);
                recordCount++;
                if (recordCount % 100 == 0)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        mainPage.NotifyUser($"Added new primenumber {primeNumberItem.PrimeNumber} to database table, primenumbers count is {recordCount}({numberItemsCount}).", NotifyType.StatusMessage);
                    });
                }
            }
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ImportedRecordsCountTextBlock.Text = $"Imported primenumbers count is {recordCount}({numberItemsCount}).";
            });
        }

        private void StartImportDataProgressRing()
        {
            ImportDataProgressRing.IsActive = true;
            ImportDataProgressRing.Visibility = Visibility.Visible;
        }

        private void StopImportDataProgressRing()
        {
            ImportDataProgressRing.IsActive = false;
            ImportDataProgressRing.Visibility = Visibility.Collapsed;
        }
    }
}

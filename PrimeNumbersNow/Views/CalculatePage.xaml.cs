using PrimeNumbersNow.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace PrimeNumbersNow.Views
{
    public sealed partial class CalculatePage : Page
    {
        CancellationTokenSource calculateCancellationTokenSource { get; set; }

        CancellationToken calculateCancellationToken { get; set; }

        readonly MainPage mainPage;

        BigInteger biggestPrimeNumber { get; set; }

        List<PrimeNumberItem> primeNumberItemsList { get; set; } = new List<PrimeNumberItem>();
        HashSet<BigInteger> primeNumberHashSet { get; set; } = new HashSet<BigInteger>();

        DispatcherTimer dispatcherTimer { get; set; }

        BigInteger countOfprimenumberCandidates { get; set; }
        BigInteger countOfPrimenumbers { get; set; }

        BigInteger countOfMinutes { get; set; }

        Dictionary<BigInteger, BigInteger> oneStepDictionary { get; set; } = new Dictionary<BigInteger, BigInteger>();
        Dictionary<BigInteger, BigInteger> tempOneStepDictionary { get; set; } = new Dictionary<BigInteger, BigInteger>();

        public CalculatePage()
        {
            InitializeComponent();

            // do cache the state of the UI when suspending/navigating
            // this is necessary for EratosthenesSieveNow when navigating
            //NavigationCacheMode = NavigationCacheMode.Required;

            mainPage = MainPage.CurrentMainPage;
        }

        private async void DispatcherTimer_Tick(object sender, object e)
        {
            countOfMinutes++;
            RateTextBlock.Text = $"Calculation rate is {countOfPrimenumbers / countOfMinutes} primenumbers/minute ({countOfPrimenumbers}/{countOfMinutes}). Stepped through {countOfprimenumberCandidates} primenumber candidates.";
            BiggestPrimeNumberTextBlock.Text = await App.PrimeNumberRepo.GetBiggestPrimeNumberAsStringAsync();
            var count = await App.PrimeNumberRepo.GetPrimeNumberItemsCountAsync();
            NumberOfPrimenumbersTextBlock.Text = count.ToString();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // code here
            if (await App.PrimeNumberRepo.GetPrimeNumberItemsCountAsync() == 0)
            {
                await App.PrimeNumberRepo.AddNewPrimeNumberItemAsStringAsync(2.ToString());
                await App.PrimeNumberRepo.AddNewPrimeNumberItemAsStringAsync(3.ToString());
                await App.PrimeNumberRepo.AddNewPrimeNumberItemAsStringAsync(5.ToString());
            }

            biggestPrimeNumber = BigInteger.Parse(await App.PrimeNumberRepo.GetBiggestPrimeNumberAsStringAsync());
            BiggestPrimeNumberTextBlock.Text = biggestPrimeNumber.ToString();
            var count = await App.PrimeNumberRepo.GetPrimeNumberItemsCountAsync();
            NumberOfPrimenumbersTextBlock.Text = count.ToString();

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Interval = new TimeSpan(0, 1, 0);
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            // code here
        }


        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // code here
            if (calculateCancellationToken.CanBeCanceled)
            {
                calculateCancellationTokenSource.Cancel();
            }
            if (calculateCancellationTokenSource != null)
            {
                calculateCancellationTokenSource.Dispose();
            }
            if (dispatcherTimer.IsEnabled)
            {
                dispatcherTimer.Stop();
            }
            // code here
        }

        #region MenuAppBarButton
        private void HomeAppBarButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            mainPage.GoToHomePage();
        }
        #endregion MenuAppBarButton

        private async void CalculateDataButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (CalculateDataButton.Content.ToString() == "Cancel")
            {
                if (calculateCancellationToken.CanBeCanceled)
                {
                    calculateCancellationTokenSource.Cancel();
                }
                if (dispatcherTimer.IsEnabled)
                {
                    dispatcherTimer.Stop();
                }
                CalculateDataButton.Content = "Calculate primenumbers";
                return;
            }

            if (CalculateDataButton.Content.ToString() == "Calculate primenumbers")
            {
                if (biggestPrimeNumber > 0)
                {
                    CalculateDataButton.Content = "Cancel";
                    mainPage.NotifyUser("Please wait or cancel. Calculation rate is shown after every passed minute.", NotifyType.StatusMessage);

                    StartCalculateDataProgressRing();

                    countOfprimenumberCandidates = 0;
                    countOfPrimenumbers = 0;
                    countOfMinutes = 0;
                    dispatcherTimer.Start();

                    biggestPrimeNumber = BigInteger.Parse(await App.PrimeNumberRepo.GetBiggestPrimeNumberAsStringAsync());

                    BigInteger x = GetIdOfPrimenumber(biggestPrimeNumber);

                    primeNumberItemsList = await App.PrimeNumberRepo.GetAllPrimeNumberItemsAsync();
                    foreach (PrimeNumberItem primeNumberItem in primeNumberItemsList)
                    {
                        primeNumberHashSet.Add(BigInteger.Parse(primeNumberItem.PrimeNumber));
                    }

                    try
                    {
                        calculateCancellationTokenSource = new CancellationTokenSource();
                        calculateCancellationToken = calculateCancellationTokenSource.Token;

                        //add cancellationToken to this task
                        await Task.Run(async () => await CalculateDataAsync(x), calculateCancellationToken);
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
                            CalculateDataButton.Content = "Calculate primenumbers";

                            dispatcherTimer.Stop();
                            RateTextBlock.Text = string.Empty;

                            StopCalculateDataProgressRing();
                        });
                    }
                }
            }
        }

        private async Task CalculateDataAsync(BigInteger id)
        {
            while (!calculateCancellationTokenSource.IsCancellationRequested)
            {
                if (calculateCancellationTokenSource.IsCancellationRequested)
                {
                    break;
                }

                id++;
                countOfprimenumberCandidates += 2; //include even and odd numbers

                //eratosthenes
                await Task.Run(() =>
                {
                    tempOneStepDictionary = new Dictionary<BigInteger, BigInteger>();
                    foreach (KeyValuePair<BigInteger, BigInteger> element in oneStepDictionary)
                    {
                        BigInteger newValue;
                        if (element.Value <= 0)
                        {
                            newValue = element.Key - 1;
                        }
                        else
                        {
                            newValue = element.Value - 1;
                        }
                        tempOneStepDictionary.Add(element.Key, newValue);
                    }
                    oneStepDictionary = tempOneStepDictionary;
                });

                if (oneStepDictionary.Any(e => e.Value == 0))
                {
                    continue; //continue while
                }

                BigInteger primeNumberCandidate = GetPrimenumberOfId(id);

                #region filter primeNumberCandidate on last digit
                string lastDigit = primeNumberCandidate.ToString().Substring(primeNumberCandidate.ToString().Length - 1, 1);

                switch (lastDigit)
                {
                    case "1": //possible primenumber
                        break;
                    case "3": //possible primenumber
                        break;
                    case "7": //possible primenumber
                        break;
                    case "9": //possible primenumber
                        break;
                    default:
                        continue; //continue while
                }
                #endregion filter primeNumberCandidate on last digit

                bool isPrimeNumber = true;
                //modulus is anyway needed, boring!
                await Task.Run(() =>
                {
                    foreach (BigInteger primenumber in primeNumberHashSet)
                    {
                        if (primeNumberCandidate % primenumber == 0)
                        {
                            if (!oneStepDictionary.Any(e => e.Key == primenumber))
                            {
                                oneStepDictionary.Add(primenumber, primenumber);
                            }

                            isPrimeNumber = false;
                            break;
                        }

                        //primeNumberCandidate is not divisible by bigger primes
                        if (primenumber >= (primeNumberCandidate + 1) / 2)
                        {
                            break;
                        }
                    }
                });

                if (isPrimeNumber)
                {
                    countOfPrimenumbers++;
                    await App.PrimeNumberRepo.AddNewPrimeNumberItemAsStringAsync(primeNumberCandidate.ToString());

                    BigInteger lastPrimeInHashSet = primeNumberHashSet.LastOrDefault();
                    if (lastPrimeInHashSet != null)
                    {
                        //get list when needed, no extra roundtrip to database
                        if (lastPrimeInHashSet < (primeNumberCandidate + 1) / 2)
                        {
                            primeNumberItemsList = await App.PrimeNumberRepo.GetAllPrimeNumberItemsAsync();
                            foreach (PrimeNumberItem primeNumberItem in primeNumberItemsList)
                            {
                                primeNumberHashSet.Add(BigInteger.Parse(primeNumberItem.PrimeNumber));
                            }
                        }
                    }
                    else
                    {
                        primeNumberItemsList = await App.PrimeNumberRepo.GetAllPrimeNumberItemsAsync();
                        foreach (PrimeNumberItem primeNumberItem in primeNumberItemsList)
                        {
                            primeNumberHashSet.Add(BigInteger.Parse(primeNumberItem.PrimeNumber));
                        }
                    }

                    if (countOfPrimenumbers % 100 == 0)
                    {
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            mainPage.NotifyUser($"Added new primenumber {primeNumberCandidate} to database table.", NotifyType.StatusMessage);
                        });
                    }
                }
            }
        }

        BigInteger GetPrimenumberOfId(BigInteger i)
        {
            return i * 2 + 1;
        }

        BigInteger GetIdOfPrimenumber(BigInteger biggestPrimenumber)
        {
            return (biggestPrimenumber - 1) / 2;
        }

        private void StartCalculateDataProgressRing()
        {
            CalculateDataProgressRing.IsActive = true;
            CalculateDataProgressRing.Visibility = Visibility.Visible;
        }

        private void StopCalculateDataProgressRing()
        {
            CalculateDataProgressRing.IsActive = false;
            CalculateDataProgressRing.Visibility = Visibility.Collapsed;
        }

        #region validate the old way
        //private async void ValidateDataAppBarButton_Tapped(object sender, TappedRoutedEventArgs e)
        //{
        //    try
        //    {
        //        await Task.Run(async () => await ValidatekAsync());
        //    }
        //    catch (TaskCanceledException)
        //    {
        //        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
        //        {
        //            mainPage.NotifyUser("Task was cancelled.", NotifyType.ErrorMessage);
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
        //        {
        //            mainPage.NotifyUser(ex.Message, NotifyType.ErrorMessage);
        //        });
        //    }
        //}

        //private async Task ValidatekAsync()
        //{
        //    int faultyRecordCount = 0;
        //    List<PrimeNumberItem> primeNumberItemsList = await App.PrimeNumberRepo.GetAllPrimeNumberItemsAsync();

        //    //calculate primenumbers the old way
        //    foreach (PrimeNumberItem primeNumberItem in primeNumberItemsList)
        //    {
        //        bool isPrimeNumber = true;
        //        for (int i = 2; i < int.Parse(primeNumberItem.PrimeNumber); i++)
        //        {
        //            if (int.Parse(primeNumberItem.PrimeNumber) % i == 0)
        //            {
        //                isPrimeNumber = false;
        //                break;
        //            }
        //        }

        //        if (!isPrimeNumber)
        //        {
        //            faultyRecordCount++;
        //        }

        //        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
        //        {
        //            mainPage.NotifyUser(string.Format("PrimeNumberRepo. Validating primenumber {0}. Faulty {1}.", primeNumberItem.PrimeNumber, faultyRecordCount.ToString()), NotifyType.StatusMessage);
        //        });
        //    }

        //    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
        //    {
        //        mainPage.NotifyUser(string.Format("PrimeNumberRepo. Faulty {0}.", faultyRecordCount.ToString()), NotifyType.StatusMessage);
        //    });


        //    PrimeNumberItem lastPrimeNumberItem = primeNumberItemsList.LastOrDefault();

        //    List<string> calculatedPrimeNumbers = new List<string>();

        //    //calculate primenumbers the old way
        //    for (int j = 2; j <= int.Parse(lastPrimeNumberItem.PrimeNumber); j++)
        //    {
        //        bool isPrimeNumber = true;
        //        for (int i = 2; i < j; i++)
        //        {
        //            if (j % i == 0)
        //            {
        //                isPrimeNumber = false;
        //                break;
        //            }
        //        }

        //        if (isPrimeNumber)
        //        {
        //            calculatedPrimeNumbers.Add(j.ToString());
        //        }
        //    }

        //    faultyRecordCount = 0;
        //    //can we find them un database table?
        //    foreach (string calculatedPrimeNumber in calculatedPrimeNumbers)
        //    {
        //        PrimeNumberItem primeNumberItem = await App.PrimeNumberRepo.SearchPrimeNumberItemAsync(calculatedPrimeNumber);
        //        if (primeNumberItem == null)
        //        {
        //            faultyRecordCount++;
        //        }
        //        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
        //        {
        //            mainPage.NotifyUser(string.Format("PrimeNumbers calculated the old way. Validating primenumber {0}. Faulty {1}.", primeNumberItem.PrimeNumber, faultyRecordCount.ToString()), NotifyType.StatusMessage);
        //        });
        //    }
        //    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
        //    {
        //        mainPage.NotifyUser(string.Format("PrimeNumbers calculated the old way. Faulty {0}.", faultyRecordCount.ToString()), NotifyType.StatusMessage);
        //    });
        //}
        #endregion validate the old way
    }
}

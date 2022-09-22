using PrimeNumbersNow.Data;
using PrimeNumbersNow.Models;
using SQLite;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

namespace PrimeNumbersNow.Repository
{
    public class PrimeNumberRepository
    {
        public SQLiteAsyncConnection db { get; private set; }

        public PrimeNumberRepository()
        {
            db = new SQLiteAsyncConnection(App.DatabasePath);

            Task.Run(async () => await CreateTableAsync()).Wait();
        }

        public async Task CreateTableAndSeedAsync()
        {
            CreateTableResult createTableResult = await db.CreateTableAsync<PrimeNumberItem>();
            switch (createTableResult)
            {
                case CreateTableResult.Created:
                    // Seed the table
                    foreach (BigInteger primeNumber in SmallPrimes.smallPrimes)
                    {
                        int x = await AddNewPrimeNumberItemAsStringAsync(primeNumber.ToString());
                    }
                    break;
                case CreateTableResult.Migrated:
                    break;
                default:
                    break;
            }
        }

        public async Task CreateTableAsync()
        {
            CreateTableResult createTableResult = await db.CreateTableAsync<PrimeNumberItem>();
            switch (createTableResult)
            {
                case CreateTableResult.Created:
                    break;
                case CreateTableResult.Migrated:
                    break;
                default:
                    break;
            }
        }

        public async Task<PrimeNumberItem> SearchPrimeNumberItemAsync(string primeNumber)
        {
            return await db.Table<PrimeNumberItem>().FirstOrDefaultAsync(i => i.PrimeNumber == primeNumber).ConfigureAwait(false);
        }

        public async Task<int> AddNewPrimeNumberItemAsStringAsync(string primeNumber)
        {
            return await db.InsertAsync(new PrimeNumberItem { PrimeNumber = primeNumber }).ConfigureAwait(false);
        }

        public async Task<PrimeNumberItem> GetPrimeNumberItemAsync(int id)
        {
            return await db.Table<PrimeNumberItem>().FirstOrDefaultAsync(i => i.Id == id).ConfigureAwait(false);
        }

        public async Task<List<PrimeNumberItem>> GetAllPrimeNumberItemsAsync()
        {
            return await db.Table<PrimeNumberItem>().OrderBy(i => i.Id).ToListAsync().ConfigureAwait(false);
        }

        public async Task<List<BigInteger>> GetAllPrimeNumberItemsAsBigIntegerAsync()
        {
            List<BigInteger> bigIntegerPrimeNumberItems = new List<BigInteger>();

            List<PrimeNumberItem> primeNumberItems = await db.Table<PrimeNumberItem>().OrderBy(i => i.Id).ToListAsync().ConfigureAwait(false);
            foreach (PrimeNumberItem primeNumberItem in primeNumberItems)
            {
                bigIntegerPrimeNumberItems.Add(BigInteger.Parse(primeNumberItem.PrimeNumber));
            }
            return bigIntegerPrimeNumberItems;
        }

        public async Task<int> GetPrimeNumberItemsCountAsync()
        {
            return await db.Table<PrimeNumberItem>().CountAsync().ConfigureAwait(false);
        }

        public async Task<string> GetBiggestPrimeNumberAsStringAsync()
        {
            // Reverse the order and take first, it is now the last item
            PrimeNumberItem primeNumberItem = await db.Table<PrimeNumberItem>().OrderByDescending(x => x.Id).FirstOrDefaultAsync().ConfigureAwait(false);
            if (primeNumberItem != null)
            {
                return primeNumberItem.PrimeNumber;
            }
            else
            {
                return "0";
            }
        }

        public async Task<PrimeNumberItem> GetBiggestPrimeNumberItemAsync()
        {
            // Reverse the order and take first, it is now the last item
            return await db.Table<PrimeNumberItem>().OrderByDescending(x => x.Id).FirstOrDefaultAsync().ConfigureAwait(false);
        }

        public async Task<int> DeleteDatabaseTableAsync()
        {
            return await db.DropTableAsync<PrimeNumberItem>();
        }
    }
}

using System;

namespace PrimeNumbersNow.Helpers
{
    internal class HelpFileName
    {
        public static string GetNewNumbersFileName(string fileName)
        {
            var dateTime = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"{fileName}_{dateTime}.json";
        }
    }
}

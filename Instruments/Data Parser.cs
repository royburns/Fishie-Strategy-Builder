// Data_Parser Class
// Part of Forex Strategy Builder
// Website http://forexsb.com/
// Copyright (c) 2006 - 2011 Miroslav Popov - All rights reserved.
// This code or any part of it cannot be used in other applications without a permission.

using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Forex_Strategy_Builder
{
    /// <summary>
    /// Provide methods of parsing data string.
    /// </summary>
    public class Data_Parser
    {
        /// <summary>
        /// Gets the the data array
        /// </summary>
        public Bar[] Bar { get; private set; }

        /// <summary>
        /// Parses the input data string.
        /// </summary>
        /// <param name="dataString">The input data string.</param>
        /// <returns>The number of parsed bars.</returns>
        public int Parse(string dataString)
        {
            int bars = 0;

            try
            {
                Regex regexDataString = AnalyseInput(dataString);
                bars = CountDataBars(dataString, regexDataString);
                Bar = ParseInput(dataString, regexDataString, bars);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, Language.T("Data File Loading"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return bars;
        }

        /// <summary>
        /// Gets a compiled general regex.
        /// </summary>
        private static Regex GeneralDataFileRegex
        {
            get
            {
                const string spacePattern  = @"[\t ;,]";
                const string datePattern   = @"\d{1,4}[\./-]\d{1,4}[\./-]\d{1,4}";
                //const string timePattern   = @"\d{2}(:\d{2}){1,2}";
                const string pricePattern  = @"\d+([\.,]\d+)?";
                const string volumePattern = @"\d{1,10}";

                // A data line has to start with date string followed by time string
                var regex = new Regex("^" +  // Start of the string
                    spacePattern + "*" +  // Zero or more white spaces
                    datePattern +        // Valid date pattern
                    spacePattern + "+" +  // One or more spaces
                    //timePattern +        // Valid time pattern
                    spacePattern + "+" +  // One or more spaces
                    pricePattern +        // Price
                    spacePattern + "+" +  // One or more spaces
                    pricePattern +        // Price
                    spacePattern + "+" +  // One or more spaces
                    pricePattern +        // Price
                    spacePattern + "+" +  // One or more spaces
                    pricePattern +        // Price
                    spacePattern + "+" +  // One or more spaces
                    volumePattern +        // Optional volume
                    spacePattern + "*"    // Zero or more white spaces
                    , RegexOptions.Compiled);

                return regex;
            }
        }

        /// <summary>
        /// Analyses the input data string.
        /// </summary>
        /// <param name="dataString">The input data string.</param>
        /// <returns>Matched regex for the data string.</returns>
        private Regex AnalyseInput(string dataString)
        {
            string datePattern = GetDateMatchPattern(dataString);
            string pricePattern = PriceMatchPattern(dataString);

            string dataMatchPattern = "^[\t ;,]*" +
                datePattern + @"[\t ;,]+(?<hour>\d{2}):(?<min>\d{2})(:(?<sec>\d{2}))?[\t ;,]+" +
                pricePattern + @"[\t ;,]+(?<volume>\d+)[\t ;,]*$";

            return new Regex(dataMatchPattern, RegexOptions.Compiled);
        }

        /// <summary>
        /// Gets the date regex pattern that matches the data file.
        /// </summary>
        /// <param name="dataString">The data file content.</param>
        /// <returns>Date regex pattern.</returns>
        private string GetDateMatchPattern(string dataString)
        {
            string line;
            int yearPos = 0;
            int monthPos = 0;
            int dayPos = 0;
            const string datePattern = @"(?<1>\d{1,4})[\./-](?<2>\d{1,4})[\./-](?<3>\d{1,4})";
            var regexDate = new Regex(datePattern, RegexOptions.Compiled);

            var stringReader = new StringReader(dataString);
            while ((line = stringReader.ReadLine()) != null)
            {
                Match matchDate = regexDate.Match(line);

                if (!matchDate.Success)
                    continue;

                int pos1 = int.Parse(matchDate.Result("$1"));
                int pos2 = int.Parse(matchDate.Result("$2"));
                int pos3 = int.Parse(matchDate.Result("$3"));

                // Determines the year index
                if (yearPos == 0)
                {
                    if (pos1 > 31)
                    {
                        yearPos = 1;
                        monthPos = 2;
                        dayPos = 3;
                        break;
                    }
                    if (pos3 > 31)
                    {
                        yearPos = 3;
                    }
                }

                // Determines the day index
                if (dayPos == 0 && yearPos > 0)
                {
                    if (yearPos == 1)
                    {
                        dayPos = 2;
                        monthPos = 3;
                        break;
                    }
                    if (yearPos == 3)
                    {
                        if (pos1 > 12)
                        {
                            dayPos = 1;
                            monthPos = 2;
                            break;
                        }
                        if (pos2 > 12)
                        {
                            monthPos = 1;
                            dayPos = 2;
                            break;
                        }
                    }
                }

                // Determines the month index
                if (dayPos > 0 && yearPos > 0)
                {
                    if (yearPos != 1 && dayPos != 1)
                        monthPos = 1;
                    else if (yearPos != 2 && dayPos != 2)
                        monthPos = 2;
                    else if (yearPos != 3 && dayPos != 3)
                        monthPos = 3;
                }

                if (yearPos > 0 && monthPos > 0 && dayPos > 0)
                    break;
            }
            stringReader.Close();

            // If the date format is not recognized we try to find the number of changes
            if (yearPos == 0 || monthPos == 0 || dayPos == 0)
            {
                int old1 = 0;
                int old2 = 0;
                int old3 = 0;

                int changes1 = -1;
                int changes2 = -1;
                int changes3 = -1;

                stringReader = new StringReader(dataString);
                while ((line = stringReader.ReadLine()) != null)
                {
                    Match matchDate = regexDate.Match(line);

                    if (!matchDate.Success)
                        continue;

                    int pos1 = int.Parse(matchDate.Result("$1"));
                    int pos2 = int.Parse(matchDate.Result("$2"));
                    int pos3 = int.Parse(matchDate.Result("$3"));

                    if (pos1 != old1)
                    {
                        // pos1 has changed
                        old1 = pos1;
                        changes1++;
                    }
                    if (pos2 != old2)
                    {
                        // pos2 has changed
                        old2 = pos2;
                        changes2++;
                    }
                    if (pos3 != old3)
                    {
                        // date2 has changed
                        old3 = pos3;
                        changes3++;
                    }


                    // Check number of changes
                    if (changes1 > changes2 && changes1 > changes2)
                    {
                        dayPos = 1;
                        monthPos = 2;
                        yearPos = 3;
                        break;
                    }
                    if (changes3 > changes1 && changes3 > changes2)
                    {
                        dayPos = 3;
                        monthPos = 2;
                        yearPos = 1;
                        break;
                    }
                    if (changes2 > changes1 && changes2 > changes3)
                    {
                        yearPos = 3;
                        monthPos = 1;
                        dayPos = 2;
                        break;
                    }
                }
                stringReader.Close();

                if (yearPos > 0)
                {
                    // The year position is known
                    if (yearPos == 1)
                    {
                        if (changes3 > changes2)
                        {
                            monthPos = 2;
                            dayPos = 3;
                        }
                        else if (changes2 > changes3)
                        {
                            monthPos = 3;
                            dayPos = 2;
                        }
                    }
                    else if (yearPos == 3)
                    {
                        if (changes2 > changes1)
                        {
                            monthPos = 1;
                            dayPos = 2;
                        }
                        else if (changes1 > changes2)
                        {
                            monthPos = 2;
                            dayPos = 1;
                        }
                    }
                }

                // If we don't know the year position but know that the day is somewhere in the end.
                // The year must be on the other end of the pattern because the year doesn't stay in the middle.
                if (yearPos == 0 && dayPos == 1)
                {
                    yearPos = 3;
                    monthPos = 2;
                }
                if (yearPos == 0 && dayPos == 3)
                {
                    yearPos = 1;
                    monthPos = 2;
                }

                if (yearPos == 0)
                {
                    // The year position is unknown
                    if (changes1 >= 0 && changes2 > changes1 && changes3 > changes2)
                    {
                        yearPos = 1;
                        monthPos = 2;
                        dayPos = 3;
                    }
                    else if (changes1 >= 0 && changes3 > changes1 && changes2 > changes3)
                    {
                        yearPos = 1;
                        monthPos = 3;
                        dayPos = 2;
                    }
                    else if (changes2 >= 0 && changes1 > changes2 && changes3 > changes1)
                    {
                        yearPos = 2;
                        monthPos = 1;
                        dayPos = 3;
                    }
                    else if (changes2 >= 0 && changes3 > changes2 && changes1 > changes3)
                    {
                        yearPos = 2;
                        monthPos = 3;
                        dayPos = 1;
                    }
                    else if (changes3 >= 0 && changes1 > changes3 && changes2 > changes1)
                    {
                        yearPos = 3;
                        monthPos = 1;
                        dayPos = 2;
                    }
                    else if (changes3 >= 0 && changes2 > changes3 && changes1 > changes2)
                    {
                        yearPos = 3;
                        monthPos = 2;
                        dayPos = 1;
                    }
                }
            }

            string dateMatchPattern = "";
            if (yearPos * monthPos * dayPos > 0)
            {
                if (yearPos == 1 && monthPos == 2 && dayPos == 3)
                    dateMatchPattern = @"(?<year>\d{1,4})[\./-](?<month>\d{1,4})[\./-](?<day>\d{1,4})";
                else if (yearPos == 3 && monthPos == 1 && dayPos == 2)
                    dateMatchPattern = @"(?<month>\d{1,4})[\./-](?<day>\d{1,4})[\./-](?<year>\d{1,4})";
                else if (yearPos == 3 && monthPos == 2 && dayPos == 1)
                    dateMatchPattern = @"(?<day>\d{1,4})[\./-](?<month>\d{1,4})[\./-](?<year>\d{1,4})";
            }
            else
            {
                throw new Exception(Language.T("Could not determine the date format!"));
            }

            return dateMatchPattern;
        }

        /// <summary>
        /// Determines the price pattern.
        /// </summary>
        /// <param name="dataString">The data file content.</param>
        /// <returns>Price match pattern.</returns>
        private string PriceMatchPattern(string dataString)
        {
            var regexGeneral = GeneralDataFileRegex;
            const string columnSeparator = @"[\t ;,]+";
            string priceMatchPattern = "";
            string line;
            var sr = new StringReader(dataString);

            while ((line = sr.ReadLine()) != null)
            {
                if (!regexGeneral.IsMatch(line))
                    continue;

                var matchPrice = Regex.Match(line, columnSeparator +
                        @"(?<1>\d+([\.,]\d+)?)" + columnSeparator +
                        @"(?<2>\d+([\.,]\d+)?)" + columnSeparator +
                        @"(?<3>\d+([\.,]\d+)?)" + columnSeparator +
                        @"(?<4>\d+([\.,]\d+)?)" + columnSeparator);

                double price2 = ParseDouble(matchPrice.Result("$2"));
                double price3 = ParseDouble(matchPrice.Result("$3"));

                const double epsilon = 0.000001;
                if (price2 > price3 + epsilon)
                {
                    priceMatchPattern =
                        @"(?<open>\d+([\.,]\d+)?)" + columnSeparator +
                        @"(?<high>\d+([\.,]\d+)?)" + columnSeparator +
                        @"(?<low>\d+([\.,]\d+)?)"  + columnSeparator +
                        @"(?<close>\d+([\.,]\d+)?)";
                    break;
                }
                if (price3 > price2 + epsilon)
                {
                    priceMatchPattern =
                        @"(?<open>\d+([\.,]\d+)?)" + columnSeparator +
                        @"(?<low>\d+([\.,]\d+)?)"  + columnSeparator +
                        @"(?<high>\d+([\.,]\d+)?)" + columnSeparator +
                        @"(?<close>\d+([\.,]\d+)?)";
                    break;
                }
            }
            sr.Close();

            if (priceMatchPattern == "")
                throw new Exception(Language.T("Could not determine the price columns order!"));

            return priceMatchPattern;
        }

        /// <summary>
        /// Counts the valid data lines.
        /// </summary>
        /// <param name="dataFile">The data file.</param>
        /// <param name="regexDataFile">The data file regex.</param>
        /// <returns>Count of matched lines as bars.</returns>
        private static int CountDataBars(string dataFile, Regex regexDataFile)
        {
            string line;
            var bars = 0;
            var stringReader = new StringReader(dataFile);
            while ((line = stringReader.ReadLine()) != null)
                if (regexDataFile.IsMatch(line))
                    bars++;
            stringReader.Close();

            if (bars == 0)
                throw new Exception(Language.T("Could not count the data bars!"));

            return bars;
        }

        /// <summary>
        /// Parses the input data file.
        /// </summary>
        /// <param name="dataFile">The data file as string.</param>
        /// <param name="regexDataFile">The compiled regex.</param>
        /// <param name="barsCount">The count of bars of the data file.</param>
        /// <returns>Returns a parsed bar array.</returns>
        private Bar[] ParseInput(string dataFile, Regex regexDataFile, int barsCount)
        {
            var barList = new Bar[barsCount];

            string line;
            var bar = 0;
            var stringReader = new StringReader(dataFile);

            while ((line = stringReader.ReadLine()) != null)
            {
                var match = regexDataFile.Match(line);
                if (!match.Success) continue;

                var year = int.Parse(match.Groups["year"].Value);
                year = CorrectProblemYear2000(year);
                var month = int.Parse(match.Groups["month"].Value);
                var day = int.Parse(match.Groups["day"].Value);
                var hour = int.Parse(match.Groups["hour"].Value);
                var min = int.Parse(match.Groups["min"].Value);
                var seconds = match.Groups["sec"].Value;
                var sec = (seconds == "" ? 0 : int.Parse(seconds));

                barList[bar].Time   = new DateTime(year, month, day, hour, min, sec);
                barList[bar].Open   = ParseDouble(match.Groups["open"].Value);
                barList[bar].High   = ParseDouble(match.Groups["high"].Value);
                barList[bar].Low    = ParseDouble(match.Groups["low"].Value);
                barList[bar].Close  = ParseDouble(match.Groups["close"].Value);
                barList[bar].Volume = int.Parse(match.Groups["volume"].Value);

                bar++;
            }

            stringReader.Close();

            return barList;
        }

        /// <summary>
        /// Parses a value as double from a string.
        /// </summary>
        /// <param name="input">String to parse.</param>
        /// <returns>A value as double.</returns>
        private double ParseDouble(string input)
        {
            var separator = NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;

            if (separator == "." && input.Contains(","))
                input = input.Replace(",", separator);
            else if (separator == "," && input.Contains("."))
                input = input.Replace(".", separator);

            return double.Parse(input);
        }

        /// <summary>
        /// Fixes wrong year interpretation. 
        /// For example 08 must be 2008 instead of 8.
        /// </summary>
        private int CorrectProblemYear2000(int year)
        {
            if (year < 100)
                year += 2000;
            if (year > DateTime.Now.Year)
                year -= 100;
            return year;
        }
    }
}

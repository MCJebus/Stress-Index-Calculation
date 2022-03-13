using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

using DataInputFormats;

namespace StressCalculator
{
    class StressCalculator
    {
        static void Main(string[] args)
        {
            //Filepath to read from.
            string filePath = "RR.csv";

            /* List of RR Intervals with custom class to hold timestamp and RR Interval.
                Dictionary could not be used as timestamps are not unique in csv files. */
            List<StressDataEntry> rrList = DataRead(filePath);

            StringBuilder sb = new StringBuilder();
            //StressIndexes holds each 30 second timestamp and its stress index.
            Dictionary<DateTime, double> StressIndexes = DataManipulation(rrList, sb);

            DataWrite(StressIndexes, sb);

            Console.WriteLine("Stress Indexes have been calculated.");
            Console.ReadKey();

        }

        static List<StressDataEntry> DataRead(string filePath) {

            List<StressDataEntry> rrList = new List<StressDataEntry>();

            /*These are all the formats that the timestamps come in, f for milliseconds has to be exact which is why 3 different
              formats are needed. */
            string[] timestampFormats = {"yyyy:M:d:H:m:s:f", "yyyy:M:d:H:m:s:ff", "yyyy:M:d:H:m:s:fff"};

            //Block to read all data from RR.csv and add each entry to rrList.
            string[] lines = System.IO.File.ReadAllLines(filePath);
            foreach(string line in lines)
            {
                string[] columns = line.Split(',');

                StressDataEntry record = new StressDataEntry();

                DateTime timestamp;
                System.DateTime.TryParseExact(columns[0], timestampFormats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out timestamp);
                record.Timestamp = timestamp;

                double rrInterval = 0;
                Double.TryParse(columns[1], out rrInterval);
                record.RrInterval = rrInterval;

                rrList.Add(record);

            }

            return rrList;

        }

        static Dictionary<DateTime, double> DataManipulation(List<StressDataEntry> rrList, StringBuilder sb) {

            //minstamp is second in array because first is header ("time,rr"). 
            DateTime minStamp = rrList[1].Timestamp;
            DateTime maxStamp = rrList[(rrList.Count-1)].Timestamp;

            //Tempstamp is temporary variable to account for 30 second intervals.
            DateTime tempstamp = minStamp;
            double maxRR = rrList[1].RrInterval;
            double minRR = rrList[1].RrInterval;
            //RRCounter holds each unique RR Interval and its frequency.
            Dictionary<double, int> RRCounter = new Dictionary<double, int>();
            Dictionary<DateTime, double> StressIndexes = new Dictionary<DateTime, double>();

            //Loops through all entrys in list read from RR.csv.
            for (int i = 1; i < rrList.Count; i++) {

                /*Condition to check whether the current record is in the same 30 second interval,
                if not it starts next 30 second interval data and outputs current interval data.*/
                if (rrList[i].Timestamp <= tempstamp) {

                    //Does no work if RR interval is 0 for efficiency.
                    if (rrList[i].RrInterval == 0) {

                    } else {
                        
                        /*If there is an entry for that RR interval then it increments its frequency by 1
                          or just creates a new entry for it. */
                        if (RRCounter.ContainsKey(rrList[i].RrInterval)) {
                            RRCounter[rrList[i].RrInterval] = RRCounter[rrList[i].RrInterval] + 1;
                        } else {
                            RRCounter.Add(rrList[i].RrInterval, 1);
                        }

                        //Determines max and min RR interval for this 30 second interval.
                        if (rrList[i].RrInterval > maxRR) {
                            maxRR = rrList[i].RrInterval;
                        } else if (rrList[i].RrInterval < minRR) {
                            minRR = rrList[i].RrInterval;
                        }
                    }

                } else if (rrList[i].Timestamp <= maxStamp) {

                    //Stress index calculation done here then added to list once 30 seconds is up.
                    double variationScope = maxRR - minRR;
                    double mode = 0;
                    int modeFrequency = 0;
                    //Determines mode in dictionary.
                    foreach (KeyValuePair<double, int> kvp in RRCounter) {
                        if (kvp.Value >= modeFrequency) {
                            mode = kvp.Key;
                            modeFrequency = kvp.Value;
                        }
                    }
                    double modeAmplitude = ((double)modeFrequency / rrList.Count);
                    double RawStressIndex;
                    if (variationScope == 0) {
                        RawStressIndex = 0;
                    } else {
                        RawStressIndex = (double)modeAmplitude / (2 * mode * variationScope);
                    }
                    double readableStressIndex = (double)RawStressIndex * 1000000 * 2.5;

                    //Block to output variables to console, used for debugging to check whether variables are correct.
                    Console.WriteLine(rrList[i].Timestamp);
                    Console.WriteLine("Mode: " + mode);
                    Console.WriteLine("Mode Frequency: " + modeFrequency);
                    Console.WriteLine("Mode Amplitude: " + modeAmplitude);
                    Console.WriteLine("Raw: " + RawStressIndex);
                    Console.WriteLine("Stress Index: " + readableStressIndex);
                    Console.WriteLine(rrList.Count);
                    Console.WriteLine();

                    //At this point the time stamp and the stress index are found and can be stored for writing later.
                    StressIndexes.Add(tempstamp, readableStressIndex);

                    //Writing to StressIndexesExtra.csv with all information in correct formats.
                    //File.AppendAllText(stressIndexExtraPath, tempstamp.ToString("yyyy:M:d:H:m:s:fff") + "," + mode.ToString() + ","
                    //    + modeFrequency.ToString() + "," + modeAmplitude.ToString() + "," + RawStressIndex.ToString() + "," 
                    //    + readableStressIndex.ToString() + "\r\n");

                     sb.Append(tempstamp.ToString("yyyy:M:d:H:m:s:fff") + "," + mode.ToString() + ","
                        + modeFrequency.ToString() + "," + modeAmplitude.ToString() + "," + RawStressIndex.ToString() + "," 
                        + readableStressIndex.ToString() + "\r\n");                   

                    //Resetting max and min values aswell as dictionary at end of 30 second interval.
                    maxRR = rrList[i].RrInterval;
                    minRR = rrList[i].RrInterval;
                    RRCounter.Clear();
                    tempstamp = tempstamp.AddSeconds(30);
                }
            }

            return StressIndexes;

        }
         static void DataWrite(Dictionary<DateTime, double> StressIndexes, StringBuilder sb) {

            //Filepaths to write to.
            string stressIndexPath = "StressIndexes.csv";
            string stressIndexExtraPath = "StressIndexesExtra.csv";

            //Writing to StressIndexes.csv using StressIndexes dictionary and in correct formats.
            using (var writer = new StreamWriter(@stressIndexPath)) {
                writer.WriteLine("Time,Stress Index");
                foreach (KeyValuePair<DateTime, double> pair in StressIndexes) {
                    writer.WriteLine(pair.Key.ToString("yyyy:M:d:H:m:s:fff") + "," + pair.Value.ToString("0.00"));
                }
            }
            using (var writer = new StreamWriter(@stressIndexExtraPath)) {
                writer.WriteLine("Timestamp,Mode,Mode Frequency,Mode Amplitude,Raw,Stress Index");
                writer.WriteLine(sb.ToString());
            }
    }
    
    }

}

//dotnet publish --configuration Release --self-contained --runtime win10-x64 /p:PublishSingleFile=True /p:PublishTrimmed=True /p:IncludeAllContentForSelfExtract=True
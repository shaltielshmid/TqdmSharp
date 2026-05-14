using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using TqdmSharp;

namespace TqdmSharp.Tests
{
    public class ProgressBarTests
    {
        /// <summary>
        /// Tests that Step() correctly increments progress without skipping values.
        /// This is a regression test for issue #2 where calling Step() three times
        /// on a progress bar with total=3 would show 0/3 => 1/3 => 3/3 instead of
        /// 0/3 => 1/3 => 2/3 => 3/3.
        /// </summary>
        [Fact]
        public void Step_ShouldNotSkipProgressValues()
        {
            // Capture console output
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            var bar = new Tqdm.ProgressBar(total: 3, useColor: false, printsPerSecond: 100);
            
            // Track all progress values shown
            var progressValues = new List<int>();
            
            // First Step: -1 -> 0
            bar.Step();
            string output1 = consoleOutput.ToString();
            if (output1.Contains("[ 0 / 3")) progressValues.Add(0);
            
            // Second Step: 0 -> 1
            consoleOutput.GetStringBuilder().Clear();
            bar.Step();
            string output2 = consoleOutput.ToString();
            if (output2.Contains("[ 1 / 3")) progressValues.Add(1);
            
            // Third Step: 1 -> 2
            consoleOutput.GetStringBuilder().Clear();
            bar.Step();
            string output3 = consoleOutput.ToString();
            if (output3.Contains("[ 2 / 3")) progressValues.Add(2);
            
            // Finish to complete the progress bar
            consoleOutput.GetStringBuilder().Clear();
            bar.Finish();
            string output4 = consoleOutput.ToString();
            if (output4.Contains("[ 3 / 3")) progressValues.Add(3);
            
            // Reset console output
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            
            // Assert that all progress values are present and in order
            Assert.Equal(4, progressValues.Count);
            Assert.Equal(0, progressValues[0]);
            Assert.Equal(1, progressValues[1]);
            Assert.Equal(2, progressValues[2]);
            Assert.Equal(3, progressValues[3]);
        }

        /// <summary>
        /// Tests that Progress() correctly displays the specified progress value.
        /// This is a regression test for issue #2 where Progress(2) on a bar with
        /// total=3 would display 3/3 instead of 2/3.
        /// </summary>
        [Fact]
        public void Progress_ShouldDisplayCorrectValue()
        {
            // Capture console output
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            var bar = new Tqdm.ProgressBar(total: 3, useColor: false, printsPerSecond: 100);
            
            // Progress(0) should show 0/3
            bar.Progress(0);
            string output0 = consoleOutput.ToString();
            Assert.Contains("[ 0 / 3", output0);
            
            // Progress(1) should show 1/3
            consoleOutput.GetStringBuilder().Clear();
            bar.Progress(1);
            string output1 = consoleOutput.ToString();
            Assert.Contains("[ 1 / 3", output1);
            
            // Progress(2) should show 2/3, NOT 3/3 (the bug was here)
            consoleOutput.GetStringBuilder().Clear();
            bar.Progress(2);
            string output2 = consoleOutput.ToString();
            Assert.Contains("[ 2 / 3", output2);
            Assert.DoesNotContain("[ 3 / 3", output2);
            
            // Reset console output
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        }

        /// <summary>
        /// Tests that Progress(total) shows 100% completion correctly.
        /// </summary>
        [Fact]
        public void Progress_AtTotal_ShouldShow100Percent()
        {
            // Capture console output
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            var bar = new Tqdm.ProgressBar(total: 3, useColor: false, printsPerSecond: 100);
            
            // Progress(3) on a bar with total=3 should show 100%
            bar.Progress(3);
            string output = consoleOutput.ToString();
            Assert.Contains("100", output);
            Assert.Contains("[ 3 / 3", output);
            
            // Reset console output
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        }

        /// <summary>
        /// Tests the Wrap function to ensure it displays progress correctly for collections.
        /// </summary>
        [Fact]
        public void Wrap_ShouldShowCorrectProgressForEachItem()
        {
            // Capture console output
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            var collection = new List<int> { 10, 20, 30 };
            var results = new List<int>();
            
            foreach (var item in Tqdm.Wrap(collection, printsPerSecond: 100)) {
                results.Add(item);
            }
            
            string output = consoleOutput.ToString();
            
            // All items should have been processed
            Assert.Equal(3, results.Count);
            Assert.Equal(10, results[0]);
            Assert.Equal(20, results[1]);
            Assert.Equal(30, results[2]);
            
            // The output should show progress for 0, 1, 2 items processed
            Assert.Contains("[ 0 / 3", output);
            Assert.Contains("[ 1 / 3", output);
            Assert.Contains("[ 2 / 3", output);
            
            // Reset console output
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        }
    }
}

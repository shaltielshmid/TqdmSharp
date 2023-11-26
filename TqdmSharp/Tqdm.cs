using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace TqdmSharp {
    /// <summary>
    /// The Tqdm class offers utility functions to wrap collections and enumerables with a ProgressBar, 
    /// providing a simple and effective way to track and display progress in console applications for various iterative operations.
    /// </summary>
    public static class Tqdm {
        /// <summary>
        /// The ProgressBar class offers a customizable console progress bar for tracking and displaying the progress of iterative tasks. 
        /// It features various themes, real-time updates, and supports both simple and exponential moving average rate calculations.
        /// </summary>
        public class ProgressBar {
            // Parameterized configuration
            private readonly bool _useExponentialMovingAverage;
            private readonly double _alpha;
            private readonly int _width;
            private readonly int _printsPerSecond;
            private readonly bool _useColor;
            // Total is set initially, but can be dynamically updated with each step
            private int _total;
            private int _current;

            // State
            private readonly Stopwatch _stopWatch;
            private readonly List<double> _timeDeque;
            private readonly List<int> _iterDeque;
            private DateTime _startTime;
            private DateTime _prevTime;
            private int _nUpdates = 0;

            // Dynamic configuration that updates as progress applies
            private int _period = 1;
            private int _smoothCount = 50;
            private int _prevIterations = 0;
            private int _prevLength = 0;
            
            // Theme and output
            private readonly string _rightPad = "|";
            private char[] _themeBars;
            private string _label = "";

            /// <summary>
            /// Initializes a new instance of the ProgressBar class.
            /// </summary>
            /// <param name="useExpMovingAvg">Whether to use exponential moving average for rate calculation.</param>
            /// <param name="alpha">The smoothing factor for exponential moving average.</param>
            /// <param name="total">The total number of iterations expected.</param>
            /// <param name="width">The width of the progress bar in characters.</param>
            /// <param name="printsPerSecond">The estimated number of updates to the progress bar per second.</param>
            /// <param name="useColor">Indicates whether to use colored output for the progress bar.</param>
            /// <remarks>The prints per second is not an absolute number, and gets constantly tuned as the process progresses.</remarks>
            public ProgressBar(bool useExpMovingAvg = true, double alpha = 0.1, int total = -1, int width = 40, int printsPerSecond = 10, bool useColor = false) {
                // Update the state
                _stopWatch = Stopwatch.StartNew();
                _startTime = DateTime.Now;
                _prevTime = DateTime.Now;
                _timeDeque = new();
                _iterDeque = new();

                _useExponentialMovingAverage = useExpMovingAvg;
                _alpha = alpha;
                _total = total;
                _width = width;
                _printsPerSecond = printsPerSecond;
                _useColor = useColor;

                SetThemeAscii();

                ArgumentNullException.ThrowIfNull(_themeBars);
            }

            /// <summary>
            /// Resets the progress bar to its initial state.
            /// </summary>
            public void Reset() {
                // Reset counters
                _stopWatch.Reset();
                _startTime = DateTime.Now;
                _prevTime = DateTime.Now;
                _prevIterations = 0;

                // Clear histories
                _timeDeque.Clear();
                _iterDeque.Clear();

                // Reset config
                _period = 1;
                _nUpdates = 0;
                _current = 0;
                _total = 0;
                _label = "";
                _prevLength = 0;
            }

            /// <summary>
            /// Sets a label that appears at the end of the progress bar.
            /// </summary>
            /// <param name="text">The label text.</param>
            public void SetLabel(string text) {
                _label = text;
            }

            /// <summary>
            /// Sets the progress bar theme to basic characters of spaces + '#'.
            /// </summary>
            public void SetThemeBasic() {
                _themeBars = new[] { ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', '#' };
            }

            /// <summary>
            /// Sets the progress bar theme to ASCII characters.
            /// </summary>
            public void SetThemeAscii() {
                _themeBars = new[] { ' ', '.', ':', '-', '=', '≡', '#', '█', '█' };
            }

            /// <summary>
            /// Sets the progress bar theme to Python-style characters.
            /// </summary>
            public void SetThemePython() {
                _themeBars = Enumerable.Range(0x2587, 0x258F - 0x2587).Reverse().Select(i => (char)i).Prepend(' ').ToArray();
            }

            /// <summary>
            /// Sets the progress bar theme using a custom array of characters.
            /// </summary>
            /// <param name="bars">An array of characters to use in the progress bar. Must be exactly 9 characters.</param>
            public void SetTheme(char[] bars) {
                if (bars.Length != 9)
                    throw new ArgumentException("Must contain exactly 9 characters.", nameof(bars));
                _themeBars = bars;
            }

            /// <summary>
            /// Finalizes the progress bar display.
            /// </summary>
            public void Finish() {
                Progress(_total, _total);
                Console.WriteLine();
            }

            /// <summary>
            /// Updates the progress bar with the current progress and total.
            /// </summary>
            /// <param name="current">The current progress.</param>
            /// <param name="total">The total number of iterations expected. This will update the internal total counter given in the constructor.</param>
            public void Progress(int current, int total) {
                this._total = total;
                Progress(current);
            }

            /// <summary>
            /// Updates the progress bar with the current progress.
            /// </summary>
            /// <param name="current">The current progress.</param>
            public void Progress(int current) {
                this._current = current;
                // _period is a number which is contantly tuned based on how often there are updates,
                // to try and update the screen ~N times a second (parameter)
                if (current % _period != 0) return;

                _nUpdates++;

                // Figure out our current state - how long passed and how many iterations since the last update
                double elapsed = _stopWatch.Elapsed.TotalSeconds;
                int iterations = current - _prevIterations;
                _prevIterations = current;

                DateTime now = DateTime.Now;
                double timeDiff = (now - _prevTime).TotalSeconds;
                _prevTime = now;

                // In order to make the timing smoother, we average over the last N items (adjusted on the go)
                // If we passed that number, just pop the top item
                if (_timeDeque.Count >= _smoothCount) _timeDeque.RemoveAt(0);
                if (_iterDeque.Count >= _smoothCount) _iterDeque.RemoveAt(0);
                _timeDeque.Add(timeDiff);
                _iterDeque.Add(iterations);

                // Calculate the average rate of progress either as a simple progress / time or as an exponential
                // moving average, with alpha as a parameter in the constructor. 
                double avgRate;
                if (_useExponentialMovingAverage) {
                    avgRate = _iterDeque[0] / _timeDeque[0];
                    for (int i = 1; i < _iterDeque.Count; i++) {
                        double r = _iterDeque[i] / _timeDeque[i];
                        avgRate = _alpha * r + (1 - _alpha) * avgRate;
                    }
                }
                else {
                    double totalTime = _timeDeque.Sum();
                    int totalIters = _iterDeque.Sum();
                    avgRate = totalIters / totalTime;
                }

                // Auto-tune the period to try and only print N times a second.
                if (_nUpdates > 10) {
                    _period = Math.Min(Math.Max(1, (int)(current / elapsed / _printsPerSecond)), 500000);
                    _smoothCount = 25 * 3;
                }

                // Calculate how much time remains and what percentage are we along
                double remaining = (_total - current) / avgRate;
                double percent = (current * 100.0) / _total;
                
                // If this will be the last update, then print a completed progress bar. 
                if (_total - current <= _period) {
                    percent = 100;
                    current = _total;
                    remaining = 0;
                }

                // Count what percentage of the bar we are printing. 
                // Keep both a double and an int so we can calculate the relative remainder. 
                double fills = (double)current / _total * _width;
                int ifills = (int)fills;

                // Store the beginning of the line, so we can move back there for the next print
                int curCursorTop = Console.CursorTop;

                // Build our output string
                // Start by typing "backspace" over the previous print, and then add a \r in case anything was added. 
                var sb = new StringBuilder();
                
                // Append the total number of filled bars
                if (_useColor) sb.Append("\u001b[32m ");
                sb.Append(new string(_themeBars[8], ifills));
                // If we aren't at the end, append the partial bar
                if (current != _total) 
                    sb.Append(_themeBars[(int)(8 * (fills - ifills))]);

                // Append the filler spaces and the padding on the right
                sb.Append(new string(_themeBars[0], _width - ifills));
                sb.Append(_rightPad);
                if (_useColor) sb.Append("\u001b[1m\u001b[31m");

                // Print the percent with 1 fixed point, followed by the current / total numbers, and the elapsed/remaining time
                sb.Append($" {percent:F1}% ");
                if (_useColor) sb.Append("\u001b[34m");
                sb.Append($"[ {current:N0} / {_total:N0} | ");
                sb.Append($"{elapsed:F0}s < {remaining:F0}s ] ");

                // Finally, if there is one, print the label
                sb.Append(_label);
                sb.Append(' ');
                if (_useColor) sb.Append("\u001b[0m\u001b[32m\u001b[0m ");
                Console.Write(sb.ToString() + new string(' ', Math.Max(0, _prevLength - sb.Length)));

                // Move the cursor position back
                Console.SetCursorPosition(0, curCursorTop);

                // Store the length of the string so that we can clear it later
                _prevLength = sb.Length;
            }

            /// <summary>
            /// Advances the progress bar by one step, automatically updating the progress count internally.
            /// This method is useful for scenarios where the progress is incremented in a regular manner
            /// and eliminates the need for external progress tracking.
            /// </summary>
            public void Step() {
                Progress(_current + 1);
            }

            /// <summary>
            /// Clears the previous console line and prints a new line, ensuring subsequent prints appear on the next line.
            /// </summary>
            /// <param name="text">The text to be printed on the new line.</param>
            public void PrintLine(string text) {
                // Clear the previous line by resetting the cursor position and overwriting with spaces
                Console.Write(new string(' ', Math.Min(_prevLength, Console.BufferWidth - 1))); // Clear the buffer
                Console.CursorLeft = 0;
                
                // Print the new line of text
                Console.WriteLine(text);
                _prevLength = 0;
            }
        }

        /// <summary>
        /// Wraps a collection with a progress bar for iteration, providing visual feedback on progress.
        /// </summary>
        /// <param name="collection">The collection to iterate over.</param>
        /// <param name="width">The width of the progress bar.</param>
        /// <param name="printsPerSecond">The update frequency of the progress bar.</param>
        /// <param name="useColor">Indicates whether to use colored output for the progress bar.</param>
        /// <returns>An enumerable that iterates over the collection with progress tracking.</returns> 
        public static IEnumerable<T> Wrap<T>(ICollection<T> collection, int width = 40, int printsPerSecond = 10, bool useColor = false) {
            return Tqdm.Wrap(collection, out var _, width, printsPerSecond, useColor);
        }

        /// <summary>
        /// Wraps an enumerable with a specified total count with a progress bar for iteration, providing visual feedback on progress.
        /// </summary>
        /// <param name="enumerable">The enumerable to iterate over.</param>
        /// <param name="total">The total number of expected items in the enumerable.</param>
        /// <param name="width">The width of the progress bar.</param>
        /// <param name="printsPerSecond">The update frequency of the progress bar.</param>
        /// <param name="useColor">Indicates whether to use colored output for the progress bar.</param>
        /// <returns>An enumerable that iterates over the collection with progress tracking.</returns>
        public static IEnumerable<T> Wrap<T>(IEnumerable<T> enumerable, int total, int width = 40, int printsPerSecond = 10, bool useColor = false) {
            return Tqdm.Wrap(enumerable, total, out var _, width, printsPerSecond, useColor);
        }

        /// <summary>
        /// Wraps a collection with a progress bar for iteration and provides the progress bar instance for external control (like custom labels).
        /// </summary>
        /// <param name="collection">The collection to iterate over.</param>
        /// <param name="bar">The progress bar instance used for tracking.</param>
        /// <param name="width">The width of the progress bar.</param>
        /// <param name="printsPerSecond">The update frequency of the progress bar.</param>
        /// <param name="useColor">Indicates whether to use colored output for the progress bar.</param>
        /// <returns>An enumerable that iterates over the collection with progress tracking.</returns>
        public static IEnumerable<T> Wrap<T>(ICollection<T> collection, out ProgressBar bar, int width = 40, int printsPerSecond = 10, bool useColor = false) {
            int total = collection.Count;
            return Tqdm.Wrap(collection, total, out bar, width, printsPerSecond,useColor);
        }

        /// <summary>
        /// Wraps an enumerable with a specified total count with a progress bar for iteration and provides the progress bar instance for external control (like custom labels).
        /// </summary>
        /// <param name="enumerable">The enumerable to iterate over.</param>
        /// <param name="total">The total number of items in the enumerable.</param>
        /// <param name="bar">The progress bar instance used for tracking.</param>
        /// <param name="width">The width of the progress bar.</param>
        /// <param name="printsPerSecond">The update frequency of the progress bar.</param>
        /// <param name="useColor">Indicates whether to use colored output for the progress bar.</param>
        /// <returns>An enumerable that iterates over the collection with progress tracking.</returns>
        public static IEnumerable<T> Wrap<T>(IEnumerable<T> enumerable, int total, out ProgressBar bar, int width = 40, int printsPerSecond = 10, bool useColor = false) {
            bar = new ProgressBar(total: total, width: width, printsPerSecond: printsPerSecond, useColor: useColor);
            return InternalWrap(enumerable, total, bar);
        }

        private static IEnumerable<T> InternalWrap<T>(IEnumerable<T> enumerable, int total, ProgressBar bar) {
            int count = 0;
            foreach (var item in enumerable) {
                bar.Progress(count, total);
                count++;
                yield return item;
            }
            bar.Finish();
        }
    }
}
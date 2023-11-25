# TqdmSharp

[![NuGet](https://img.shields.io/nuget/v/TqdmSharp.svg)](https://www.nuget.org/packages/TqdmSharp/)

TqdmSharp is a C# implementation of the tqdm progress bar, providing an easy-to-use and visually appealing way to track progress in console applications. Inspired by the [cpptqdm](https://github.com/aminnj/cpptqdm) project, we extend our gratitude to its creator for the inspiration and groundwork laid out in the original C++ implementation.

## Features

- Simple API for wrapping collections and enumerables.
- Customizable progress bar themes.
- Real-time progress updates.
- Supports exponential moving average for rate calculation.
- Adjustable width and update frequency.

## Installation

### .NET CLI
```bash
dotnet add package MinHashSharp
```

### NuGet Package Manager
```powershell
Install-Package MinHashSharp
```

## Usage Examples

### Standard Use
Easily wrap a collection for progress tracking. Ideal for quick integration with existing collections:
```csharp
var arr = Enumerable.Range(0, 100).ToArray();
foreach (int item in Tqdm.Wrap(arr)) {
    // Perform your operations here
}
```

### Adding a Label to the Progress Bar
Enhance clarity by adding a descriptive label to the progress bar, useful for distinguishing different stages or types of iterations:
```csharp
var arr = Enumerable.Range(0, 100).ToArray();
foreach (int item in Tqdm.Wrap(arr, out var bar)) {
    bar.SetLabel("Processing stage");
    // Your processing logic here
}
```

### Specifying Enumerable with a Count
When working with enumerables where the total count is known, provide it directly to track progress accurately:
```csharp
var enumerable = Enumerable.Range(0, 100);
foreach (int item in Tqdm.Wrap(enumerable, 100)) {
    // Iteration tasks here
}
```

### Basic Usage of the ProgressBar Class
Directly use the ProgressBar class for more control and customization over the progress tracking:
```csharp
int count = 10;
var bar = new Tqdm.ProgressBar(total: count);
for (int i = 0; i < count; i++) {
    bar.Progress(i);
    // Custom operation
}
```

### Dynamically Updating the Total Counter
Modify the total counter as your operation progresses, useful for processes with a variable number of iterations:
```csharp
int count = 10;
var bar = new Tqdm.ProgressBar(total: count);
for (int i = 0; i < count; i++) {
    if (i % 7 == 0) count *= 2;
    bar.Progress(i, count);
    // Adjusted operation based on new count
}
```

### Exploring Additional Parameters

TqdmSharp offers additional parameters for fine-tuning the progress bar's behavior, including the ability to use exponential moving average for rate calculation, adjusting the width of the progress bar, and setting the update frequency for real-time progress updates.

## Demonstration

To showcase the functionality and visual appeal of TqdmSharp, here's a simple demonstration using the `Wrap` method with color enhancement. In this example, we iterate over a range of numbers, with each iteration pausing briefly to simulate a longer-running process. The `useColor: true` parameter adds a vibrant touch to the progress bar, making it more visually engaging. Observe how the progress bar updates in real-time with each iteration:

```cs
var arr = Enumerable.Range(0, 100).ToArray();
foreach (int item in Tqdm.Wrap(arr, useColor: true)) {
    // do something to not complete instantly
    Thread.Sleep(50);
}
```
![](images/example.gif)

## Acknowledgements

Special thanks to the [cpptqdm](https://github.com/aminnj/cpptqdm) project for inspiring the design and functionality of TqdmSharp.

## Contributions

We warmly welcome contributions to TqdmSharp! Whether you're fixing bugs, improving the documentation, or adding new features, your help makes this project better for everyone.

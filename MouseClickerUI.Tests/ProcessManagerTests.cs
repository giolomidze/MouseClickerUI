using MouseClickerUI.Services;

namespace MouseClickerUI.Tests;

public class ProcessManagerTests
{
    [Fact]
    public void LoadProcesses_FirstCall_ReturnsProcessList()
    {
        // Arrange
        var processes = new IProcessData[]
        {
            new MockProcessData("notepad", "Untitled - Notepad", 1234),
            new MockProcessData("chrome", "Google Chrome", 5678)
        };
        var processEnumerator = new TestProcessEnumerator(processes);
        var manager = new ProcessManager(processEnumerator);

        // Act
        var result = manager.LoadProcesses();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("chrome", result[0].ProcessName); // Ordered alphabetically
        Assert.Equal("notepad", result[1].ProcessName);
    }

    [Fact]
    public void LoadProcesses_UnchangedProcesses_ReturnsEmptyList()
    {
        // Arrange
        var processes = new IProcessData[]
        {
            new MockProcessData("notepad", "Untitled - Notepad", 1234),
            new MockProcessData("chrome", "Google Chrome", 5678)
        };
        var processEnumerator = new TestProcessEnumerator(processes);
        var manager = new ProcessManager(processEnumerator);

        // First call to populate cache
        manager.LoadProcesses();

        // Act - Second call with same processes
        var result = manager.LoadProcesses();

        // Assert
        Assert.Empty(result); // Should return empty when cache matches
    }

    [Fact]
    public void LoadProcesses_ChangedProcesses_ReturnsNewList()
    {
        // Arrange
        var initialProcesses = new IProcessData[]
        {
            new MockProcessData("notepad", "Untitled - Notepad", 1234)
        };
        var processEnumerator = new TestProcessEnumerator(initialProcesses);
        var manager = new ProcessManager(processEnumerator);

        // First call to populate cache
        manager.LoadProcesses();

        // Act - Update process list
        var newProcesses = new IProcessData[]
        {
            new MockProcessData("notepad", "Untitled - Notepad", 1234),
            new MockProcessData("chrome", "Google Chrome", 5678) // New process added
        };
        processEnumerator.SetProcesses(newProcesses);

        var result = manager.LoadProcesses();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.ProcessName == "chrome");
    }

    [Fact]
    public void ClearCache_AfterClear_ForcesFreshLoad()
    {
        // Arrange
        var processes = new IProcessData[]
        {
            new MockProcessData("notepad", "Untitled - Notepad", 1234)
        };
        var processEnumerator = new TestProcessEnumerator(processes);
        var manager = new ProcessManager(processEnumerator);

        // First call to populate cache
        manager.LoadProcesses();

        // Act
        manager.ClearCache();
        var result = manager.LoadProcesses();

        // Assert
        Assert.Single(result); // Should return data after cache clear
    }

    [Fact]
    public void LoadProcesses_FiltersProcessesWithoutWindowTitle()
    {
        // Arrange
        var processes = new IProcessData[]
        {
            new MockProcessData("notepad", "Untitled - Notepad", 1234),
            new MockProcessData("svchost", "", 5678), // No window title
            new MockProcessData("chrome", "Google Chrome", 9012)
        };
        var processEnumerator = new TestProcessEnumerator(processes);
        var manager = new ProcessManager(processEnumerator);

        // Act
        var result = manager.LoadProcesses();

        // Assert
        Assert.Equal(2, result.Count); // Should only include processes with window titles
        Assert.DoesNotContain(result, p => p.ProcessName == "svchost");
    }

    [Fact]
    public void LoadProcesses_OrdersProcessesByName()
    {
        // Arrange
        var processes = new IProcessData[]
        {
            new MockProcessData("zzzlast", "Last Process", 1234),
            new MockProcessData("aaafirst", "First Process", 5678),
            new MockProcessData("mmmiddle", "Middle Process", 9012)
        };
        var processEnumerator = new TestProcessEnumerator(processes);
        var manager = new ProcessManager(processEnumerator);

        // Act
        var result = manager.LoadProcesses();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("aaafirst", result[0].ProcessName);
        Assert.Equal("mmmiddle", result[1].ProcessName);
        Assert.Equal("zzzlast", result[2].ProcessName);
    }

    /// <summary>
    /// Test implementation of IProcessEnumerator that returns mock process data.
    /// </summary>
    private class TestProcessEnumerator : IProcessEnumerator
    {
        private IProcessData[] _processes;

        public TestProcessEnumerator(IProcessData[] processes)
        {
            _processes = processes;
        }

        public void SetProcesses(IProcessData[] processes)
        {
            _processes = processes;
        }

        public IProcessData[] GetProcesses()
        {
            return _processes;
        }
    }

    /// <summary>
    /// Mock implementation of IProcessData for testing.
    /// </summary>
    private class MockProcessData : IProcessData
    {
        public string ProcessName { get; }
        public string MainWindowTitle { get; }
        public int Id { get; }
        public IntPtr MainWindowHandle { get; }

        public MockProcessData(string processName, string mainWindowTitle, int id)
        {
            ProcessName = processName;
            MainWindowTitle = mainWindowTitle;
            Id = id;
            MainWindowHandle = new IntPtr(id); // Use ID as handle for simplicity
        }

        public void Dispose()
        {
            // Nothing to dispose in mock
        }
    }
}

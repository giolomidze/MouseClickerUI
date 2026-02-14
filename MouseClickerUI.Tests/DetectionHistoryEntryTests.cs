using MouseClickerUI.Models;

namespace MouseClickerUI.Tests;

public class DetectionHistoryEntryTests
{
    [Fact]
    public void DisplayName_WithWindowTitle_ReturnsProcessNameDashWindowTitle()
    {
        // Arrange
        var entry = new DetectionHistoryEntry
        {
            ProcessName = "notepad",
            WindowTitle = "Untitled - Notepad"
        };

        // Act & Assert
        Assert.Equal("notepad - Untitled - Notepad", entry.DisplayName);
    }

    [Fact]
    public void DisplayName_EmptyWindowTitle_ReturnsProcessNameOnly()
    {
        // Arrange
        var entry = new DetectionHistoryEntry
        {
            ProcessName = "notepad",
            WindowTitle = ""
        };

        // Act & Assert
        Assert.Equal("notepad", entry.DisplayName);
    }

    [Fact]
    public void DisplayName_WhitespaceWindowTitle_ReturnsProcessNameOnly()
    {
        // Arrange
        var entry = new DetectionHistoryEntry
        {
            ProcessName = "notepad",
            WindowTitle = "   "
        };

        // Act & Assert
        Assert.Equal("notepad", entry.DisplayName);
    }
}

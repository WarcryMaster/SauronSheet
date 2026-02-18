using Xunit;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Domain.Tests.Entities;

public class ImportBatchTests
{
    [Fact]
    [Trait("Category", "Domain")]
    public void ImportBatch_ValidConstruction_SetsProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var filename = "bank.pdf";
        var importedCount = 5;
        var skippedCount = 2;
        var importedAt = DateTime.UtcNow;

        // Act
        var batch = new ImportBatch(id, filename, importedCount, skippedCount, importedAt);

        // Assert
        Assert.Equal(id, batch.Id);
        Assert.Equal(filename, batch.Filename);
        Assert.Equal(importedCount, batch.ImportedCount);
        Assert.Equal(skippedCount, batch.SkippedCount);
        Assert.Equal(importedAt, batch.ImportedAt);
        Assert.Equal(7, batch.TotalProcessed);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void ImportBatch_EmptyFilename_ThrowsDomainException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            new ImportBatch(Guid.NewGuid(), "", 5, 2, DateTime.UtcNow));
        
        Assert.Contains("Filename is required", exception.Message);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void ImportBatch_NegativeImportedCount_ThrowsDomainException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            new ImportBatch(Guid.NewGuid(), "test.pdf", -1, 0, DateTime.UtcNow));
        
        Assert.Contains("cannot be negative", exception.Message);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void ImportBatch_NegativeSkippedCount_ThrowsDomainException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            new ImportBatch(Guid.NewGuid(), "test.pdf", 5, -2, DateTime.UtcNow));
        
        Assert.Contains("cannot be negative", exception.Message);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void ImportBatch_TotalProcessed_CalculatesCorrectly()
    {
        // Arrange
        var batch = new ImportBatch(
            Guid.NewGuid(),
            "test.pdf",
            10,
            5,
            DateTime.UtcNow);

        // Act
        var total = batch.TotalProcessed;

        // Assert
        Assert.Equal(15, total);
    }
}

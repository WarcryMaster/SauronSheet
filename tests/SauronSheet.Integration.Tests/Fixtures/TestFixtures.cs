using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Integration.Tests.Fixtures;

/// <summary>
/// T094: Pre-populated test fixture with 100 transactions for multi-user scenarios
/// Creates realistic transaction data for concurrent delete testing
/// </summary>
public class TransactionFixture
{
    public static List<Transaction> GenerateTransactionsForUser(UserId userId, int count = 100)
    {
        var transactions = new List<Transaction>();
        var startDate = DateTime.Now.AddMonths(-3);

        for (int i = 0; i < count; i++)
        {
            var transaction = new Transaction(
                id: new TransactionId(Guid.NewGuid()),
                userId: userId,
                amount: new Money(
                    amount: (decimal)(10 + (i % 5) * 5),
                    currency: "EUR"),
                date: startDate.AddDays(i % 90),
                description: $"Test Transaction {i + 1}",
                categoryId: null,
                importedFrom: i % 3 == 0 ? "TestBank" : null);

            transactions.Add(transaction);
        }

        return transactions;
    }

    public static List<Transaction> GenerateConcurrentUserTransactions()
    {
        var user1Transactions = GenerateTransactionsForUser(new UserId("testuser1"), 50);
        var user2Transactions = GenerateTransactionsForUser(new UserId("testuser2"), 50);
        return user1Transactions.Concat(user2Transactions).ToList();
    }
}

/// <summary>
/// T095: Constraint violation scenario - transaction as part of active budget
/// Creates transaction structure that would violate budget constraint on deletion
/// </summary>
public class ConstraintViolationFixture
{
    public static Transaction CreateTransactionWithActiveBudget(UserId userId)
    {
        // Simulate a transaction that belongs to an active budget
        // In production, this would be enforced by database foreign key constraint
        return new Transaction(
            id: new TransactionId(Guid.NewGuid()),
            userId: userId,
            amount: new Money(amount: 1500.00m, currency: "EUR"),
            date: DateTime.Now.AddDays(-5),
            description: "Large purchase tracked by active monthly budget",
            categoryId: new CategoryId(Guid.NewGuid()),
            importedFrom: null);
    }

    public static List<Transaction> CreateMixedTransactions(UserId userId, int normalCount = 9, int constrainedCount = 1)
    {
        var transactions = new List<Transaction>();

        // Add normal transactions
        for (int i = 0; i < normalCount; i++)
        {
            transactions.Add(new Transaction(
                id: new TransactionId(Guid.NewGuid()),
                userId: userId,
                amount: new Money(amount: 50.00m, currency: "EUR"),
                date: DateTime.Now.AddDays(-i),
                description: $"Normal transaction {i + 1}",
                categoryId: null,
                importedFrom: null));
        }

        // Add one transaction with constraint violation
        transactions.Add(CreateTransactionWithActiveBudget(userId));

        return transactions;
    }
}

/// <summary>
/// Network error simulation for transient error testing (T096 support)
/// </summary>
public class NetworkErrorSimulator
{
    /// <summary>
    /// Simulates a timeout error for retry mechanism testing
    /// </summary>
    public static HttpRequestException CreateTimeoutError()
    {
        return new HttpRequestException("The operation timed out.");
    }

    /// <summary>
    /// Simulates a 503 Service Unavailable error (transient)
    /// </summary>
    public static HttpRequestException CreateServiceUnavailableError()
    {
        return new HttpRequestException("HTTP 503: Service Unavailable");
    }

    /// <summary>
    /// Simulates connection reset error (transient)
    /// </summary>
    public static HttpRequestException CreateConnectionResetError()
    {
        return new HttpRequestException("Connection reset by peer");
    }
}

namespace SauronSheet.Application.Features.Analytics.Queries;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Classification;
using Domain.Entities;
using Domain.Repositories;
using Domain.Specifications;
using Domain.ValueObjects;
using DTOs;
using MediatR;
using SauronSheet.Domain.Common;

/// <summary>
/// Handler for GetAnnualAnalysisQuery.
/// Loads the user's transactions for the requested year, classifies them
/// into fixed/variable income/expense rows and aggregates the summary block.
/// </summary>
public class GetAnnualAnalysisQueryHandler
    : IRequestHandler<GetAnnualAnalysisQuery, AnnualAnalysisResultDto>
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly ISubcategoryRepository _subcategoryRepo;
    private readonly IUserContext _userContext;
    private readonly IAnnualClassificationEngine _classificationEngine;

    public GetAnnualAnalysisQueryHandler(
        ITransactionRepository transactionRepo,
        ISubcategoryRepository subcategoryRepo,
        IUserContext userContext,
        IAnnualClassificationEngine classificationEngine)
    {
        _transactionRepo = transactionRepo;
        _subcategoryRepo = subcategoryRepo;
        _userContext = userContext;
        _classificationEngine = classificationEngine;
    }

    public async Task<AnnualAnalysisResultDto> Handle(
        GetAnnualAnalysisQuery request,
        CancellationToken cancellationToken)
    {
        UserId userId = new(_userContext.UserId);

        TransactionByUserSpecification userSpec = new(userId);
        TransactionByDateRangeSpecification dateSpec = new(
            new DateTime(request.Year, 1, 1),
            new DateTime(request.Year, 12, 31, 23, 59, 59));

        CompositeSpecification<Transaction> composedSpec =
            CompositeSpecification<Transaction>.And(userSpec, dateSpec);

        IReadOnlyList<Transaction> transactions = await _transactionRepo.FindBySpecificationAsync(composedSpec);

        IReadOnlyList<Subcategory> subcategories = await _subcategoryRepo.GetByUserIdAsync(userId);
        Dictionary<SubcategoryId, string> subcategoryNames = subcategories
            .ToDictionary(s => s.Id, s => s.Name.Value);

        List<Transaction> filteredTransactions = transactions
            .Where(t => !t.Amount.IsZero)
            .ToList();

        if (filteredTransactions.Count == 0)
        {
            AnnualAnalysisSummaryDto emptySummary = new(
                0m, 0m, 0m, 0m, 0m, 0m, 0m, "EUR");

            return new AnnualAnalysisResultDto(
                request.Year,
                Array.Empty<AnnualAnalysisRowDto>(),
                emptySummary,
                false,
                "EUR");
        }

        IReadOnlyList<AnnualAnalysisRowDto> rows = _classificationEngine.Classify(
            filteredTransactions,
            subcategoryNames,
            request.Year);

        decimal incomeFixed = rows
            .Where(r => r.LineType == AnalysisLineType.IncomeFixed)
            .Sum(r => r.MonthlyAmounts.Sum());

        decimal incomeVariable = rows
            .Where(r => r.LineType == AnalysisLineType.IncomeVariable)
            .Sum(r => r.MonthlyAmounts.Sum());

        decimal expenseFixed = rows
            .Where(r => r.LineType == AnalysisLineType.ExpenseFixed)
            .Sum(r => r.MonthlyAmounts.Sum());

        decimal expenseVariable = rows
            .Where(r => r.LineType == AnalysisLineType.ExpenseVariable)
            .Sum(r => r.MonthlyAmounts.Sum());

        decimal incomeTotal = incomeFixed + incomeVariable;
        decimal expenseTotal = expenseFixed + expenseVariable;
        decimal net = incomeTotal - expenseTotal;

        string currency = rows.FirstOrDefault()?.Currency ?? "EUR";

        AnnualAnalysisSummaryDto summary = new(
            incomeFixed,
            incomeVariable,
            incomeTotal,
            expenseFixed,
            expenseVariable,
            expenseTotal,
            net,
            currency);

        return new AnnualAnalysisResultDto(
            request.Year,
            rows,
            summary,
            true,
            currency);
    }
}
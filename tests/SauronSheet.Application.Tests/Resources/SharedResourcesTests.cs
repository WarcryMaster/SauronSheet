namespace SauronSheet.Application.Tests.Resources;

using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using SauronSheet.Application.Resources;
using Xunit;

/// <summary>
/// Tests for the shared localization resources (REQ-LOC-030).
/// </summary>
[Trait("Category", "Application")]
public class SharedResourcesTests
{
    private static IStringLocalizer<SharedResources> CreateLocalizer()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddLocalization();

        ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IStringLocalizer<SharedResources>>();
    }

    private static IDisposable SetCurrentUiCulture(string cultureName)
    {
        CultureInfo originalCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo(cultureName);
        return new CultureRestorer(originalCulture);
    }

    private sealed class CultureRestorer : IDisposable
    {
        private readonly CultureInfo _originalCulture;

        public CultureRestorer(CultureInfo originalCulture)
        {
            _originalCulture = originalCulture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentUICulture = _originalCulture;
        }
    }

    [Fact]
    public void Indexer_SpanishCulture_ReturnsSpanishValue()
    {
        IStringLocalizer<SharedResources> localizer = CreateLocalizer();

        using IDisposable _ = SetCurrentUiCulture("es-ES");
        string actual = localizer["Layout.SkipToContent"];

        Assert.Equal("Saltar al contenido principal", actual);
    }

    [Fact]
    public void Indexer_EnglishCulture_ReturnsEnglishValue()
    {
        IStringLocalizer<SharedResources> localizer = CreateLocalizer();

        using IDisposable _ = SetCurrentUiCulture("en-US");
        string actual = localizer["Layout.SkipToContent"];

        Assert.Equal("Skip to main content", actual);
    }

    [Fact]
    public void Indexer_MissingSpanishKey_FallsBackToEnglish()
    {
        IStringLocalizer<SharedResources> localizer = CreateLocalizer();

        using IDisposable _ = SetCurrentUiCulture("es-ES");
        string actual = localizer["Test.EnglishOnlyKey"];

        Assert.Equal("English only fallback", actual);
    }

    // ── PR5: Annual/Analysis resource keys ──

    [Fact]
    public void AnnualKeys_SpanishCulture_ReturnsSpanishValues()
    {
        IStringLocalizer<SharedResources> localizer = CreateLocalizer();

        using IDisposable _ = SetCurrentUiCulture("es-ES");
        Assert.Equal("Análisis Anual", localizer["Annual.PageTitle"]);
        Assert.Equal("Sin datos para este año", localizer["Annual.EmptyTitle"]);
        Assert.Equal("Excelente", localizer["Annual.HealthExcellent"]);
        Assert.Equal("Buena", localizer["Annual.HealthGood"]);
        Assert.Equal("Regular", localizer["Annual.HealthFair"]);
        Assert.Equal("Necesita atención", localizer["Annual.HealthNeedsAttention"]);
        Assert.Equal("Salud Financiera", localizer["Annual.HealthScore"]);
        Assert.Equal("Tasa de Ahorro", localizer["Annual.RatioSavingsRate"]);
        Assert.Equal("Mostrar detalle", localizer["Annual.ShowDetail"]);
        Assert.Equal("Ocultar detalle", localizer["Annual.HideDetail"]);
        Assert.Equal("Interanual", localizer["Annual.YoyTitle"]);
        Assert.Equal("Sin datos del año anterior", localizer["Annual.YoyNoData"]);
        Assert.Equal("Ingresos", localizer["Annual.KpiIncome"]);
        Assert.Equal("Gastos", localizer["Annual.KpiExpense"]);
        Assert.Equal("Neto", localizer["Annual.KpiNet"]);
        Assert.Equal("Coste Fijo %", localizer["Annual.KpiFixedCost"]);
        Assert.Equal("Tasa Ahorro", localizer["Annual.KpiSavingsRate"]);
        Assert.Equal("Anomalías", localizer["Annual.Anomalies"]);
        Assert.Equal("Sin anomalías", localizer["Annual.NoAnomalies"]);
        Assert.Equal("Descubrimientos", localizer["Annual.Discoveries"]);
        Assert.Equal("Sin descubrimientos", localizer["Annual.NoDiscoveries"]);
        Assert.Equal("Logros", localizer["Annual.Achievements"]);
        Assert.Equal("Sin logros", localizer["Annual.NoAchievements"]);
        Assert.Equal("Datos insuficientes", localizer["Annual.Insufficient"]);
        Assert.Equal("Predicciones", localizer["Annual.Predictions"]);
    }

    [Fact]
    public void AnnualKeys_EnglishCulture_ReturnsEnglishValues()
    {
        IStringLocalizer<SharedResources> localizer = CreateLocalizer();

        using IDisposable _ = SetCurrentUiCulture("en-US");
        Assert.Equal("Annual Analysis", localizer["Annual.PageTitle"]);
        Assert.Equal("No data for this year", localizer["Annual.EmptyTitle"]);
        Assert.Equal("Excellent", localizer["Annual.HealthExcellent"]);
        Assert.Equal("Good", localizer["Annual.HealthGood"]);
        Assert.Equal("Fair", localizer["Annual.HealthFair"]);
        Assert.Equal("Needs attention", localizer["Annual.HealthNeedsAttention"]);
        Assert.Equal("Financial Health", localizer["Annual.HealthScore"]);
        Assert.Equal("Savings Rate", localizer["Annual.RatioSavingsRate"]);
        Assert.Equal("Show detail", localizer["Annual.ShowDetail"]);
        Assert.Equal("Hide detail", localizer["Annual.HideDetail"]);
        Assert.Equal("Year over Year", localizer["Annual.YoyTitle"]);
        Assert.Equal("No data from previous year", localizer["Annual.YoyNoData"]);
        Assert.Equal("Income", localizer["Annual.KpiIncome"]);
        Assert.Equal("Expenses", localizer["Annual.KpiExpense"]);
        Assert.Equal("Net", localizer["Annual.KpiNet"]);
        Assert.Equal("Fixed Cost %", localizer["Annual.KpiFixedCost"]);
        Assert.Equal("Savings Rate", localizer["Annual.KpiSavingsRate"]);
        Assert.Equal("Anomalies", localizer["Annual.Anomalies"]);
        Assert.Equal("No anomalies", localizer["Annual.NoAnomalies"]);
        Assert.Equal("Discoveries", localizer["Annual.Discoveries"]);
        Assert.Equal("No discoveries", localizer["Annual.NoDiscoveries"]);
        Assert.Equal("Achievements", localizer["Annual.Achievements"]);
        Assert.Equal("No achievements", localizer["Annual.NoAchievements"]);
        Assert.Equal("Insufficient data", localizer["Annual.Insufficient"]);
        Assert.Equal("Predictions", localizer["Annual.Predictions"]);
    }

    [Fact]
    public void InsightsFixedKeys_SpanishCulture_ReturnsSpanishValues()
    {
        IStringLocalizer<SharedResources> localizer = CreateLocalizer();

        using IDisposable _ = SetCurrentUiCulture("es-ES");
        Assert.Equal("Datos insuficientes para generar descubrimientos.", localizer["Discovery.InsufficientData"]);
        Assert.Equal("No se generó ahorro neto este año.", localizer["Insights.NoSavings"]);
    }

    [Fact]
    public void InsightsFixedKeys_EnglishCulture_ReturnsEnglishValues()
    {
        IStringLocalizer<SharedResources> localizer = CreateLocalizer();

        using IDisposable _ = SetCurrentUiCulture("en-US");
        Assert.Equal("Not enough data to generate discoveries.", localizer["Discovery.InsufficientData"]);
        Assert.Equal("No net savings generated this year.", localizer["Insights.NoSavings"]);
    }

    [Fact]
    public void AnomalyFixedKeys_SpanishCulture_ReturnsSpanishValues()
    {
        IStringLocalizer<SharedResources> localizer = CreateLocalizer();

        using IDisposable _ = SetCurrentUiCulture("es-ES");
        Assert.Equal("{0}: pico excepcional (€{1:F2}) sin repetición el año anterior.", localizer["Anomaly.Exceptional"]);
        Assert.Equal("{0}: gasto extraordinario (€{1:F2}) sobre 3× media (€{2:F2}).", localizer["Anomaly.Extraordinary"]);
        Assert.Equal("{0}: anomalía (€{1:F2}) sobre umbral μ+2σ (€{2:F2}).", localizer["Anomaly.Generic"]);
    }

    [Fact]
    public void AnomalyFixedKeys_EnglishCulture_ReturnsEnglishValues()
    {
        IStringLocalizer<SharedResources> localizer = CreateLocalizer();

        using IDisposable _ = SetCurrentUiCulture("en-US");
        Assert.Equal("{0}: exceptional spike (€{1:F2}) with no repeated spike in previous year.", localizer["Anomaly.Exceptional"]);
        Assert.Equal("{0}: extraordinary expense (€{1:F2}) above 3× mean (€{2:F2}).", localizer["Anomaly.Extraordinary"]);
        Assert.Equal("{0}: anomaly (€{1:F2}) above μ+2σ threshold (€{2:F2}).", localizer["Anomaly.Generic"]);
    }

    [Fact]
    public void AnnualExtendedKeys_AreLocalizedForSpanishAndEnglish()
    {
        IStringLocalizer<SharedResources> localizer = CreateLocalizer();

        using (IDisposable _ = SetCurrentUiCulture("es-ES"))
        {
            Assert.Equal("Año anterior", localizer["Annual.NavPreviousYear"]);
            Assert.Equal("Año siguiente", localizer["Annual.NavNextYear"]);
            Assert.Equal("{0} · Puesto #{1} de {2}", localizer["Annual.YearRankSummary"]);
            Assert.Equal("Fijo", localizer["Annual.FixedKeyword"]);
            Assert.Equal("Sin datos para este año. Añade transacciones para ver el resumen anual.", localizer["Insights.EmptyYear"]);
            Assert.Equal("Sin descubrimientos", localizer["Discovery.TitleNoDiscoveries"]);
        }

        using (IDisposable _ = SetCurrentUiCulture("en-US"))
        {
            Assert.Equal("Previous year", localizer["Annual.NavPreviousYear"]);
            Assert.Equal("Next year", localizer["Annual.NavNextYear"]);
            Assert.Equal("{0} · Rank #{1} of {2}", localizer["Annual.YearRankSummary"]);
            Assert.Equal("Fixed", localizer["Annual.FixedKeyword"]);
            Assert.Equal("No data for this year. Add transactions to see the annual summary.", localizer["Insights.EmptyYear"]);
            Assert.Equal("No discoveries", localizer["Discovery.TitleNoDiscoveries"]);
        }
    }

    [Fact]
    public void JavaScriptChartKeys_AreLocalizedForSpanishAndEnglish()
    {
        IStringLocalizer<SharedResources> localizer = CreateLocalizer();

        using (IDisposable _ = SetCurrentUiCulture("es-ES"))
        {
            Assert.Equal("Ingresos", localizer["js.chart.series.income"]);
            Assert.Equal("Gastos", localizer["js.chart.series.expenses"]);
            Assert.Equal("Importe anomalía", localizer["js.chart.series.anomalyAmount"]);
            Assert.Equal("Media histórica", localizer["js.chart.series.historicalMean"]);
            Assert.Equal("Año", localizer["js.chart.period.yearLabel"]);
            Assert.Equal("{0}: €{1}", localizer["js.chart.tooltip.amount"]);
        }

        using (IDisposable _ = SetCurrentUiCulture("en-US"))
        {
            Assert.Equal("Income", localizer["js.chart.series.income"]);
            Assert.Equal("Expenses", localizer["js.chart.series.expenses"]);
            Assert.Equal("Anomaly amount", localizer["js.chart.series.anomalyAmount"]);
            Assert.Equal("Historical mean", localizer["js.chart.series.historicalMean"]);
            Assert.Equal("Year", localizer["js.chart.period.yearLabel"]);
            Assert.Equal("{0}: €{1}", localizer["js.chart.tooltip.amount"]);
        }
    }

    [Fact]
    public void ImportAndParserKeys_AreLocalizedForSpanishAndEnglish()
    {
        IStringLocalizer<SharedResources> localizer = CreateLocalizer();

        using (IDisposable _ = SetCurrentUiCulture("es-ES"))
        {
            Assert.Equal("Solo se aceptan archivos Excel (.xls, .xlsx).", localizer["Import.Error.InvalidExtension"]);
            Assert.Equal("No se pudo procesar el archivo subido. Comprueba el formato e inténtalo de nuevo.", localizer["Import.Error.ParseFailed"]);
            Assert.Equal("Formato de fecha inválido", localizer["Import.Error.InvalidDateFormat"]);
            Assert.Equal("Formato de importe inválido", localizer["Import.Error.InvalidAmountFormat"]);
            Assert.Equal("Duplicado", localizer["Import.Error.Duplicate"]);
            Assert.Equal("Formato de fecha inválido: '{0}'", localizer["Import.Parser.InvalidDate"]);
            Assert.Equal("Formato de importe inválido: '{0}'", localizer["Import.Parser.InvalidAmount"]);
        }

        using (IDisposable _ = SetCurrentUiCulture("en-US"))
        {
            Assert.Equal("Only Excel files (.xls, .xlsx) are accepted.", localizer["Import.Error.InvalidExtension"]);
            Assert.Equal("Could not parse the uploaded file. Please check the format and try again.", localizer["Import.Error.ParseFailed"]);
            Assert.Equal("Invalid date format", localizer["Import.Error.InvalidDateFormat"]);
            Assert.Equal("Invalid amount format", localizer["Import.Error.InvalidAmountFormat"]);
            Assert.Equal("Duplicate", localizer["Import.Error.Duplicate"]);
            Assert.Equal("Invalid date format: '{0}'", localizer["Import.Parser.InvalidDate"]);
            Assert.Equal("Invalid amount format: '{0}'", localizer["Import.Parser.InvalidAmount"]);
        }
    }

    [Fact]
    public void SystemCategoryKeys_AreLocalizedForSpanishAndEnglish()
    {
        IStringLocalizer<SharedResources> localizer = CreateLocalizer();

        using (IDisposable _ = SetCurrentUiCulture("es-ES"))
        {
            Assert.Equal("Salario", localizer["category.system.salary"]);
            Assert.Equal("Supermercado", localizer["category.system.groceries"]);
            Assert.Equal("Comer fuera", localizer["category.system.dining-out"]);
            Assert.Equal("Regalos dados", localizer["category.system.gifts-given"]);
            Assert.Equal("Otros gastos", localizer["category.system.other-expense"]);
        }

        using (IDisposable _ = SetCurrentUiCulture("en-US"))
        {
            Assert.Equal("Salary", localizer["category.system.salary"]);
            Assert.Equal("Groceries", localizer["category.system.groceries"]);
            Assert.Equal("Dining Out", localizer["category.system.dining-out"]);
            Assert.Equal("Gifts Given", localizer["category.system.gifts-given"]);
            Assert.Equal("Other Expense", localizer["category.system.other-expense"]);
        }
    }
}

/**
 * charts.js — Chart.js initialization functions for SauronSheet analytics.
 * Phase 4: Dashboard charts (pie, line, bar).
 * Requires Chart.js (latest) CDN loaded in _Layout.cshtml.
 */

const designColors = [
    '#556B2F', // primary
    '#3b71ca', // semantic-info
    '#14a44d', // semantic-success
    '#e4a11b', // semantic-warning
    '#dc4c64', // semantic-danger
    '#435425', // primary-active
    '#6c757d', // muted
    '#212529'  // ink
];

/**
 * Initialize a pie chart for category spending breakdown.
 * @param {string} canvasId - Canvas element ID
 * @param {Array} categoryData - Array of {categoryName, amount, percentage, categoryColor}
 */
function initCategoryPieChart(canvasId, categoryData) {
    const canvas = document.getElementById(canvasId);
    if (!canvas || !categoryData || categoryData.length === 0) return;

    const labels = categoryData.map(d => d.categoryName);
    const data = categoryData.map(d => d.amount);
    
    // Shuffle design colors randomly
    const shuffledColors = [...designColors].sort(() => 0.5 - Math.random());
    const colors = categoryData.map((d, i) => shuffledColors[i % shuffledColors.length]);

    const ctx = canvas.getContext('2d');
    if (canvas._chartInstance) {
        canvas._chartInstance.destroy();
    }
    canvas._chartInstance = new Chart(ctx, {
        type: 'pie',
        data: {
            labels: labels,
            datasets: [{
                data: data,
                backgroundColor: colors,
                borderColor: '#ffffff',
                borderWidth: 2
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: { 
                    position: 'top',
                },
                title: {
                    display: true,
                    text: 'Spending by Category'
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            const item = categoryData[context.dataIndex];
                            return `${item.categoryName}: €${item.amount.toFixed(2)} (${item.percentage}%)`;
                        }
                    }
                }
            }
        }
    });
}

/**
 * Initialize a line chart for monthly spending trends.
 * @param {string} canvasId - Canvas element ID
 * @param {Array} monthlyData - Array of {monthName, totalExpenses, totalIncome}
 */
function initMonthlyTrendsChart(canvasId, monthlyData) {
    const canvas = document.getElementById(canvasId);
    if (!canvas || !monthlyData || monthlyData.length === 0) return;

    const labels = monthlyData.map(d => d.monthName);

    const ctx = canvas.getContext('2d');
    if (canvas._chartInstance) {
        canvas._chartInstance.destroy();
    }
    canvas._chartInstance = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Expenses',
                    data: monthlyData.map(d => d.totalExpenses),
                    borderColor: '#EF4444',
                    backgroundColor: 'rgba(239, 68, 68, 0.1)',
                    tension: 0.3,
                    fill: true
                },
                {
                    label: 'Income',
                    data: monthlyData.map(d => d.totalIncome),
                    borderColor: '#10B981',
                    backgroundColor: 'rgba(16, 185, 129, 0.1)',
                    tension: 0.3,
                    fill: true
                }
            ]
        },
        options: {
            responsive: true,
            plugins: { legend: { position: 'bottom' } },
            scales: {
                y: { beginAtZero: true, ticks: { callback: v => '€' + v } }
            }
        }
    });
}

/**
 * Initialize a bar chart for yearly spending comparison.
 * @param {string} canvasId - Canvas element ID
 * @param {Array} yearlyData - Array of {monthName, year1Amount, year2Amount}
 * @param {string} year1Label - Label for year 1
 * @param {string} year2Label - Label for year 2
 */
function initYearlyComparisonChart(canvasId, yearlyData, year1Label, year2Label) {
    const canvas = document.getElementById(canvasId);
    if (!canvas || !yearlyData || yearlyData.length === 0) return;

    const labels = yearlyData.map(d => d.monthName);

    const ctx = canvas.getContext('2d');
    if (canvas._chartInstance) {
        canvas._chartInstance.destroy();
    }
    canvas._chartInstance = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [
                {
                    label: year1Label || 'Year 1',
                    data: yearlyData.map(d => d.year1Amount),
                    backgroundColor: '#3B82F6'
                },
                {
                    label: year2Label || 'Year 2',
                    data: yearlyData.map(d => d.year2Amount),
                    backgroundColor: '#8B5CF6'
                }
            ]
        },
        options: {
            responsive: true,
            plugins: { legend: { position: 'bottom' } },
            scales: {
                y: { beginAtZero: true, ticks: { callback: v => '€' + v } }
            }
        }
    });
}

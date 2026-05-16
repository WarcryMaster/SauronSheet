/**
 * charts.js — Chart.js initialization functions for SauronSheet analytics.
 * Phase 4: Dashboard charts (stacked area, line, bar).
 * Requires Chart.js (latest) CDN loaded in _Layout.cshtml.
 *
 * Color palette from DESIGN.md (SauronSheet Olive):
 *   primary:        #556B2F
 *   primary-active: #435425
 *   semantic-info:    #3b71ca
 *   semantic-success: #14a44d
 *   semantic-warning: #e4a11b
 *   semantic-danger:  #dc4c64
 */

const designColors = [
    '#556B2F', // primary — Olive Green
    '#3b71ca', // semantic-info
    '#14a44d', // semantic-success
    '#e4a11b', // semantic-warning
    '#dc4c64', // semantic-danger
    '#435425', // primary-active
    '#9b59b6', // amethyst
    '#e67e22', // carrot
    '#1abc9c', // turquoise
    '#e84393', // prunus avium
    '#6c5ce7', // blurple
    '#00b894', // mint
    '#fdcb6e', // sun
    '#0984e3', // electron blue
    '#ff9f43', // bright orange
    '#10ac84', // sea green
    '#ff6b6b', // coral red
    '#48dbfb', // sky blue
    '#feca57', // bright gold
    '#ff9ff3', // light pink
    '#5f27cd', // deep purple
    '#01a3a4', // aqua
    '#ee5253', // bright red
    '#22a6b3', // cyan
    '#badc58'  // light lime
];

// Shared chart defaults — DESIGN.md aligned
const chartDefaults = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
        legend: {
            position: 'bottom',
            labels: {
                usePointStyle: true,
                pointStyle: 'circle',
                padding: 16,
                font: {
                    size: 12
                }
            }
        }
    },
    scales: {
        x: {
            grid: {
                display: false
            }
        },
        y: {
            beginAtZero: true,
            grid: {
                color: 'rgba(0, 0, 0, 0.05)'
            },
            ticks: {
                callback: v => '€' + v
            }
        }
    }
};

/**
 * Initialize a stacked area chart for spending by category over months.
 * Each category becomes a filled dataset stacked on top of others.
 * @param {string} canvasId - Canvas element ID
 * @param {Array} monthlyCategoryData - Array of {month, monthName, categoryName, amount}
 */
function initCategoryStackedChart(canvasId, monthlyCategoryData) {
    const canvas = document.getElementById(canvasId);
    if (!canvas || !monthlyCategoryData || monthlyCategoryData.length === 0) return;

    // Collect all unique months (sorted) and categories
    const months = [...new Set(monthlyCategoryData.map(d => d.month))]
        .sort((a, b) => a - b);
    const monthLabels = months.map(m => {
        const entry = monthlyCategoryData.find(d => d.month === m);
        return entry ? entry.monthName : `Month ${m}`;
    });

    const categories = [...new Set(monthlyCategoryData.map(d => d.categoryName))];

    // Build one dataset per category
    const datasets = categories.map((cat, idx) => {
        const color = designColors[idx % designColors.length];
        const data = months.map(m => {
            const entry = monthlyCategoryData.find(d => d.month === m && d.categoryName === cat);
            return entry ? Number(entry.amount) : 0;
        });

        return {
            label: cat,
            data: data,
            backgroundColor: hexToRgba(color, 0.55),
            borderColor: color,
            borderWidth: 2,
            fill: true,
            tension: 0.3,
            pointRadius: 2,
            pointHoverRadius: 5
        };
    });

    const ctx = canvas.getContext('2d');
    if (canvas._chartInstance) {
        canvas._chartInstance.destroy();
    }
    canvas._chartInstance = new Chart(ctx, {
        type: 'line',
        data: { labels: monthLabels, datasets: datasets },
        options: {
            ...chartDefaults,
            scales: {
                ...chartDefaults.scales,
                x: {
                    ...chartDefaults.scales.x,
                    stacked: true
                },
                y: {
                    ...chartDefaults.scales.y,
                    stacked: true
                }
            },
            interaction: {
                mode: 'index',
                intersect: false
            },
            plugins: {
                ...chartDefaults.plugins,
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            return `${context.dataset.label}: €${context.parsed.y.toFixed(2)}`;
                        }
                    }
                }
            }
        }
    });
}

/**
 * Initialize a line chart for monthly spending trends (income vs expenses).
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
                    borderColor: '#dc4c64', // semantic-danger — DESIGN.md
                    backgroundColor: hexToRgba('#dc4c64', 0.1),
                    tension: 0.3,
                    fill: true,
                    pointRadius: 2,
                    pointHoverRadius: 5,
                    borderWidth: 2
                },
                {
                    label: 'Income',
                    data: monthlyData.map(d => d.totalIncome),
                    borderColor: '#14a44d', // semantic-success — DESIGN.md
                    backgroundColor: hexToRgba('#14a44d', 0.1),
                    tension: 0.3,
                    fill: true,
                    pointRadius: 2,
                    pointHoverRadius: 5,
                    borderWidth: 2
                }
            ]
        },
        options: {
            ...chartDefaults,
            interaction: {
                mode: 'index',
                intersect: false
            }
        }
    });
}

/**
 * Initialize a bar chart for yearly spending comparison.
 * Shows 4 bars per month: Income Y1, Expenses Y1, Income Y2, Expenses Y2.
 * Uses solid borders with semi-transparent fills.
 * @param {string} canvasId - Canvas element ID
 * @param {Array} yearlyData - Array of {monthName, year1Income, year1Expenses, year2Income, year2Expenses}
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

    const y1 = year1Label || 'Year 1';
    const y2 = year2Label || 'Year 2';

    canvas._chartInstance = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [
                {
                    label: `${y1} Income`,
                    data: yearlyData.map(d => d.year1Income),
                    backgroundColor: 'rgba(20, 164, 77, 0.2)',
                    borderColor: '#14a44d',
                    borderWidth: 2,
                    borderRadius: 4,
                    borderSkipped: false
                },
                {
                    label: `${y1} Expenses`,
                    data: yearlyData.map(d => d.year1Expenses),
                    backgroundColor: 'rgba(220, 76, 100, 0.2)',
                    borderColor: '#dc4c64',
                    borderWidth: 2,
                    borderRadius: 4,
                    borderSkipped: false
                },
                {
                    label: `${y2} Income`,
                    data: yearlyData.map(d => d.year2Income),
                    backgroundColor: 'rgba(13, 110, 53, 0.2)',
                    borderColor: '#0d6e35',
                    borderWidth: 2,
                    borderRadius: 4,
                    borderSkipped: false
                },
                {
                    label: `${y2} Expenses`,
                    data: yearlyData.map(d => d.year2Expenses),
                    backgroundColor: 'rgba(180, 50, 70, 0.2)',
                    borderColor: '#b43246',
                    borderWidth: 2,
                    borderRadius: 4,
                    borderSkipped: false
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        usePointStyle: true,
                        pointStyle: 'circle',
                        padding: 16,
                        font: { size: 12 }
                    }
                }
            },
            scales: {
                x: {
                    grid: { display: false }
                },
                y: {
                    beginAtZero: true,
                    grid: { color: 'rgba(0, 0, 0, 0.05)' },
                    ticks: { callback: v => '€' + v }
                }
            }
        }
    });
}

/**
 * Convert a hex color to rgba string with given alpha.
 * @param {string} hex — e.g. "#556B2F"
 * @param {number} alpha — 0..1
 * @returns {string} rgba(r, g, b, a)
 */
function hexToRgba(hex, alpha) {
    const r = parseInt(hex.slice(1, 3), 16);
    const g = parseInt(hex.slice(3, 5), 16);
    const b = parseInt(hex.slice(5, 7), 16);
    return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}

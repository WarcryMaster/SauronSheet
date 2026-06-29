/**
 * charts.js — Chart.js initialization functions for SauronSheet analytics.
 * Phase 7.1: Counters, skeleton loading, HTMX pill selector integration.
 * Requires Chart.js (latest) CDN loaded in _Layout.cshtml.
 *
 * Colors: read from CSS custom properties (DESIGN.md tokens).
 * Fallback hex values provided for when CSS vars are unavailable.
 *
 * ## Legend Ordering Contract
 * The handler (backend) is the single source of truth for dataset/legend order.
 * Backend MUST sort categories by total amount descending (ties broken by name ascending).
 * Frontend MUST NOT reorder, sort, or deduplicate datasets — it MUST preserve the
 * array order received from the server. Chart.js renders legend in dataset insertion
 * order, so the visual legend will match the handler's sort automatically.
 */
'use strict';

// Resolve a CSS custom property to its computed value, with fallback.
function cssVar(name, fallback) {
    if (typeof getComputedStyle === 'undefined') return fallback;
    const value = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
    return value || fallback;
}

// Design token palette resolved from CSS — with hardcoded fallbacks matching DESIGN.md
const tokens = {
    brand:       cssVar('--brand', '#556B2F'),
    brandDark:   cssVar('--brand-dark', '#435425'),
    brandLight:  cssVar('--brand-light', '#f4f7ee'),
    success:     cssVar('--semantic-success', '#14a44d'),
    danger:      cssVar('--semantic-danger', '#dc4c64'),
    warning:     cssVar('--semantic-warning', '#e4a11b'),
    info:        cssVar('--semantic-info', '#3b71ca'),
    ink:         cssVar('--ink', '#212529'),
    muted:       cssVar('--muted', '#6c757d'),
    hairline:    cssVar('--hairline', '#dee2e6')
};

const designColors = [
    tokens.brand,       // Olive Green
    tokens.info,        // Info blue
    tokens.success,     // Success green
    tokens.warning,     // Warning amber
    tokens.danger,      // Danger red
    tokens.brandDark,   // Olive dark
    '#9b59b6',          // amethyst
    '#e67e22',          // carrot
    '#1abc9c',          // turquoise
    '#e84393',          // prunus avium
    '#6c5ce7',          // blurple
    '#00b894',          // mint
    '#fdcb6e',          // sun
    '#0984e3',          // electron blue
    '#ff9f43',          // bright orange
    '#10ac84',          // sea green
    '#ff6b6b',          // coral red
    '#48dbfb',          // sky blue
    '#feca57',          // bright gold
    '#ff9ff3',          // light pink
    '#5f27cd',          // deep purple
    '#01a3a4',          // aqua
    '#ee5253',          // bright red
    '#22a6b3',          // cyan
    '#badc58'           // light lime
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
};

/**
 * Destroy all Chart.js instances on the page.
 * Called by HTMX before-swap to prevent memory leaks.
 */
function destroyAllCharts() {
    // Chart.js auto-registers instances — find all canvas elements with charts
    document.querySelectorAll('canvas').forEach(canvas => {
        const instance = Chart.getChart(canvas);
        if (instance) {
            instance.destroy();
        }
    });
}

/**
 * Initialize a stacked area chart for spending by category over months.
 *
 * PRECONDITION: monthlyCategoryData MUST be sorted by the handler so that
 * categories appear in descending total-amount order.
 * Chart.js renders legend in dataset insertion order — this function MUST NOT
 * reorder or deduplicate categories. The dataset array is built by iterating
 * categories in the exact order they first appear in the data.
 *
 * @param {string} canvasId - Canvas element ID
 * @param {Array<{year: number, month: number, monthName: string, categoryName: string, amount: number}>} monthlyCategoryData
 */
function initCategoryStackedChart(canvasId, monthlyCategoryData) {
    const canvas = document.getElementById(canvasId);
    if (!canvas || !monthlyCategoryData || monthlyCategoryData.length === 0) return;

    // Destroy existing instance before recreating
    const existing = Chart.getChart(canvas);
    if (existing) existing.destroy();

    // Build unique (year, month) period keys, sorted chronologically
    const periodKeys = [...new Set(monthlyCategoryData.map(d => `${d.year}-${String(d.month).padStart(2, '0')}`))]
        .sort();
    const monthLabels = periodKeys.map(key => {
        const [y, m] = key.split('-');
        const entry = monthlyCategoryData.find(d => d.year === Number(y) && d.month === Number(m));
        return entry ? `${entry.monthName} ${y}` : `Month ${m} ${y}`;
    });

    // Preserve category order from data — handler controls legend order
    const categories = [];
    const seen = new Set();
    for (const d of monthlyCategoryData) {
        if (!seen.has(d.categoryName)) {
            seen.add(d.categoryName);
            categories.push(d.categoryName);
        }
    }

    const datasets = categories.map((cat, idx) => {
        const color = designColors[idx % designColors.length];
        const data = periodKeys.map(key => {
            const [y, m] = key.split('-');
            const entry = monthlyCategoryData.find(
                d => d.year === Number(y) && d.month === Number(m) && d.categoryName === cat
            );
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
    new Chart(ctx, {
        type: 'line',
        data: { labels: monthLabels, datasets: datasets },
        options: {
            ...chartDefaults,
            scales: {
                ...chartDefaults.scales,
                x: { ...chartDefaults.scales.x, stacked: true },
                y: { ...chartDefaults.scales.y, stacked: true }
            },
            interaction: {
                mode: 'index',
                intersect: false
            },
            plugins: {
                ...chartDefaults.plugins,
                legend: {
                    ...chartDefaults.plugins.legend,
                    reverse: true  // Reverse legend so top of chart = first in legend
                },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            return `${context.dataset.label}: €${context.parsed.y.toFixed(2)}`;
                        },
                        // Reverse tooltip items so top of chart = first in tooltip
                        afterBody: function() {
                            // This callback is used to customize tooltip rendering
                            return '';
                        }
                    },
                    itemSort: function(a, b) {
                        // Reverse sort: higher dataset index (top of chart) appears first in tooltip
                        return b.datasetIndex - a.datasetIndex;
                    }
                }
            }
        }
    });
}

/**
 * Initialize a line chart for monthly spending trends (income vs expenses).
 *
 * PRECONDITION: monthlyData is ordered chronologically by the handler.
 * This function MUST NOT reorder entries. Labels are built from each entry's
 * own `year` and `monthName` fields to support multi-year ranges.
 *
 * @param {string} canvasId - Canvas element ID
 * @param {Array<{year: number, month: number, monthName: string, totalExpenses: number, totalIncome: number}>} monthlyData
 */
function initMonthlyTrendsChart(canvasId, monthlyData) {
    const canvas = document.getElementById(canvasId);
    if (!canvas || !monthlyData || monthlyData.length === 0) return;

    const existing = Chart.getChart(canvas);
    if (existing) existing.destroy();

    const labels = monthlyData.map(d => `${d.monthName} ${d.year}`);

    const ctx = canvas.getContext('2d');
    new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Expenses',
                    data: monthlyData.map(d => d.totalExpenses),
                    borderColor: tokens.danger,
                    backgroundColor: hexToRgba(tokens.danger, 0.1),
                    tension: 0.3,
                    fill: true,
                    pointRadius: 2,
                    pointHoverRadius: 5,
                    borderWidth: 2
                },
                {
                    label: 'Income',
                    data: monthlyData.map(d => d.totalIncome),
                    borderColor: tokens.success,
                    backgroundColor: hexToRgba(tokens.success, 0.1),
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
 * @param {string} canvasId - Canvas element ID
 * @param {Array} yearlyData - Array of {monthName, year1Income, year1Expenses, year2Income, year2Expenses}
 * @param {string} year1Label - Label for year 1
 * @param {string} year2Label - Label for year 2
 */
function initYearlyComparisonChart(canvasId, yearlyData, year1Label, year2Label) {
    const canvas = document.getElementById(canvasId);
    if (!canvas || !yearlyData || yearlyData.length === 0) return;

    const existing = Chart.getChart(canvas);
    if (existing) existing.destroy();

    const labels = yearlyData.map(d => d.monthName);
    const y1 = year1Label || 'Year 1';
    const y2 = year2Label || 'Year 2';

    const ctx = canvas.getContext('2d');
    new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [
                {
                    label: `${y1} Income`,
                    data: yearlyData.map(d => d.year1Income),
                    backgroundColor: hexToRgba(tokens.success, 0.2),
                    borderColor: tokens.success,
                    borderWidth: 2,
                    borderRadius: 4,
                    borderSkipped: false
                },
                {
                    label: `${y1} Expenses`,
                    data: yearlyData.map(d => d.year1Expenses),
                    backgroundColor: hexToRgba(tokens.danger, 0.2),
                    borderColor: tokens.danger,
                    borderWidth: 2,
                    borderRadius: 4,
                    borderSkipped: false
                },
                {
                    label: `${y2} Income`,
                    data: yearlyData.map(d => d.year2Income),
                    backgroundColor: hexToRgba('#0d6e35', 0.2),
                    borderColor: '#0d6e35',
                    borderWidth: 2,
                    borderRadius: 4,
                    borderSkipped: false
                },
                {
                    label: `${y2} Expenses`,
                    data: yearlyData.map(d => d.year2Expenses),
                    backgroundColor: hexToRgba('#b43246', 0.2),
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
 * Initialize the annual trend line chart (income vs expense over 12 months).
 *
 * PRECONDITION: data.income and data.expense MUST contain exactly 12 values,
 * indexed January (0) to December (11). The handler is the source of truth
 * for the monthly order; this function MUST NOT reorder the arrays.
 *
 * @param {HTMLCanvasElement} canvas - Canvas element reference (from Alpine.js $refs)
 * @param {{labels: string[], income: number[], expense: number[]}} data - Parsed JSON payload
 */
function initAnnualTrendChart(canvas, data) {
    if (!canvas || !data || !Array.isArray(data.labels) || data.labels.length === 0) return;

    const existing = Chart.getChart(canvas);
    if (existing) existing.destroy();

    const ctx = canvas.getContext('2d');
    new Chart(ctx, {
        type: 'line',
        data: {
            labels: data.labels,
            datasets: [
                {
                    label: 'Income',
                    data: data.income,
                    borderColor: tokens.success,
                    backgroundColor: hexToRgba(tokens.success, 0.1),
                    tension: 0.3,
                    fill: true,
                    pointRadius: 3,
                    pointHoverRadius: 5,
                    borderWidth: 2
                },
                {
                    label: 'Expenses',
                    data: data.expense,
                    borderColor: tokens.danger,
                    backgroundColor: hexToRgba(tokens.danger, 0.1),
                    tension: 0.3,
                    fill: true,
                    pointRadius: 3,
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
 * Initialize the annual fixed/variable distribution donut chart.
 *
 * PRECONDITION: data.values MUST contain exactly 4 values in this order:
 * Income Fixed, Income Variable, Expense Fixed, Expense Variable. The handler
 * controls the order; this function MUST NOT reorder or deduplicate segments.
 *
 * @param {HTMLCanvasElement} canvas - Canvas element reference (from Alpine.js $refs)
 * @param {{labels: string[], values: number[]}} data - Parsed JSON payload
 */
function initAnnualDistributionChart(canvas, data) {
    if (!canvas || !data || !Array.isArray(data.values) || data.values.length === 0) return;

    const existing = Chart.getChart(canvas);
    if (existing) existing.destroy();

    const donutColors = [
        tokens.success,
        hexToRgba(tokens.success, 0.65),
        tokens.danger,
        hexToRgba(tokens.danger, 0.65)
    ];

    const ctx = canvas.getContext('2d');
    new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: data.labels,
            datasets: [{
                data: data.values,
                backgroundColor: donutColors,
                borderColor: '#fff',
                borderWidth: 2
            }]
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
                },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            return `${context.label}: €${Number(context.parsed).toFixed(2)}`;
                        }
                    }
                }
            }
        }
    });
}

/**
 * Initialize a grouped bar chart for multi-year comparison (REQ-003).
 *
 * PRECONDITION: data contains parallel arrays for income, expenses, savings,
 * and balances, one entry per year. The handler controls the array order
 * (chronological ascending); this function MUST NOT reorder.
 *
 * @param {HTMLCanvasElement} canvas - Canvas element reference
 * @param {{labels: string[], income: number[], expenses: number[], savings: number[], balances: number[], highlightYear: number}} data
 */
function initMultiYearChart(canvas, data) {
    if (!canvas || !data || !Array.isArray(data.labels) || data.labels.length < 2) return;

    const existing = Chart.getChart(canvas);
    if (existing) existing.destroy();

    const highlightIdx = data.labels.indexOf(String(data.highlightYear));
    const defaultBg = hexToRgba(tokens.info, 0.6);
    const highlightBg = hexToRgba(tokens.brand, 0.85);

    const ctx = canvas.getContext('2d');
    new Chart(ctx, {
        type: 'bar',
        data: {
            labels: data.labels,
            datasets: [
                {
                    label: 'Income',
                    data: data.income,
                    backgroundColor: data.income.map((_, i) =>
                        i === highlightIdx ? hexToRgba(tokens.success, 0.75) : hexToRgba(tokens.success, 0.35)
                    ),
                    borderColor: data.income.map((_, i) =>
                        i === highlightIdx ? tokens.success : hexToRgba(tokens.success, 0.5)
                    ),
                    borderWidth: 2,
                    borderRadius: 4,
                    borderSkipped: false
                },
                {
                    label: 'Expenses',
                    data: data.expenses,
                    backgroundColor: data.expenses.map((_, i) =>
                        i === highlightIdx ? hexToRgba(tokens.danger, 0.75) : hexToRgba(tokens.danger, 0.35)
                    ),
                    borderColor: data.expenses.map((_, i) =>
                        i === highlightIdx ? tokens.danger : hexToRgba(tokens.danger, 0.5)
                    ),
                    borderWidth: 2,
                    borderRadius: 4,
                    borderSkipped: false
                },
                {
                    label: 'Net',
                    data: data.balances,
                    backgroundColor: data.balances.map((_, i) =>
                        i === highlightIdx ? hexToRgba(tokens.brand, 0.85) : hexToRgba(tokens.brand, 0.4)
                    ),
                    borderColor: data.balances.map((_, i) =>
                        i === highlightIdx ? tokens.brandDark : hexToRgba(tokens.brand, 0.55)
                    ),
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
                },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            return `${context.dataset.label}: €${Number(context.parsed.y).toFixed(2)}`;
                        }
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
 * Initialize a line chart for monthly evolution (REQ-004).
 * Shows 12 months of income/expense/savings with average lines.
 *
 * @param {HTMLCanvasElement} canvas - Canvas element reference
 * @param {{labels: string[], income: number[], expenses: number[], savings: number[], avgIncome: number|null, avgExpense: number|null}} data
 */
function initMonthlyEvolutionChart(canvas, data) {
    if (!canvas || !data || !Array.isArray(data.labels) || data.labels.length === 0) return;

    const existing = Chart.getChart(canvas);
    if (existing) existing.destroy();

    const datasets = [
        {
            label: 'Income',
            data: data.income,
            borderColor: tokens.success,
            backgroundColor: hexToRgba(tokens.success, 0.08),
            tension: 0.3,
            fill: true,
            pointRadius: 3,
            pointHoverRadius: 5,
            borderWidth: 2,
            order: 1
        },
        {
            label: 'Expenses',
            data: data.expenses,
            borderColor: tokens.danger,
            backgroundColor: hexToRgba(tokens.danger, 0.08),
            tension: 0.3,
            fill: true,
            pointRadius: 3,
            pointHoverRadius: 5,
            borderWidth: 2,
            order: 1
        },
        {
            label: 'Savings',
            data: data.savings,
            borderColor: tokens.info,
            backgroundColor: hexToRgba(tokens.info, 0.08),
            tension: 0.3,
            fill: true,
            pointRadius: 3,
            pointHoverRadius: 5,
            borderWidth: 2,
            borderDash: [5, 3],
            order: 1
        }
    ];

    // Add average lines if available (rendered as horizontal dashed lines)
    if (data.avgIncome != null) {
        datasets.push({
            label: 'Avg Income',
            data: Array(12).fill(data.avgIncome),
            borderColor: hexToRgba(tokens.success, 0.5),
            backgroundColor: 'transparent',
            borderWidth: 1,
            borderDash: [8, 4],
            pointRadius: 0,
            pointHoverRadius: 0,
            fill: false,
            order: 0
        });
    }

    if (data.avgExpense != null) {
        datasets.push({
            label: 'Avg Expense',
            data: Array(12).fill(data.avgExpense),
            borderColor: hexToRgba(tokens.danger, 0.5),
            backgroundColor: 'transparent',
            borderWidth: 1,
            borderDash: [8, 4],
            pointRadius: 0,
            pointHoverRadius: 0,
            fill: false,
            order: 0
        });
    }

    const ctx = canvas.getContext('2d');
    new Chart(ctx, {
        type: 'line',
        data: { labels: data.labels, datasets: datasets },
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
                        font: { size: 11 }
                    }
                },
                tooltip: {
                    mode: 'index',
                    intersect: false
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
            },
            interaction: {
                mode: 'index',
                intersect: false
            }
        }
    });
}

/**
 * Initialize a donut chart for category distribution (REQ-005).
 *
 * PRECONDITION: data.categories is ordered by the handler descending by amount.
 * This function MUST NOT reorder.
 *
 * @param {HTMLCanvasElement} canvas - Canvas element reference
 * @param {{labels: string[], values: number[]}} data
 */
function initCategoryDonutChart(canvas, data) {
    if (!canvas || !data || !Array.isArray(data.values) || data.values.length === 0) return;

    const existing = Chart.getChart(canvas);
    if (existing) existing.destroy();

    const ctx = canvas.getContext('2d');
    new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: data.labels,
            datasets: [{
                data: data.values,
                backgroundColor: data.values.map((_, idx) => designColors[idx % designColors.length]),
                borderColor: '#fff',
                borderWidth: 2
            }]
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
                        padding: 14,
                        font: { size: 11 }
                    }
                },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            const total = context.dataset.data.reduce((a, b) => a + b, 0);
                            const pct = total > 0 ? ((context.parsed / total) * 100).toFixed(1) : '0.0';
                            return `${context.label}: €${Number(context.parsed).toFixed(2)} (${pct}%)`;
                        }
                    }
                }
            }
        }
    });
}

/**
 * Initialize anomaly chart (REQ-008).
 * Uses bars for anomaly amounts and a dashed line for historical mean.
 *
 * @param {HTMLCanvasElement} canvas
 * @param {{labels: string[], values: number[], means: number[], types: string[]}} data
 */
function initAnomalyChart(canvas, data) {
    if (!canvas || !data || !Array.isArray(data.labels) || data.labels.length === 0) return;

    const existing = Chart.getChart(canvas);
    if (existing) existing.destroy();

    const barColors = (data.types || []).map(type => {
        if (type === 'exceptional') return hexToRgba(tokens.warning, 0.8);
        if (type === 'extraordinary') return hexToRgba(tokens.danger, 0.8);
        return hexToRgba(tokens.info, 0.8);
    });

    const ctx = canvas.getContext('2d');
    new Chart(ctx, {
        type: 'bar',
        data: {
            labels: data.labels,
            datasets: [
                {
                    type: 'bar',
                    label: 'Anomaly amount',
                    data: data.values,
                    backgroundColor: barColors,
                    borderColor: barColors,
                    borderWidth: 1,
                    borderRadius: 4,
                    borderSkipped: false
                },
                {
                    type: 'line',
                    label: 'Historical mean',
                    data: data.means,
                    borderColor: tokens.muted,
                    backgroundColor: 'transparent',
                    borderDash: [6, 4],
                    borderWidth: 2,
                    tension: 0,
                    pointRadius: 2,
                    pointHoverRadius: 4
                }
            ]
        },
        options: {
            ...chartDefaults,
            plugins: {
                ...chartDefaults.plugins,
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            return `${context.dataset.label}: €${Number(context.parsed.y).toFixed(2)}`;
                        }
                    }
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

/**
 * charts.js — Chart.js initialization functions for SauronSheet analytics.
 * Phase 7.1: Counters, skeleton loading, HTMX pill selector integration.
 * Requires Chart.js (latest) CDN loaded in _Layout.cshtml.
 *
 * Colors: read from CSS custom properties (DESIGN.md tokens).
 * Fallback hex values provided for when CSS vars are unavailable.
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
 * @param {string} canvasId - Canvas element ID
 * @param {Array} monthlyCategoryData - Array of {month, monthName, categoryName, amount}
 * @param {number} year - Year for x-axis labels (e.g. 2025)
 */
function initCategoryStackedChart(canvasId, monthlyCategoryData, year) {
    const canvas = document.getElementById(canvasId);
    if (!canvas || !monthlyCategoryData || monthlyCategoryData.length === 0) return;

    // Destroy existing instance before recreating
    const existing = Chart.getChart(canvas);
    if (existing) existing.destroy();

    const months = [...new Set(monthlyCategoryData.map(d => d.month))]
        .sort((a, b) => a - b);
    const yearSuffix = year ? ` ${year}` : '';
    const monthLabels = months.map(m => {
        const entry = monthlyCategoryData.find(d => d.month === m);
        return entry ? `${entry.monthName}${yearSuffix}` : `Month ${m}${yearSuffix}`;
    });

    const categories = [...new Set(monthlyCategoryData.map(d => d.categoryName))];

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
 * @param {number} year - Year for x-axis labels (e.g. 2025)
 */
function initMonthlyTrendsChart(canvasId, monthlyData, year) {
    const canvas = document.getElementById(canvasId);
    if (!canvas || !monthlyData || monthlyData.length === 0) return;

    const existing = Chart.getChart(canvas);
    if (existing) existing.destroy();

    const yearSuffix = year ? ` ${year}` : '';
    const labels = monthlyData.map(d => `${d.monthName}${yearSuffix}`);

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

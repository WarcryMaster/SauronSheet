import { Page } from '@playwright/test';

/**
 * Set a Flatpickr date field safely, bypassing fp.setDate() which crashes on
 * Mobile Chrome (internal TypeError: Cannot set properties of undefined (setting 'tabIndex')).
 *
 * Instead, sets the native input value and Flatpickr's internal state directly.
 *
 * @param page   Playwright Page
 * @param id     The id of the original input element (Flatpickr attaches _flatpickr to it)
 * @param dateStr Date string in YYYY-MM-DD format
 */
export async function setFlatpickrDate(page: Page, id: string, dateStr: string): Promise<void> {
    await page.waitForFunction((selector) => {
        const el = document.getElementById(selector) as HTMLInputElement | null;
        return el !== null && (el as any)._flatpickr !== undefined;
    }, id, { timeout: 10000 });

    await page.evaluate(({ elementId, date }) => {
        const el = document.getElementById(elementId) as HTMLInputElement;
        const fp = (el as any)._flatpickr;
        el.value = date;
        const parsed = new Date(date + 'T00:00:00');
        fp.selectedDates = [parsed];
        fp.currentYear = parsed.getFullYear();
        fp.currentMonth = parsed.getMonth();
        fp.updateValue();
    }, { elementId: id, date: dateStr });
}

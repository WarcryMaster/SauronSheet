// Bulk Delete Selection Logic (Feature 004)
// Tracks selected transaction IDs and manages UI state

(function()  {
    const MAX_SELECTIONS = 1000;
    const COUNTDOWN_DURATION = 5;

    // Track selected IDs in a Set for efficient lookups
    const selectedIds = new Set();
    const selectAllCheckbox = document.getElementById('selectAllCheckbox');
    const transactionCheckboxes = document.querySelectorAll('.transaction-checkbox');
    const deleteSelectedBtn = document.getElementById('deleteSelectedBtn');
    const selectionCounter = document.getElementById('selectionCounter');
    const bulkDeleteForm = document.getElementById('bulkDeleteForm');
    const selectedIdsInput = document.getElementById('selectedIds');

    // Modal elements
    const confirmDeleteModal = document.getElementById('confirmDeleteModal');
    const confirmDeleteBtn = document.getElementById('confirmDeleteBtn');
    const cancelDeleteBtn = document.getElementById('cancelDeleteBtn');
    const countdownTimer = document.getElementById('countdownTimer');
    const countdownBar = document.getElementById('countdownBar');
    const deleteCountDisplay = document.getElementById('deleteCountDisplay');
    const pluralS = document.getElementById('pluralS');
    let countdownInterval = null;

    /**
     * T071: Update selection counter display
     */
    function updateCounter() {
        const count = selectedIds.size;
        const maxCount = Math.min(document.querySelectorAll('.transaction-checkbox').length, MAX_SELECTIONS);
        selectionCounter.textContent = `${count} / ${maxCount} selected`;
        deleteSelectedBtn.disabled = count === 0;
    }

    /**
     * T072: Select All / Deselect All functionality
     */
    selectAllCheckbox.addEventListener('change', function() {
        const isChecked = this.checked;
        let count = 0;

        transactionCheckboxes.forEach(checkbox => {
            if (isChecked && count < MAX_SELECTIONS) {
                checkbox.checked = true;
                selectedIds.add(checkbox.value);
                count++;
            } else if (!isChecked) {
                checkbox.checked = false;
                selectedIds.delete(checkbox.value);
            }
        });

        updateCounter();
        updateSelectAllState();
    });

    /**
     * T073: Per-checkbox toggle - add/remove from Set
     */
    transactionCheckboxes.forEach(checkbox => {
        checkbox.addEventListener('change', function() {
            if (this.checked) {
                // Max 1000 selections
                if (selectedIds.size < MAX_SELECTIONS) {
                    selectedIds.add(this.value);
                } else {
                    this.checked = false;
                    return;
                }
            } else {
                selectedIds.delete(this.value);
            }

            updateCounter();
            updateSelectAllState();
        });
    });

    /**
     * Update Select All checkbox state based on individual checkboxes
     */
    function updateSelectAllState() {
        const allChecked = Array.from(transactionCheckboxes).every(cb => cb.checked);
        const someChecked = Array.from(transactionCheckboxes).some(cb => cb.checked);
        selectAllCheckbox.checked = allChecked;
        selectAllCheckbox.indeterminate = someChecked && !allChecked;
    }

    /**
     * Modal confirmation countdown and deletion
     */
    const modal = new bootstrap.Modal(confirmDeleteModal);

    deleteSelectedBtn.addEventListener('click', function() {
        deleteCountDisplay.textContent = selectedIds.size;
        pluralS.textContent = selectedIds.size === 1 ? '' : 's';
        modal.show();
        startCountdown();
    });

    cancelDeleteBtn.addEventListener('click', function() {
        clearCountdown();
        modal.hide();
    });

    confirmDeleteModal.addEventListener('hidden.bs.modal', function() {
        clearCountdown();
    });

    /**
     * Start 5-second countdown timer
     */
    function startCountdown() {
        let remaining = COUNTDOWN_DURATION;
        confirmDeleteBtn.disabled = true;

        countdownTimer.textContent = remaining;
        countdownBar.style.width = '100%';

        countdownInterval = setInterval(function() {
            remaining--;
            countdownTimer.textContent = remaining;

            const percentage = (remaining / COUNTDOWN_DURATION) * 100;
            countdownBar.style.width = percentage + '%';
            countdownBar.setAttribute('aria-valuenow', remaining);

            if (remaining <= 0) {
                clearCountdown();
                confirmDeleteBtn.disabled = false;
            }
        }, 1000);
    }

    /**
     * Clear countdown timer
     */
    function clearCountdown() {
        if (countdownInterval) {
            clearInterval(countdownInterval);
            countdownInterval = null;
        }
        confirmDeleteBtn.disabled = true;
    }

    /**
     * Delete confirmation
     */
    confirmDeleteBtn.addEventListener('click', function() {
        // Populate hidden input with selected IDs (JSON array)
        const idsArray = Array.from(selectedIds);
        selectedIdsInput.value = JSON.stringify(idsArray);

        // Submit form
        bulkDeleteForm.submit();
    });

    // Initialize counter on page load
    updateCounter();
})();

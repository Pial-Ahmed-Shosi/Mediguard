/**
 * MediGuard - Batch Stock Entry & FEFO Tracking Script
 * Handles FEFO order display, client-side profit margin calculations, and date validations.
 */

document.addEventListener('DOMContentLoaded', function () {
    initBatchEntryForm();
});

function initBatchEntryForm() {
    const form = document.getElementById('batchEntryForm');
    const medicineSelect = document.getElementById('medicineSelect');
    const purchaseInput = document.getElementById('purchasePriceInput');
    const sellingInput = document.getElementById('sellingPriceInput');
    const marginBadge = document.getElementById('profitMarginBadge');
    const mfgDateInput = document.getElementById('manufacturingDateInput');
    const expiryDateInput = document.getElementById('expiryDateInput');
    const alertBox = document.getElementById('batchValidationSummary');
    const errorList = document.getElementById('batchErrorList');

    if (!form) return;

    // 1. Calculate Profit Margin on Pricing Input
    function updateProfitMargin() {
        const purchasePrice = parseFloat(purchaseInput.value) || 0;
        const sellingPrice = parseFloat(sellingInput.value) || 0;

        if (sellingPrice > 0 && purchasePrice >= 0) {
            const margin = ((sellingPrice - purchasePrice) / sellingPrice) * 100;
            marginBadge.textContent = margin.toFixed(1) + '%';

            if (margin < 0) {
                marginBadge.className = 'badge bg-danger fs-6 py-2 px-3';
            } else if (margin < 15) {
                marginBadge.className = 'badge bg-warning text-dark fs-6 py-2 px-3';
            } else {
                marginBadge.className = 'badge bg-success fs-6 py-2 px-3';
            }
        } else {
            marginBadge.textContent = '0.0%';
            marginBadge.className = 'badge bg-secondary fs-6 py-2 px-3';
        }
    }

    purchaseInput?.addEventListener('input', updateProfitMargin);
    sellingInput?.addEventListener('input', updateProfitMargin);

    // 2. Medicine Selection Change -> Fetch Existing Batches (FEFO Order)
    medicineSelect?.addEventListener('change', function () {
        const medicineId = this.value;
        if (medicineId) {
            fetchExistingBatchesFEFO(medicineId);
        } else {
            resetFEFOPanel();
        }
    });

    // 3. Date Rules & Form Validation on Submit
    form.addEventListener('submit', function (e) {
        const errors = validateBatchForm();

        if (errors.length > 0) {
            e.preventDefault();
            showValidationErrors(errors);
        } else {
            hideValidationErrors();
        }
    });

    function validateBatchForm() {
        const errors = [];
        const mfgVal = mfgDateInput.value;
        const expiryVal = expiryDateInput.value;

        if (!medicineSelect.value) {
            errors.push("Please select a medicine.");
        }

        if (!mfgVal) {
            errors.push("Manufacturing Date is required.");
        }

        if (!expiryVal) {
            errors.push("Expiry Date is required.");
        }

        if (mfgVal && expiryVal) {
            const mfgDate = new Date(mfgVal);
            const expiryDate = new Date(expiryVal);
            const today = new Date();
            today.setHours(0, 0, 0, 0);

            // Criteria 1: Expiry Date must be later than Manufacturing Date
            if (expiryDate <= mfgDate) {
                errors.push("Expiry Date must be later than the Manufacturing Date.");
            }

            // Criteria 2: Expiry Date must be later than today
            if (expiryDate <= today) {
                errors.push("Expiry Date must be in the future (later than today).");
            }

            // Criteria 3: Expiry Date must be at least 30 days after Manufacturing Date
            const diffTime = expiryDate.getTime() - mfgDate.getTime();
            const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
            if (diffDays < 30) {
                errors.push("Expiry Date must be at least 30 days after the Manufacturing Date.");
            }
        }

        return errors;
    }

    function showValidationErrors(errors) {
        errorList.innerHTML = '';
        errors.forEach(err => {
            const li = document.createElement('li');
            li.textContent = err;
            errorList.appendChild(li);
        });
        alertBox.classList.remove('d-none');
        alertBox.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }

    function hideValidationErrors() {
        alertBox.classList.add('d-none');
        errorList.innerHTML = '';
    }
}

/**
 * Fetches existing batches for selected medicine and renders in FEFO order (ExpiryDate ASC)
 */
function fetchExistingBatchesFEFO(medicineId) {
    const placeholder = document.getElementById('fefoPlaceholder');
    const loading = document.getElementById('fefoLoading');
    const container = document.getElementById('fefoBatchesContainer');
    const list = document.getElementById('fefoBatchesList');
    const countBadge = document.getElementById('batchCountBadge');

    placeholder.classList.add('d-none');
    container.classList.add('d-none');
    loading.classList.remove('d-none');

    fetch(`/Inventory/GetBatchesByMedicine?medicineId=${medicineId}`)
        .then(response => {
            if (!response.ok) {
                throw new Error('Failed to load batches');
            }
            return response.json();
        })
        .then(batches => {
            loading.classList.add('d-none');

            // Sort FEFO: Earliest Expiry First
            batches.sort((a, b) => new Date(a.expiryDate) - new Date(b.expiryDate));

            countBadge.textContent = batches.length;

            if (batches.length === 0) {
                list.innerHTML = `
                    <div class="text-center py-4 text-muted">
                        <i class="bi bi-inbox fs-3 d-block mb-1"></i>
                        No active stock lots found for this medicine.
                    </div>`;
            } else {
                list.innerHTML = batches.map(batch => renderBatchCard(batch)).join('');
            }

            container.classList.remove('d-none');
        })
        .catch(err => {
            loading.classList.add('d-none');
            placeholder.classList.remove('d-none');
            countBadge.textContent = '0';
            console.warn('FEFO load fallback or API endpoint unavailable:', err);
        });
}

function renderBatchCard(batch) {
    const expiry = new Date(batch.expiryDate);
    const formattedExpiry = expiry.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
    
    // Days until expiry badge styling
    const today = new Date();
    const diffDays = Math.ceil((expiry - today) / (1000 * 60 * 60 * 24));
    
    let statusClass = 'bg-success';
    let statusText = `${diffDays} days left`;

    if (diffDays <= 0) {
        statusClass = 'bg-danger';
        statusText = 'EXPIRED';
    } else if (diffDays <= 60) {
        statusClass = 'bg-warning text-dark';
        statusText = `Expiring soon (${diffDays}d)`;
    }

    return `
        <div class="card border rounded p-2 position-relative shadow-sm">
            <div class="d-flex justify-content-between align-items-center mb-1">
                <span class="fw-bold text-dark"><i class="bi bi-tag-fill text-secondary me-1"></i>${batch.batchNumber}</span>
                <span class="badge ${statusClass}">${statusText}</span>
            </div>
            <div class="d-flex justify-content-between text-muted small">
                <span>Expires: <strong>${formattedExpiry}</strong></span>
                <span>Qty: <span class="badge bg-primary rounded-pill">${batch.remainingQuantity} units</span></span>
            </div>
        </div>
    `;
}

function resetFEFOPanel() {
    document.getElementById('fefoPlaceholder').classList.remove('d-none');
    document.getElementById('fefoBatchesContainer').classList.add('d-none');
    document.getElementById('fefoLoading').classList.add('d-none');
    document.getElementById('batchCountBadge').textContent = '0';
}
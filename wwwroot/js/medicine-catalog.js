document.addEventListener("DOMContentLoaded", function () {
    const searchInput = document.getElementById('searchKeyword');
    const categoryFilter = document.getElementById('categoryFilter');
    const manufacturerFilter = document.getElementById('manufacturerFilter');
    const rxOnlyFilter = document.getElementById('rxOnlyFilter');
    const resetBtn = document.getElementById('resetFiltersBtn');
    const tableContainer = document.getElementById('medicineTableContainer');

    let debounceTimer = null;

    // Attach Event Listeners for Filtering
    if (searchInput) {
        searchInput.addEventListener('input', function () {
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(() => {
                fetchFilteredMedicines();
            }, 300); // 300ms debounce
        });
    }

    if (categoryFilter) categoryFilter.addEventListener('change', fetchFilteredMedicines);
    if (manufacturerFilter) manufacturerFilter.addEventListener('change', fetchFilteredMedicines);
    if (rxOnlyFilter) rxOnlyFilter.addEventListener('change', fetchFilteredMedicines);

    if (resetBtn) {
        resetBtn.addEventListener('click', function () {
            searchInput.value = '';
            categoryFilter.value = '';
            manufacturerFilter.value = '';
            rxOnlyFilter.checked = false;
            fetchFilteredMedicines();
        });
    }

    // AJAX Call to update partial grid
    function fetchFilteredMedicines() {
        const keyword = searchInput ? searchInput.value.trim() : '';
        const categoryId = categoryFilter ? categoryFilter.value : '';
        const manufacturerId = manufacturerFilter ? manufacturerFilter.value : '';
        const rxOnly = rxOnlyFilter ? rxOnlyFilter.checked : false;

        const params = new URLSearchParams({
            searchKeyword: keyword,
            categoryId: categoryId,
            manufacturerId: manufacturerId,
            rxOnly: rxOnly
        });

        tableContainer.style.opacity = '0.5';

        fetch(`/Medicine/FilterMedicines?${params.toString()}`, {
            method: 'GET',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        })
        .then(response => {
            if (!response.ok) {
                throw new Error('Failed to fetch updated catalog items.');
            }
            return response.text();
        })
        .then(html => {
            tableContainer.innerHTML = html;
            tableContainer.style.opacity = '1';
        })
        .catch(error => {
            console.error('Error updating catalog grid:', error);
            tableContainer.innerHTML = `<div class="alert alert-danger m-3">Error loading medicine catalog data. Please try again.</div>`;
            tableContainer.style.opacity = '1';
        });
    }

    // Add Stock Modal Event Listener (Event Delegation)
    document.addEventListener('click', function (e) {
        const addStockBtn = e.target.closest('.btn-add-stock');
        if (addStockBtn) {
            const medicineId = addStockBtn.getAttribute('data-medicine-id');
            const medicineName = addStockBtn.getAttribute('data-medicine-name');

            const modalMedicineIdInput = document.getElementById('modalMedicineId');
            const modalMedicineNameInput = document.getElementById('modalMedicineName');
            const addStockModalEl = document.getElementById('addStockModal');

            if (modalMedicineIdInput && modalMedicineNameInput && addStockModalEl) {
                modalMedicineIdInput.value = medicineId;
                modalMedicineNameInput.value = medicineName;

                const bootstrapModal = new bootstrap.Modal(addStockModalEl);
                bootstrapModal.show();
            }
        }
    });

    // Handle Stock Save
    const saveStockBtn = document.getElementById('saveStockBtn');
    if (saveStockBtn) {
        saveStockBtn.addEventListener('click', function () {
            const form = document.getElementById('addStockForm');
            if (!form.checkValidity()) {
                form.reportValidity();
                return;
            }

            const formData = new FormData(form);
            fetch('/Batch/CreateBatch', {
                method: 'POST',
                body: formData,
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            })
            .then(res => res.json())
            .then(data => {
                if (data.success) {
                    const addStockModalEl = document.getElementById('addStockModal');
                    const modalInstance = bootstrap.Modal.getInstance(addStockModalEl);
                    if (modalInstance) modalInstance.hide();
                    form.reset();
                    fetchFilteredMedicines(); // Refresh grid data
                } else {
                    alert(data.message || 'Error saving batch.');
                }
            })
            .catch(err => {
                console.error('Batch save error:', err);
                alert('An error occurred while saving the stock batch.');
            });
        });
    }
});
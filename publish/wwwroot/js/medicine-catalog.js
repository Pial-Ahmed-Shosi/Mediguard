document.addEventListener('DOMContentLoaded', function () {
    const searchInput = document.getElementById('searchKeyword');
    const categorySelect = document.getElementById('categoryFilter');
    const manufacturerSelect = document.getElementById('manufacturerFilter');
    const rxOnlyCheckbox = document.getElementById('rxOnlyFilter');
    const tableContainer = document.getElementById('medicineTableContainer');

    let debounceTimeout = null;

    // Build query params and execute the AJAX fetch
    function fetchMedicines() {
        tableContainer.style.opacity = '0.5'; // Visual loading indicator

        const params = new URLSearchParams({
            search: searchInput.value.trim(),
            categoryId: categorySelect.value,
            manufacturerId: manufacturerSelect.value,
            rxOnly: rxOnlyCheckbox.checked
        });

        fetch(`/Medicine/LoadMedicineTable?${params.toString()}`, {
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        })
        .then(response => {
            if (!response.ok) throw new Error('Network response was not ok');
            return response.text();
        })
        .then(html => {
            tableContainer.innerHTML = html;
            tableContainer.style.opacity = '1';
        })
        .catch(error => {
            console.error('Error fetching medicine table:', error);
            tableContainer.innerHTML = `<div class="alert alert-danger m-3">Failed to load catalog data. Please try again.</div>`;
            tableContainer.style.opacity = '1';
        });
    }

    // Input event listeners
    searchInput.addEventListener('input', function () {
        clearTimeout(debounceTimeout);
        debounceTimeout = setTimeout(fetchMedicines, 300); // 300ms debounce for typing
    });

    categorySelect.addEventListener('change', fetchMedicines);
    manufacturerSelect.addEventListener('change', fetchMedicines);
    rxOnlyCheckbox.addEventListener('change', fetchMedicines);

    // Initial load call
    fetchMedicines();
});      

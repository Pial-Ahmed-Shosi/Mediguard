document.addEventListener("DOMContentLoaded", function () {
    const searchInput = document.getElementById('searchUserInput');
    const filterButtons = document.querySelectorAll('#quickFilters button');
    const tableContainer = document.getElementById('tableContainer');
    
    let currentFilter = 'All';
    let searchTimeout = null;

    // Handle Quick Filters Clicks
    filterButtons.forEach(btn => {
        btn.addEventListener('click', function () {
            // Update active styling
            filterButtons.forEach(b => b.classList.remove('active'));
            this.classList.add('active');
            
            // Set current filter and fetch data
            currentFilter = this.getAttribute('data-role');
            fetchFilteredUsers();
        });
    });

    // Handle Search Input with 300ms Debounce
    searchInput.addEventListener('input', function () {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(() => {
            fetchFilteredUsers();
        }, 300);
    });

    // Perform AJAX Call to Reload the Table Partial
    function fetchFilteredUsers() {
        const searchQuery = searchInput.value.trim();
        
        // Note: Make sure the Backend controller handles this route to return PartialView("_UserListTable", model)
        const url = `/User/GetFilteredUsers?filter=${encodeURIComponent(currentFilter)}&search=${encodeURIComponent(searchQuery)}`;
        
        // Optional: Show loading state
        tableContainer.style.opacity = '0.5';

        fetch(url)
            .then(response => {
                if (!response.ok) throw new Error('Network response failed.');
                return response.text();
            })
            .then(htmlContent => {
                tableContainer.innerHTML = htmlContent;
                tableContainer.style.opacity = '1';
            })
            .catch(error => {
                console.error('Error fetching filtered users:', error);
                tableContainer.innerHTML = `<div class="alert alert-danger m-4">Failed to load user data. Please try again.</div>`;
                tableContainer.style.opacity = '1';
            });
    }
});
document.addEventListener('DOMContentLoaded', function () {
    const searchInput = document.getElementById('posSearchInput');
    const resultsGrid = document.getElementById('posResultsGrid');
    const btnCheckout = document.getElementById('btnCheckout');
    const btnClearCart = document.getElementById('btnClearCart');
    
    // --- 1. Keyboard Shortcuts Management ---
    document.addEventListener('keydown', function (e) {
        // F2: Focus Search
        if (e.key === 'F2') {
            e.preventDefault();
            searchInput.focus();
        }
        // F8: Checkout
        else if (e.key === 'F8') {
            e.preventDefault();
            btnCheckout.click();
        }
        // ESC: Clear Cart
        else if (e.key === 'Escape') {
            e.preventDefault();
            clearCart();
        }
    });

    // --- 2. Barcode Scanner / Search Trap ---
    // Barcode scanners act like rapid keyboards that append 'Enter' (KeyCode 13) at the end.
    searchInput.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' || e.keyCode === 13) {
            e.preventDefault(); // Prevent form submission or unwanted drop-downs
            
            const query = searchInput.value.trim();
            if (query.length > 0) {
                // Instantly process barcode or exact match search (< 200ms goal)
                processBarcodeScan(query);
            }
        }
    });

    // Live search for typing (debounce to prevent overwhelming API)
    let searchTimeout;
    searchInput.addEventListener('input', function (e) {
        clearTimeout(searchTimeout);
        const query = searchInput.value.trim();
        
        searchTimeout = setTimeout(() => {
            if (query.length > 2) {
                fetchSearchResults(query);
            } else if (query.length === 0) {
                resultsGrid.innerHTML = ''; // Clear results if empty
            }
        }, 300); // 300ms debounce for manual typing
    });

    // --- 3. Core Functions ---

    function processBarcodeScan(barcode) {
        // Here you would make a rapid AJAX call to your backend.
        // Example: fetch(`/POS/ScanBarcode?barcode=${barcode}`)
        console.log(`Processing barcode scan: ${barcode}`);
        
        // Mocking an instant response for the acceptance criteria
        const matchedItem = {
            id: '123',
            brandName: 'Napa Extend',
            price: 15.00,
            isRx: false,
            stock: 50
        };

        if (matchedItem) {
            addToCart(matchedItem);
            searchInput.value = ''; // Clear input for next scan
            searchInput.focus();    // Auto-focus immediately returned
        } else {
            alert('Item not found!');
            searchInput.select();
        }
    }

    function fetchSearchResults(query) {
        // AJAX call to fetch matching items. Mocked rendering below.
        // fetch(`/POS/Search?q=${query}`).then(...).then(renderResultsGrid)
        
        // Mock data to demonstrate UI acceptance criteria
        const mockResults = [
            { id: 1, brandName: 'Sergel 20mg', genericName: 'Esomeprazole', price: 7.00, stock: 120, isRx: true },
            { id: 2, brandName: 'Napa Extra', genericName: 'Paracetamol', price: 2.50, stock: 0, isRx: false },
            { id: 3, brandName: 'Seclo 20mg', genericName: 'Omeprazole', price: 5.00, stock: 45, isRx: true }
        ];
        renderResultsGrid(mockResults);
    }

    function renderResultsGrid(items) {
        resultsGrid.innerHTML = items.map(item => {
            const isOutOfStock = item.stock <= 0;
            const cardClass = isOutOfStock ? 'product-card out-of-stock' : 'product-card';
            const rxBadge = item.isRx ? `<span class="indicator-badge badge-rx">Rx</span>` : '';
            const stockBadge = isOutOfStock ? `<span class="badge bg-secondary mb-2">Out of Stock</span>` : `<span class="badge bg-success bg-opacity-10 text-success mb-2">Stock: ${item.stock}</span>`;

            return `
                <div class="${cardClass}" onclick="${isOutOfStock ? '' : `addToCartMock('${item.id}', '${item.brandName}',${item.price})`}">
                    ${rxBadge}
                    <div class="brand-name">${item.brandName}</div>
                    <div class="generic-name">${item.genericName}</div>
                    ${stockBadge}
                    <div class="price-stock">
                        <span class="unit-price">৳${item.price.toFixed(2)}</span>
                    </div>
                </div>
            `;
        }).join('');
    }

    function addToCart(item) {
        // Cart logic goes here
        console.log('Added to cart:', item);
        document.getElementById('emptyCartMsg').style.display = 'none';
        
        // Acceptance Criteria: Focus automatically returns to the search input after every item addition.
        searchInput.focus();
    }

    function clearCart() {
        if(confirm("Are you sure you want to clear the cart?")) {
            console.log("Cart cleared");
            document.getElementById('emptyCartMsg').style.display = 'block';
            document.getElementById('cartTotalAmount').innerText = '৳0.00';
            searchInput.focus();
        }
    }

    // Expose mock add to cart for the inline onclick handler
    window.addToCartMock = function(id, name, price) {
        addToCart({ id, brandName: name, price });
    };

    // Initialize View
    searchInput.focus();
});
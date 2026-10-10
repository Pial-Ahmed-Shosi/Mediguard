$(document).ready(function () {
   
    // TICKET 4: Filtering & Live Search Logic
   
    let activeRole = 'All';
    let activeStatus = 'All';
    let debounceTimer;

    const $searchInput =$('#userSearchInput');
    const $tableContainer =$('#userTableContainer');

    // Debounced search input handler (300ms delay)
    if ($searchInput.length) {$searchInput.on('input', function () {
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(() => fetchFilteredUsers(), 300);
        });
    }

    // Role and Status tab filtering
    $('#roleFilterTabs .nav-link').on('click', function (e) {
        e.preventDefault();
        $('#roleFilterTabs .nav-link').removeClass('active');
        $(this).addClass('active');

        const role = $(this).data('role');
        const status = $(this).data('status');

        if (role !== undefined) {
            activeRole = role;
            activeStatus = 'All';
        }
        if (status !== undefined) {
            activeStatus = status;
            activeRole = 'All';
        }

        fetchFilteredUsers();
    });

    // Expose table refresh function globally
    window.refreshUserList = function () {
        fetchFilteredUsers();
    };

    function fetchFilteredUsers() {
        const searchTerm = $searchInput.val() || '';
        const params = new URLSearchParams({
            searchTerm: searchTerm,
            role: activeRole,
            status: activeStatus
        });

        $tableContainer.css('opacity', '0.5');

        $.ajax({
            url: `/User/GetFilteredUsers?${params.toString()}`,
            type: 'GET',
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            success: function (html) {
                $tableContainer.html(html);
            },
            error: function (xhr, status, error) {
                console.error('Error filtering users:', error);
                showToast('error', 'Failed to load staff list.');
            },
            complete: function () {
                $tableContainer.css('opacity', '1');
            }
        });
    }


    //  Modal Triggers & Event Handlers
  

    // Event delegation for "Assign Role" buttons in the dynamic user table
    $(document).on('click', '.assign-role-btn', function (e) {
        e.preventDefault();
        const $assignBtn = $(this);
        const userId = $assignBtn.data('id') || $assignBtn.data('userid');
        
        // Extract row metadata if attributes are not explicitly set on button
        const $row = $assignBtn.closest('tr');
        const userName = $assignBtn.data('name') || $row.find('td:first').text().trim();
        const userRole = $assignBtn.data('role') || $row.find('td:nth-child(3) .badge').text().trim();

        openRoleAssignmentModal(userId, userName, userRole);
    });

    // Bind modal save button event
    $('#btnSavePermissions').off('click').on('click', function (e) {
        e.preventDefault();
        submitPermissions();
    });
});

/**
 * Open Permission Modal and initiate permission loading
 * @param {string|number} userId 
 * @param {string} userName 
 * @param {string} userRole 
 */
function openRoleAssignmentModal(userId, userName, userRole) {
    if (!userId) {
        showToast('error', 'Invalid staff user selected.');
        return;
    }

    // Reset previous modal inputs and tooltips
    resetPermissionModal();

    // Populate user profile info
    $('#modalUserId').val(userId);
    $('#modalUserName').text(userName || 'User #' + userId);
    $('#modalUserRole').text(userRole || 'Staff Member');

    if (userName) {
        const initials = userName.split(' ').map(n => n[0]).join('').toUpperCase().substring(0, 2);
        $('#modalUserAvatar').text(initials);
    } else {
        $('#modalUserAvatar').text('MG');
    }

    // Open modal instance
    const modalElement = document.getElementById('roleAssignmentModal');
    if (modalElement) {
        const modalInstance = bootstrap.Modal.getOrCreateInstance(modalElement);
        modalInstance.show();
    }

    // Asynchronously load user permissions
    loadUserPermissions(userId);
}

/**
 * Resets modal form controls and active tooltips
 */
function resetPermissionModal() {
    const $form =$('#roleAssignmentForm, #updatePermissionsForm');
    if ($form.length && $form[0].reset) {$form[0].reset();
    }

    $('.perm-checkbox, .perm-check').prop('checked', false);
    
    const mediSwitch = $('#perm_b2b_mediplus, #mediPlusSwitch');
    mediSwitch.prop('checked', false).prop('disabled', false);

    const wrapper = $('#mediPlusSwitchWrapper, #mediPlusTooltipContainer');
    wrapper.removeAttr('title').removeAttr('data-bs-original-title');
    
    if (wrapper.length) {
        const existingTooltip = bootstrap.Tooltip.getInstance(wrapper[0]);
        if (existingTooltip) {
            existingTooltip.dispose();
        }
    }

    $('#mediPlusCard').css('opacity', '1');
    $('#roleAssignmentForm').removeClass('d-none');
    $('#permissionModalLoader').addClass('d-none');
    $('#btnSavePermissions').prop('disabled', false);
}

/**
 * GET existing permissions via AJAX
 * @param {string|number} userId 
 */
function loadUserPermissions(userId) {
    $('#roleAssignmentForm').addClass('d-none');
    $('#permissionModalLoader').removeClass('d-none');
    $('#btnSavePermissions').prop('disabled', true);

    $.ajax({
        url: `/User/Permissions/${encodeURIComponent(userId)}`,
        type: 'GET',
        dataType: 'json',
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
        success: function (response) {
            $('#permissionModalLoader').addClass('d-none');
            $('#roleAssignmentForm').removeClass('d-none');
            $('#btnSavePermissions').prop('disabled', false);

            if (response && response.success !== false) {
                populatePermissionForm(response.data || response);
            } else {
                showToast('error', response.message || 'Failed to fetch user permissions.');
            }
        },
        error: function (xhr, status, error) {
            $('#permissionModalLoader').addClass('d-none');
            showToast('error', 'Error loading permissions. Please try again.');
            console.error('Permission GET Error:', error);
        }
    });
}

/**
 * Populates form controls and handles Medi+ conditional flag logic
 * @param {Object} data 
 */
function populatePermissionForm(data) {
    const permissions = data.permissions || data.Permissions || [];
    
    // Check if Medi+ subscription is active on pharmacy tenant
    const isMediPlusActive = data.isMediPlusActive !== undefined 
        ? data.isMediPlusActive 
        : (data.IsMediPlusActive !== undefined ? data.IsMediPlusActive : false);

    // 1. Populate Standard Permission Checkboxes
    $('.perm-checkbox, .perm-check').each(function () {
        const key = $(this).val();$(this).prop('checked', permissions.includes(key));
    });

    // 2. Handle Medi+ B2B Access Logic
    const mediSwitch = $('#perm_b2b_mediplus, #mediPlusSwitch');
    const mediWrapper = $('#mediPlusSwitchWrapper, #mediPlusTooltipContainer');
    const mediCard = $('#mediPlusCard');

    if (isMediPlusActive === true) {
        mediSwitch.prop('disabled', false);
        mediSwitch.prop('checked', permissions.includes('b2b.mediplus.access'));
        mediCard.css('opacity', '1');

        if (mediWrapper.length) {
            const existingTooltip = bootstrap.Tooltip.getInstance(mediWrapper[0]);
            if (existingTooltip) {
                existingTooltip.dispose();
            }
        }
    } else {
        // Tenant does NOT have active Medi+ subscription
        mediSwitch.prop('checked', false);
        mediSwitch.prop('disabled', true);
        mediCard.css('opacity', '0.65');

        // Attach required tooltip text
        mediWrapper.attr('title', 'Requires Active Medi+ Subscription');
        if (mediWrapper.length) {
            new bootstrap.Tooltip(mediWrapper[0]);
        }
    }
}

/**
 * Submits updated permissions via POST /User/UpdatePermissions
 */
function submitPermissions() {
    const userId = $('#modalUserId').val();
    if (!userId) {
        showToast('error', 'Missing user ID.');
        return;
    }

    // Collect checked permission keys
    const selectedPermissions = [];

    // Category Checkboxes
    $('.perm-checkbox:checked, .perm-check:checked').each(function () {
        selectedPermissions.push($(this).val());
    });

    // Medi+ Switch
    const mediSwitch = $('#perm_b2b_mediplus, #mediPlusSwitch');
    if (!mediSwitch.is(':disabled') && mediSwitch.is(':checked')) {
        const mediVal = mediSwitch.val() || 'b2b.mediplus.access';
        if (!selectedPermissions.includes(mediVal)) {
            selectedPermissions.push(mediVal);
        }
    }

    const payload = {
        userId: userId,
        permissions: selectedPermissions
    };

    // Set UI loading state
    setSaveButtonLoading(true);

    $.ajax({
        url: '/User/UpdatePermissions',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(payload),
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
        success: function (response) {
            setSaveButtonLoading(false);

            if (response && (response.success === true || response.status === 'success')) {
                showToast('success', response.message || 'Permissions updated successfully.');

                // Hide Modal
                const modalElement = document.getElementById('roleAssignmentModal');
                if (modalElement) {
                    const modalInstance = bootstrap.Modal.getInstance(modalElement);
                    if (modalInstance) {
                        modalInstance.hide();
                    }
                }

                // Reload user list table if available
                if (typeof window.refreshUserList === 'function') {
                    window.refreshUserList();
                }
            } else {
                showToast('error', response.message || 'Failed to update permissions.');
            }
        },
        error: function (xhr, status, error) {
            setSaveButtonLoading(false);
            let errMsg = 'An error occurred while saving permissions.';
            if (xhr.responseJSON && xhr.responseJSON.message) {
                errMsg = xhr.responseJSON.message;
            }
            showToast('error', errMsg);
            console.error('Permission POST Error:', error);
        }
    });
}

/**
 * Sets button loading state to prevent double submission
 * @param {boolean} isLoading 
 */
function setSaveButtonLoading(isLoading) {
    const btn = $('#btnSavePermissions');
    const spinner = $('#saveBtnSpinner');
    const text = $('#saveBtnText');

    if (isLoading) {
        btn.prop('disabled', true);
        spinner.removeClass('d-none');
        text.html('Saving...');
    } else {
        btn.prop('disabled', false);
        spinner.addClass('d-none');
        text.html('<i class="bi bi-check2-circle me-1"></i>Save Changes');
    }
}

/**
 * Toast alert handler
 */
function showToast(type, message) {
    if (window.MediGuard && typeof window.MediGuard.showToast === 'function') {
        window.MediGuard.showToast(type, message);
        return;
    }

    if (typeof Swal !== 'undefined') {
        Swal.fire({
            toast: true,
            position: 'top-end',
            icon: type === 'success' ? 'success' : 'error',
            title: message,
            showConfirmButton: false,
            timer: 3000
        });
        return;
    }

    const alertClass = type === 'success' ? 'alert-success' : 'alert-danger';
    const toastHtml = `
        <div class="alert ${alertClass} alert-dismissible fade show position-fixed top-0 end-0 m-3 shadow" style="z-index: 9999;" role="alert">
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        </div>`;
    $('body').append(toastHtml);
    setTimeout(function () {
        $('.alert').fadeOut(500, function () {$(this).remove(); });
    }, 3000);
}
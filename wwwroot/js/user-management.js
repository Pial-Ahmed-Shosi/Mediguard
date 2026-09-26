/**
 * MediGuard - Staff Permission Management
 * Implementation for Jira Ticket 5
 */

$(document).ready(function () {
    // Bind modal save button event (prevent duplicate bindings)
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
    const modalInstance = bootstrap.Modal.getOrCreateInstance(modalElement);
    modalInstance.show();

    // Asynchronously load user permissions
    loadUserPermissions(userId);
}

/**
 * Resets modal form controls and active tooltips
 */
function resetPermissionModal() {
    $('#roleAssignmentForm')[0].reset();
    $('.perm-checkbox').prop('checked', false);
    
    const mediSwitch = $('#perm_b2b_mediplus');
    mediSwitch.prop('checked', false).prop('disabled', false);

    const wrapper = $('#mediPlusSwitchWrapper');
    wrapper.removeAttr('title').removeAttr('data-bs-original-title');
    
    const existingTooltip = bootstrap.Tooltip.getInstance(wrapper[0]);
    if (existingTooltip) {
        existingTooltip.dispose();
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
    $('.perm-checkbox').each(function () {
        const key = $(this).val();$(this).prop('checked', permissions.includes(key));
    });

    // 2. Handle Medi+ B2B Access Logic
    const mediSwitch = $('#perm_b2b_mediplus');
    const mediWrapper = $('#mediPlusSwitchWrapper');
    const mediCard = $('#mediPlusCard');

    if (isMediPlusActive === true) {
        mediSwitch.prop('disabled', false);
        mediSwitch.prop('checked', permissions.includes('b2b.mediplus.access'));
        mediCard.css('opacity', '1');

        const existingTooltip = bootstrap.Tooltip.getInstance(mediWrapper[0]);
        if (existingTooltip) {
            existingTooltip.dispose();
        }
    } else {
        // Tenant does NOT have active Medi+ subscription
        mediSwitch.prop('checked', false);
        mediSwitch.prop('disabled', true);
        mediCard.css('opacity', '0.65');

        // Attach exact required tooltip text
        mediWrapper.attr('title', 'Requires Active Medi+ Subscription');
        new bootstrap.Tooltip(mediWrapper[0]);
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

    // Category 1, 2, 3 Checkboxes
    $('.perm-checkbox:checked').each(function () {
        selectedPermissions.push($(this).val());
    });

    // Category 4 Medi+ Switch
    const mediSwitch = $('#perm_b2b_mediplus');
    if (!mediSwitch.is(':disabled') && mediSwitch.is(':checked')) {
        selectedPermissions.push('b2b.mediplus.access');
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
        success: function (response) {
            setSaveButtonLoading(false);

            if (response && (response.success === true || response.status === 'success')) {
                showToast('success', response.message || 'Permissions updated successfully.');

                // Hide Modal
                const modalElement = document.getElementById('roleAssignmentModal');
                const modalInstance = bootstrap.Modal.getInstance(modalElement);
                if (modalInstance) {
                    modalInstance.hide();
                }

                // Optional list reload callback if defined on user table
                if (typeof refreshUserList === 'function') {
                    refreshUserList();
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
 * Sets button loading state to prevent double submit
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